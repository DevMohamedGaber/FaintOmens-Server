using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using Game.Network;
namespace Game
{
    [RequireComponent(typeof(Rigidbody))] // kinematic, only needed for OnTrigger
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class Entity : NetworkBehaviourNonAlloc {
        [Header("Components")]
        public NavMeshAgent agent;
    #pragma warning disable CS0109 // member does not hide accessible member
        public new Collider collider;
    #pragma warning restore CS0109 // member does not hide accessible member
    #region Variables
        #region Static
        [Header("Entity Static")]
        public DamageType classType;
        public ScriptableSkill[] skillTemplates;
        //public SkillEffectsManager skillEffectsManager;
        [HideInInspector] public bool inSafeZone;
        protected double stunTimeEnd;
        #endregion
        #region Synced
        [Header("Entity Synced")]
        [SerializeField, SyncVar] byte _level;
        public int level {
            get => (int)_level;
            set => _level = (byte)value;
        }
        [SerializeField, SyncVar] GameObject _target;
        public Entity target {
            get => _target != null  ? _target.GetComponent<Entity>() : null;
            set => _target = value != null ? value.gameObject : null;
        }
        [SyncVar, HideInInspector] public sbyte currentSkill = -1;
        [SyncVar] public double lastCombatTime;
        public SyncListSkill skills = new SyncListSkill();
        public SyncListBuff buffs = new SyncListBuff(); // active buffs
        #endregion
    #endregion //Variables
    #region Attributes
        #region Health
        [Header("Health")]
        [SerializeField] protected LinearInt _healthMax = new LinearInt{baseValue=100};
        public virtual int healthMax {
            get {
                int result = _healthMax.Get(level);
                foreach (Skill skill in skills)
                    if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                        result += passiveSkill.healthMaxBonus.Get(skill.level);
                for (int i = 0; i < buffs.Count; ++i)
                    result += buffs[i].healthMaxBonus;
                return result;
            }
        }
        public bool invincible = false; // GMs, Npcs, ...
        [SyncVar, SerializeField] int _health = 1;
        public int health {
            get => Mathf.Min(_health, healthMax);
            set => _health = Mathf.Clamp(value, 0, healthMax);
        }
        public bool healthRecovery = true; // can be disabled in combat etc.
        [SerializeField] protected LinearInt _healthRecoveryRate = new LinearInt{baseValue=1};
        public virtual int healthRecoveryRate {
            get {
                float result = _healthRecoveryRate.Get(level);
                foreach(Skill skill in skills)
                    if(skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                        result += passiveSkill.healthPercentPerSecondBonus.Get(skill.level);
                for(int i = 0; i < buffs.Count; i++)
                    result += buffs[i].healthPercentPerSecondBonus;
                return Convert.ToInt32(result * healthMax);
            }
        }
        #endregion
        #region Mana
        [Header("Mana")]
        [SerializeField] protected LinearInt _manaMax = new LinearInt{baseValue=100};
        public virtual int manaMax {
            get {
                int result = _manaMax.Get(level);
                foreach (Skill skill in skills)
                    if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                        result += passiveSkill.manaMaxBonus.Get(skill.level);
                for (int i = 0; i < buffs.Count; ++i)
                    result += buffs[i].manaMaxBonus;
                return result;
            }
        }
        [SyncVar] int _mana = 1;
        public int mana {
            get => Mathf.Min(_mana, manaMax);
            set => _mana = Mathf.Clamp(value, 0, manaMax);
        }
        public bool manaRecovery = true; // can be disabled in combat etc.
        [SerializeField] protected LinearInt _manaRecoveryRate = new LinearInt{baseValue=1};
        public virtual int manaRecoveryRate {
            get {
                float result = _manaRecoveryRate.Get(level);
                foreach (Skill skill in skills)
                    if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                        result += passiveSkill.manaPercentPerSecondBonus.Get(skill.level);
                foreach (Buff buff in buffs)
                    result += buff.manaPercentPerSecondBonus;
                return Convert.ToInt32(result * manaMax);
            }
        }
        #endregion
        #region Attack
        [Header("Attack")]
        [SerializeField] protected LinearInt _p_atk = new LinearInt{baseValue=1};
        public virtual int p_atk {
            get {
                // base + passives + buffs
                int result = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).damageBonus.Get(skill.level)).Sum();
                result += buffs.Sum(buff => buff.damageBonus);
                return result;
            }
        }
        [SerializeField] protected LinearInt _m_atk = new LinearInt{baseValue=1};
        public virtual int m_atk {
            get {
                int result = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).damageBonus.Get(skill.level)).Sum();
                result += buffs.Sum(buff => buff.damageBonus);
                return result;
            }
        }
        #endregion
        #region Defense
        [Header("Defense")]
        [SerializeField] protected LinearInt _p_def = new LinearInt{baseValue=1};
        public virtual int p_def {
            get {
                // base + passives + buffs
                int result = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).defenseBonus.Get(skill.level)).Sum();
                result += buffs.Sum(buff => buff.defenseBonus);
                return result;
            }
        }
        [SerializeField] protected LinearInt _m_def = new LinearInt{baseValue=1};
        public virtual int m_def {
            get {
                // base + passives + buffs
                int result = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).defenseBonus.Get(skill.level)).Sum();
                result += buffs.Sum(buff => buff.defenseBonus);
                return result;
            }
        }
        #endregion
        #region Block
        [Header("Block")]
        [SerializeField] protected LinearFloat _blockChance;
        public virtual float blockChance {
            get {
                float result = 0;
                foreach (Skill skill in skills)
                    if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                        result += passiveSkill.blockChanceBonus.Get(skill.level);
                foreach (Buff buff in buffs)
                    result += buff.blockChanceBonus;
                return result;
            }
        }
        [SerializeField] protected LinearFloat _untiBlockChance;
        public virtual float untiBlockChance {
            get {
                float result = 0;
                /* base + passives + buffs
                float passiveBonus = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).bonusBlockChance.Get(skill.level)).Sum();
                float buffBonus = buffs.Sum(buff => buff.bonusBlockChance);*/
                return result;
            }
        }
        #endregion
        #region Critical
        [Header("Critical")]
        [SerializeField] protected LinearFloat _critRate;
        public virtual float critRate {
            get {
                float result = 0;
                foreach (Skill skill in skills)
                    if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                        result += passiveSkill.criticalChanceBonus.Get(skill.level);
                foreach (Buff buff in buffs)
                    result += buff.criticalChanceBonus;
                return result;
            }
        }
        [SerializeField] protected LinearFloat _critDmg;
        public virtual float critDmg {
            get {
                float result = 0;
                /* base + passives + buffs
                float passiveBonus = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).bonusCriticalChance.Get(skill.level)).Sum();
                float buffBonus = buffs.Sum(buff => buff.bonusCriticalChance);*/
                return result;
            }
        }
        [SerializeField] protected LinearFloat _antiCrit;
        public virtual float antiCrit {
            get {
                float result = 0;
                /* base + passives + buffs
                float passiveBonus = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).bonusCriticalChance.Get(skill.level)).Sum();
                float buffBonus = buffs.Sum(buff => buff.bonusCriticalChance);*/
                return result;
            }
        }
        #endregion
        #region Stun
        [SerializeField] protected LinearFloat _untiStunChance;
        public virtual float untiStunChance {
            get {
                return 0;//_untiStunChance.Get(level);
            }
        }
        #endregion
        #region Speed
        [Header("Speed")]
        [SerializeField] protected LinearFloat _speed = new LinearFloat{baseValue=5};
        public virtual float speed {
            get {
                float result = 0;
                foreach (Skill skill in skills)
                    if (skill.level > 0 && skill.data is PassiveSkill passiveSkill)
                        result += passiveSkill.speedBonus.Get(skill.level);
                foreach (Buff buff in buffs)
                    result += buff.speedBonus;
                return result;
            }
        }
        #endregion
        #region Helpers
        public int MyAtkType {
            get {
                if(classType == DamageType.Physical)
                    return p_atk;
                return m_atk;
            }
        }
        //Battle Power
        public virtual uint battlepower {
            get {
                return Convert.ToUInt32(healthMax + manaMax + m_atk + p_atk + m_def + p_def + 
                (int)(blockChance + untiBlockChance + critRate + critDmg + antiCrit));
            }
        }
        public float HealthPercent() => (health != 0 && healthMax != 0) ? (float)health / (float)healthMax : 0;
        public float ManaPercent() => (mana != 0 && manaMax != 0) ? (float)mana / (float)manaMax : 0;
        #endregion
    #endregion //Attributes
    #region General Server Functions
        protected virtual void Start() {}
        protected abstract EntityState UpdateServer();
        public virtual void OnAggro(Entity entity) {}
        public override void OnStartServer() {
            // health recovery every second
            if(healthRecovery || manaRecovery)
                InvokeRepeating("Recover", 1, 1);
            // dead if spawned without health
            //if (health == 0) state = EntityState.Dead;
        }
        public virtual bool IsWorthUpdating() => netIdentity.observers == null || netIdentity.observers.Count > 0 || IsHidden();
        void Update() {
            // only update if it's worth updating (see IsWorthUpdating comments)
            // -> we also clear the target if it's hidden, so that players don't keep hidden (respawning) monsters as target,
            //    hence don't show them as target again when they are shown again
            if (IsWorthUpdating()) {
                // always apply speed to agent
                agent.speed = speed;
                CleanupBuffs();
                if(target != null && target.IsHidden()) 
                    target = null;
                //state = UpdateServer();
            }
        }
        // visibility
        [Server]
        public void Hide() => netIdentity.visible = Visibility.ForceHidden;
        [Server]
        public void Show() => netIdentity.visible = Visibility.Default;
        public bool IsHidden() => netIdentity.visible == Visibility.ForceHidden;
        public float VisRange() => ((SpatialHashingInterestManagement)NetworkServer.aoi).visRange;
        // look at a transform while only rotating on the Y axis (to avoid weird tilts) 
        public void LookAtY(Vector3 position) => transform.LookAt(new Vector3(position.x, transform.position.y, position.z));
        public bool IsMoving() => agent.pathPending || agent.remainingDistance > agent.stoppingDistance || agent.velocity != Vector3.zero;
        public virtual void Navigate(Vector3 destination, float stoppingDistance) {
            agent.stoppingDistance = stoppingDistance;
            agent.destination = destination;
        }
        public abstract void Warp(Vector3 destination);
        public abstract void ResetMovement();
        [Server] public void Revive(float healthPercentage = 1) {
            health = Mathf.RoundToInt(healthMax * healthPercentage);
        }
        [Server] public void Recover() {
            if (enabled && health > 0) {
                if (healthRecovery) health += healthRecoveryRate;
                if (manaRecovery) mana += manaRecoveryRate;
            }
        }
        // client rpc
        [ClientRpc] void RpcOnDamageReceived(int amount, AttackType damageType) {}
        // combat
        public int GetSkillIndexByName(int skillName) {
            for(int i = 0; i < skills.Count; ++i)
                if (skills[i].name == skillName)
                    return i;
            return -1;
        }
        public int GetBuffIndexByName(int buffName) {
            for (int i = 0; i < buffs.Count; ++i)
                if (buffs[i].name == buffName)
                    return i;
            return -1;
        }
        public virtual bool CanAttack(Entity entity) => health > 0 && entity.health > 0 && entity != this &&
                !inSafeZone && !entity.inSafeZone &&
                !NavMesh.Raycast(transform.position, entity.transform.position, out NavMeshHit hit, NavMesh.AllAreas);
        public bool CastCheckSelf(Skill skill, bool checkSkillReady = true) => skill.CheckSelf(this, checkSkillReady);

        // the second check validates the target and corrects it for the skill if
        // necessary (e.g. when trying to heal an npc, it sets target to self first)
        // (skill shots that don't need a target will just return true if the user
        //  wants to cast them at a valid position)
        public bool CastCheckTarget(Skill skill) => skill.CheckTarget(this);

        // the third check validates the distance between the caster and the target
        // (target entity or target position in case of skill shots)
        // note: castchecktarget already corrected the target (if any), so we don't
        //       have to worry about that anymore here
        public bool CastCheckDistance(Skill skill, out Vector3 destination) => skill.CheckDistance(this, out destination);

        // starts casting
        public void StartCastSkill(Skill skill) {
            // start casting and set the casting end time
            skill.castTimeEnd = NetworkTime.time + skill.castTime;
            // save modifications
            skills[currentSkill] = skill;
            //skill.Apply(this);// let the skill template handle the action
            // rpc for client sided effects
            // -> pass that skill because skillIndex might be reset in the mean time, we never know
            RpcSkillCastStarted(skill);
        }
        [Server] public void CancelCastSkill(bool resetCurrentSkill = true) {
            // reset cast time, otherwise if a buff has a 10s cast time and we cancel the cast after 1s, then we would have 
            // to wait 9 more seconds before we can attempt to cast it again.
            // -> we cancel it in any case. players will have to wait for 'casttime' when attempting another cast anyway. 
            if (currentSkill != -1) {
                Skill skill = skills[currentSkill];
                skill.castTimeEnd = NetworkTime.time - skill.castTime;
                skills[currentSkill] = skill;
                // reset current skill
                RpcSkillCastCanceled();
                if (resetCurrentSkill)
                    currentSkill = -1;
            }
        }
        public void FinishCastSkill(Skill skill) {
            // * check if we can currently cast a skill (enough mana etc.)
            // * check if we can cast THAT skill on THAT target
            // note: we don't check the distance again. the skill will be cast even if the target walked a bit while we 
            // casted it (it's simply better gameplay and less frustrating)
            if (CastCheckSelf(skill)/* && CastCheckTarget(skill)*/) {
                skill.Apply(this);// let the skill template handle the action
                // rpc for client sided effects
                // -> pass that skill because skillIndex might be reset in the mean time, we never know
                RpcSkillCastFinished(skill);
                mana -= skill.manaCosts; // decrease mana in any case
                skill.cooldownEnd = NetworkTime.time + skill.cooldown; // start the cooldown (and save it in the struct)
                //if(this is Player && target is Monster targetMonster) {
                //    skill.AddExp(targetMonster.rewardSkillExperience);
                //}
                skills[currentSkill] = skill; // save any skill modifications in any case
            } else {
                currentSkill = -1;  // not all requirements met. no need to cast the same skill again
            }
        }
        public void AddOrRefreshBuff(Buff buff) { // reset if already in buffs list, otherwise add
            int index = GetBuffIndexByName(buff.name);
            if(index != -1) buffs[index] = buff;
            else buffs.Add(buff);
        }
        void CleanupBuffs() { // helper function to remove all buffs that ended
            for(int i = 0; i < buffs.Count; ++i) {
                if(buffs[i].BuffTimeRemaining() == 0) {
                    buffs.RemoveAt(i);
                    --i;
                }
            }
        }
        [ClientRpc] public void RpcSkillCastStarted(Skill skill) {}
        // skill cast finished rpc for client sided effects
        // note: no need to pass skillIndex, currentSkill is synced anyway
        [ClientRpc] public void RpcSkillCastFinished(Skill skill) {}
        [ClientRpc] public void RpcSkillCastCanceled() {}
        [Server] public virtual int DealDamageAt(Entity entity, int amount, float stunChance = 0, float stunTime = 0) {
            int damageDealt = CalculateDamage(entity, amount, out AttackType damageType);

            entity.ApplyDamage(damageDealt, stunChance, stunTime); // deal the damage
            
            //let's make sure to pull aggro in any case so that archers are still attacked if they are outside of the aggro range
            entity.OnAggro(this);

            // show effects on clients
            if(damageType == AttackType.Block) entity.RpcOnDamageBlocked();
            else entity.RpcOnDamageReceived(damageDealt, damageType);

            // reset last combat time for both
            lastCombatTime = NetworkTime.time;
            entity.lastCombatTime = NetworkTime.time;
            if(entity is Player player) {
                player.ApplyDamageToEquipments(damageDealt / Storage.data.player.equipmentCount);
            }

            return damageDealt;
        }
        [Server] public int CalculateDamage(Entity entity, int amount, out AttackType damageType) {
            damageType = AttackType.Normal;
            int damageDealt = 0;

            // don't deal any damage if entity is invincible
            if (!entity.invincible) {
                // block? (we use < not <= so that block rate 0 never blocks)
                if (UnityEngine.Random.value < entity.blockChance - untiBlockChance) {
                    damageType = AttackType.Block;
                    return 0;
                }
                else { // deal damage
                    // subtract defense (but leave at least 1 damage, otherwise it may be frustrating for weaker players)
                    damageDealt = Mathf.Max(amount - (classType == DamageType.Physical ? entity.p_def : entity.m_def), 1);

                    // critical hit?
                    if (UnityEngine.Random.value < critRate - entity.antiCrit) {
                        damageDealt += (int)(damageDealt * critDmg);
                        damageType = AttackType.Crit;
                    }
                }
            }
            return damageDealt;
        }
        [Server] void ApplyDamage(int damageDealt, float stunChance=0, float stunTime=0) {
            health -= damageDealt;
            if(UnityEngine.Random.value < stunChance  - untiStunChance) {// stun?
                // dont allow a short stun to overwrite a long stun
                // => if a player is hit with a 10s stun, immediately followed by a 1s stun, we don't want it to end in 1s!
                double newStunEndTime = NetworkTime.time + stunTime;
                stunTimeEnd = Math.Max(newStunEndTime, stunTimeEnd);
            }
        }
        [ClientRpc] void RpcOnDamageBlocked() {}
        /*void InstantiateEffect(int index) {
            if(skillEffectsManager != null)
                skillEffectsManager.InstantiateEffect(index);
        }*/
        [Server] protected virtual void OnDeath() {
            // movement/target/cast
            ResetMovement();
            target = null;
            CancelCastSkill();
            // clear buffs that shouldn't remain after death
            for(int i = 0; i < buffs.Count; ++i) {
                if (!buffs[i].remainAfterDeath) {
                    buffs.RemoveAt(i);
                    --i;
                }
            }
        }
    #endregion //Server Functions
    // ontrigger
        protected virtual void OnTriggerEnter(Collider col) {
            // check if trigger first to avoid GetComponent tests for environment
            if (col.isTrigger && col.GetComponent<Components.SafeZone>())
                inSafeZone = true;
        }

        protected virtual void OnTriggerExit(Collider col) {
            // check if trigger first to avoid GetComponent tests for environment
            if (col.isTrigger && col.GetComponent<Components.SafeZone>())
                inSafeZone = false;
        }
    }
}