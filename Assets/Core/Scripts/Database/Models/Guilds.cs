using SQLite;
namespace Game.DatabaseModels
{
    public class Guilds
    {
        [PrimaryKey, AutoIncrement]
        public uint id { get; set; }
        public string name { get; set; }
        public byte tribeId { get; set; }
        public byte level { get; set; }
        public uint br { get; set; }
        public uint experience { get; set; }
        public bool autoAccept { get; set; }
        public string notice { get; set; }
        public byte joinLevel { get; set; }
        public uint wealth { get; set; }
        public uint wood { get; set; }
        public uint stone { get; set; }
        public uint iron { get; set; }
        public uint food { get; set; }
        public byte hall { get; set; }
        public byte academy { get; set; }
        public byte storage { get; set; }
        public byte shop { get; set; }
    }
}