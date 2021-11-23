// Rubberband navmesh movement.
//
// How it works:
// - local player sends new position to server every 100ms
// - server validates the move
// - server broadcasts it to other clients
//   - clients apply it via agent.destination to get free interpolation
// - server also detects teleports to warp the client if needed
//
// The great part about this solution is that the client can move freely, but
// the server can still intercept with:
//   * agent.Warp()
//   * rubberbanding.ResetMovement()
// => all those calls are detected here and forced to the client.
//
// Note: no LookAtY needed because we move everything via .destination
// Note: only syncing .destination would save a lot of bandwidth, but it's way
//       too complicated to get right with both click AND wasd movement.
using UnityEngine;
using UnityEngine.AI;
using Mirror;
namespace Game.Network
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NetworkNavMeshAgentRubberbanding : NetworkBehaviourNonAlloc
    {
        public NavMeshAgent agent; // assign in Inspector (instead of GetComponent)
        public Entity entity;

        // remember last serialized values for dirty bit
        double lastSentTime; // double for long term precision

        // check if a move is valid (the 'rubber' part)
        bool IsValidDestination(Vector3 position)
        {
            return entity.health > 0 &&
                (((Player)entity).state == EntityState.Idle || ((Player)entity).state == EntityState.Moving);
        }

        [Command]
        void CmdMoved(Vector3 position)
        {
            // rubberband (check if valid move)
            if (IsValidDestination(position))
            {
                // set position via .destination to get free interpolation
                agent.stoppingDistance = 0;
                agent.destination = position;

                // set dirty to trigger a OnSerialize next time, so that other clients
                // know about the new position too
                SetDirtyBit(1);
            }
            else
            {
                // otherwise keep current position and set dirty so that OnSerialize
                // is trigger. it will warp eventually when getting too far away.
                SetDirtyBit(1);
            }
        }

        void Update()
        {
            // NOTE: no automatic warp detection on server.
            //       Entity.Warp calls RpcWarped for 100% reliable detection.

            // local player can move freely. detect position changes.
            if (isLocalPlayer)
            {
                // send position every send interval no matter what.
                // -> a minimum-moved-distance can cause agent positions to get
                //    slightly out of sync at times. it's just not wroth it.
                if (NetworkTime.time >= lastSentTime + syncInterval)// &&
                    //Vector3.Distance(transform.position, lastSentPosition) > epsilon)
                {
                    // host sets dirty without cmd/overwriting destination/etc.
                    if (isServer)
                        SetDirtyBit(1);
                    // client sends to server to broadcast/set destination/etc.
                    else
                        CmdMoved(transform.position);

                    lastSentTime = NetworkTime.time;
                }
            }
        }

        // 100% reliable warp. instead of trying to detect it based on speed etc.
        [ClientRpc]
        public void RpcWarp(Vector3 position)
        {
            agent.Warp(position);
        }

        // force reset movement on localplayer
        // => always call rubberbanding.ResetMovement instead of agent.ResetMovement
        //    when using Rubberbanding.
        // => there is no decent way to detect .ResetMovement on server while doing
        //    rubberband movement on client. it would always lead to false positives
        //    and accidental resets. this is the 100% safe way to do it here.
        [Server]
        public void ResetMovement()
        {
            // force reset on target
            TargetResetMovement(transform.position);

            // set dirty so onserialize notifies others
            SetDirtyBit(1);
        }

        // note: with rubberband movement, the server's player position always lags
        //       behind a bit. if server resets movement and then tells client to
        //       reset it, client will reset it while already behind ahead.
        // => solution: include reset position so we don't get out of sync.
        // -> if local player moves to B then player position on server is always
        //    a bit behind. if server resets movement then the player will stop
        //    abruptly where it is on server and on client, which is not the same
        //    yet. we need to stay in sync.
        [TargetRpc]
        void TargetResetMovement(Vector3 resetPosition)
        {
            // reset path and velocity
            //Debug.LogWarning(name + "(local=" + isLocalPlayer + ") TargetResetMovement @ " + resetPosition);
            agent.ResetMovement();
            agent.Warp(resetPosition);
        }

        // server-side serialization
        // used for the server to broadcast positions to other clients too
        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            // always send position so client knows if he's too far off and needs warp
            // we also need it for wasd movement anyway
            writer.WriteVector3(transform.position);

            // always send speed in case it's modified by something
            writer.WriteFloat(agent.speed);

            // note: we don't send stopping distance because we always use '0' here
            // (because we always send the latest position every sendInterval)
            return true;
        }

        // client-side deserialization
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            // read position, speed, movement type in any case, so that we read
            // exactly what we write
            Vector3 position = reader.ReadVector3();
            float speed = reader.ReadFloat();

            // we can only apply the position if the agent is on the navmesh
            // (might not be while falling from the sky after joining, etc.)
            if (agent.isOnNavMesh)
            {
                // we can only move the agent to a position that is on the navmesh.
                // (might not if the agent walked into an instance, server
                //  broadcasted the new position to us, and proximity checker hasn't
                //  yet realized that the agent is out of sight. so it's not
                //  destroyed yet)
                // => 0.1f distance for network imprecision that might happen.
                // => if it happens when we simply do nothing and hope that the next
                //    update will be on a navmesh again.
                //    (if we were to Destroy it then we might get out of sync if
                //     the agent comes back out of the instance and was in proximity
                //     range the whole time)
                // NOTE: we *could* also call agent.proxchecker.Hide() and later
                //       Show() if agent is on a valid navmesh again. but let's keep
                //       the agents in front of the portal instead so we see what's
                //       happening. it's highly unlikely that an instance will be in
                //       proximity range of a player not in that instance anyway.
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
                {
                    // ignore for local player since he can move freely
                    if (!isLocalPlayer)
                    {
                        agent.stoppingDistance = 0;
                        agent.speed = speed;
                        agent.destination = position;
                    }

                    // rubberbanding: if we are too far off because of a rapid position
                    // change or latency or server side teleport, then warp
                    // -> agent moves 'speed' meter per seconds
                    // -> if we are speed * 2 units behind, then we teleport
                    //    (using speed is better than using a hardcoded value)
                    // -> we use speed * 2 for update/network latency tolerance. player
                    //    might have moved quit a bit already before OnSerialize was called
                    //    on the server.
                    if (Vector3.Distance(transform.position, position) > agent.speed * 2 && agent.isOnNavMesh)
                    {
                        agent.Warp(position);
                        //Debug.LogWarning(name + "(local=" + isLocalPlayer + ") rubberbanding to " + position);
                    }
                }
                else Debug.Log("NetworkNavMeshAgent.OnDeserialize: new position not on NavMesh, name=" + name + " new position=" + position + ". This could happen if the agent was warped to a dungeon instance that isn't on the local player.");
            }
            else Debug.LogWarning("NetworkNavMeshAgent.OnDeserialize: agent not on NavMesh, name=" + name + " position=" + transform.position + " new position=" + position);
        }
    }
}
