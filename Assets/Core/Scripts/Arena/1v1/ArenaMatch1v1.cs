/*
- notify
- confirm
- wait
- start
*/
using System;
using Game.Components;
namespace Game.Arena
{
    [Serializable]
    public struct ArenaMatch1v1
    {
        public short id;
        public Player player1;
        public bool player1Ready;
        public int player1Dmg;
        public Player player2;
        public bool player2Ready;
        public int player2Dmg;
        public ArenaMatchState state;
        ArenaRoomSingle room => ArenaSystem.manager.a1v1.rooms[id];
        public void NotifyPlayers()
        {
            double startTime = DateTime.Now.AddSeconds(Storage.data.arena.startTime).ToOADate();
            // player 1
            player1.own.occupation = PlayerOccupation.ReadyArena1v1;
            player1.occupationId = id;
            player1.TargetNotifiyArenaMatch1v1();
            // player 2
            player2.own.occupation = PlayerOccupation.ReadyArena1v1;
            player2.occupationId = id;
            player2.TargetNotifiyArenaMatch1v1();

            room.WaitForAcceptanceOrCancelMatch();
            state = ArenaMatchState.Notified;
        }
        public bool Accept(uint pId)
        {
            if(!IsPartOfThisMatch(pId))
                return false;
            
            if(player1.id == pId)
            {
                player1Ready = true;
            }
            else
            {
                player2Ready = true;
            }
            if(IsReady())
            {
                ArenaRoomSingle room = this.room;

                player1.TargetHideNotifiyArenaMatch1v1();
                player1.TeleportToEventMap(EventMaps.Arena1v1, room.transform.position, room.entry1);
                player1.own.occupation = PlayerOccupation.InMatchArena1v1;
                player1Ready = false;
                
                player2.TargetHideNotifiyArenaMatch1v1();
                player2.TeleportToEventMap(EventMaps.Arena1v1, room.transform.position, room.entry2);
                player2.own.occupation = PlayerOccupation.InMatchArena1v1;
                player2Ready = false;

                room.WaitForPlayersToTransport();
                state = ArenaMatchState.WaitingPlayersToTeleport;
            }
            return true;
        }
        public bool Refuse(uint pId)
        {
            if(!IsPartOfThisMatch(pId))
                return false;
            
            player1.own.occupation = PlayerOccupation.None;
            player1.occupationId = -1;

            player2.own.occupation = PlayerOccupation.None;
            player2.occupationId = -1;

            if(pId == player1.id)
            {
                player1.TargetRefusedArenaMatch1v1();
                player2.TargetCanceledArenaMatch1v1();
                ArenaSystem.Register1v1(player2);
            }
            if(pId == player2.id)
            {
                player2.TargetRefusedArenaMatch1v1();
                player1.TargetCanceledArenaMatch1v1();
                ArenaSystem.Register1v1(player1);
            }
            room.WrapUp();
            return true;
        }
        public void Cancel()
        {
            if(state == ArenaMatchState.WaitingPlayersToTeleport)
                WrapUp();
        }
        public void ConfirmTeleport(uint pId)
        {
            if(!IsPartOfThisMatch(pId))
                return;
            
            if(pId == player1.id)
                player1Ready = true;
            else if(pId == player2.id)
                player2Ready = true;
            
            if(IsReady())
            {
                ArenaSystem.manager.a1v1.rooms[id].StartCountDown();
                double startTime = DateTime.Now.AddSeconds(Storage.data.arena.startTime).ToOADate();
                player1.TargetStartCountDown(startTime);
                player2.TargetStartCountDown(startTime);
                state = ArenaMatchState.CountingDown;
            }
        }
        public void EndIfNotReady()
        {
            // in case after acceptance one or both players didn't confirm entering the room
            if(player1Ready)
            {
                // set p1 winner
            }
            else if(player2Ready)
            {
                // set p2 winner
            }
            else
            {
                // cancel match
            }
        }
        public void Start()
        {
            player1.target = player2.GetComponent<Entity>();
            player2.target = player1.GetComponent<Entity>();
            state = ArenaMatchState.Started;
        }
        public void AddDamage(uint pId, int dmg)
        {
            if(player1.id == pId)
            {
                player1Dmg += dmg;
            }
            else if(player2.id == pId)
            {
                player2Dmg += dmg;
            }
        }
        public bool DeclareWinner(uint pId)
        {
            if(!IsPartOfThisMatch(pId))
                return false;
            if(player1.id == pId)
            {
                SetWinner(player1);
                SetLosser(player2);
            }
            else if(player2.id == pId)
            {
                SetWinner(player2);
                SetLosser(player1);
            }
            state = ArenaMatchState.Finished;
            room.WrapUp();
            return true;
        }
        public void SetWinner(Player winner)
        {
            winner.AddArena1v1Win();
            if(winner.id == player1.id) {
                winner.TargetShowResultArena1v1(true, player1Dmg, player2Dmg);
            }
            else
            {
                winner.TargetShowResultArena1v1(true, player2Dmg, player1Dmg);
            }
        }
        public void SetLosser(Player losser)
        {
            losser.AddArena1v1Loss();
            if(losser.id == player1.id)
            {
                losser.TargetShowResultArena1v1(false, player1Dmg, player2Dmg);
            }
            else
            {
                losser.TargetShowResultArena1v1(false, player2Dmg, player1Dmg);
            }
        }
        public bool LeaveMatch(uint pId)
        {
            if(!IsPartOfThisMatch(pId))
                return false;
            if(state == ArenaMatchState.Finished)
            {
                Leave(pId);
            }
            else
            {
                Concede(pId);
            }
            return true;
        }
        bool Concede(uint pId)
        {
            if(!IsPartOfThisMatch(pId))
                return false;
            return DeclareWinner(player1.id == pId ? player2.id : player1.id);
        }
        public void Leave(uint pId)
        {
            if (player1 != null && 
                player1.id == pId && 
                player1.own.occupation == PlayerOccupation.InMatchArena1v1 && 
                player1.occupationId == id)
            {
                player1.TargetHideResultArena1v1();
                player1.TeleportToLastLocation(true);
                player1.occupationId = -1;
                player1.own.occupation = PlayerOccupation.None;
                if(player1.health == 0)
                {
                    player1.Revive();
                }
            }
            else if (player2 != null && 
                    player2.id == pId &&
                    player2.own.occupation == PlayerOccupation.InMatchArena1v1 &&
                    player2.occupationId == id)
            {
                player2.TargetHideResultArena1v1();
                player2.TeleportToLastLocation(true);
                player2.occupationId = -1;
                player2.own.occupation = PlayerOccupation.None;
                if(player2.health == 0)
                {
                    player2.Revive();
                }
            }
        }
        public void TimeIsUp()
        {
            DeclareWinner(player1Dmg > player2Dmg ? player1.id : player2.id);
        }
        public void WrapUp()
        {
            if(player1 != null && player1.own.occupation == PlayerOccupation.InMatchArena1v1 && player1.occupationId == id)
                Leave(player1.id);

            if(player2 != null && player2.own.occupation == PlayerOccupation.InMatchArena1v1 && player2.occupationId == id)
                Leave(player2.id);

            ArenaSystem.matches1v1.Remove(id);
        }
        public bool IsReady() => player1Ready && player2Ready;
        bool IsPartOfThisMatch(uint pId) => pId == player1.id || pId == player2.id;
    }
}