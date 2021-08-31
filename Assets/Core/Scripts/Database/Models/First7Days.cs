namespace Game.DatabaseModels
{
    public class First7Days
    {
        [SQLite.PrimaryKey] public uint id { get; set; }
        public string signUp { get; set; }
        public string recharged { get; set; }
        public string rechargeRewards { get; set; }
    }
}