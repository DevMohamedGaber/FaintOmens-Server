namespace Game.DatabaseModels
{
    public class Teams
    {
        [SQLite.PrimaryKey] public uint id { get; set; }
        public uint leaderId { get; set; }
        public ExperiaceShareType share { get; set; }
    }
}