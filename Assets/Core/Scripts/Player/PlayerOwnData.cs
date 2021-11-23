using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Game.Achievements;
namespace Game
{
    public class PlayerOwnData : NetworkBehaviourNonAlloc
    {
        [SerializeField] Player own;
    #region Basic Informations
        [Header("Basic")]
        [SyncVar] public uint gold;
        [SyncVar] public uint diamonds;
        [SyncVar] public uint b_diamonds;
        [SyncVar] public uint popularity;
        [SyncVar] public double createdAt;
        [SyncVar] public double reviveTime;
        [SyncVar] public byte inventorySize;
        [SyncVar] public double nextRiskyActionTime;
        [SyncVar] public PlayerOccupation occupation = PlayerOccupation.None;
        [SyncVar] public byte cityId;
        [SyncVar] public bool showClothing = true;
        [SyncVar] public PrivacyLevel privacy;
        public SyncListItemSlot inventory = new SyncListItemSlot();
        public SyncListItemSlot accessories = new SyncListItemSlot();
        public SyncListUShort wardrobe = new SyncListUShort();
        public SyncListItemSlot equipment = new SyncListItemSlot();
        public SyncListClothing clothing = new SyncListClothing();
    #endregion
    #region Attribute Points
        [Header("Attributes")]
        [SyncVar] public ushort strength;
        [SyncVar] public ushort intelligence;
        [SyncVar] public ushort vitality;
        [SyncVar] public ushort endurance;
        [SyncVar] public ushort freepoints;
    #endregion
    #region Daily
        [Header("Dailys")]
        [SyncVar] public byte dailyQuests;
        public SyncListByte dsDays = new SyncListByte();
        public SyncListByte dsRewards = new SyncListByte();
    #endregion
    #region Military
        [Header("Military")]
        [SyncVar] public ushort killStrike;
        [SyncVar] public ushort MonstersKillCount;
        [SyncVar] public ushort TodayHonor;
        [SyncVar] public uint TotalHonor;
        [SyncVar] public uint MonsterPoints;
        [SyncVar] public byte militaryRank;
    #endregion
    #region Guild
        [Header("Guild")]
        [SyncVar] public GuildRank guildRank;
        public SyncListByte guildSkills = new SyncListByte() {0, 0, 0, 0, 0, 0};
    #endregion
    #region Tribe
        [Header("Tribe")]
        //[SyncVar] public Tribe tribe;
        [SyncVar] public TribeRank tribeRank;
        [SyncVar] public byte tribeQuests;
        [SyncVar] public uint tribeGoldContribution;
        [SyncVar] public uint tribeDiamondContribution;
    #endregion
    #region Social
        [Header("Social")]
        [SyncVar] public Marriage marriage;
        [SyncVar] public Team team;
        public SyncListFriend friends = new SyncListFriend();
    #endregion
    #region Arena
        [Header("Arena 1v1")]
        [SyncVar] public ushort arena1v1WinsToday;
        [SyncVar] public ushort arena1v1LossesToday;
        [SyncVar] public ushort arena1v1Points;
    #endregion
    #region Character Related
        [SyncVar] public bool shareExpWithPet;
        [SyncVar] public VIP vip;
        [SyncVar] public Archive archive = new Archive();
        [SyncVar] public AutoMode auto = new AutoMode();
        public SyncListPets pets = new SyncListPets();
        public SyncListMounts mounts = new SyncListMounts();
        public SyncListQuest quests = new SyncListQuest();
        public SyncListMail mailBox = new SyncListMail();
        public SyncListUShort titles = new SyncListUShort();
        public SyncListAchievements achievements = new SyncListAchievements();
        //public SyncListHotEventsProgress HotEventsProgress = new SyncListHotEventsProgress();
        public SyncDictionaryIntDouble itemCooldowns = new SyncDictionaryIntDouble();
    #endregion
    #region Invitations
        public SyncListTeamInvitations teamInvitations = new SyncListTeamInvitations();
        public SyncListFriendRequest friendRequests = new SyncListFriendRequest();
        public SyncListMarriageProposals marriageProposals = new SyncListMarriageProposals();
        public SyncListTradeInvitations tradeInvitations = new SyncListTradeInvitations();
    #endregion
    #region Leveling
        [SyncVar, SerializeField] uint _experience;
        public uint experience {
            get => _experience;
            set {
                _experience = value;
                if(own.level == Storage.data.player.lvlCap)
                    return;
                
                while(own.level < Storage.data.player.lvlCap && _experience >= expMax) {
                    _experience -= expMax;// subtract current level's required exp, then level up
                    own.AddLevel();
                }
                // set to expMax if there is still too much exp remaining
                if (_experience > expMax) _experience = expMax;
            }
        }
        public uint expMax => Storage.data.player.expMax[own.level - 1];
    #endregion
        public void SetDailySignInInfo(string dsInfo) {
            string[] signedRewards = dsInfo.Split('-');
            //days
            byte[] dsDaysList = Utils.StringToByteArray(signedRewards[0]);
            if(dsDaysList.Length > 0)
                for(int i = 0; i < dsDaysList.Length; i++)
                    dsDays.Add(dsDaysList[i]);
            //rewards
            byte[] dsRewardsList = Utils.StringToByteArray(signedRewards[1]);;
            if(dsRewardsList.Length > 0)
                for(int i = 0; i < dsRewardsList.Length; i++)
                    dsRewards.Add(dsRewardsList[i]);
        }
        public string GetDailySignInInfo() {
            string res = "";
            if(dsDays.Count > 0)
                for(int i = 0; i < dsDays.Count; i++)
                    res += i < dsDays.Count - 1 ? $"{dsDays[i]}," : dsDays[i].ToString();
            res += "-";
            if(dsRewards.Count > 0)
                for(int i = 0; i < dsRewards.Count; i++)
                    res += i < dsRewards.Count - 1 ? $"{dsRewards[i]}," : dsRewards[i].ToString();
            return res;
        }
    }
}