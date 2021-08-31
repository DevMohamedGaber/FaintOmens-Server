namespace Game.DatabaseModels
{
    public class Friends
    {
        [SQLite.PrimaryKey]
        public uint id { get; set; }
        public uint friend { get; set; }
        public byte level { get; set; }
        public int exp { get; set; }
    } 
}