using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System;
using System.Linq;
using Mirror;
//using Game.Achievements;
using Game.Network;
using Game.Components;
using Game.ControlPanel;
namespace Game
{
    [RequireComponent(typeof(NetworkName))]
    [RequireComponent(typeof(PlayerOwnData))]
    [RequireComponent(typeof(ChatComponent))]
    public class Player : Entity
    {
        public static Dictionary<uint, Player> onlinePlayers = new Dictionary<uint, Player>();
        [Header("Player Components")]
        [SerializeField] NetworkNavMeshAgentRubberbanding rubberbanding;
        public ChatComponent chat;
        public PlayerOwnData own;
        [Header("Player Static variables")]   
        [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f;
        public bool continueCastAfterStunned = true;
    #region Sync Vars
        [SyncVar] public uint id;
        [SyncVar] public EntityState state = EntityState.Idle;
        [SyncVar] public PlayerClassData classInfo;
        [SyncVar] public Gender gender;
        [SyncVar] public byte cityId;
        [SyncVar] public PrivacyLevel privacy;
        [SyncVar] public byte avatar;
        [SyncVar] public byte frame;
        [SyncVar] public byte tribeId;
        [SyncVar] public GuildPublicInfo guild;
        [SyncVar] public uint teamId = 0;
        [SyncVar] public ushort activeTitle = 0;
        [SyncVar] public bool showWardrop = true;
        [SyncVar] public ActiveMount mount;
        [SyncVar] GameObject _nextTarget;
        public Entity nextTarget {
            get => _nextTarget != null  ? _nextTarget.GetComponent<Entity>() : null;
            set => _nextTarget = value != null ? value.gameObject : null;
        }
        [SyncVar] GameObject _activePet;
        public Pet activePet {
            get => _activePet != null  ? _activePet.GetComponent<Pet>() : null;
            set => _activePet = value != null ? value.gameObject : null;
        }
        public SyncListItemSlot equipment = new SyncListItemSlot();
        public SyncListWardrop wardrobe = new SyncListWardrop();
    #endregion //Variables
    #region Cached Vars
        public ulong accId;
        ScriptableTotalGemLevels totalGemLevelBonus;
        ScriptableTotalPlusLevels totalPlusLevelBonus;
        public Vector3 lastLocation;
        public Game.Achievements.InprogressAchievements inprogressAchievements;
        public short occupationId = -1;
        public uint tradeId = 0;
        public byte suspiciousActivities = 0;
        public Languages currentLang = Languages.En;
        bool IsEn => currentLang == Languages.En;
        public byte AvailableFreeRespawn;
        public GuildRecallRequest guildRecallRequest;
        public GuildInvitation[] guildInvitations;
        public int[] signUp7Days;
        public int[] recharge7Days;
        public int[] recharge7DaysRewards;
        public int selectedMountIndex = -1;
        int respawnRequested = -1;
        int useSkillWhenCloser = -1;
        bool cancelActionRequested;
        int pendingSkill = -1;
        Vector3 pendingDestination;
        bool pendingDestinationValid;
        Vector3 pendingVelocity;
        bool pendingVelocityValid;
        bool isTeleporting;
        List<int> growthEquips;
        bool isWearingGrowthItem => growthEquips.Count > 0;
        City city => Storage.data.cities[cityId];
        public Vector3 petDestination => transform.position - transform.right * collider.bounds.size.x;
        ushort MaxHonorPerDay => (ushort)(Storage.data.player.dailyHonor + own.vip.data.bonusHonor);
        public bool stillIn7Days => DateTime.FromOADate(own.createdAt).AddDays(7) >= DateTime.Now;
        public double allowedLogoutTime => lastCombatTime + Storage.data.player.combatLogoutDelay;
        public double remainingLogoutTime => NetworkTime.time < allowedLogoutTime ? (allowedLogoutTime - NetworkTime.time) : 0;
    #endregion
    #region Attributes
        #region Basics(Health/Mana/Speed)
        public override int healthMax {
            get {
                int result = _healthMax.Get(level) + own.vitality * Storage.data.AP_Vitality + classInfo.hp;
                for(int i = 0; i < equipment.Count; i++) {
                    if(equipment[i].amount > 0 && equipment[i].item.data != null)
                        result += (int)equipment[i].item.GetSocketOfType(BonusType.hp) + equipment[i].item.health;
                }
                for(int i = 0; i < own.accessories.Count; i++) {
                    if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                        result += (int)own.accessories[i].item.GetSocketOfType(BonusType.hp) + own.accessories[i].item.health;
                }
                result += activePet != null ? activePet.healthMax : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].healthMax / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].healthMax / 2 : own.mounts[i].healthMax;
                }
                result += own.militaryRank > -1 ? ScriptableMilitaryRank.dict[own.militaryRank].hp : 0;
                if(own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++) {
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.hp.active : title.hp.notActive;
                    }
                }
                result += InGuild() ? ScriptableGuildSkill.dict[0].Get(own.guildSkills[0]) * Storage.data.AP_Vitality : 0;
                return base.healthMax + result;
            }
        }
        public override int healthRecoveryRate => base.healthRecoveryRate + _healthRecoveryRate.Get(level);
        public override int manaMax {
            get {
                int result = _manaMax.Get(level) + own.intelligence * Storage.data.AP_Intelligence_MANA + classInfo.mp;
                for(int i = 0; i < equipment.Count; i++) {
                    if(equipment[i].amount > 0 && equipment[i].item.data != null)
                        result += equipment[i].item.mana;
                }
                for(int i = 0; i < own.accessories.Count; i++) {
                    if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                        result += own.accessories[i].item.mana;
                }
                result += activePet != null ? activePet.manaMax : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].manaMax / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].manaMax / 2 : own.mounts[i].manaMax;
                }
                result += own.militaryRank > -1 ? ScriptableMilitaryRank.dict[own.militaryRank].mp : 0;
                if(own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++) {
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.mp.active : title.mp.notActive;
                    }
                }
                result += InGuild() ? ScriptableGuildSkill.dict[2].Get(own.guildSkills[2]) * Storage.data.AP_Intelligence_MANA : 0;
                return base.manaMax + result;
            }
        }
        public override int manaRecoveryRate => base.manaRecoveryRate + _manaRecoveryRate.Get(level);
        public override float speed {
            get {
                return base.speed + _speed.Get(level) + (IsMounted() ? own.mounts[selectedMountIndex].speed : 0);
            }
        }
        #endregion
        #region Attack
        public override int p_atk {
            get {
                int result = _p_atk.Get(level) + own.strength * Storage.data.AP_Strength_ATK + classInfo.pAtk;
                for(int i = 0; i < equipment.Count; i++) {
                    if(equipment[i].amount > 0 && equipment[i].item.data != null)
                        result += (int)equipment[i].item.GetSocketOfType(BonusType.pAtk) + equipment[i].item.pAtk;
                }
                for(int i = 0; i < own.accessories.Count; i++) {
                    if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                        result += (int)own.accessories[i].item.GetSocketOfType(BonusType.pAtk) + own.accessories[i].item.pAtk;
                }
                result += activePet != null ? activePet.p_atk : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].p_atk / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].p_atk / 2 : own.mounts[i].p_atk;
                }
                result += classType == DamageType.Physical ? ScriptableMilitaryRank.dict[own.militaryRank].atk : 0;
                if(classType == DamageType.Physical && own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.pAtk.active : title.pAtk.notActive;
                }
                result += InGuild() ? ScriptableGuildSkill.dict[1].Get(own.guildSkills[1]) * Storage.data.AP_Strength_ATK : 0;
                return base.p_atk + result;
            }
        }
        public override int m_atk {
            get {
                int result = _m_atk.Get(level) + own.intelligence * Storage.data.AP_Intelligence_ATK + classInfo.mAtk;
                for(int i = 0; i < equipment.Count; i++) {
                    if(equipment[i].amount > 0 && equipment[i].item.data != null)
                        result += (int)equipment[i].item.GetSocketOfType(BonusType.mAtk) + equipment[i].item.mAtk;
                }
                for(int i = 0; i < own.accessories.Count; i++) {
                    if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                        result += (int)own.accessories[i].item.GetSocketOfType(BonusType.mAtk) + own.accessories[i].item.mAtk;
                }
                result += activePet != null ? activePet.m_atk : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].m_atk / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].m_atk / 2 : own.mounts[i].m_atk;
                }
                result += classType == DamageType.Magical ? ScriptableMilitaryRank.dict[own.militaryRank].atk : 0;
                if(classType == DamageType.Magical && own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.mAtk.active : title.mAtk.notActive;
                }
                result += InGuild() ? ScriptableGuildSkill.dict[2].Get(own.guildSkills[2]) * Storage.data.AP_Intelligence_ATK : 0;
                return base.m_atk + result;
            }
        }
        #endregion
        #region Defense
        public override int p_def {
            get {
                int result = _p_def.Get(level) + own.endurance * Storage.data.AP_Endurance + own.strength * Storage.data.AP_Strength_DEF
                            + classInfo.pDef;
                for(int i = 0; i < equipment.Count; i++) {
                    if(equipment[i].amount > 0 && equipment[i].item.data != null)
                        result += (int)equipment[i].item.GetSocketOfType(BonusType.pDef) + equipment[i].item.pDef;
                }
                for(int i = 0; i < own.accessories.Count; i++) {
                    if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                        result += (int)own.accessories[i].item.GetSocketOfType(BonusType.pDef) + own.accessories[i].item.pDef;
                }
                result += activePet != null ? activePet.p_def : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].p_def / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].p_def / 2 : own.mounts[i].p_def;
                }
                result += classType == DamageType.Physical ? ScriptableMilitaryRank.dict[own.militaryRank].def : 0;
                if(classType == DamageType.Physical && own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.pDef.active : title.pDef.notActive;
                }
                result += InGuild() ? ScriptableGuildSkill.dict[1].Get(own.guildSkills[1]) * Storage.data.AP_Strength_DEF +
                            ScriptableGuildSkill.dict[3].Get(own.guildSkills[3]) * Storage.data.AP_Endurance : 0;
                return base.p_def + result;
            }
        }
        public override int m_def {
            get {
                int result = _m_def.Get(level) + own.endurance * Storage.data.AP_Endurance + own.intelligence * Storage.data.AP_Intelligence_DEF
                            + classInfo.mDef;
                for(int i = 0; i < equipment.Count; i++) {
                    if(equipment[i].amount > 0 && equipment[i].item.data != null)
                        result += (int)equipment[i].item.GetSocketOfType(BonusType.mDef) + equipment[i].item.mDef;
                }
                for(int i = 0; i < own.accessories.Count; i++) {
                    if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                        result += (int)own.accessories[i].item.GetSocketOfType(BonusType.mDef) + own.accessories[i].item.mDef;
                }
                result += activePet != null ? activePet.m_def : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].m_def / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].m_def / 2 : own.mounts[i].m_def;
                }
                result += classType == DamageType.Magical ? ScriptableMilitaryRank.dict[own.militaryRank].def : 0;
                if(classType == DamageType.Magical && own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.mDef.active : title.mDef.notActive;
                }
                result += InGuild() ? ScriptableGuildSkill.dict[2].Get(own.guildSkills[2]) * Storage.data.AP_Intelligence_DEF +
                            ScriptableGuildSkill.dict[3].Get(own.guildSkills[3]) * Storage.data.AP_Endurance : 0;
                return base.m_def + result;
            }
        }
        #endregion
        #region Block
        public override float blockChance {
            get {
                float result = _blockChance.Get(level) + classInfo.block;
                for(int i = 0; i < equipment.Count; i++) {
                    if(equipment[i].amount > 0 && equipment[i].item.data != null)
                        result += equipment[i].item.GetSocketOfType(BonusType.block) + equipment[i].item.block;
                }
                for(int i = 0; i < own.accessories.Count; i++) {
                    if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                        result += own.accessories[i].item.GetSocketOfType(BonusType.block) + own.accessories[i].item.block;
                }
                result += activePet != null ? activePet.blockChance : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].block / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].blockChance / 2 : own.mounts[i].blockChance;
                }
                result= own.militaryRank > -1 ? ScriptableMilitaryRank.dict[own.militaryRank].block : 0;
                if(own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.block.active : title.block.notActive;
                }
                result += InGuild() ? (float)ScriptableGuildSkill.dict[5].Get(own.guildSkills[5]) * .1f : 0;
                return base.blockChance + result;
            }
        }
        public override float untiBlockChance {
            get {
                float result = _untiBlockChance.Get(level) + classInfo.untiBlock;
                result += activePet != null ? activePet.untiBlockChance : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].antiBlock / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].untiBlockChance / 2 : own.mounts[i].untiBlockChance;
                }
                result += own.militaryRank > -1 ? ScriptableMilitaryRank.dict[own.militaryRank].untiBlock : 0;
                if(own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.antiBlock.active : title.antiBlock.notActive;
                }
                return base.untiBlockChance + result;
            }
        }
        #endregion
        #region Critical
        public override float critRate {
            get {
                float result = _critRate.Get(level) + classInfo.crit;
                for(int i = 0; i < equipment.Count; i++) {
                    if(equipment[i].amount > 0 && equipment[i].item.data != null)
                        result += equipment[i].item.GetSocketOfType(BonusType.crit) + equipment[i].item.critRate;
                }
                for(int i = 0; i < own.accessories.Count; i++) {
                    if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                        result += own.accessories[i].item.GetSocketOfType(BonusType.crit) + own.accessories[i].item.critRate;
                }
                result += activePet != null ? activePet.critRate : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].critRate / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].criticalChance / 2 : own.mounts[i].criticalChance;
                }
                result += own.militaryRank > -1 ? ScriptableMilitaryRank.dict[own.militaryRank].crit : 0;
                if(own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.critRate.active : title.critRate.notActive;
                }
                result += InGuild() ? (float)ScriptableGuildSkill.dict[4].Get(own.guildSkills[4]) * .1f : 0;
                return base.critRate + result;
            }
        }
        public override float critDmg {
            get {
                float result = _critDmg.Get(level) + classInfo.critDmg;
                result += activePet != null ? activePet.critDmg : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].critDmg / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].criticalRate / 2 : own.mounts[i].criticalRate;
                }
                result += own.militaryRank > -1 ? ScriptableMilitaryRank.dict[own.militaryRank].critDmg : 0;
                if(own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.critDmg.active : title.critDmg.notActive;
                }
                return base.critDmg + result;
            }
        }
        public override float antiCrit {
            get {
                float result = _antiCrit.Get(level) + classInfo.untiCrit;
                result += activePet != null ? activePet.antiCrit : 0;
                if(own.pets.Count > 0) {
                    for(int i = 0; i < own.pets.Count; i++) {
                        if(own.pets[i].status == SummonableStatus.Saved)
                            result += own.pets[i].antiCrit / 2;
                    }
                }
                if(own.mounts.Count > 0) {
                    for(int i = 0; i < own.mounts.Count; i++)
                        result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].untiCriticalChance / 2 : own.mounts[i].untiCriticalChance;
                }
                result += own.militaryRank > -1 ? ScriptableMilitaryRank.dict[own.militaryRank].untiCrit : 0;
                if(own.titles.Count > 0) {
                    for(int i = 0; i < own.titles.Count; i++)
                        if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                            result += own.titles[i] == activeTitle ? title.antiCrit.active : title.antiCrit.notActive;
                }
                return base.antiCrit + result;
            }
        }
        #endregion
        #region Helpers
        public override uint battlepower => Convert.ToUInt32(healthMax + manaMax + m_atk + p_atk + m_def + p_def + 
        (blockChance + untiBlockChance + critRate + critDmg + antiCrit + untiStunChance) * 100);
        #endregion
    #endregion //Attributes
    #region Basic Functions and Helpers
        void CheckForNewFeatureOpened() {
            /*if(level >= 3) {
                GetNewDailyQuest();
            }*/
        }
        sbyte NextSkill() {
            for (sbyte i = 1; i < skills.Count; i++) {
                if(CastCheckSelf(skills[i])) return i;
            }
            return 0;
        }
        void UseNextTargetIfAny() {
            // use next target if the user tried to target another while casting (target is locked while casting so skill
            // isn't applied to an invalid target accidentally)
            if (nextTarget != null) {
                target = nextTarget;
                nextTarget = null;
            }
        }
        public bool IsFighting() => NetworkTime.time - lastCombatTime < 5d;
        public bool InEvent() => own.occupation == PlayerOccupation.InMatchArena1v1;
        public bool IsOccupied() {
            if(own.occupation != PlayerOccupation.None) {
                if(own.occupation == PlayerOccupation.RegisteredArena1v1) {
                    Notify("Already registered in Arena, please wait for a match or unregister first", "انت مسجل بالحلبة بالفعل, برجاء انتظار مباراة او الغاء التسجيل اولا");
                    return true;
                }
            }
            return false;
        }
    #endregion //Basic Functions
    #region General Server Functions
        protected override void Start() {
            // do nothing if not spawned (=for character selection previews)
            if (!isServer && !isClient) return;

            base.Start();
            onlinePlayers[id] = this;
            growthEquips = new List<int>();
            //for(int i = 0; i < buffs.Count; ++i)
                //if(buffs[i].BuffTimeRemaining() > 0)
                    //buffs[i].data.SpawnEffect(this, this);

            SetGuildOnline(0);
            UIManager.data.homePage.UpdateOnlineCount();
        }
        void Update() {
            state = UpdateServer();
        }
        public override void OnStartServer() {
            base.OnStartServer();
            if(own.pets.Count > 0) {
                for(int i = 0; i < own.pets.Count; i++) {
                    if(activePet != null) break;
                    if(own.pets[i].status == SummonableStatus.Deployed) {
                        PetSystem.Summon(this, own.pets[i].id);
                        break;
                    }
                }
            }
            if(InGuild())
                StartUpdatePlayerBRInGuildInfo();
        }
        public void NextAction(double nextAction = 1) => own.nextRiskyActionTime += nextAction;
        public bool CanTakeAction() {
            if(NetworkTime.time >= own.nextRiskyActionTime)
                return true;
            NotifyRiskyActionTime();
            return false;
        }
        void OnDestroy() {
            if (onlinePlayers.TryGetValue(id, out Player entry) && entry == this)
                onlinePlayers.Remove(id);
            
            if (!isServer && !isClient)
                return;
            if(activePet != null)
                NetworkServer.Destroy(activePet.gameObject);
            
            if(InGuild())
                StopUpdatePlayerBRInGuildInfo();

            UIManager.data.homePage.UpdateOnlineCount();
        }
        public IEnumerator<WaitForSeconds> CheckUps(double lastSaved) {
            yield return new WaitForSeconds(5);

            Database.singleton.LoadFriends(this);
            Database.singleton.LoadMailBox(this);
            //Database.singleton.LoadAchievements(this);
            CheckGifts(lastSaved);
            if(stillIn7Days) Database.singleton.Load7Days(this);
            if(PreviewSystem.cashe.ContainsKey(id)) PreviewSystem.cashe.Remove(id);
        }
        void CheckGifts(double lastSaved) {
            if(lastSaved == 0) {// new player mail
                MailSystem.Send(Storage.data.newPlayerMail[(int)currentLang], id);
            }
        }
        public static uint BalanceExpReward(uint reward, int attackerLevel, int victimLevel, int maxLevelDifference = 20) {
            // level difference 10 means 10% extra/less per level.
            // level difference 20 means 5% extra/less per level.
            // so the percentage step depends on the level difference:
            float percentagePerLevel = 1f / maxLevelDifference;
            // calculate level difference. it should cap out at +- maxDifference to avoid power level exploits where a level 1
            // player kills a level 100 monster and immediately gets millions of experience points and levels up to level 50
            // instantly. this would be bad for MMOs. instead, we only consider +- maxDifference.
            int levelDiff = Mathf.Clamp(victimLevel - attackerLevel, -maxLevelDifference, maxLevelDifference);
            // calculate the multiplier. it will be +10%, +20% etc. when killing higher level monsters. it will be -10%, -20%
            // etc. when killing lower level monsters.
            float multiplier = 1 + levelDiff * percentagePerLevel;
            // calculate reward
            return Convert.ToUInt32(reward * multiplier);
        }
        public override void Warp(Vector3 destination)
        {
            agent.Warp(destination);
            rubberbanding.RpcWarp(destination);
        }
        public override void ResetMovement()
        {
            agent.ResetMovement();
            rubberbanding.ResetMovement();
        }
        
    #endregion //Server Functions
    #region State-Machine Events
        // checks
        bool EventDied() => health == 0;
        bool EventTargetDisappeared() => target == null;
        bool EventTargetDied() => target != null && target.health == 0;
        bool EventSkillRequest() => currentSkill > -1 && currentSkill < skills.Count;
        bool EventSkillFinished() => currentSkill > -1 && currentSkill < skills.Count && skills[currentSkill].CastTimeRemaining() == 0;
        bool EventMoveStart() => state != EntityState.Moving && IsMoving(); // only fire when started moving
        bool EventMoveEnd() => state == EntityState.Moving && !IsMoving(); // only fire when stopped moving
        bool EventStunned() => NetworkTime.time <= stunTimeEnd;
        // states
        protected override EntityState UpdateServer() {
            if(state == EntityState.Idle)     return UpdateServer_IDLE();
            if(state == EntityState.Moving)   return UpdateServer_MOVING();
            if(state == EntityState.Casting)  return UpdateServer_CASTING();
            if(state == EntityState.Stunned)  return UpdateServer_STUNNED();
            if(state == EntityState.Dead)     return UpdateServer_DEAD();
            Log($"[UpdateServer] Invalid State: {state}");
            return EntityState.Idle;
        }
        [Server] EntityState UpdateServer_IDLE() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventDied()) {
                // we died.
                OnDeath();
                return EntityState.Dead;
            }
            if (EventStunned()) {
                ResetMovement();
                return EntityState.Stunned;
            }
            if (EventCancelAction()) {
                // the only thing that we can cancel is the target
                target = null;
                return EntityState.Idle;
            }
            if (EventMoveStart()) {
                // cancel casting (if any)
                CancelCastSkill();
                return EntityState.Moving;
            }
            if (EventSkillRequest()) {
                // don't cast while mounted (no MOUNTED state because we'd need MOUNTED_STUNNED, etc. too)
                Skill skill = skills[currentSkill];
                nextTarget = target; // return to this one after any corrections by CastCheckTarget
                Vector3 destination;
                if (CastCheckSelf(skill) /*&& CastCheckTarget(skill)*/ && CastCheckDistance(skill, out destination)) {
                    // start casting and cancel movement in any case (player might move into attack range * 0.8 but as
                    // soon as we are close enough to cast, we fully commit to the cast.)
                    if(IsMounted()) MountUnsummon();
                    ResetMovement();
                    StartCastSkill(skill);
                    return EntityState.Casting;
                } else {
                    // checks failed. reset the attempted current skill.
                    currentSkill = -1;
                    nextTarget = null; // nevermind, clear again (otherwise it's shown in UITarget)
                    return EntityState.Idle;
                }
            }
            return EntityState.Idle; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_MOVING() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if (EventDied()) {
                OnDeath();// we died.
                return EntityState.Dead;
            }
            if (EventStunned()) {
                ResetMovement();
                return EntityState.Stunned;
            }
            if (EventMoveEnd()) {
                return EntityState.Idle;// finished moving. do whatever we did before.
            }
            if (EventCancelAction()) {
                CancelCastSkill(); // cancel casting (if any) and stop moving
                //ResetMovement(); <- done locally. doing it here would reset localplayer to the slightly behind server position otherwise
                return EntityState.Idle;
            }
            // SPECIAL CASE: Skill Request while doing rubberband movement
            // -> we don't really need to react to it
            // -> we could just wait for move to end, then react to request in IDLE
            // -> BUT player position on server always lags behind in rubberband movement
            // -> SO there would be a noticeable delay before we start to cast
            //
            // SOLUTION:
            // -> start casting as soon as we are in range
            // -> BUT don't ResetMovement. instead let it slide to the final position while already starting to cast
            // -> NavMeshAgentRubberbanding won't accept new positions while casting anyway, so this is fine
            if (EventSkillRequest()) {
                Vector3 destination;
                Skill skill = skills[currentSkill];
                if (CastCheckSelf(skill) && CastCheckTarget(skill) && CastCheckDistance(skill, out destination))
                {
                    //Debug.Log("MOVING->EventSkillRequest: early cast started while sliding to destination...");
                    // ResetMovement(); <- DO NOT DO THIS.
                    if(IsMounted())
                        MountUnsummon();
                    StartCastSkill(skill);
                    return EntityState.Casting;
                }
            }
            return EntityState.Moving; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_CASTING() {
            // keep looking at the target for server & clients (only Y rotation)
            if(target) LookAtY(target.transform.position);
            // events sorted by priority (e.g. target doesn't matter if we died)
            // IMPORTANT: nextTarget might have been set while casting, so make sure to handle it in any case here. it should
            // definitely be null again after casting was finished.
            // => this way we can reliably display nextTarget on the client if it's != null, so that UITarget always shows
            //    nextTarget>target (this just feels better)
            if(EventDied()) {
                // we died.
                OnDeath();
                UseNextTargetIfAny(); // if user selected a new target while casting
                return EntityState.Dead;
            }
            if(EventStunned()) {
                // cancel cast & movement (only clear current skill if we don't continue cast after stunned)
                CancelCastSkill(!continueCastAfterStunned);
                ResetMovement();
                return EntityState.Stunned;
            }
            if(EventMoveStart()) {
                // we do NOT cancel the cast if the player moved, and here is why:
                // * local player might move into cast range and then try to cast.
                // * server then receives the Cmd, goes to CASTING state, then
                //   receives one of the last movement updates from the local player
                //   which would cause EventMoveStart and cancel the cast.
                // * this is the price for rubberband movement.
                // => if the player wants to cast and got close enough, then we have
                //    to fully commit to it. there is no more way out except via
                //    cancel action. any movement in here is to be rejected.
                //    (many popular MMOs have the same behaviour too)
                //

                // we do NOT reset movement either. allow sliding to final position.
                // (NavMeshAgentRubberbanding doesn't accept new ones while CASTING)
                //ResetMovement(); <- DO NOT DO THIS

                // we do NOT return "CASTING". EventMoveStart would constantly fire
                // while moving for skills that allow movement. hence we would
                // always return "CASTING" here and never get to the castfinished
                // code below.
                //return "CASTING";
            }
            if (EventCancelAction()) {
                // cancel casting
                CancelCastSkill();
                UseNextTargetIfAny(); // if user selected a new target while casting
                return EntityState.Idle;
            }
            if (EventTargetDisappeared()) {
                // cancel if the target matters for this skill
                if (skills[currentSkill].cancelCastIfTargetDied) {
                    CancelCastSkill();
                    UseNextTargetIfAny(); // if user selected a new target while casting
                    return EntityState.Idle;
                }
            }
            if (EventTargetDied()) {
                // cancel if the target matters for this skill
                if (skills[currentSkill].cancelCastIfTargetDied) {
                    CancelCastSkill();
                    UseNextTargetIfAny(); // if user selected a new target while casting
                    return EntityState.Idle;
                }
            }
            if (EventSkillFinished()) {
                //Debug.Log("Skill called finished, cast time: " + skills[currentSkill].CastTimeRemaining());
                // apply the skill after casting is finished
                // note: we don't check the distance again. it's more fun if players
                //       still cast the skill if the target ran a few steps away
                Skill skill = skills[currentSkill];
                //animator.enabled = false;
                // apply the skill on the target
                FinishCastSkill(skill);

                // clear current skill for now
                currentSkill = -1;
                // use next target if the user tried to target another while casting
                UseNextTargetIfAny();

                // go back to IDLE
                return EntityState.Idle;
            }
            if (EventMoveEnd()) {} // don't care
            if (EventSkillRequest()) {} // don't care
            return EntityState.Casting; // nothing interesting happened
        }
        [Server] EntityState UpdateServer_STUNNED() {
            // events sorted by priority (e.g. target doesn't matter if we died)
            if(EventDied()) {
                // we died.
                OnDeath();
                return EntityState.Dead;
            }
            if(EventStunned()) {
                return EntityState.Stunned;
            }
            // go back to idle if we aren't stunned anymore and process all new events there too
            return EntityState.Idle;
        }
        [Server] EntityState UpdateServer_DEAD() {
            ActionRespawn();
            if (EventMoveStart()) {
                // this should never happen, rubberband should prevent from moving while dead.
                // TODO: Kick player out of game
                Debug.LogWarning("Player " + name + " moved while dead. This should not happen.");
                return EntityState.Dead;
            }

            return EntityState.Dead; // nothing interesting happened
        }
    #endregion
    #region Modifiers
        [Server] public void ChangeName(string newName) {
            name = newName;
        }
        [Server] public void AddExp(uint amount, bool sharable = true) {
            amount = Math.Max(amount, 0);
            if(amount < 1)
                return;
            if(sharable && own.shareExpWithPet && activePet != null) {
                if(activePet.Feed((uint)Math.Floor(amount * Storage.data.pet.expShare))) {
                    own.experience += (uint)Math.Floor(amount * (1f - Storage.data.pet.expShare));
                }
            }
            else own.experience += amount;
        }
        [Server] public void AddLevel() {
            level++;
            own.freepoints += Storage.data.player.pointsPerLevel;
            CheckForNewFeatureOpened();
            TargetOnLevelUp();
            // updates
            if(InTeam())
                TeamSystem.SetMemberLevel(this);
            if(InGuild())
                GuildSystem.SetMemberLevel(guild.id, id, (byte)level);
            // check achievements
            if(inprogressAchievements.playerLevel != null && inprogressAchievements.playerLevel.IsFulfilled(this))
                inprogressAchievements.playerLevel.OnAchieved(this);
        }
        [Server] public void AddSkillExp(ushort skillId, uint exp) {
            int index = skills.IndexOf(skillId);
            if(index < 0) {
                Log($"[AddSkillExp] invalid skill id={skillId}");
                return;
            }
            Skill usedSkill = skills[index];
            usedSkill.AddExp(exp);
            skills[index] = usedSkill;
            // achievements
        }
        [Server] public void AddGold(uint amount) {
            if(amount < 1) return;
            own.gold += Math.Max(amount, 0);
            //HotEventsSystem.OnCurrency(this, HotEventTypes.GatherGold, amount);
            ArchiveGainedGold(amount);
        }
        [Server] public void UseGold(uint amount) {
            if(amount < 1) return;
            own.gold -= Math.Max(amount, 0);
            own.archive.usedGold += amount;
            //HotEventsSystem.OnCurrency(this, HotEventTypes.GatherGold, amount);
            ArchiveUsedGold(amount);
        }
        [Server] public void AddDiamonds(uint amount) {
            if(amount < 1) return;
            own.diamonds += amount;
            //HotEventsSystem.OnCurrency(this, HotEventTypes.GatherDiamonds, amount);
            own.archive.gainedDiamonds += amount;
        }
        [Server] public void UseDiamonds(uint amount) {
            if(amount < 1) return;
            own.diamonds -= amount;
            own.archive.usedDiamonds += amount;
        }
        [Server] public void AddBDiamonds(uint amount) {
            if(amount < 1) return;
            own.b_diamonds += amount;
            //HotEventsSystem.OnCurrency(this, HotEventTypes.GatherDiamonds, amount);
            own.archive.gainedBDiamonds += amount;
        }
        [Server] public void UseBDiamonds(uint amount) {
            if(amount < 1) return;
            own.b_diamonds -= amount;
            own.archive.usedBDiamonds += amount;
            //HotEventsSystem.OnCurrency(this, HotEventTypes.GatherDiamonds, amount);
        }
        [Server] public bool AddHonor(uint amount, bool addRemains = false) {
            if(amount < 1) return false;
            if((own.TodayHonor + amount) <= MaxHonorPerDay) {
                own.TodayHonor += (ushort)amount;
                own.TotalHonor += amount;
                //HotEventsSystem.OnCurrency(this, HotEventTypes.GatherHonor, amount);
                return true;
            }
            else if(addRemains && own.TodayHonor < MaxHonorPerDay) {
                ushort remains = (ushort)Math.Max(MaxHonorPerDay - own.TodayHonor, 0);
                own.TotalHonor += remains;
                own.TodayHonor = MaxHonorPerDay;
                //HotEventsSystem.OnCurrency(this, HotEventTypes.GatherHonor, remains);
                Notify("Reached max honor for today", "وصلت للحد الاقصي من نقاط الشرف لليوم");
                return true;
            }
            return false;
        }
        [Server] public void AddPopularity(uint amount) {
            if(amount < 1) return;
            own.popularity += amount;
            if(inprogressAchievements.popularity != null && inprogressAchievements.popularity.IsFulfilled(this))
                inprogressAchievements.popularity.OnAchieved(this);
        }
        [Server] public bool AddGuildContribution(uint amount) {
            if(amount < 1) 
                return false;
            if(InGuild()) {
                own.guildContribution += amount;
                //if(inprogressAchievements.addGConts != null && inprogressAchievements.addGConts.IsFulfilled(this))
                //    inprogressAchievements.addGConts.OnAchieved(this);
                return true;
            }
            return false;
        }
        [Server] public bool UseGuildContribution(uint amount) {
            if(amount < 1) 
                return false;
            if(InGuild()) {
                own.guildContribution -= amount;
                //if(inprogressAchievements.useGConts != null && inprogressAchievements.useGConts.IsFulfilled(this))
                //    inprogressAchievements.useGConts.OnAchieved(this);
                return true;
            }
            return false;
        }
        [Server] public void AddArena1v1Win() {
            own.arena1v1WinsToday++;
            own.archive.arena1v1Wins++;
            own.arena1v1Points += Storage.data.arena.pointsOnWin;
            if(own.arena1v1Points > own.archive.highestArena1v1Points) {
                own.archive.highestArena1v1Points = own.arena1v1Points;
                // TODO: check achievement
            }
        }
        [Server] public void AddArena1v1Loss() {
            own.arena1v1LossesToday++;
            own.archive.arena1v1Losses++;
            own.arena1v1Points = (ushort)Math.Max(own.arena1v1Points - Storage.data.arena.pointsOnLoss, 0);
        }
    #endregion
    #region Combat
        [Command] public void CmdSetTarget(NetworkIdentity ni) {
            if (ni != null) {
                // can directly change it, or change it after casting?
                if (state == EntityState.Idle || state == EntityState.Moving || state == EntityState.Stunned)
                    target = ni.GetComponent<Entity>();
                else if (state == EntityState.Casting)
                    nextTarget = ni.GetComponent<Entity>();
            }
        }
        public override bool CanAttack(Entity entity) => base.CanAttack(entity) && (entity is Pet && entity != activePet);
        [Server] public override int DealDamageAt(Entity entity, int amount, float stunChance=0, float stunTime=0) {
            // deal damage with the default function
            amount = base.DealDamageAt(entity, amount, stunChance, stunTime);
            int entityLevel = 1;

            if(entity is Player player){
                OnDamageDealtToPlayer(player, amount);
                entityLevel = player.level;
            }
            else if(entity is WorldBoss worldBoss){
                OnDamageDealtToWorldBoss(worldBoss, amount);
                entityLevel = worldBoss.level;
            }
            else if(entity is Monster monster){
                OnDamageDealtToMonster(monster);
                entityLevel = monster.level;
            }
            else if(entity is Meteor meteor) {
                OnDamageDealtToMeteor(meteor);
                entityLevel = meteor.level;
            }

            // let pet know that we attacked something
            if (activePet != null && activePet.autoAttack && activePet.target == null)
                activePet.OnAggro(entity);
            // add exp to skill
            Debug.Log(currentSkill);
            if(currentSkill > -1 && skills[currentSkill].CanLevelUp()) {
                AddSkillExp(skills[currentSkill].id, 
                BalanceExpReward((uint)amount, level, entityLevel, Storage.data.player.skillExpToLevelDiff));
            }
            if(entity.health == 0 && isWearingGrowthItem) {
                AddExpToRandomGrowthEquipment(amount, entityLevel);
            }

            // TODO: check amount of damage of a single hit to give achievement
            return amount;
        }
        [Server] public void OnDamageDealtToPlayer(Player enemy, int amount) {
            // died ?
            if(enemy.health == 0) {
                if(own.occupation == PlayerOccupation.InMatchArena1v1) OnKillPlayer_Arena1v1(enemy);
                else OnKillPlayer_Normal(enemy);// normal pvp
            }
            // still alive
            else {
                if(own.occupation == PlayerOccupation.InMatchArena1v1) ArenaSystem.AddDamage1v1(this, amount);
                // world boss and dungeon dmg counter
            }
        }
        [Server] public void OnDamageDealtToWorldBoss(WorldBoss boss, int amount) {
            // ranking
            /*List<WorldBossRank> list = ScheduledEventsHandler.singleton.WorldBossRankList;
            int index = list.FindIndex(p => p.id == id);
            if(boss.health == 0) {
                // last hit
                if(index > -1) {
                    WorldBossRank rank = list[index];
                    rank.damage += amount;
                    rank.lastHit = true;
                    ScheduledEventsHandler.singleton.WorldBossRankList[index] = rank;
                } else {
                    ScheduledEventsHandler.singleton.WorldBossRankList.Add(new WorldBossRank(id, name, classId, tribeId, amount, true));
                }
                //ScheduledEventsHandler.singleton.EndWorldBoss();
                own.MonsterPoints += 100;
            }
            else {
                if(index > -1) {
                    WorldBossRank rank = list[index];
                    rank.damage += amount; 
                    ScheduledEventsHandler.singleton.WorldBossRankList[index] = rank;
                } else {
                    ScheduledEventsHandler.singleton.WorldBossRankList.Add(new WorldBossRank(id, name, classId, tribeId, amount));
                }
            }
            AddGold((int)Mathf.Ceil((float)((amount / boss.healthMax) * boss.gold)));*/
        }
        [Server] public void OnDamageDealtToMonster(Monster monster) {
            if(monster.health == 0) {
                monster.OnKilled(this);
                // share kill rewards with party or only for self
                if(InTeam()) {
                    List<Player> members = GetTeamMembersInProximity();
                    for(int i = 0; i < members.Count; i++) {
                        members[i].AddExp((uint)Mathf.Floor(CalculateTeamExperienceShare(
                            monster.rewardExperience, members.Count, members[i].level, monster.level
                        ) * own.team.GetBonus(members[i].id)));
                        members[i].QuestsOnKilled(monster);
                    }
                }
                else {
                    AddExp(BalanceExpReward(monster.rewardExperience, level, monster.level));
                    own.MonsterPoints++;
                    QuestsOnKilled(monster);
                }
            }
            if(own.auto.on) CollectNearestLoots();
        }
        [Server] public void OnDamageDealtToMeteor(Meteor meteor) {
            if(meteor.health == 0) meteor.OnDestroyed(this);
            else meteor.CheckWaves();
        }
        [Server] void OnKillPlayer_Normal(Player enemy) {
            own.killStrike++;
            enemy.own.killStrike = 0;
            if(own.killStrike % 10 == 0) {
                ChatComponent.SendSystemMsg($"{name} Killed {own.killStrike} stright");
            }
            if(own.TodayHonor < MaxHonorPerDay) {
                uint incHonor = enemy.level < level ? 10u : 20u;
                AddHonor(incHonor);
            }
            // TODO: PK achievements check (killcount)
        }
        [Server] void OnKillPlayer_Arena1v1(Player enemy) {
            if(ArenaSystem.matches1v1.TryGetValue(occupationId, out Game.Arena.ArenaMatch1v1 match)) {
                if(!match.DeclareWinner(id)) {
                    Log($"[OnKillPlayer_Arena1v1] player isn't member of match p1={match.player1.id} p2={match.player2.id}");
                }
            } else {
                Log($"[OnKillPlayer_Arena1v1] invalid match id ocpId=" + occupationId);
                own.occupation = PlayerOccupation.None;
                occupationId = -1;
            }
        }
        [Server] protected override void OnDeath() {
            base.OnDeath();
            own.MonstersKillCount = 0;
            if(own.occupation != PlayerOccupation.InMatchArena1v1) {
                own.reviveTime = DateTime.Now.ToOADate();
                TargetShowRespawn(AvailableFreeRespawn);
            }
        }
    #endregion
    #region Skills
        [Command] public void CmdCancelAction() => cancelActionRequested = true;
        bool EventCancelAction() {
            bool result = cancelActionRequested;
            cancelActionRequested = false; // reset
            return result;
        }
        [Command] public void CmdUseSkill(sbyte skillIndex) {
            Debug.Log("Sent " + skillIndex);
            // validate
            if ((state == EntityState.Idle || state == EntityState.Moving || state == EntityState.Casting) &&
                0 <= skillIndex && skillIndex < skills.Count) {
                // skill learned and can be casted?
                if (skills[skillIndex].level > 0 && skills[skillIndex].IsReady()) {
                    currentSkill = skillIndex;
                }
            }
        }

    #endregion
    #region Class Promotion
        [Command] public void CmdStartPromotionQuest() {
            /*if(ScriptableClass.dict[classId].nextClass != null) {
                ScriptableClass nextClass = ScriptableClass.dict[classId].nextClass;
                if(HasActiveQuest(nextClass.startQuest.name)) {
                    Notify("Promotion quest has been accepted already", "تم قبول مهمة الترقية مسبقا");
                    return;
                }
                if(level < nextClass.reqLevel) {
                    Notify("Didn't reach the required Level yet", "لم تصل للمستوي المطلوب بعد");
                    return;
                }
                if(battlepower < nextClass.reqBR) {
                    Notify("Didn't reach the required BR yet", "لم تصل للقوة المطلوب بعد");
                    return;
                }
                if(own.militaryRank < nextClass.reqMilitaryRank) {
                    Notify("Didn't reach the required Military Rank yet", "لم تصل للرتبة العسكرية المطلوبة بعد");
                    return;
                }
                own.quests.Add(new Quest(nextClass.startQuest));
            }
            else Notify("Reched Max Promotions.", "وصلت لاعلي ترقية بالفعل.");*/
        }
        [Command] public void CmdPromoteClass() {
            /*if(ScriptableClass.dict[classId].nextClass != null) {
                ScriptableClass nextClass = ScriptableClass.dict[classId].nextClass;
                if(level < nextClass.reqLevel) {
                    Notify("Didn't reach the required Level yet", "لم تصل للمستوي المطلوب بعد");
                    return;
                }
                if(battlepower < nextClass.reqBR) {
                    Notify("Didn't reach the required BR yet", "لم تصل للقوة المطلوب بعد");
                    return;
                }
                if(own.militaryRank < nextClass.reqMilitaryRank) {
                    Notify("Didn't reach the required Military Rank yet", "لم تصل للرتبة العسكرية المطلوبة بعد");
                    return;
                }
                for(int i = 0; i < nextClass.UpgradeItems.Length; i++) {
                    if(InventoryCountById(nextClass.UpgradeItems[i].item.name) < nextClass.UpgradeItems[i].amount) 
                        Notify("Still missing required Items", "ما ذال ينقصك بعض المتطلبات");
                        return;
                }
                for(int i = 0; i < nextClass.UpgradeItems.Length; i++)
                    InventoryRemove(nextClass.UpgradeItems[i].item.name, nextClass.UpgradeItems[i].amount);
                classId++;
                Notify("You have been promoted", "لقد تم ترقيتك بنجاح");
            }
            else Notify("Reched Max Promotions", "وصلت لاعلي ترقية بالفعل");*/
        }
    #endregion
    #region Daily Sign & 7days
        //sign in
        [Command] public void CmdSignInToday() {
            byte today = (byte)DateTime.Now.Day;
            if(own.dsDays.Count > 0) {
                for(int i = 0; i < own.dsDays.Count; i++) {
                    if(own.dsDays[i] == today) {
                        Notify("Already signed in", "تم تسجيل الدخول مسبقا");
                        return;
                    }
                }
            }
            own.dsDays.Add(today);
        }
        [Command] public void CmdCollectDailySignReward(int index) {
            if(index < 0 || index >= Storage.data.dailySignRewards.Length) {
                Notify("Unknown reward has been chosen", "لم تختر جائزة بشكل صحيح");
                Log($"[CmdCollectDailySignReward] invalid index[{index}]");
                return;
            }
            if(own.dsRewards.Count > 0) {
                for(int i = 0; i < own.dsRewards.Count; i++) {
                    if(own.dsRewards[i] == index) {
                        Notify("Rewared already has been recieved", "تم استلام الجائزة بالفعل");
                        return;
                    }
                }
            }
            DailySignRewards rewardsInfo = Storage.data.dailySignRewards[index];
            if(own.dsDays.Count < rewardsInfo.daysCount) {
                Notify("Didn't signin the required days", "لم تسجل دخول بالعدد المطلوب");
                return;
            }
            InventoryAddOrMailItems(rewardsInfo.rewards);
            own.dsRewards.Add((byte)index);
        }
        //7days sign up
        [Command] public void CmdGet7DaysSignedDays() => TargetSet7DaysSignUp(signUp7Days);
        [TargetRpc] public void TargetSet7DaysSignUp(int[] signedList) {}
        [Command] public void CmdCollect7DaysSignUpReward(int day) {
            if(!stillIn7Days) {
                int daysDiff = DateTime.FromOADate(own.createdAt).Day - DateTime.Now.Day;
                Notify("The event period has ended", "فترة الحدث قد انتهت");
                if(daysDiff > 1)
                    Log($"[CmdCollect7DaysSignUpReward] out of period with {daysDiff}");
                return;
            }
            if(day < 1 || day > 7) {
                Notify("Please select a proper day", "برجاء اختيار يوم مناسب");
                Log($"[Collect7DaysSignUpReward] invalid day=({day})");
                return;
            }
            if(day > DateTime.Now.Day) {
                int days = day - DateTime.Now.Day;
                Notify($"Sign up {days} more days for reward", " سجل دخول عدد {days} ايام اخري للجائزة");
                return;
            }
            if(signUp7Days.Length > 0) {
                for(int i = 0; i < signUp7Days.Length; i++) {
                    if(signUp7Days[i] == day) {
                        Notify("Reward already recieved", "تم استلام الجائزة بالفعل");
                        return;
                    }
                }
            }
            InventoryAddOrMailItems(Storage.data.signup7daysRewards[day - 1].items);
            Array.Resize(ref signUp7Days, signUp7Days.Length + 1);
            signUp7Days[signUp7Days.Length - 1] = day;
            TargetSet7DaysSignUp(signUp7Days);
            Notify("Reward recieved", "تم استلام الجائزة");
        }
        //7days recharge
        [Command] public void CmdCollect7DaysRechargeReward(int day) {
            /*if(!own._7days.recharge.Contains(day) && DateTime.Now.Day >= DateTime.FromOADate(own.createdAt).AddDays(day).Day && own.vip.todayRecharge > 0) {
                ScriptableItemAndAmount[] rewards = new ScriptableItemAndAmount[0];
                for(int i = 0; i < Storage.data.Recharge7DaysEventsRewards.Length; i++) {
                    if(Storage.data.Recharge7DaysEventsRewards[i].day == day)
                        rewards = Storage.data.Recharge7DaysEventsRewards[i].rewards;
                }
                for(int i = 0; i < rewards.Length; i++) {
                    InventoryAddOrMail(new Item(rewards[i].item), rewards[i].amount);
                }
                Array.Resize(ref own._7days.recharge, own._7days.recharge.Length +1);
                own._7days.recharge[own._7days.recharge.Length -1] = day;
            }*/
        }
    #endregion
    #region Hot Events
        [Command] public void CmdClaimHotEventReward(int eventIndex, int objectiveIndex) {
            /*if(eventIndex > 0 && eventIndex < HotEventsSystem.events.Count) {
                HotEvent hotEvent = HotEventsSystem.events[eventIndex];
                if(hotEvent.id == own.HotEventsProgress[eventIndex].id) {
                    if(HotEventsSystem.IsFulfilled(this, eventIndex, objectiveIndex)) {
                        for(int i = 0; i < hotEvent.objectives[objectiveIndex].rewards.Count; i++) {// rewards
                            HotEventReward reward = hotEvent.objectives[objectiveIndex].rewards[i];
                            if(reward.type == "gold") {
                                AddGold(reward.amount);
                            } else if(reward.type == "diamonds") {
                                AddDiamonds(reward.amount);
                            } else if(reward.type == "b.diamonds") {
                                own.b_diamonds += reward.amount;
                            } else if(reward.type == "honor") {
                                AddHonor((int)reward.amount);
                            } else {
                                if(ScriptableItem.dict.TryGetValue(Convert.ToInt32(reward.type), out ScriptableItem itemData))
                                    InventoryAddOrMail(new Item(itemData), reward.amount);
                            }
                        }
                        HotEventProgress progress = own.HotEventsProgress[eventIndex];
                        progress.completeTimes[objectiveIndex]++;
                        own.HotEventsProgress[eventIndex] = progress;
                        return;
                    }
                }
            }
            TargetNotify("Hot Event Not Found");*/
        }
        [Server] public void HotEventsOnCraft(string itemName, int amount) {
            /*if(itemName != "" && amount > 0) {
                HotEventsSystem.OnCrafted(this, itemName, amount);
            }*/
        }
    #endregion
    #region Tribe
        [Command] public void CmdDonateToTribe(uint goldAmount, uint diamondsAmount) {
            if(own.gold >= goldAmount && own.diamonds >= diamondsAmount) {
                TribeSystem.Donate(tribeId, goldAmount, diamondsAmount);
                UseGold(goldAmount);
                own.diamonds -= diamondsAmount;
                return;
            }
            TargetNotify("Please, Try again.");
        }
    #endregion
    #region Guild
        public bool InGuild() => guild.id > 0;
        [Server] public void SetGuildOnline(double online) {
            if (InGuild())
                GuildSystem.SetGuildOnline(guild.id, id, online);
        }
        [Command] public void CmdGetAvailableGuildsToJoin() {
            if(level < Storage.data.guild.minJoinLevel)
                TargetNotify("You're already in a guild.");
            else if(!InGuild())
                TargetSetAvailableGuildsToJoin(GuildSystem.guildsJoinInfo[tribeId]);
            else
                TargetNotify("You're already in a guild.");
        }
        [Command] public void CmdCreateGuild(string guildName) => GuildSystem.CreateGuild(this, guildName);
        [Command] public void CmdSendJoinRequestToGuild(uint guildId) {
            if(!InGuild())
                GuildSystem.SendJoinRequest(this, guildId);
            else 
                TargetNotify("you are already in a guild.");
        }
        [Command] public void CmdSendGuildInvitationToTarget() {
            if(target is Player) 
                SendGuildInvitation(((Player)target).id);
            else TargetNotify("please select a player.");
        }
        [Command] public void CmdSendGuildInvitation(uint playerId) {
            SendGuildInvitation(playerId);
        }
        [Server] public void SendGuildInvitation(uint playerId) {
            if(!InGuild())
                TargetNotify("you're not in guild.");
                return;
            if(!guild.data.CanInvite(id, playerId))
                TargetNotify("you can't invite this player.");
                return;
            if(Player.onlinePlayers.TryGetValue(playerId, out Player player)) {
                if(player.InGuild())
                    TargetNotify("this player is Already in guild.");
                    return;
                player.AddGuildInvitation(new GuildInvitation {
                    id = guild.id,
                    name = guild.name,
                    level = guild.data.level,
                    senderId = id,
                    senderName = name
                });
                player.TargetShowGuildInvitationNotification();
                return;
            }
            TargetNotify("this player is offline.");
        }
        [Command] public void CmdTerminateGuild() {
            if (InGuild())// validate
                GuildSystem.TerminateGuild(guild.id, id);
        }
        [Command] public void CmdLeaveGuild() {
            if (InGuild())// validate
                GuildSystem.LeaveGuild(guild.id, id);
        }
        void AddGuildInvitation(GuildInvitation inv) {
            Array.Resize(ref guildInvitations, guildInvitations.Length + 1);
            guildInvitations[guildInvitations.Length - 1] = inv;
        }
        void RemoveGuildInvitation(int index) {
            if(guildInvitations.Length > 1) {
                guildInvitations[index] = guildInvitations[guildInvitations.Length - 1];
                Array.Resize(ref guildInvitations, guildInvitations.Length - 1);
            }
            else guildInvitations = new GuildInvitation[]{};
        }
        [Command] public void CmdGuildInviteAccept(int index) {
            if(InGuild())
                TargetNotify("you're already in a guild.");
                return;
                
            if(guildInvitations.Length > index)
                if(GuildSystem.AddToGuild(guildInvitations[index].id, guildInvitations[index].senderId, this))
                    RemoveGuildInvitation(index);
            else
                TargetNotify("invitation not found.");
                return;
        }
        [Command] public void CmdGuildInviteDecline(int index) {
            RemoveGuildInvitation(index);
        }
        [Command] public void CmdGuildDonateGold(uint amount) {
            if(InGuild()) {
                uint validAmount = amount - (uint)(amount % Storage.data.guild.goldToContribution);
                if(own.gold >= validAmount) {
                    UseGold(validAmount);
                    GuildSystem.IncreaceMemberContribution(this, (validAmount / Storage.data.guild.goldToContribution));
                }
            }
        }
        [Command] public void CmdGuildDonateDiamonds(uint amount) {
            if(InGuild()) {
                uint validAmount = amount - (uint)(amount % Storage.data.guild.diamondToContribution);
                if(own.diamonds >= validAmount) {
                    UseDiamonds(validAmount);
                    GuildSystem.IncreaceMemberContribution(this, (validAmount / Storage.data.guild.diamondToContribution));
                }
            }
        }
        [Command] public void CmdBuyItemsFromGuildShop(int index, uint count) {
            if (InGuild() && index < Storage.data.guildShop.Length && count > 0) {
                Item item = new Item(Storage.data.guildShop[index].item);
                uint totalCost = count * Storage.data.guildShop[index].cost;
                if (GuildSystem.GetMemberById(guild.id, id).contribution >= totalCost)
                    if (InventoryAdd(item, count)) 
                        GuildSystem.DecreaseMemberContribution(this, totalCost);
            }
        }
        [Command] public void CmdGuildKick(uint member) {
            if (InGuild())
                GuildSystem.KickFromGuild(guild.id, id, member);
        }
        [Command] public void CmdGuildPromote(uint member) {
            if (InGuild())
                GuildSystem.PromoteMember(guild.id, id, member);
        }
        [Command] public void CmdGuildDemote(uint member) {
            if (InGuild())
                GuildSystem.DemoteMember(guild.id, id, member);
        }
        [Command] public void CmdLearnGuildSkill(int index) {
            if(!InGuild()) {
                Notify("You're not in a Guild", "انت لست بتحالف");
                return;
            }
            GuildSystem.OnLearnedSkill(this, index);
        }
        [Command] public void CmdTransfarMasterRank(uint newMaster) {
            if(InGuild())
                GuildSystem.TransfarMastership(guild.id, id, newMaster);
                return;
            Notify("you're not in a guild.", "انت لست بتحالف");
        }
        [Server] public void StartUpdatePlayerBRInGuildInfo() => StartCoroutine(GuildSystem.UpdatePlayerBRInGuildInfo(id, guild.id));
        [Server] public void StopUpdatePlayerBRInGuildInfo() => StopCoroutine(GuildSystem.UpdatePlayerBRInGuildInfo(id, guild.id));
        [Command] public void CmdGetGuildMembersList() {
            if(InGuild())
                TargetSetGuildMembersListIntoWindow(GuildSystem.members[guild.id]);
            else TargetNotify("you are not in a guild.");
        }
        [Command] public void CmdAnswerGuildRecall(bool answer) {
            if(guildRecallRequest.id < 1 || !InGuild()) return;
            if(answer)
                TeleportTo(guildRecallRequest.city, guildRecallRequest.position);
            guildRecallRequest = GuildRecallRequest.Empty;
        }
        [Server] public void SetGuildRecallRequest(GuildRecallRequest request) {
            guildRecallRequest = request;
            TargetShowGuildRecall(request);
        }
        [Command] public void CmdGuildJoinRequests() {
            if(!InGuild()) {
                Notify("You're not in a guild", "لست مسجل بنقابة");
                return;
            }
            if(GuildSystem.joinRequests.TryGetValue(guild.id, out GuildJoinRequest[] requests)) {
                if(requests.Length > 0) {
                    for(int i = 0; i < requests.Length; i++) {
                        if(Player.onlinePlayers.TryGetValue(requests[i].id, out Player player)) {
                            requests[i].br = player.battlepower;
                            requests[i].level = (byte)player.level;
                        }
                    }
                    GuildSystem.joinRequests[guild.id] = requests;
                    TargetSetGuildJoinRequests(requests);
                    return;
                }
                Notify("");
            }
            else Notify("");
        }
        [Command] public void CmdAcceptGuildJoinRequest(uint requesterId, bool answer) {
            if(own.guildRank < Storage.data.guild.inviteMinRank) {
                Notify("You don't have permission to upgrade", "ليس لديك الصلاحية لتطوير");
                return;
            }
            if(guild.data.IsFull()) {
                Notify("The Guild is Full", "النقابة ممتلىء");
                return;
            }
                
            else {
                if(answer) GuildSystem.AcceptJoinRequest(guild.id, requesterId, this);
                else GuildSystem.RemoveJoinRequest(guild.id, requesterId);
                TargetSetGuildJoinRequests(GuildSystem.joinRequests[guild.id]);
            }
        }
        [Command] public void CmdGuildUpgradeHall() {
            if(!InGuild()) {
                Notify("You're not in a guild", "لست مسجل بنقابة");
                return;
            }
            if(own.guildRank < Storage.data.guild.promoteMinRank) {
                Notify("You don't have permission to upgrade", "ليس لديك الصلاحية لتطوير");
                return;
            }
            GuildSystem.UpgradeHall(this);
        }
        [Command] public void CmdSetGuildNotice(string notice) {
            if(!CanTakeAction())
                return;
            // validate (only allow changes every few seconds to avoid bandwidth issues)
            if (InGuild()) {
                GuildSystem.SetGuildNotice(guild.id, id, notice); // try to set notice
            }
            NextAction();
        }
        [TargetRpc] public void TargetSetAvailableGuildsToJoin(GuildJoinInfo[] data) {}
        [TargetRpc] public void TargetJoinedGuild() {}
        [TargetRpc] public void TargetShowGuildInvitationNotification() {}
        [TargetRpc] public void TargetSetGuildMembersListIntoWindow(GuildMember[] membersList) {}
        [TargetRpc] public void TargetShowGuildRecall(GuildRecallRequest request) {}
        [TargetRpc] public void TargetSetGuildJoinRequests(GuildJoinRequest[] data) {}
    #endregion
    #region Team
    public bool InTeam() => teamId > 0;
    public List<Player> GetTeamMembersInProximity() {
        List<Player> players = new List<Player>();
        foreach(NetworkConnection conn in netIdentity.observers.Values) {
            Player player = conn.identity.GetComponent<Player>();
            if(player != null && player.teamId == teamId)
                players.Add(player);
        }
        return players;
    }
    public static uint CalculateTeamExperienceShare(uint total, int memberCount, int memberLevel, int killedLevel) {
        // bonus percentage based on how many members there are
        float bonusPercentage = memberCount > 1 ? (memberCount-1) * Storage.data.team.bonusPerMemeber : 0;
        // calculate the share via ceil, so that uneven numbers still result in at least 'total' in the end. for example:
        //   4/2=2 (good)      5/2=2 (bad. 1 point got lost)      ceil(5/(float)2) = 3 (good!)
        uint share = (uint)Mathf.Ceil(total / (float)memberCount);

        // balance experience reward for the receiver's level. this is important
        // to avoid crazy power leveling where a level 1 hero would get a LOT of
        // level ups if his friend kills a level 100 monster once.
        uint balanced = BalanceExpReward(share, memberLevel, killedLevel);
        uint bonus = Convert.ToUInt32(balanced * bonusPercentage);

        return (uint)(balanced + bonus);
    }
    [Command] public void CmdFormTeam() {
        if(InTeam()) {
            Notify("You already in a team", "انت مسجل في فريق بالفعل");
            return;
        }
        TeamSystem.FormTeam(this);
    }
    [Command] public void CmdSendTeamInvitation(uint otherId) => SendTeamInvitation(otherId);
    [Server] void SendTeamInvitation(uint otherId) {
        if(!CanTakeAction()) 
            return;
        if(!Server.IsPlayerIdWithInServer(otherId) || id == otherId) {
            Notify("Invalid player ID", "رقم تعريفي غير صحيح");
            Log("[CmdTeamInvite] Invalid playerID: " + otherId);
            return;
        }
        if(!InTeam()) {
            TeamSystem.FormTeam(this);
            Notify("You've formed a team", "قمت بتشكيل فريق");
            //return;
        }
        if(own.team.IsFull) {
            Notify("Team is full", "الفريق مكتمل");
            return;
        }
        if(onlinePlayers.TryGetValue(otherId, out Player other)) {
            if(other.InTeam()) {
                Notify("Player already in a team", "اللاعب مسجل فى فريق بالفعل");
                return;
            }
            other.own.teamInvitations.Add(new TeamInvitation {
                id = own.team.id,
                senderId = id,
                senderName = name,
                senderLevel = (byte)level
            });
            NotifySentInvitation();
            NextAction(Storage.data.team.teamInviteWaitSeconds);
        }
        else NotifyPlayerOffline();
    }
    [Command] public void CmdSendTeamInvitationToTarget() {
        if(target is Player) SendTeamInvitation(((Player)target).id);
        else Notify("Invalid player ID", "رقم تعريفي غير صحيح");
    }
    [Command] public void CmdTeamInvitationAccept(int index) {
        if(InTeam()) {
            Notify("You're already in a team", "انت مسجل بفريق بالفعل");
            return;
        }
        if(own.teamInvitations.Count < 1) {
            Notify("No invitations found", "لا يوجد دعوات");
            return;
        }
        if(index < 0 || index >= own.teamInvitations.Count) {
            Notify("Please select an invitation", "برجاء اختيار دعوة");
            return;
        }
        if(TeamSystem.teams.TryGetValue(own.teamInvitations[index].id, out Team team)) {
            if(team.IsFull) {
                Notify("This team is full", "هذا الفريق ممتلىء");
                own.teamInvitations.RemoveAt(index);
                return;
            }
            TeamSystem.JoinTeam(own.teamInvitations[index].id, this);
            Notify("You've joined a team", "لقد اشتركت في فريق");
        }
        else {
            Notify("Team not found", "الفريق غير موجود");
            own.teamInvitations.RemoveAt(index);
        }
    }
    [Command] public void CmdTeamInvitationRefuse(int index) {
        if(own.teamInvitations.Count < 1) {
            Notify("No invitations found", "لا يوجد دعوات");
            return;
        }
        if(index < 0 || index >= own.teamInvitations.Count) {
            Notify("Please select an invitation", "برجاء اختيار دعوة");
            return;
        }
        own.teamInvitations.RemoveAt(index);
    }
    [Command] public void CmdTeamKickMember(uint memberId) {
        if(!InTeam()) {
            Notify("You're not in a team", "انت لست مسجل بفريق");
            return;
        }
        if(!Server.IsPlayerIdWithInServer(memberId)) {
            Notify("Invalid player ID", "رقم تعريفي غير صحيح");
            Log("[CmdTeamInvite] Invalid playerID: " + memberId);
            return;
        }
        TeamSystem.KickFromTeam(this, memberId);// try to kick. party system will do all the validation.
    }
    [Command] public void CmdLeaveTeam() {
        if(InTeam()) TeamSystem.LeaveTeam(teamId, id);
        else Notify("You're not in a team", "انت لست مسجل بفريق");
    }
    [Command] public void CmdDisbandTeam() {
        if(InTeam()) TeamSystem.DisbandTeam(this);
        else Notify("You're not in a team", "انت لست مسجل بفريق");
    }
    #endregion
    #region Workshop
        //plus
        [Command] public void CmdWorkshopPlus(int index, WorkshopOperationFrom from, int luckItem) => WorkshopSystem.Plus(this, index, from, luckItem);
        [TargetRpc] public void TargetItemEnhanceSuccess() {}
        [TargetRpc] public void TargetItemEnhanceFailure() {}
        //socket
        [Command] public void CmdWorkshopUnlockSocket(int index, WorkshopOperationFrom from, int socketIndex) => WorkshopSystem.UnlockSocket(this, index, from, socketIndex);
        [Command] public void CmdWorkshopRemoveGemFromSocket(int index, WorkshopOperationFrom from, int socketIndex) => WorkshopSystem.RemoveGem(this, index, from, socketIndex);
        [Command] public void CmdWorkshopAddGemInSocket(int index, WorkshopOperationFrom from, int socketIndex, int gemIndex) => WorkshopSystem.AddGem(this, index, from, socketIndex, gemIndex);
        // quality
        [Command] public void CmdWorkshopUpgradeItemQuality(int index, WorkshopOperationFrom from, int feedItem) => WorkshopSystem.QualityGrowth(this, index, from, feedItem);
        // craft
        [Command] public void CmdCraft(int recipeId, uint amount) => WorkshopSystem.Craft(this, recipeId, amount);
    #endregion
    #region Inventory
        bool InventoryOperationsAllowed() => state != EntityState.Dead && state != EntityState.Stunned;
        public int GetInventoryIndex(ushort itemId) {
            for (int i = 0; i < own.inventory.Count; ++i) {
                ItemSlot slot = own.inventory[i];
                if (slot.amount > 0 && slot.item.id == itemId)
                    return i;
            }
            return -1;
        }
        public int InventorySlotsFree() {
            int free = 0;
            foreach (ItemSlot slot in own.inventory)
                if (slot.amount == 0)
                    ++free;
            return free;
        }
        public int InventorySlotsOccupied() {
            int occupied = 0;
            foreach (ItemSlot slot in own.inventory)
                if (slot.amount > 0)
                    ++occupied;
            return occupied;
        }
        public uint InventoryCount(Item item) {
            uint amount = 0;
            foreach (ItemSlot slot in own.inventory)
                if (slot.amount > 0 && slot.item.Equals(item))
                    amount += (uint)slot.amount;
            return amount;
        }
        public bool InventoryRemove(int itemId, uint amount) {
            for (int i = 0; i < own.inventory.Count; ++i) {
                ItemSlot slot = own.inventory[i];
                if (slot.amount > 0 && slot.item.id == itemId) {
                    amount -= slot.DecreaseAmount(amount);
                    own.inventory[i] = slot;
                    if (amount == 0) return true;
                }
            }
            return false;
        }
        public bool InventoryCanAdd(Item item, int amount) {
            for(int i = 0; i < own.inventory.Count; i++) {
                if (own.inventory[i].isEmpty)
                    amount -= item.maxStack;
                else if (own.inventory[i].item.Equals(item))
                    amount -= (int)(own.inventory[i].item.maxStack - own.inventory[i].amount);
                if (amount <= 0)
                    return true;
            }
            return false;
        }
        public bool InventoryCanAddItems(ItemSlot[] Items) {
            foreach (var item in Items) {
                // go through each slot
                for (int i = 0; i < own.inventory.Count; ++i)
                {
                    int amount = (int)item.amount;
                    // empty? then subtract maxstack
                    if (own.inventory[i].amount == 0)
                        amount -= item.item.maxStack;
                    // not empty. same type too? then subtract free amount (max-amount)
                    // note: .Equals because name AND dynamic variables matter (petLevel etc.)
                    else if (own.inventory[i].item.Equals(item.item))
                        amount -= (int)(own.inventory[i].item.maxStack - own.inventory[i].amount);

                    // were we able to fit the whole amount already?
                    if (amount <= 0) return true;
                }
            }
            // if we got here than amount was never <= 0
            return false;
        }
        public bool InventoryAdd(Item item, uint amount, bool canMail = false) {
            int i;
            if(InventoryCanAdd(item, (int)amount)) {
                for(i = 0; i < own.inventory.Count; i++) {
                    if (own.inventory[i].amount > 0 && own.inventory[i].item.Equals(item)) {
                        ItemSlot temp = own.inventory[i];
                        amount -= temp.IncreaseAmount(amount);
                        own.inventory[i] = temp;
                    }
                    if(amount <= 0) return true;
                }
                for(i = 0; i < own.inventory.Count; i++) {
                    if (own.inventory[i].amount == 0) {
                        uint add = (uint)Mathf.Min(amount, item.maxStack);
                        own.inventory[i] = new ItemSlot(item, add);
                        amount -= add;
                    }
                    if(amount <= 0) return true;
                }
                if(amount != 0) {//send mail
                    SendFullBagItemMail(new MailItemSlot(item, amount));
                }
            } else if(canMail) {
                SendFullBagItemMail(new MailItemSlot(item, amount));
                return true;
            }
            Notify("Bag is full", "الحقيبة ممتلىء");
            return false;
        }
        void SendFullBagItemMail(MailItemSlot mailedItem) {
            Mail mail = new Mail();
            mail.subject = IsEn ? "Full bag!!" : "الحقيبة ممتلىء!!";
            mail.content = IsEn ? "Your bag is full please make space to collect the following items" : "الحقيبة ممتلىء, برجاء تفريغ اماكن لتحصيل المرفقات التالية";
            mail.AddItem(mailedItem);
            own.mailBox.Add(mail);
        }
        public bool InventoryAdd(ItemSlot itemSlot) => InventoryAdd(itemSlot.item, itemSlot.amount);
        public uint InventoryCountById(int itemId) {
            uint amount = 0;
            for(int i = 0; i < own.inventory.Count; i++) {
                if (own.inventory[i].amount > 0 && own.inventory[i].item.id == itemId)
                    amount += own.inventory[i].amount;
            }
            return amount;
        }
        public float GetItemCooldown(string cooldownCategory) {
            int hash = cooldownCategory.GetStableHashCode();// get stable hash to reduce bandwidth
            if(own.itemCooldowns.TryGetValue(hash, out double cooldownEnd)) { // find cooldown for that category
                return NetworkTime.time >= cooldownEnd ? 0 : (float)(cooldownEnd - NetworkTime.time);
            }
            return 0; // none found
        }
        public void SetItemCooldown(string cooldownCategory, float cooldown) {
            int hash = cooldownCategory.GetStableHashCode(); // get stable hash to reduce bandwidth
            own.itemCooldowns[hash] = NetworkTime.time + cooldown; // save end time
        }
        [Command] public void CmdUseInventoryItem(int index) {
            if (InventoryOperationsAllowed() && 0 <= index && index < own.inventory.Count && own.inventory[index].amount > 0 
                && own.inventory[index].item.data is UsableItem usable) {
                // note: we don't decrease amount / destroy in all cases because some items may swap to other slots in .Use()
                if (usable.CanUse(this, index)) {
                    // .Use might clear the slot, so we backup the Item first for the Rpc
                    Item item = own.inventory[index].item;
                    usable.Use(this, index);
                    RpcUsedItem(item);
                }
            }
        }
        [Server] public void InventoryAddOrMailItems(List<ItemSlot> items, string subject ="", string content = "") {
            if(items.Count < 1) return;
            List<ItemSlot> toMail = new List<ItemSlot>();
            for(int i = 0; i < items.Count; i++)
                if(!InventoryAdd(items[i])) toMail.Add(items[i]);
            if(toMail.Count > 0) {
                Mail mail = new Mail();
                mail.subject = subject;
                mail.content = content;
                mail.DefineItems(toMail);
                own.mailBox.Add(mail);
            }
        }
        [Server] public void InventoryAddOrMailItems(ItemSlot[] items, string subject ="", string content = "") {
            if(items.Length < 1) return;
            List<ItemSlot> toMail = new List<ItemSlot>();
            for(int i = 0; i < items.Length; i++)
                if(!InventoryAdd(items[i])) toMail.Add(items[i]);
            if(toMail.Count > 0) {
                Mail mail = new Mail();
                mail.subject = subject;
                mail.content = content;
                mail.DefineItems(toMail);
                own.mailBox.Add(mail);
            }
        }
        [Command] public void CmdSortInventory() {
            List<ItemSlot> SlotsNotEmpty = new List<ItemSlot>();
            for(int i = 0; i < own.inventorySize; i++) if(own.inventory[i].amount > 0) SlotsNotEmpty.Add(own.inventory[i]);
            SlotsNotEmpty.Sort(SortItems);
            for (int i = 0; i < SlotsNotEmpty.Count; i++) own.inventory[i] = SlotsNotEmpty[i];
            for (int i = SlotsNotEmpty.Count; i < own.inventorySize; i++) own.inventory[i] = new ItemSlot();
        }
        int SortItems(ItemSlot a, ItemSlot b) {
            if(a.item.id < b.item.id) return 1;
            if(a.item.id > b.item.id) return -1;
            return 0;
        }
        [Command] public void CmdOpenInventorySlots(int slotsCount) {
            int stoneIndex = GetInventoryIndex(5455);
            ItemSlot stones = new ItemSlot();
            if(stoneIndex > -1) stones = own.inventory[stoneIndex];
            else
                TargetNotify("You don't have any of the Materials required.");
                return;

            if(stones.amount < slotsCount)
                TargetNotify("You don't have enough Keys.");
                return;

            byte newInventorySize = (byte)(own.inventorySize + slotsCount);
            if(newInventorySize > Storage.data.player.maxInventorySize) { // if (newInventorySize) more that max slots return to max
                slotsCount -= newInventorySize - Storage.data.player.maxInventorySize;
                newInventorySize = Storage.data.player.maxInventorySize;
            }

            own.inventorySize = newInventorySize;
            for(int i = 0; i < slotsCount; i++) own.inventory.Add(new ItemSlot());
            stones.DecreaseAmount((uint)slotsCount);
            own.inventory[stoneIndex] = stones;
            TargetNotify($"${slotsCount} Slots has been Unlocked.");
        }
        [Command] public void CmdSellInventoryItemsForGold(int[] items) {
            if(items.Length > 0) {
                uint receivedGold = 0;
                for (int i = 0; i < items.Length; i++) {
                    if(own.inventory[items[i]].amount > 0)
                        receivedGold += (uint)own.inventory[items[i]].item.data.sellPrice * (uint)own.inventory[items[i]].amount;
                        own.inventory[items[i]] = new ItemSlot();
                }
                AddGold(receivedGold);
                TargetNotify($"you have sold ${items.Length} items for ${receivedGold} Gold.");
            }
            TargetNotify("No items to be sold.");
        }
        [Command] public void CmdBuyItemsFromInventoryShop(int index, uint count) {
            if(health < 1)
                TargetNotify("You have to be alive to buy from the shop.");
                return;

            if(index < 0 || index > Storage.data.inventoryShopItems.Length)
                TargetNotify("You have to select item from the shop.");
                return;

            if(count < 1)
                TargetNotify("You have to select at least 1 item.");
                return;

            Item item = Storage.data.inventoryShopItems[index];
            if (own.gold >= (count * item.data.buyPrice)) {
                if (InventoryAdd(item, count))
                    UseGold((uint)(count * item.data.buyPrice));
                    //TargetNotify($"You've bought [${item.data.name}]x${count}.");
                    return;
            }
            TargetNotify("You don't have enough Gold.");
        }
        [Server] public void SwapInventoryEquip(int inventoryIndex, int equipmentIndex) {
            // validate: make sure that the slots actually exist in the inventory and in the equipment
            if (InventoryOperationsAllowed() && 0 <= inventoryIndex && inventoryIndex < own.inventory.Count &&
                0 <= equipmentIndex && equipmentIndex < equipment.Count) {
                // item slot has to be empty (unequip) or equipable
                ItemSlot slot = own.inventory[inventoryIndex];
                if(slot.amount == 0 || slot.item.data is EquipmentItem itemData && itemData.CanEquip(this, inventoryIndex, equipmentIndex)) {
                    // swap them
                    ItemSlot temp = equipment[equipmentIndex];
                    equipment[equipmentIndex] = slot;
                    own.inventory[inventoryIndex] = temp;
                    UpdateEquipmentInfo();
                }
            }
        }
        [Command] public void CmdSwapInventoryEquip(int inventoryIndex, int equipmentIndex) {
            SwapInventoryEquip(inventoryIndex, equipmentIndex);
        }
        [ClientRpc] public void RpcUsedItem(Item item) {}
    #endregion
    #region Equipments
        [Server] public void UpdateEquipmentInfo() {
            // gems
            int gemLevel = 0;
            for(int i = 0; i < equipment.Count; i++) {
                if(!equipment[i].isEmpty) {
                    gemLevel += equipment[i].item.GetTotalGemLevels();
                }
            }
            for(int i = 0; i < own.accessories.Count; i++) {
                if(!own.accessories[i].isEmpty) {
                    gemLevel += own.accessories[i].item.GetTotalGemLevels();
                }
            }
            totalGemLevelBonus = ScriptableTotalGemLevels.GetBonus(gemLevel);
            // pluses
            int plusLevel = 0;
            for(int i = 0; i < equipment.Count; i++) {
                if(!equipment[i].isEmpty) {
                    plusLevel += equipment[i].item.plus;
                }
            }
            for(int i = 0; i < own.accessories.Count; i++) {
                if(!own.accessories[i].isEmpty) {
                    plusLevel += own.accessories[i].item.plus;
                }
            }
            totalPlusLevelBonus = ScriptableTotalPlusLevels.GetBonus(plusLevel);
            // Growth items
            growthEquips.Clear();
            for(int i = 0; i < equipment.Count; i++) {
                if(!equipment[i].isEmpty && equipment[i].item.data is EquipmentItem && equipment[i].item.quality.isGrowth) {
                    growthEquips.Add(i);
                }
            }
            for(int i = 0; i < own.accessories.Count; i++) {
                if(!own.accessories[i].isEmpty && own.accessories[i].item.data is EquipmentItem && own.accessories[i].item.quality.isGrowth) {
                    growthEquips.Add(equipment.Count + i);
                }
            }
        }
        [Server] public void ApplyDamageToEquipments(int dmg) {
            if(dmg > 1) {
                int randomSlot = Utils.random.Next(0, (equipment.Count + Storage.data.player.accessoriesCount) - 1);
                if(randomSlot < equipment.Count) {
                    if(!equipment[randomSlot].isEmpty)
                        equipment[randomSlot].item.ApplyDamage(dmg);
                }
                else {
                    randomSlot -= equipment.Count;
                    if(!own.accessories[randomSlot].isEmpty)
                        own.accessories[randomSlot].item.ApplyDamage(dmg);
                }
            }
        }
        [Server] public void AddExpToRandomGrowthEquipment(int dmg, int entityLevel) {
            ushort exp = (ushort)BalanceExpReward((uint)dmg, level, entityLevel, Storage.data.player.skillExpToLevelDiff);
            if(growthEquips.Count == 1) {
                if(growthEquips[0] < equipment.Count) {
                    ItemSlot slot = equipment[growthEquips[0]];
                    slot.item.quality.AddExp(exp);
                    equipment[growthEquips[0]] = slot;
                }
                else {
                    ItemSlot slot = own.accessories[growthEquips[0] - equipment.Count];
                    slot.item.quality.AddExp(exp);
                    own.accessories[growthEquips[0] - equipment.Count] = slot;
                }
            }
            else {
                int randIndex = Utils.random.Next(0, growthEquips.Count - 1);
                if(growthEquips[randIndex] < equipment.Count) {
                    ItemSlot slot = equipment[growthEquips[randIndex]];
                    slot.item.quality.AddExp(exp);
                    equipment[growthEquips[randIndex]] = slot;
                }
                else {
                    ItemSlot slot = own.accessories[growthEquips[randIndex] - equipment.Count];
                    slot.item.quality.AddExp(exp);
                    own.accessories[growthEquips[randIndex] - equipment.Count] = slot;
                }
            }
        }
    #endregion
    #region Wardrobe
        [Command] public void CmdWardrobeEquip(ushort wardrobeId) => WardrobeSystem.Equip(this, wardrobeId);
        [Command] public void CmdWardrobeUnEquip(int index) => WardrobeSystem.Unequip(this, index);
        [Command] public void CmdWardrobeSwitchVisibility() {
            if(!CanTakeAction())
                return;
            showWardrop = !showWardrop;
            NextAction(.5d);
        }
        [Command] public void CmdWardrobeSynthesize(int mainIndex, bool isEquiped, int otherIndex, int blessIndex) 
                                => WardrobeSystem.Synthesize(this, mainIndex, isEquiped, otherIndex, blessIndex);
    #endregion
    #region Mail
        [Command] public void CmdMarkAsSeen(int index) {
            if(own.mailBox.Count < 1) {
                TargetNotify("The mail box is empty.");
                return;
            }
            if(index < 0 || index > own.mailBox.Count) {
                TargetNotify("Please select a mail.");
                return;
            }
            Mail mail = own.mailBox[index];
            mail.opened = true;
            own.mailBox[index] = mail;
        }
        [Command] public void CmdCollectMail(int index) => CollectMail(index);
        [Server] public void CollectMail(int index) {
            if(own.mailBox.Count < 1) {
                Notify("The mail box is empty.");
                return;
            }
            if(index < 0 || index > own.mailBox.Count) {
                Notify("Please select a mail.");
                return;
            }
            if(own.mailBox[index].IsEmpty()) {
                Notify("This mail is already Empty.");
                return;
            }
            Mail mail = own.mailBox[index];
            if(!mail.currency.recieved) {
                AddGold(mail.currency.gold);
                own.diamonds += mail.currency.diamonds;
                own.b_diamonds += mail.currency.b_diamonds;
                mail.currency.recieved = true;
            }
            if(mail.items.Length > 0) {
                bool[] recievedItems = new bool[mail.items.Length];
                for(int i = 0; i < mail.items.Length; i++) {
                    if(mail.items[i].recieved) continue;
                    if(InventoryAdd(mail.items[i].item, mail.items[i].amount)){
                        MailItemSlot itemSlot = mail.items[i];
                        itemSlot.recieved = true;
                        mail.items[i] = itemSlot;
                        recievedItems[i] = true;
                    }
                }
                TargetUpdateMailItems(index, recievedItems);
            } 
            own.mailBox[index] = mail;
        }
        [TargetRpc] public void TargetUpdateMailItems(int index, bool[] recievedItems) {}
        [Command] public void CmdCollectAllMails() {
            if(own.mailBox.Count > 0)
                for(int i = 0; i < own.mailBox.Count; i++)
                    CollectMail(i);
            else TargetNotify("The mail box is empty.");
        }
        [Command] public void CmdDeleteMail(int index) => DeleteMail(index);
        [Server] public void DeleteMail(int index) {
            if(own.mailBox.Count < 1) {
                TargetNotify("The mail box is empty.");
                return;
            }
            if(index < 0 || index > own.mailBox.Count) {
                TargetNotify("Please select a mail.");
                return;
            }
            if(own.mailBox[index].id > 0)
                Database.singleton.DeleteMail(own.mailBox[index].id);
            own.mailBox.RemoveAt(index);
        }
        [Command] public void CmdDeleteAllMails() {
            if(own.mailBox.Count > 0) {
                Database.singleton.DeleteAllMails(own.mailBox);
                own.mailBox.Clear();
            }
            else TargetNotify("The mail box is empty.");
        }
    #endregion
    #region VIP
        [Server] public void AddVIPPoints(int points) {
            VIP vip = own.vip;
            vip.points += points;
            while(vip.points >= vip.data.nextLevelpoints) {
                vip.points -= vip.data.nextLevelpoints;
                vip.level++;
            }
            own.vip = vip;
        }
        [Command] public void CmdGetFirstReward(int level) {
            
        }
    #endregion
    #region Market
        /*[Command] public void CmdGetMarketItems() {
            List<MarketItemClient> result = new List<MarketItemClient>();
            foreach(MarketItem auctionItem in MarketSystem.items) {
                if(ScriptableItem.dict.TryGetValue(auctionItem.item, out ScriptableItem itemData)) {
                    MarketItemClient clientItem = new MarketItemClient();
                    // info
                    clientItem.id = auctionItem.id;
                    clientItem.sellerId = auctionItem.sellerId;
                    clientItem.price = auctionItem.price;
                    clientItem.endtime = auctionItem.endtime;
                    clientItem.category = auctionItem.category;
                    // item 
                    Item item = new Item(itemData);
                    item.plus = auctionItem.plus;
                    item.plus_progress = auctionItem.plus_progress;
                    item.qualityId = auctionItem.qualityId;
                    item.socket1 = auctionItem.socket1;
                    item.socket2 = auctionItem.socket2;
                    item.socket3 = auctionItem.socket3;
                    item.socket4 = auctionItem.socket4;
                    item.bound = true;
                    clientItem.item = new ItemSlot(item, auctionItem.amount);

                    result.Add(clientItem);
                }
            }
            Market = result;
        }
        [Command] public void CmdBuyMarketItem(int id) {
            if(MarketSystem.Buy(this, id)) {
                int index = Market.FindIndex(i => i.id == id);
                Market.RemoveAt(index);
            }
        }
        [Command] public void CmdAddMarketItem(int slot, int amount, int price, int hoursIndex) {
            if(slot > -1 && slot < own.inventorySize) 
            {
                ItemSlot item = own.inventory[slot];

                if(amount > 0 && price > 1 && item.amount >= amount && !item.item.bound && item.item.data.tradable) 
                {
                    MarketOfferTime offerTime= Storage.data.MarketOfferTimes[hoursIndex];
                    if(own.gold >= offerTime.fee) 
                    {
                        own.gold -= offerTime.fee;
                        item.DecreaseAmount(amount);
                        own.inventory[slot] = item;
                        MarketItem marketItem = new MarketItem();
                        marketItem.sellerId = id;
                        marketItem.price = price;
                        marketItem.endtime = offerTime.time();
                        marketItem.category = 0;
                        marketItem.item = item.item.id;
                        marketItem.plus = item.item.plus;
                        marketItem.plus_progress = item.item.plus_progress;
                        marketItem.qualityId = item.item.qualityId;
                        marketItem.socket1 = item.item.socket1;
                        marketItem.socket2 = item.item.socket2;
                        marketItem.socket3 = item.item.socket3;
                        marketItem.socket4 = item.item.socket4;
                        marketItem.amount = amount;

                        MarketSystem.Add(marketItem);
                    }
                }
            }
        }*/
    #endregion
    #region Teleportation
        public bool CanTeleportTo(int mapIndex) => mapIndex > -1 && mapIndex < Storage.data.cities.Length;
        [Server] public void TeleportTo(byte targetCity, Vector3 targetLocation, bool fromEvent = false) {
            if(CanTeleportTo(targetCity)) {
                Hide();
                enabled = false;
                if(fromEvent || targetCity != cityId) {
                    isTeleporting = true;
                    cityId = (byte)targetCity;
                }
                target = null;
                Warp(targetLocation == Vector3.zero ? city.StartingPoint() : targetLocation);
                if(!fromEvent && targetCity == cityId) {
                    enabled = true;
                    Show();
                }
            }
            else TargetNotify("Please select a map to teleport to.");
        }
        [Command] public void CmdTeleportTo(byte targetCity, Vector3 targetLocation) => TeleportTo(targetCity, targetLocation);
        [Command] public void CmdNpcTeleport(int index) {
            /*if (state == EntityState.Idle && target != null && target.health > 0 && target is Npc &&
                Utils.ClosestDistance(this, target) <= Storage.data.playerInteractionRange) {
                TeleportNPCOffer dest = ((Npc)target).teleports[index];
                City cityInfo = Storage.data.cities[dest.city];
                if(own.gold >= dest.cost && dest.city > -1 && level >= cityInfo.minLvl) {
                    UseGold(dest.cost);
                    TeleportTo((byte)dest.city, cityInfo.StartingPoint());
                }
            }*/
        }
        [Command] public void CmdTeleportToQuestLocation(Vector3 questLocation) {
            // TODO: Check for teleport item before teleporting the player
            agent.Warp(questLocation);
            target = null;
        }
        [Server] public void TeleportToEventMap(EventMaps eventType, Vector3 mapPos, Vector3 entryPos) {
            Hide();
            enabled = false;
            target = null;
            isTeleporting = true;
            TargetLoadEventMap(eventType, mapPos);
            lastLocation = transform.position;
            Warp(entryPos);
        }
        [Command] public void CmdConfirmTeleport() {
            if(isTeleporting) {
                enabled = true;
                Show();
                isTeleporting = false;

                if(own.occupation == PlayerOccupation.None)
                    return;
                
                if(own.occupation == PlayerOccupation.InMatchArena1v1 && occupationId != -1) {
                    ArenaSystem.ConfirmTeleportToMatch1v1(this);
                }
            }
        }
        [Server] public void TeleportToLastLocation(bool fromEvent) {
            if(lastLocation != Vector3.zero) {
                TeleportTo(cityId ,lastLocation, fromEvent);
                lastLocation = Vector3.zero;
            }
        }
        [TargetRpc] public void TargetLoadEventMap(EventMaps eventType, Vector3 mapPos) {}
    #endregion
    #region Quests
        public int GetQuestIndexByName(int questId) {
            for(int i = 0; i < own.quests.Count; ++i)
                if (own.quests[i].id == questId)
                    return i;
            return -1;
        }
        public bool HasCompletedQuest(int questId) {
            foreach (Quest quest in own.quests)
                if (quest.id == questId && quest.completed)
                    return true;
            return false;
        }
        public bool CanAcceptQuest(ScriptableQuest quest) {
            // not too many quests yet?
            // has required level?
            // not accepted yet?
            // has finished predecessor quest (if any)?
            return level >= quest.requiredLevel &&          // has required level?
                GetQuestIndexByName(quest.name) == -1 && // not accepted yet?
                (quest.predecessor == null || HasCompletedQuest(quest.predecessor.name));
        }
        public bool HasActiveQuest(int questId) {
            foreach (Quest quest in own.quests)
                if (quest.id == questId && !quest.completed)
                    return true;
            return false;
        }
        [Command] public void CmdAcceptQuest(int npcQuestIndex) {
            /* validate use collider point(s) to also work with big entities
            if(state == EntityState.Idle && target != null && target.health > 0 && target is Npc npc &&
                0 <= npcQuestIndex && npcQuestIndex < npc.quests.Length &&
                Utils.ClosestDistance(this, target) <= Storage.data.playerInteractionRange) {
                ScriptableQuestOffer npcQuest = npc.quests[npcQuestIndex];
                if(npcQuest.acceptHere && CanAcceptQuest(npcQuest.quest))
                    own.quests.Add(new Quest(npcQuest.quest));
            }*/
        }
        public bool CanCompleteQuest(int questName) { // helper function to check if the player can complete a quest
            int index = GetQuestIndexByName(questName);// has the quest and not completed yet?
            if (index != -1 && !own.quests[index].completed) {
                Quest quest = own.quests[index];
                if(quest.IsFulfilled(this)) { // fulfilled?
                    // enough space for reward item (if any)?
                    return quest.rewardItems.Length == 0 || InventoryCanAddItems(quest.rewardItems);
                }
            }
            return false;
        }
        [Server] public void QuestsOnKilled(Entity victim) {
            for(int i = 0; i < own.quests.Count; ++i)
                if (!own.quests[i].completed)
                    own.quests[i].OnKilled(this, i, victim);
        }
        [Server] public void QuestsOnCraft(ScriptableItem item, uint amount) {
            for (int i = 0; i < own.quests.Count; ++i)
                if (!own.quests[i].completed && level >= own.quests[i].requiredLevel)
                    own.quests[i].data.OnCrafted(this, i, item, amount);
        }
        [ServerCallback] public void QuestsOnLocation(Collider location) { // called by OnTriggerEnter on client and server. use callback.
            // call OnLocation in all active (not completed) quests
            for (int i = 0; i < own.quests.Count; ++i)
                if (!own.quests[i].completed)
                    own.quests[i].OnLocation(this, i, location);
        }
        [Command] public void CmdCompleteQuest(int name) {
            int index = GetQuestIndexByName(name);
            if(index > -1) {
                Quest quest = own.quests[index];
                if (CanCompleteQuest(quest.id)) {
                    quest.OnCompleted(this);// call quest.OnCompleted to remove quest items from inventory, etc.
                    // gain rewards
                    AddGold(quest.rewardGold + (uint)(quest.rewardGold * (int)quest.quality) / 2u);
                    own.experience += quest.rewardExperience;
                    if (quest.rewardItems != null)
                        foreach(var item in quest.rewardItems) InventoryAdd(item.item, item.amount);
                    
                    if(quest.type == QuestType.Daily) { // daily quests
                        own.quests.RemoveAt(index);
                        GetNewDailyQuest();
                    } else {// complete quest if general
                        quest.completed = true;
                        own.quests[index] = quest;
                    }
                }
            }
        }
        [Server] public void GetNewDailyQuest() {
            if(level >= 30 && own.dailyQuests < Storage.data.dailyQuestsLimitPerDay) {
                int count = own.quests.Where(q => q.type == QuestType.Daily).ToList().Count;
                if(count == 0) {
                    Quest quest = Storage.data.GetRandomDailyQuest(level);
                    quest.type = QuestType.Daily;
                    quest.quality = (Quality)(new System.Random().Next(0, 5));
                    own.quests.Add(quest);
                }
            }
        }
    #endregion
    #region Mall
        [Command] public void CmdBuyItemsFromMall(int categoryIndex, int itemIndex, uint amount) {
            if(categoryIndex < 0 || categoryIndex > Storage.data.ItemMallContent.Length || amount < 1 || 
                itemIndex < 0 || itemIndex > Storage.data.ItemMallContent[categoryIndex].items.Length)
                TargetNotify("Please Select an Item");
                return;
            Item item = Storage.data.ItemMallContent[categoryIndex].items[itemIndex];
            bool usingBound = Storage.data.ItemMallContent[categoryIndex].bound;
            bool hasPrice = !usingBound ? (own.diamonds >= item.data.itemMallPrice * amount) :
                                (own.b_diamonds >= item.data.itemMallPrice * Storage.data.BoundToUnboundRatio * amount);
            if(hasPrice) {
                if(InventoryAdd(item, amount)) {
                    if(usingBound) UseBDiamonds((uint)(item.data.itemMallPrice * Storage.data.BoundToUnboundRatio * amount));
                    else UseDiamonds((uint)(item.data.itemMallPrice * amount));
                    return;
                }
                TargetNotify("Inventory is Full, Please make space");
                return;
            }
            TargetNotify($"Insuficint Amount of {(usingBound ? "Bound Diamonds" : "Diamonds")}");
        }
    #endregion
    #region Pet
        public bool CanUnsummonPet() => activePet != null && (state == EntityState.Idle || state == EntityState.Moving)
                                    && (activePet.state == EntityState.Idle || activePet.state == EntityState.Moving);
        [Command] public void CmdPetSummon(ushort petId) => PetSystem.Summon(this, petId);
        [Command] public void CmdPetUnsummon() => PetSystem.Unsummon(this);
        [Command] public void CmdPetFeedx1(ushort petId, ushort selectedFeed) => PetSystem.Feed(this, petId, selectedFeed, 1u);
        [Command] public void CmdPetFeedx10(ushort petId, ushort selectedFeed) => PetSystem.Feed(this, petId, selectedFeed, 10u);
        [Command] public void CmdPetActivate(ushort itemName) => PetSystem.Activate(this, itemName);
        [Command] public void CmdPetUpgrade(ushort petId) => PetSystem.Upgrade(this, petId);
        [Command] public void CmdPetStarUp(ushort petId) => PetSystem.StarUp(this, petId);
        [Command] public void CmdPetTrain(ushort petId) => PetSystem.Train(this, petId);
        [Command] public void CmdPetChangeExpShare() {
            if(!CanTakeAction())
                return;
            own.shareExpWithPet = !own.shareExpWithPet;
            NextAction(.5d);
        }
    #endregion
    #region Mount
        public bool IsMounted() => mount.canMount && mount.mounted;
        [Command] public void CmdMountActivate(ushort itemName) => MountSystem.Activate(this, itemName);
        [Command] public void CmdMountDeploy(ushort mountId) => MountSystem.Deploy(this, mountId);
        [Command] public void CmdMountRecall() => MountSystem.Recall(this);
        [Command] public void CmdMountFeedx1(ushort mountId, ushort selectedFeed) => MountSystem.Feed(this, mountId, selectedFeed, 1u);
        [Command] public void CmdMountFeedx10(ushort mountId, ushort selectedFeed) => MountSystem.Feed(this, mountId, selectedFeed, 10u);
        [Command] public void CmdMountUpgrade(ushort mountId) => MountSystem.Upgrade(this, mountId);
        [Command] public void CmdMountStarUp(ushort mountId) => MountSystem.StarUp(this, mountId);
        [Command] public void CmdMountSummon() {
            if(mount.canMount) {
                ActiveMount info = mount;
                info.mounted = true;
                mount = info;
            }
            else Notify("Select a mount first", "اختر راكب اولا");
        }
        [Command] public void CmdMountUnsummon() => MountUnsummon();
        void MountUnsummon() {
            if(mount.canMount) {
                ActiveMount info = mount;
                info.mounted = false;
                mount = info;
            }
            else Notify("Select a mount first", "اختر راكب اولا");
        }
    #endregion
    #region Auto Fight
        void CollectNearestLoots() {
            /*GameObject[] objects = GameObject.FindGameObjectsWithTag("Monster");
            List<Monster> monsters = objects.Select(go => go.GetComponent<Monster>()).Where(m => m.health == 0 && m.HasLoot()).ToList();
            List<Monster> sorted = monsters.OrderBy(m => Vector3.Distance(transform.position, m.transform.position)).ToList();

            if (sorted.Count > 0) {
                for(int i = 0; i < sorted.Count; i++) {
                    if(sorted[i].HasLoot()) {
                        Monster monster = sorted[i];
                        if(own.auto.collectGold) {
                            own.gold += monster.gold;
                            monster.gold = 0;
                        }

                        if(own.auto.collectitems && monster.inventory.Count > 0) {
                            for (int s = 0; s < monster.inventory.Count; s++) {
                                ItemSlot slot = monster.inventory[s];
                                if (InventoryAdd(slot.item, slot.amount)) {
                                    slot.amount = 0;
                                    monster.inventory[s] = slot;
                                }
                            }
                        }
                    }
                    
                }
            }*/
        }
    #endregion
    #region Titles
    [Command] public void CmdActivateTitle(int itemIndex) {
        ItemSlot slot = own.inventory[itemIndex];
        if(slot.amount < 1)
            Notify("Item Not Found.");
            return;

        int titleId = ((TitleItem)slot.item.data).titleId;
        if(own.titles.FindIndex(t => t == titleId) != -1)
            Notify("Title already activated.");
            return;

        slot.DecreaseAmount(1);
        own.inventory[itemIndex] = slot;
        own.titles.Add((ushort)titleId);
    }
    [Command] public void CmdSetActiveTitle(int titleId) {
        if(titleId == activeTitle)
            Notify("This Title is Already Active.");
            return;
        if(!own.titles.Contains((ushort)titleId))
            Notify("This Title is not Activated.");
            return;
        activeTitle = (ushort)titleId;
    }
    #endregion
    #region Preview
    [Command] public void CmdPreviewPlayerInfo(uint playerId) => PreviewPlayerInfo(playerId);
    [Server] void PreviewPlayerInfo(uint playerId) {
        if(!Server.IsPlayerIdWithInServer(playerId)) {
            Notify("Player ID not valid.", "الرقم التعريفي للاعب غير صحيح.");
            Log($"[PreviewPlayerInfo] invalid target ID: {playerId}");
            return;
        }
        PreviewPlayerData info = PreviewSystem.GetPlayerInfo(playerId);
        if(info.status)
            TargetShowPlayerPreview(info);
            return;
        Notify("Player data not found.", "لم نجد بيانات اللاعب.");
    } 
    [Command] public void CmdPreviewTargetPlayerInfo() {
        if(target == null || !(target is Player))
            Notify("Please select a player first.", "برجاء اختيار هدف اولا.");
            return;
        PreviewPlayerInfo(((Player)target).id);
    }
    [TargetRpc] public void TargetShowPlayerPreview(PreviewPlayerData info) {}
    #endregion
    #region Ranking
    [Command] public void CmdGetRankingData(RankingCategory category) {
        switch(category) {
            // players
            case RankingCategory.PlayerBR:
                TargetSetBasicRankingValue(RankingSystem.playerRankingBR);
                break;
            case RankingCategory.PlayerLevel:
                TargetSetBasicRankingValue(RankingSystem.playerRankingLvl);
                break;
            case RankingCategory.PlayerHonor:
                TargetSetBasicRankingValue(RankingSystem.playerRankingHnr);
                break;
            // guilds
            case RankingCategory.GuildBR:
                TargetSetBasicRankingValue(RankingSystem.guildRankingBR);
                break;
            case RankingCategory.GuildLevel:
                TargetSetBasicRankingValue(RankingSystem.guildRankingLvl);
                break;
            // tribes
            case RankingCategory.TribeBR:
                TargetSetBasicRankingValue(RankingSystem.tribeRankingBR);
                break;
            case RankingCategory.TribeWins:
                TargetSetBasicRankingValue(RankingSystem.tribeRankingWins);
                break;
            // summonables
            case RankingCategory.PetBR:
                TargetSetSummonableRankingValue(RankingSystem.petRankingBR);
                break;
            case RankingCategory.PetLvl:
                TargetSetSummonableRankingValue(RankingSystem.petRankingLvl);
                break;
            case RankingCategory.MountBR:
                TargetSetSummonableRankingValue(RankingSystem.mountRankingBR);
                break;
            case RankingCategory.MountLvl:
                TargetSetSummonableRankingValue(RankingSystem.mountRankingLvl);
                break;
        }
    }
    [TargetRpc] public void TargetSetBasicRankingValue(RankingBasicData[] data) {}
    [TargetRpc] public void TargetSetSummonableRankingValue(SummonableRankingData[] data) {}
    #endregion
    #region Respawn
        [TargetRpc] public void TargetShowRespawn(byte freeRespawn) {}
        [TargetRpc] public void TargetHideRespawn() {}
        [Command] public void CmdRespawn(int choice) { respawnRequested = choice; }
        string ActionRespawn() {
            if(respawnRequested < 0 || respawnRequested > 2) return "DEAD";
            if(respawnRequested == 0) { // in city
                Warp(Storage.data.cities[cityId].StartingPoint());
                Revive(0.5f);
                respawnRequested = -1;
                own.reviveTime = 0;
                TargetHideRespawn();
                return "IDLE";
            }
            else if(respawnRequested == 1) { // here
                if(AvailableFreeRespawn > 0) { // free
                    agent.Warp(transform.position);
                    respawnRequested = -1;
                    AvailableFreeRespawn--;
                    own.reviveTime = 0;
                    Revive(0.5f);
                    TargetHideRespawn();
                    return "IDLE";
                }
                else { // item
                    int ReviveItemIndex = GetInventoryIndex(Storage.data.player.inplaceReviveItemId);
                    if(ReviveItemIndex > -1) {
                        if(InventoryRemove(own.inventory[ReviveItemIndex].item.id, 1)) {
                            agent.Warp(transform.position);
                            Revive(0.5f);
                            respawnRequested = -1;
                            own.reviveTime = 0;
                            TargetHideRespawn();
                            return "IDLE";
                        }
                    }
                    else Notify($"You don't have enough items to revive.");
                }
            }
            else if(respawnRequested == 2) { // here for diamonds
                if(own.diamonds >= Storage.data.player.inplaceReviveCost) {
                    UseDiamonds(Storage.data.player.inplaceReviveCost);
                    agent.Warp(transform.position);
                    Revive(0.5f);
                    respawnRequested = -1;
                    own.reviveTime = 0;
                    TargetHideRespawn();
                    return "IDLE";
                }
                else Notify($"You don't have enough Diamonds to revive.");
            }
            return "DEAD";
        }
    #endregion
    #region Friends
        bool IsFriend(uint fId) {
            if(own.friends.Count > 0) {
                for(int i = 0; i < own.friends.Count; i++)
                    if(own.friends[i].id == fId)
                        return true;
            }
            return false;
        }
        [Command] public void CmdRefreshOnlineFriends() {
            if(own.friends.Count < 1) return;
            for(int i = 0; i < own.friends.Count; i++) {
                if(Player.onlinePlayers.TryGetValue(own.friends[i].id, out Player friend)) {
                    Friend temp = own.friends[i];
                    temp.name = friend.name;
                    temp.level = (byte)friend.level;
                    temp.classInfo = classInfo;
                    temp.br = friend.battlepower;
                    temp.avatar = friend.avatar;
                    temp.lastOnline = 0;
                    own.friends[i] = temp;
                }
                else if(own.friends[i].IsOnline()) {
                    own.friends[i] = Database.singleton.LoadOfflineFriend(own.friends[i].id);
                }
            }
        }
        [Command] public void CmdSendFriendRequest(uint fId) {
            if(!Server.IsPlayerIdWithInServer(fId)) {
                Notify("Please enter a valid player ID", "برجاء التاكد من صحة ال ID");
                Log($"[CmdSendFriendRequest] invalid target ID: {fId}");
                return;
            }
            if(own.friends.Count == Storage.data.maxFriendsCount) {
                Notify("Friends list is full", "قائمة الاصدقاء ممتلئ");
                return;
            }
            if(IsFriend(fId)) {
                Notify("Already a friend", "صديق بالفعل");
                return;
            }
            if(Player.onlinePlayers.TryGetValue(fId, out Player friend)) {
                if(friend.own.friends.Count == Storage.data.maxFriendsCount) {
                    Notify("Target's friend list is full", "قائمة اصدقاء الهدف ممتلئ");
                    return;
                }
                friend.own.friendRequests.Add(new FriendRequest {
                    id = id,
                    name = name,
                    level = (byte)level,
                    br = battlepower,
                    tribe = tribeId
                });
                Notify("Friend request has been sent.", "تم ارسال طلب الصداقة.");
            }
            else {
                Notify("Target isn't Online.", "الهدف غير متصل.");
                return;
            }
        }
        [Command] public void CmdAcceptFriendRequest(int index) {
            if(own.friendRequests.Count < 1) {
                Notify("No friend requests found", "لا يوجد طلبات صداقة");
                return;
            }
            if(index < 0 || index > own.friendRequests.Count) {
                Notify("Please select a request", "برجاء اختيار طلب");
                return;
            }
            if(own.friends.Count == Storage.data.maxFriendsCount) {
                Notify("Friends list is full", "قائمة الاصدقاء ممتلئ");
                return;
            }
            if(IsFriend(own.friendRequests[index].id)) {
                Notify("This player is already a friend", "هذا اللاعب صديق لك بالفعل");
                return;
            }
            if(Player.onlinePlayers.TryGetValue(own.friendRequests[index].id, out Player friend)) {
                own.friends.Add(new Friend {
                    id = friend.id,
                    name = friend.name,
                    level = (byte)friend.level,
                    classInfo = friend.classInfo,
                    avatar = friend.avatar,
                    tribe = friend.tribeId,
                    br = friend.battlepower,
                    lastOnline = 0
                });
                friend.own.friends.Add(new Friend {
                    id = id,
                    name = name,
                    level = (byte)level,
                    classInfo = classInfo,
                    avatar = avatar,
                    tribe = tribeId,
                    br = battlepower,
                    lastOnline = 0
                });
                friend.Notify($"{name} accepted your friend request", $"{name} قبل طلب صداقتك");
            } else {
                own.friends.Add(Database.singleton.LoadOfflineFriend(own.friendRequests[index].id));
            }
        }
        [Command] public void CmdRefuseFriendRequest(int index) {
            if(own.friendRequests.Count < 1) {
                Notify("No friend requests found", "لا يوجد طلبات صداقة");
                return;
            }
            if(index < 0 || index > own.friendRequests.Count) {
                Notify("Please select a request", "برجاء اختيار طلب");
                return;
            }
            own.friendRequests.RemoveAt(index);
        }
        [Command] public void CmdRemoveFriend(uint fId) {
            if(!Server.IsPlayerIdWithInServer(fId)) {
                Notify("Please enter a valid player ID", "برجاء التاكد من صحة ال ID");
                Log($"[CmdRemoveFriend] invalid target ID: {fId}");
                return;
            }
            if(own.friends.Count == 0) {
                Notify("Friends list is Empty", "قائمة الاصدقاء فارغة");
                return;
            }
            if(!IsFriend(fId)) {
                Notify("This player isn't a friend", "هذا اللاعب ليس صديقك");
                return;
            }
            for(int i = 0; i < own.friends.Count; i++){
                if(own.friends[i].id == fId)
                    own.friends.RemoveAt(i);
                    break;
            }
            if(Player.onlinePlayers.TryGetValue(fId, out Player friend)) {
                if(friend.own.friends.Count > 0) {
                    for(int i = 0; i < friend.own.friends.Count; i++) {
                        if(friend.own.friends[i].id == id)
                            friend.own.friends.RemoveAt(i);
                            break;
                    }
                }
            }
            else Database.singleton.RemoveFriend(id, fId);
        }
        [Server] public void RemoveFriend(uint fId) {
            if(own.friends.Count < 1) return;
            for(int i = 0; i < own.friends.Count; i++){
                if(own.friends[i].id == fId)
                    own.friends.RemoveAt(i);
                    break;
            }
        }
        [Server] public void SetFriendOnline(uint fId) {
            if(own.friends.Count < 1) return;
            for(int i = 0; i < own.friends.Count; i++) {
                if(own.friends[i].id == fId) {
                    Friend temp = own.friends[i];
                    temp.lastOnline = 0;
                    own.friends[i] = temp;
                }
            }
        }
    #endregion
    #region Attributes
    [Command] public void CmdIncreaseVitality() {
        if(own.freepoints > 0) {
            own.vitality++;
            own.freepoints--;
        }
        else Notify("No available points to use", "لا يوجد نقاط قدرة كافية");
    }
    [Command] public void CmdIncreaseStrength() {
        if(own.freepoints > 0) {
            own.strength++;
            own.freepoints--;
        }
        else Notify("No available points to use", "لا يوجد نقاط قدرة كافية");
    }
    [Command] public void CmdIncreaseIntelligence() {
        if(own.freepoints > 0) {
            own.intelligence++;
            own.freepoints--;
        }
        else Notify("No available points to use", "لا يوجد نقاط قدرة كافية");
    }
    [Command] public void CmdIncreaseEndurance() {
        if(own.freepoints > 0) {
            own.endurance++;
            own.freepoints--;
        }
        else Notify("No available points to use", "لا يوجد نقاط قدرة كافية");
    }
    #endregion
    #region Military Rank
        [Command] public void CmdPromoteMilitaryRank() {
            if(ScriptableMilitaryRank.dict.TryGetValue(own.militaryRank, out ScriptableMilitaryRank rank)) {
                if(rank.next != null) {
                    if(level < rank.level) {
                        Notify("You didn't meet the required level yet", "لم تحقق المستوي المطلوب بعد");
                        return;
                    }
                    if(own.TotalHonor < rank.honor) {
                        Notify("You didn't meet the required honor yet", "لم تحقق نقاط الشرف المطلوبة بعد");
                        return;
                    }
                    if(own.MonsterPoints < rank.monsterPoints) {
                        Notify("You didn't meet the required Monster Points yet", "لم تحقق نقاط الوحوش المطلوبة بعد");
                        return;
                    }
                    own.militaryRank++;
                    Notify("You have been promoted successfully", "تم ترقيتك بنجاح");
                }
                else Notify("Reached max military rank", "لقد وصلت لاعلي رتبة عسكرية");
            }
        }
    #endregion
    #region Achievements
    [Command] public void CmdRecieveAchievementReward(ushort achId) {
        if(!own.achievements.Has(achId)) {
            Notify("Can't recieve unfulfilled achievement's reward", "لا يمكنك اقتناء جائزة انجاز غير محقق");
            Log("[CmdRecieveAchievementReward] unfulfilled ID: " + achId);
            return;
        }
    }
    [Server] public void ArchiveGainedGold(uint amount) {
        if(amount < 1) return;
        own.archive.gainedGold += amount;
        if(inprogressAchievements.gainedGold != null && inprogressAchievements.gainedGold.IsFulfilled(this))
            inprogressAchievements.gainedGold.OnAchieved(this);
    }
    [Server] public void ArchiveUsedGold(uint amount) {
        if(amount < 1) return;
        own.archive.usedGold += amount;
        if(inprogressAchievements.usedGold != null && inprogressAchievements.usedGold.IsFulfilled(this))
            inprogressAchievements.usedGold.OnAchieved(this);
    }
    #endregion
    #region Marriage
        public bool IsMarried() => own.marriage.spouse > 0;
        [Command] public void CmdSendMarriageProposal(uint sId, MarriageType type) => MarriageSystem.SendMarriageProposal(this, sId, type);
        [Command] public void CmdRefuseMarriageProposal(int index) => MarriageSystem.RefuseMarriageProposal(this, index);
        [Command] public void CmdAcceptMarriageProposal(int index) => MarriageSystem.AcceptMarriageProposal(this, index);
        [TargetRpc] public void TargetAnnounceMarriage(string husband, string wife) {}
    #endregion
    #region Arena
        [Command] public void CmdRegisterInArena1v1() => ArenaSystem.Register1v1(this);
        [Command] public void CmdUnRegisterInArena1v1() => ArenaSystem.UnRegister1vs1(this);
        [Command] public void CmdAcceptChallengeArena1v1() => ArenaSystem.AcceptMatch1v1(this);
        [Command] public void CmdRefuseChallengeArena1v1() => ArenaSystem.RefuseMatch1v1(this);
        [Command] public void CmdLeaveArena1v1() => ArenaSystem.LeaveMatch1v1(this);
        [TargetRpc] public void TargetNotifiyArenaMatch1v1() {}
        [TargetRpc] public void TargetHideNotifiyArenaMatch1v1() {}
        [TargetRpc] public void TargetShowResultArena1v1(bool win, int dmg, int opponentDmg) {}
        [TargetRpc] public void TargetHideResultArena1v1() {}
        [TargetRpc] public void TargetRefusedArenaMatch1v1() {}
        [TargetRpc] public void TargetCanceledArenaMatch1v1() {}
    #endregion
    #region Trade
        public bool IsTrading() => tradeId != 0;
        [Server] public void InitiateTrade(uint tId) {
            tradeId = tId;
            TargetInitiateTrade();
        }
        [Server] public void CancelTrade() {
            tradeId = 0;
            TargetCloseTrade();
        }
        [Command] public void CmdTradeInvite(uint playerId) => TradeSystem.Invite(this, playerId);
        [Command] public void CmdTradeAcceptInvitation(int index) => TradeSystem.AcceptInvitation(this, index);
        [Command] public void CmdTradeRefuseInvitation(int index) => TradeSystem.RefuseInvitation(this, index);
        [Command] public void CmdTradeConfirmOffer(TradeOfferContent offerContent) => TradeSystem.Confirm(this, offerContent);
        [Command] public void CmdTradeAcceptOffer() => TradeSystem.Accept(this);
        [TargetRpc] public void TargetInitiateTrade() {} // show trade window
        [TargetRpc] public void TargetShowConfirmedTradeOffer(ItemSlot[] offeredItems, uint offeredGold, uint offeredDiamonds) {}
        [TargetRpc] public void TargetAcceptTradeOffer() {}
        [TargetRpc] public void TargetAcceptedMyTradeOffer() {}
        [TargetRpc] public void TargetCloseTrade() {} // hide trade window
    #endregion
    #region Others
        [TargetRpc] public void TargetStartCountDown(double startTime) {}
        [Command] public void CmdSetCurrentLanguage(Languages language) => currentLang = language;
        [Command] public void CmdNotifyIfPlayerOffline(uint pId) {
            if(!Player.onlinePlayers.ContainsKey(pId)) 
                NotifyPlayerOffline();
        }
        [Server] public void ChangeGender() {
            gender = gender == Gender.Male ? Gender.Female : Gender.Male;
        }

    #endregion
    #region Purchase
    [Command] public void CmdValidatePurchasePackage(string receipt) {
        if(PurchaseManager.singleton.Validate(this, receipt))
            TargetConfirmPurchase();
    }
    [TargetRpc] public void TargetConfirmPurchase() {}
    #endregion
    #region Notify
        [Server] public void Notify(string en = "", string ar = "") => TargetNotify(IsEn ? en : ar);
        public void NotifyNotEnoughGold() => Notify("Not enough gold", "لا يوجد ذهب كاف");
        public void NotifyNotEnoughDiamonds() => Notify("Not enough diamonds", "لا يوجد جواهر كاف");
        public void NotifyNotEnoughMaterials() => Notify("Not enough materials", "ينقصك بعض الطلبات");
        public void NotifyPlayerOffline() => Notify("Player is offline now", "اللاعب غير متوفر حاليا");
        public void NotifySentInvitation() => Notify("Invitation has been sent", "تم ارسال الدعوة");
        public void NotifyNotEnoughInventorySpace() => Notify("Not enough inventory space", "لا يوجد مكان متاح بالحقيبة");
        public void NotifyRiskyActionTime() {
            double nextActionTime = (double)Mathf.Ceil((float)(own.nextRiskyActionTime - NetworkTime.time));
            Notify($"Please try again after {nextActionTime}s", $"برجاء المحاولة مجددا بعد {nextActionTime} ثانية");
        }
        [Server] public void Log(string log = "") {
            UIManager.data.logsList.Add($"[{DateTime.Now}]{name}({id}): {log}");
            suspiciousActivities++;
        }
        // success and failure
        [TargetRpc] public void TargetNotify(string msg) {}
        public void NotifySuccess(NotifySuccessType type = NotifySuccessType.Default) => TargetNotifySuccess(type);
        [TargetRpc] public void TargetNotifySuccess(NotifySuccessType type) {}
        public void NotifyFailure(NotifySuccessType type = NotifySuccessType.Default) => TargetNotifyFailure(type);
        [TargetRpc] public void TargetNotifyFailure(NotifySuccessType type) {}
        [TargetRpc] public void TargetOnLevelUp() {}
    #endregion
    #region ServerCallbacks
        [ServerCallback] public override void OnAggro(Entity entity) {
            if (activePet != null && activePet.defendOwner)
                activePet.OnAggro(entity);
            
            if(own.auto.on) {
                if(entity == this) target = null;
                //FindNearestTarget();
                if(entity != null && CanAttack(entity)) {
                    if (target == null) {
                        target = entity;
                    }
                } else if (entity != target) { // no need to check dist for same target
                    float oldDistance = Vector3.Distance(transform.position, target.transform.position);
                    float newDistance = Vector3.Distance(transform.position, entity.transform.position);
                    if (newDistance < oldDistance * 0.8) target = entity;
                }
            }
        }
        protected override void OnTriggerEnter(Collider col) {
            // call base function too
            base.OnTriggerEnter(col);
            // quest location? (we use .CompareTag to avoid .tag allocations)
            if (col.CompareTag("QuestLocation"))
                QuestsOnLocation(col);
        }
    #endregion   
    }
    [Serializable] public struct DailySignRewards
    {
        public int daysCount;
        public List<ItemSlot> rewards;
    }
    [Serializable] public struct AutoMode
    {
        public bool on;
        public int lastskill;
        public float followDistance;
        public bool collectGold;
        public bool collectitems;
        public double hpRecovery;
        public double manaRecovery;
        public string[] hpRecoveryPotions;
        public string[] manaRecoveryPotions;
    }
}
