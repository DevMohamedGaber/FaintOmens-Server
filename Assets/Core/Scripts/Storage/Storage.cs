using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using sg = Game.StorageData;
namespace Game
{
    public class Storage : MonoBehaviour
    {
        public static Storage data;
        
        public sg.Account account;
        public sg.Player player;
        public sg.NewPlayer newPlayer;
        public sg.Wardrobe wardrobe;
        public sg.Pet pet;
        public sg.Mount mount;
        public sg.Arena arena;
        public sg.Item item;
        public sg.DefaultAchievements achievements;
        public sg.Guild guild;
        public sg.Marriage marriage;
        public sg.Meteor meteor;
        public sg.Team team;
        public sg.Monsters monsters;
        
        //public Dictionary<PlayerClass, GameObject> classPrefabs = new Dictionary<PlayerClass, GameObject>();
        public ScriptableQuest[] dailyQuests;
        public DailySignRewards[] dailySignRewards;
        public First7DaysEventRewards[] SignUp7DaysEventsRewards;
        public First7DaysEventRewards[] Recharge7DaysEventsRewards;
        public City[] cities;
        public Item[] inventoryShopItems;
        public MallItemsCategory[] ItemMallContent;
        public GuildSkill[] guildSkills;
        // info
        public int maxDailyQuests = 20;
        public int dailyQuestsLimitPerDay = 5;
        public int UpdateBRInGuildIntervalInMins = 1;
        public ExponentialUInt itemUpgradeCost = new ExponentialUInt{ multiplier=5000, baseValue=2f }; // make int
        public int maxFriendsCount = 100;
        public int playerClassPromotionCount = 4;
        public float playerInteractionRange = 4;
        public float guildInviteWaitSeconds = 3;
        public int charactersPerAccount = 4;
        public BuffSkill offenderBuff;
        public BuffSkill murdererBuff;
        public ItemSlotArray[] signup7daysRewards;
        
        [Header("Wardrobe")]
        public int wardropUpgradeItem;
        public ExponentialInt wardropUpgradeStones;
        public ExponentialUInt wardropUpgradeGold;

        [Header("Ratios")]
        public int BoundToUnboundRatio = 2;
        public int AP_Vitality = 100;
        public int AP_Strength_ATK = 10;
        public int AP_Strength_DEF = 5;
        public int AP_Intelligence_ATK = 10;
        public int AP_Intelligence_DEF = 5;
        public int AP_Intelligence_MANA = 10;
        public int AP_Endurance = 15;

        [Header("Mails")]
        public Mail[] newPlayerMail;
        [Header("Shops")]
        public GuildShopItem[] guildShop;
        [Header("Loot")]
        public float lootAllowAll = 5f;
        public float lootDestroySelf = 5f;
        public GameObject lootPrefab;
        public Quest GetRandomDailyQuest(int level) {
            List<ScriptableQuest> quest = dailyQuests.Where(q => q.requiredLevel <= level).ToList();
            return new Quest(quest[new System.Random().Next(0, quest.Count - 1)]);
        }
        void Awake()
        {
            data = this;
            player.OnAwake();
            item.OnAwake();
            guild.OnAwake();
            mount.OnAwake();
        }
    }
    [Serializable] public class MallItemsCategory {
        public string category;
        public Item[] items;
        public bool bound;
    }
    [Serializable] public struct ItemSlotArray {
        public ItemSlot[] items; 
    }
}