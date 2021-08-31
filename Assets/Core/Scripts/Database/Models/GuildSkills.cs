namespace Game.DatabaseModels
{
    public class GuildSkills
    {
        [SQLite.PrimaryKey]
        public uint id { get; set; }
        public byte vitalityLvl { get; set; }
        public byte strengthLvl { get; set; }
        public byte intelligenceLvl { get; set; }
        public byte enduranceLvl { get; set; }
        public byte critLvl { get; set; }
        public byte blockLvl { get; set; }
    }
}