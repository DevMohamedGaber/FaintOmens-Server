using SQLite;
namespace Game.DatabaseModels
{
    public class Characters
    {
        [PrimaryKey, AutoIncrement]
        public uint? id { get; set; } = null;
        [Indexed]
        public ulong accId { get; set; }
        [Collation("NOCASE"), MaxLength(16)]
        public string name { get; set; }
        public PlayerClass classType { get; set; }
        public byte classRank { get; set; }
        public Gender gender { get; set; } = 0;
        public byte city { get; set; } = 0;
        public float x { get; set; } = 0;
        public float y { get; set; } = 0;
        public float z { get; set; } = 0;
        public bool online { get; set; } = false;
        public double lastsaved { get; set; } = 0;
        public double createdAt { get; set; } = 0;
        public byte avatar { get; set; } = 0;
        public byte frame { get; set; } = 0;
        public uint teamId { get; set; } = 0;
        public uint guildId { get; set; } = 0;
        public ushort activeTitle { get; set; } = 0;
        public byte vipLevel { get; set; } = 0;
        public uint spouseId { get; set; } = 0;
        public ushort activePet { get; set; } = 0;
        public ushort activeMount { get; set; } = 0;
        public PrivacyLevel privacy { get; set; } = PrivacyLevel.ShowAll;
        
        public byte level { get; set; } = 1;
        public uint experience { get; set; } = 0;
        public uint br { get; set; } = 0;

        public ushort vitality { get; set; } = 0;
        public ushort strength { get; set; } = 0;
        public ushort intelligence { get; set; } = 0;
        public ushort endurance { get; set; } = 0;
        public ushort freepoints { get; set; } = 0;

        public byte inventorySize { get; set; } = 30;
        public uint gold { get; set; } = 0;
        public uint diamonds { get; set; } = 0;
        public uint b_diamonds { get; set; } = 0;
        public uint popularity { get; set; } = 0;

        public byte tribeId { get; set; } = 1;
        public TribeRank tribeRank { get; set; } = 0;
        public uint tribeGoldContribution { get; set; } = 0;
        public uint tribeDiamondContribution { get; set; } = 0;
        
        public ushort todayHonor { get; set; } = 0;
        public uint totalHonor { get; set; } = 0;
        public ushort killCount { get; set; } = 0;
        public uint MonsterPoints { get; set; } = 0;
        public byte militaryRank { get; set; } = 0;
        
        public byte dailyQuests { get; set; } = 0;
        public byte tribeQuests { get; set; } = 0;
        public string dailySign { get; set; } = "-";
        public byte AvailableFreeRespawn { get; set; } = 3;
        public ushort arena1v1WinsToday { get; set; } = 0;
        public ushort arena1v1LossesToday { get; set; } = 0;
        public ushort arena1v1Points { get; set; } = Storage.data.arena.dailyPoints;
        
        public bool showWardrobe { get; set; } = true;
        public bool shareExpWithPet { get; set; } = true;
        
        public byte suspiciousActivities { get; set; } = 0;
    }
}