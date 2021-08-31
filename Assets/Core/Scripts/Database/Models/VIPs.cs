namespace Game.DatabaseModels
{
    public class VIPs
    {
        [SQLite.PrimaryKey]
        public uint id { get; set; }
        public int points { get; set; }
        public int quests { get; set; }
        public string rewards { get; set; }
        public bool weeklyReward { get; set; }
        public int totalRecharge { get; set; }
        public int todayRecharge { get; set; }
    }
}