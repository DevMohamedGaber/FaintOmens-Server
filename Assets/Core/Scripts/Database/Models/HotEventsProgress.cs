namespace Game.DatabaseModels
{
    public class HotEventsProgress
    {
        public uint id { get; set; }
        public int eventId { get; set; }
        public int progress { get; set; }
        public string completeTimes { get; set; }
    }
}