namespace Game.DatabaseModels
{
    public class Marriages
    {
        [SQLite.PrimaryKey]
        public uint hasband { get; set; }
        public uint wife { get; set; }
        public byte level { get; set; }
        public uint exp { get; set; }
    }
}