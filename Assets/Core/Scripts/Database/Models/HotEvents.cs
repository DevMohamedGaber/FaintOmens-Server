using SQLite;
namespace Game.DatabaseModels
{
    public class HotEvents
    {
        [PrimaryKey, AutoIncrement] public int? id { get; set; }
        public string name { get; set; }
        public HotEventTypes type { get; set; }
        public double startsAt { get; set; }
        public double endsAt { get; set; }
        public string objectives { get; set; }
        public string rewards { get; set; }
        public string description { get; set; }
        public bool renewable { get; set; }
        public bool finished { get; set; }
    }
}