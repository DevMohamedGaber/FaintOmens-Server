using UnityEngine;
using Mirror;
namespace Game
{
    [RequireComponent(typeof(NetworkNavMeshAgent))]
    public class Monster : Entity
    {
        [Header("Components")]
        public NetworkNavMeshAgent networkNavMeshAgent;
        [Header("Movement")]
        [Range(0, 1)] public float moveProbability = 0.1f;
        public float moveDistance = 10;
        public float followDistance = 20;
        [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f; // move as close as 0.8 * attackRange to a target
        [Header("Experience Reward")]
        public uint rewardExperience = 10;
        public uint rewardSkillExperience = 2;
        [Header("Loot")]
        public uint lootGoldMin = 0;
        public uint lootGoldMax = 10;
        public ItemDropChance[] dropChances;
        [Header("Respawn")]
        public float deathTime = 30f; // enough for animation & looting
        public Game.Components.MonstersSpawnArea belongingArea;
        double deathTimeEnd; // double for long term precision
        public bool respawn = true;
        public float respawnTime = 10f;
        double respawnTimeEnd; // double for long term precision
        Vector3 startPosition;
        int lastSkill = -1; // the last skill that was casted, to decide which one to cast next
        [SyncVar] public EntityState state = EntityState.Idle;
        public SyncListItemSlot inventory = new SyncListItemSlot();
        #region State-Machine Events
        // checks
        bool EventDied() => health == 0;
        bool EventDeathTimeElapsed() => state == EntityState.Dead && NetworkTime.time >= deathTimeEnd;
        bool EventRespawnTimeElapsed() => state == EntityState.Dead && respawn && NetworkTime.time >= respawnTimeEnd;
        bool EventTargetDisappeared() => target == null;
        bool EventTargetDied() => target != null && target.health == 0;
        bool EventTargetTooFarToAttack() => target != null && 0 <= currentSkill && currentSkill < skills.Count &&
                !CastCheckDistance(skills[currentSkill], out Vector3 destination);
        bool EventTargetTooFarToFollow() => target != null &&
                Vector3.Distance(startPosition, Utils.ClosestPoint(target, transform.position)) > followDistance;
        bool EventTargetEnteredSafeZone() => target != null && target.inSafeZone;
        bool EventAggro() => target != null && target.health > 0;
        bool EventSkillRequest() => 0 <= currentSkill && currentSkill < skills.Count;
        bool EventSkillFinished() => 0 <= currentSkill && currentSkill < skills.Count && skills[currentSkill].CastTimeRemaining() == 0;
        bool EventMoveEnd() => state == EntityState.Moving && !IsMoving();
        bool EventMoveRandomly() => Random.value <= moveProbability * Time.deltaTime;
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
            if (EventDied()) {
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
            if (EventTargetTooFarToFollow()) {
                // we had a target before, but it's out of follow range now.
                // clear it and go back to start. don't stay here.
                target = null;
                CancelCastSkill();
                Navigate(startPosition, 0);
                return EntityState.Moving;
            }
            if (EventTargetTooFarToAttack()) {
                // we had a target before, but it's out of attack range now.
                // follow it. (use collider point(s) to also work with big entities)
                Navigate(Utils.ClosestPoint(target, transform.position),
                        CurrentCastRange() * attackToMoveRangeRatio);
                return EntityState.Moving;
            }
            if (EventTargetEnteredSafeZone()) {
                // if our target entered the safe zone, we need to be really careful to avoid kiting.
                // -> players could pull a monster near a safe zone and then step in and out of it before/after attacks without ever getting hit by the monster
                // -> running back to start won't help, can still kit while running
                // -> warping back to start won't help, we might accidentally placed a monster in attack range of a safe zone
                // -> the 100% secure way is to die and hide it immediately. many popular MMOs do it the same way to avoid exploits.
                // => call Entity.OnDeath without rewards etc. and hide immediately
                base.OnDeath(); // no looting
                respawnTimeEnd = NetworkTime.time + respawnTime; // respawn in a while
                return EntityState.Dead;
            }
            if (EventSkillRequest()) {
                // we had a target in attack range before and trying to cast a skill on it. check self (alive, mana, weapon etc.) and target
                Skill skill = skills[currentSkill];
                if (CastCheckSelf(skill)) {
                    if (CastCheckTarget(skill)) {
                        // start casting
                        StartCastSkill(skill);
                        return EntityState.Casting;
                    } else {
                        // invalid target. clear the attempted current skill.
                        target = null;
                        currentSkill = -1;
                        return EntityState.Idle;
                    }
                } else {
                    // we can't cast this skill at the moment (cooldown/low mana/...)
                    // -> clear the attempted current skill, but keep the target to continue later
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
            if (EventMoveRandomly()) {
                // walk to a random position in movement radius (from 'start')
                // note: circle y is 0 because we add it to start.y
                Vector2 circle2D = Random.insideUnitCircle * moveDistance;
                Navigate(startPosition + new Vector3(circle2D.x, 0, circle2D.y), 0);
                return EntityState.Moving;
            }
            if (EventDeathTimeElapsed()) {} // don't care
            if (EventRespawnTimeElapsed()) {} // don't care
            if (EventMoveEnd()) {} // don't care
            if (EventSkillFinished()) {} // don't care
            if (EventTargetDisappeared()) {} // don't care
            return EntityState.Idle; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_MOVING() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventDied()) {
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
            if (EventTargetTooFarToFollow()) {
                // we had a target before, but it's out of follow range now.
                // clear it and go back to start. don't stay here.
                target = null;
                CancelCastSkill();
                Navigate(startPosition, 0);
                return EntityState.Moving;
            }
            if (EventTargetTooFarToAttack()) {
                // we had a target before, but it's out of attack range now.
                // follow it. (use collider point(s) to also work with big entities)
                Navigate(Utils.ClosestPoint(target, transform.position),
                        CurrentCastRange() * attackToMoveRangeRatio);
                return EntityState.Moving;
            }
            if (EventTargetEnteredSafeZone()) {
                // if our target entered the safe zone, we need to be really careful to avoid kiting.
                // -> players could pull a monster near a safe zone and then step in and out of it before/after attacks without ever getting hit by the monster
                // -> running back to start won't help, can still kit while running
                // -> warping back to start won't help, we might accidentally placed a monster in attack range of a safe zone
                // -> the 100% secure way is to die and hide it immediately. many popular MMOs do it the same way to avoid exploits.
                // => call Entity.OnDeath without rewards etc. and hide immediately
                base.OnDeath(); // no looting
                respawnTimeEnd = NetworkTime.time + respawnTime; // respawn in a while
                return EntityState.Dead;
            }
            if (EventAggro()) {
                // target in attack range. try to cast a first skill on it
                // (we may get a target while randomly wandering around)
                if (skills.Count > 0) currentSkill = NextSkill();
                else Debug.LogError(name + " has no skills to attack with.");
                ResetMovement();
                return EntityState.Idle;
            }
            if (EventDeathTimeElapsed()) {} // don't care
            if (EventRespawnTimeElapsed()) {} // don't care
            if (EventSkillFinished()) {} // don't care
            if (EventTargetDisappeared()) {} // don't care
            if (EventSkillRequest()) {} // don't care, finish movement first
            if (EventMoveRandomly()) {} // don't care
            return EntityState.Moving; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_CASTING() {
            // keep looking at the target for server & clients (only Y rotation)
            if (target)
                LookAtY(target.transform.position);
            // events sorted by priority (e.g. target doesn't matter if we died)
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
            if (EventTargetEnteredSafeZone()) {
                // cancel if the target matters for this skill
                if (skills[currentSkill].cancelCastIfTargetDied) {
                    // if our target entered the safe zone, we need to be really careful
                    // to avoid kiting.
                    // -> players could pull a monster near a safe zone and then step in
                    //    and out of it before/after attacks without ever getting hit by
                    //    the monster
                    // -> running back to start won't help, can still kit while running
                    // -> warping back to start won't help, we might accidentally placed
                    //    a monster in attack range of a safe zone
                    // -> the 100% secure way is to die and hide it immediately. many
                    //    popular MMOs do it the same way to avoid exploits.
                    // => call Entity.OnDeath without rewards etc. and hide immediately
                    base.OnDeath(); // no looting
                    respawnTimeEnd = NetworkTime.time + respawnTime; // respawn in a while
                    return EntityState.Dead;
                }
            }
            if (EventSkillFinished()) {
                // finished casting. apply the skill on the target.
                FinishCastSkill(skills[currentSkill]);
                // did the target die? then clear it so that the monster doesn't
                // run towards it if the target respawned
                // (target might be null if disappeared or targetless skill)
                if (target != null && target.health == 0) target = null;
                // go back to IDLE, reset current skill
                lastSkill = currentSkill;
                currentSkill = -1;
                return EntityState.Idle;
            }
            return EntityState.Casting; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_STUNNED() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventDied()) {
                OnDeath();
                return EntityState.Dead;
            }
            if (EventStunned()) {
                return EntityState.Stunned;
            }
            // go back to idle if we aren't stunned anymore and process all new
            // events there too
            return EntityState.Idle;
        }
        [Server] EntityState UpdateServer_DEAD() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventRespawnTimeElapsed()) {
                // respawn at the start position with full health, visibility, no loot
                //gold = 0;
                //inventory.Clear();
                Show();
                Warp(startPosition); // recommended over transform.position
                Revive();
                return EntityState.Idle;
            }
            if (EventDeathTimeElapsed()) {
                // we were lying around dead for long enough now.
                // hide while respawning, or disappear forever
                if (respawn) Hide();
                else NetworkServer.Destroy(gameObject);
                return EntityState.Dead;
            }
            return EntityState.Dead; // nothing interesting happened
        }
        #endregion
        public override void OnStartServer() {
            base.OnStartServer(); // call Entity's OnStartServer
            // all monsters should spawn with full health and mana
            health = healthMax;
            mana = manaMax;
            if(health == 0) {
                Debug.Log("Monster health = 0");
                health = 1000;
            }
            // load skills based on skill templates
            foreach (ScriptableSkill skillData in skillTemplates)
                skills.Add(new Skill(skillData));
        }
        protected override void Start() {
            base.Start();
            // remember start position in case we need to respawn later
            startPosition = transform.position;
        }
        public override void Warp(Vector3 destination) {
            agent.Warp(destination);
            networkNavMeshAgent.RpcWarp(destination);
        }
        public override void ResetMovement() => agent.ResetMovement();
        public override bool CanAttack(Entity entity) => base.CanAttack(entity) && (entity is Player || entity is Pet);
        public float CurrentCastRange() => 0 <= currentSkill && currentSkill < skills.Count ? skills[currentSkill].castRange : 0;
        sbyte NextSkill() {
            // find the next ready skill, starting at 'lastSkill+1' (= next one)
            // and looping at max once through them all (up to skill.Count)
            //  note: no skills.count == 0 check needed, this works with empty lists
            //  note: also works if lastSkill is still -1 from initialization
            for (sbyte i = 0; i < skills.Count; ++i) {
                sbyte index = (sbyte)((lastSkill + 1 + i) % skills.Count);
                // could we cast this skill right now? (enough mana, skill ready, etc.)
                if (CastCheckSelf(skills[index]))
                    return index;
            }
            return -1;
        }
        // death ///////////////////////////////////////////////////////////////////
        [Server] protected override void OnDeath() {
            // take care of entity stuff
            base.OnDeath();
            deathTimeEnd = NetworkTime.time + deathTime;
            respawnTimeEnd = deathTimeEnd + respawnTime; // after death time ended
            //loot
            /*uint gold = (uint)Random.Range(lootGoldMin, lootGoldMax);
            SyncListItemSlot lootItems = new SyncListItemSlot();
            if(dropChances.Length > 0) {
                for(int i = 0; i < dropChances.Length; i++) {
                    if(Random.value <= dropChances[i].probability)
                        lootItems.Add(new ItemSlot(dropChances[i].item));
                }
            }
            if(gold > 0 || lootItems.Count > 0) {
                GameObject go = Instantiate(Storage.data.lootPrefab, transform.position, transform.rotation);
                Loot loot = go.GetComponent<Loot>();
                loot.gold = gold;
                loot.items = lootItems;
                NetworkServer.Spawn(go);
            }*/
        }
        [Server] public virtual void OnKilled(Player killer) {
            killer.AddGold((uint)Random.Range(lootGoldMin, lootGoldMax));
            // drop loot
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
                //    => we don't even need closestdistance here because they are in
                //       the aggro area anyway. transform.position is perfectly fine
                if (target == null) {
                    target = entity;
                }
                else if (entity != target) { // no need to check dist for same target
                    float oldDistance = Vector3.Distance(transform.position, target.transform.position);
                    float newDistance = Vector3.Distance(transform.position, entity.transform.position);
                    if (newDistance < oldDistance * 0.8) target = entity;
                }
            }
        }
    }
}