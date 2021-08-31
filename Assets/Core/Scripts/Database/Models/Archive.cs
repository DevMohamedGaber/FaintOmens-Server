namespace Game.DatabaseModels
{
    public class Archive
    {
        [SQLite.PrimaryKey]
        public uint owner { get; set; }
        public ushort achievementPoints { get; set; }
        public ulong gainedGold { get; set; }
        public ulong usedGold { get; set; }
        public uint gainedDiamonds { get; set; }
        public uint usedDiamonds { get; set; }
        public ushort killStrike { get; set; }
        public ushort arena1v1Wins { get; set; }
        public ushort arena1v1Losses { get; set; }
        public ushort highestArena1v1Points { get; set; }
    }
}