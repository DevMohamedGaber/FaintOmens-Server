using System;
using UnityEngine;
using Mirror;
namespace Game
{
    [RequireComponent(typeof(NetworkNavMeshAgent))]
    public class Pet : Entity
    {
        [Header("Components")]
        public NetworkNavMeshAgent networkNavMeshAgent;
        [Header("Synced Vars")]
        [SyncVar] GameObject _owner;
        public Player owner
        {
            get => _owner != null  ? _owner.GetComponent<Player>() : null;
            set => _owner = value != null ? value.gameObject : null;
        }
        [SyncVar] public EntityState state = EntityState.Idle;
        [SyncVar] public Tier tier = Tier.F;
        [SyncVar] public byte stars;
        public int dataIndex;
        public PetInfo data => owner.own.pets[dataIndex];
        public ushort id => data.id;
        
        [Header("Movement")]
        public float returnDistance = 5; 
        public float followDistance = 10;
        public float teleportDistance = 20;
        [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f; // move as close as 0.8 * attackRange to a target.
        public override float speed => owner != null ? owner.speed : base.speed;

        [Header("Death")]
        public float deathTime = 30; // enough for animation
        double deathTimeEnd; // double for long term precision

        [Header("Behaviour")]
        public bool defendOwner = true; // attack what attacks the owner
        public bool autoAttack = true; // attack what the owner attacks

        int lastSkill = -1;
    #region Attributes
        #region Basics(Health/Mana/Speed)
        public override int healthMax {
            get {
                int result = data.vitality * Storage.data.AP_Vitality;
                result += data.tier != Tier.F ? (_healthMax.Get(level) / 5) * (int)data.tier : 0;
                return base.healthMax + result;
            }
        }
        public override int manaMax {
            get {
                int result = data.intelligence * Storage.data.AP_Intelligence_MANA;
                result += data.tier != Tier.F ? (_manaMax.Get(level) / 5) * (int)data.tier : 0;
                return base.manaMax + result;
            }
        }
        #endregion
        #region Attack
        public override int p_atk {
            get {
                int result = data.strength * Storage.data.AP_Strength_ATK;
                result += data.tier != Tier.F ? (_p_atk.Get(level) / 5) * (int)data.tier : 0;
                return base.p_atk + result;
            }
        }
        public override int m_atk {
            get {
                int result = data.intelligence * Storage.data.AP_Intelligence_ATK;
                result += data.tier != Tier.F ? (_m_atk.Get(level) / 5) * (int)data.tier : 0;
                return base.m_atk + result;
            }
        }
        #endregion
        #region Defense
        public override int p_def {
            get {
                int result = data.endurance * Storage.data.AP_Endurance + data.intelligence * Storage.data.AP_Strength_DEF;
                result += data.tier != Tier.F ? (_p_def.Get(level) / 5) * (int)data.tier : 0;
                return base.p_def + result;
            }
        }
        public override int m_def {
            get {
                int result = data.endurance * Storage.data.AP_Endurance + data.intelligence * Storage.data.AP_Intelligence_DEF;
                result += data.tier != Tier.F ? (_m_def.Get(level) / 5) * (int)data.tier : 0;
                return base.m_def + result;
            }
        }
        #endregion
        public override uint battlepower => Convert.ToUInt32(healthMax + manaMax + m_atk + p_atk + m_def + p_def + 
        (blockChance + untiBlockChance + critRate + critDmg + antiCrit + untiStunChance) * 100);
    #endregion
        public bool Feed(uint amount) {
            if(amount > 0) {
                PetInfo info = data;
                info.Feed(amount);
                owner.own.pets[dataIndex] = info;
                return true;
            }
            return false;
        }
        public override void OnStartServer() {
            base.OnStartServer(); // call Entity's OnStartServer
            // load skills based on skill templates
            foreach (ScriptableSkill skillData in skillTemplates)
                skills.Add(new Skill(skillData));
        }
        public override bool IsWorthUpdating() => true;
        public override void Warp(Vector3 destination) {
            agent.Warp(destination);
            networkNavMeshAgent.RpcWarp(destination);
        }
        public override void ResetMovement() => agent.ResetMovement();
        public override bool CanAttack(Entity entity) => base.CanAttack(entity) && (entity is Monster ||
                                    (entity is Player && entity != owner) || (entity is Pet pet && pet.owner != owner));
        public float CurrentCastRange() => 0 <= currentSkill && currentSkill < skills.Count ? skills[currentSkill].castRange : 0;
        sbyte NextSkill() {
            for (sbyte i = 0; i < skills.Count; ++i) {
                sbyte index = (sbyte)((lastSkill + 1 + i) % skills.Count);
                if (CastCheckSelf(skills[index]))
                    return index;
            }
            return -1;
        }
        #region State-Machine Events
        // checks
        bool EventOwnerDisappeared() => owner == null;
        bool EventDied() => health == 0;
        bool EventDeathTimeElapsed() => state == EntityState.Dead && NetworkTime.time >= deathTimeEnd;
        bool EventTargetDisappeared() => target == null;
        bool EventTargetDied() => target != null && target.health == 0;
        bool EventTargetTooFarToAttack() {
            Vector3 destination;
            return target != null &&
                0 <= currentSkill && currentSkill < skills.Count &&
                !CastCheckDistance(skills[currentSkill], out destination);
        }
        bool EventTargetTooFarToFollow() => target != null &&
                Vector3.Distance(owner.petDestination, Utils.ClosestPoint(target, transform.position)) > followDistance;
        bool EventNeedReturnToOwner() => Vector3.Distance(owner.petDestination, transform.position) > returnDistance;
        bool EventNeedTeleportToOwner() => Vector3.Distance(owner.petDestination, transform.position) > teleportDistance;
        bool EventAggro() => target != null && target.health > 0;
        bool EventSkillRequest() => 0 <= currentSkill && currentSkill < skills.Count;
        bool EventSkillFinished() => 0 <= currentSkill && currentSkill < skills.Count && skills[currentSkill].CastTimeRemaining() == 0;
        bool EventMoveEnd() => state == EntityState.Moving && !IsMoving();
        bool EventStunned() => NetworkTime.time <= stunTimeEnd;
        // events
        [Server] protected override EntityState UpdateServer() {
            if (state == EntityState.Idle)    return UpdateServer_IDLE();
            if (state == EntityState.Moving)  return UpdateServer_MOVING();
            if (state == EntityState.Casting) return UpdateServer_CASTING();
            if (state == EntityState.Stunned) return UpdateServer_STUNNED();
            if (state == EntityState.Dead)    return UpdateServer_DEAD();
            return EntityState.Idle;
        }
        [Server] EntityState UpdateServer_IDLE() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventOwnerDisappeared()) {
                // owner might disconnect or get destroyed for some reason
                NetworkServer.Destroy(gameObject);
                return EntityState.Idle;
            }
            if (EventDied()) {
                // we died.
                OnDeath();
                return EntityState.Dead;
            }
            if (EventStunned()) {
                ResetMovement();
                return EntityState.Stunned;
            }
            if (EventTargetDied()) {
                // we had a target before, but it died now. clear it.
                target = null;
                CancelCastSkill();
                return EntityState.Idle;
            }
            if (EventNeedTeleportToOwner()) {
                Warp(owner.petDestination);
                return EntityState.Idle;
            }
            if (EventNeedReturnToOwner()) {
                // return to owner only while IDLE
                target = null;
                CancelCastSkill();
                Navigate(owner.petDestination, 0);
                return EntityState.Moving;
            }
            if (EventTargetTooFarToFollow()) {
                // we had a target before, but it's out of follow range now.
                // clear it and go back to start. don't stay here.
                target = null;
                CancelCastSkill();
                Navigate(owner.petDestination, 0);
                return EntityState.Moving;
            }
            if (EventTargetTooFarToAttack()) {
                // we had a target before, but it's out of attack range now.
                // follow it. (use collider point(s) to also work with big entities)
                Navigate(Utils.ClosestPoint(target, transform.position),
                        CurrentCastRange() * attackToMoveRangeRatio);
                return EntityState.Moving;
            }
            if (EventSkillRequest()) {
                // we had a target in attack range before and trying to cast a skill
                // on it. check self (alive, mana, weapon etc.) and target
                Skill skill = skills[currentSkill];
                if (CastCheckSelf(skill) && CastCheckTarget(skill)) {
                    // start casting
                    StartCastSkill(skill);
                    return EntityState.Casting;
                } else {
                    // invalid target. reset attempted current skill cast.
                    target = null;
                    currentSkill = -1;
                    return EntityState.Idle;
                }
            }
            if (EventAggro()) {
                // target in attack range. try to cast a first skill on it
                if (skills.Count > 0) currentSkill = NextSkill();
                else Debug.LogError(name + " has no skills to attack with.");
                return EntityState.Idle;
            }
            return EntityState.Idle; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_MOVING() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventOwnerDisappeared()) {
                // owner might disconnect or get destroyed for some reason
                NetworkServer.Destroy(gameObject);
                return EntityState.Idle;
            }
            if (EventDied()) {
                // we died.
                OnDeath();
                ResetMovement();
                return EntityState.Dead;
            }
            if (EventStunned()) {
                ResetMovement();
                return EntityState.Stunned;
            }
            if (EventMoveEnd()) {
                // we reached our destination.
                return EntityState.Idle;
            }
            if (EventTargetDied()) {
                // we had a target before, but it died now. clear it.
                target = null;
                CancelCastSkill();
                ResetMovement();
                return EntityState.Idle;
            }
            if (EventNeedTeleportToOwner()) {
                Warp(owner.petDestination);
                return EntityState.Idle;
            }
            if (EventTargetTooFarToFollow()) {
                // we had a target before, but it's out of follow range now.
                // clear it and go back to start. don't stay here.
                target = null;
                CancelCastSkill();
                Navigate(owner.petDestination, 0);
                return EntityState.Moving;
            }
            if (EventTargetTooFarToAttack()) {
                // we had a target before, but it's out of attack range now.
                // follow it. (use collider point(s) to also work with big entities)
                Navigate(Utils.ClosestPoint(target, transform.position),
                        CurrentCastRange() * attackToMoveRangeRatio);
                return EntityState.Moving;
            }
            if (EventAggro()) {
                // target in attack range. try to cast a first skill on it
                // (we may get a target while randomly wandering around)
                if (skills.Count > 0) currentSkill = NextSkill();
                else Debug.LogError(name + " has no skills to attack with.");
                ResetMovement();
                return EntityState.Idle;
            }
            return EntityState.Moving; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_CASTING() {
            // keep looking at the target for server & clients (only Y rotation)
            if (target)
                LookAtY(target.transform.position);
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventOwnerDisappeared()) {
                // owner might disconnect or get destroyed for some reason
                NetworkServer.Destroy(gameObject);
                return EntityState.Idle;
            }
            if (EventDied()) {
                // we died.
                OnDeath();
                return EntityState.Dead;
            }
            if (EventStunned()) {
                CancelCastSkill();
                ResetMovement();
                return EntityState.Stunned;
            }
            if (EventTargetDisappeared()) {
                // cancel if the target matters for this skill
                if (skills[currentSkill].cancelCastIfTargetDied) {
                    CancelCastSkill();
                    target = null;
                    return EntityState.Idle;
                }
            }
            if (EventTargetDied()) {
                // cancel if the target matters for this skill
                if (skills[currentSkill].cancelCastIfTargetDied) {
                    CancelCastSkill();
                    target = null;
                    return EntityState.Idle;
                }
            }
            if (EventSkillFinished()) {
                // finished casting. apply the skill on the target.
                FinishCastSkill(skills[currentSkill]);
                // did the target die? then clear it so that the monster doesn't
                // run towards it if the target respawned
                if (target.health == 0) target = null;
                // go back to IDLE. reset current skill.
                lastSkill = currentSkill;
                currentSkill = -1;
                return EntityState.Idle;
            }
            return EntityState.Casting; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_STUNNED() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventOwnerDisappeared()) {
                // owner might disconnect or get destroyed for some reason
                NetworkServer.Destroy(gameObject);
                return EntityState.Idle;
            }
            if (EventDied()) {
                // we died.
                OnDeath();
                CancelCastSkill(); // in case we died while trying to cast
                return EntityState.Dead;
            }
            if (EventStunned()) {
                return EntityState.Stunned;
            }
            // go back to idle if we aren't stunned anymore and process all new events there too
            return EntityState.Idle;
        }
        [Server] EntityState UpdateServer_DEAD() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventOwnerDisappeared()) {
                // owner might disconnect or get destroyed for some reason
                NetworkServer.Destroy(gameObject);
                return EntityState.Dead;
            }
            if (EventDeathTimeElapsed()) {
                // we were lying around dead for long enough now. hide while respawning, or disappear forever
                NetworkServer.Destroy(gameObject);
                return EntityState.Dead;
            }
            return EntityState.Dead; // nothing interesting happened
        }
        #endregion
        [Server] public override int DealDamageAt(Entity entity, int amount, float stunChance=0, float stunTime=0) {
            // deal damage with the default function
            amount = base.DealDamageAt(entity, amount, stunChance, stunTime);
            // a monster?
            if(entity is WorldBoss) {
                owner.OnDamageDealtToWorldBoss((WorldBoss)entity, amount);
            } else if (entity is Monster) {
                // forward to owner to share rewards with everyone
                owner.OnDamageDealtToMonster((Monster)entity);
            }
            // a player?
            else if (entity is Player) {
                // forward to owner for murderer detection etc.
                owner.OnDamageDealtToPlayer((Player)entity, amount);
            }
            return amount;
        }
        [ServerCallback] public override void OnAggro(Entity entity) {
            // are we alive, and is the entity alive and of correct type?
            if (entity != null && CanAttack(entity)) {
                // no target yet(==self), or closer than current target?
                // => has to be at least 20% closer to be worth it, otherwise we
                //    may end up nervously switching between two targets
                // => we do NOT use Utils.ClosestDistance, because then we often
                //    also end up nervously switching between two animated targets,
                //    since their collides moves with the animation.
                if (target == null) {
                    target = entity;
                } else {
                    float oldDistance = Vector3.Distance(transform.position, target.transform.position);
                    float newDistance = Vector3.Distance(transform.position, entity.transform.position);
                    if (newDistance < oldDistance * 0.8) target = entity;
                }
            }
        }
        [Server] protected override void OnDeath() {
            base.OnDeath();
            deathTimeEnd = NetworkTime.time + deathTime;
        }

    }
}