namespace Game.DatabaseModels
{
    public class Quests
    {
        public uint owner { get; set; }
        public ushort id { get; set; }
        public uint progress { get; set; }
        public bool completed { get; set; }
        public QuestType type { get; set; }
        public Quality quality { get; set; }
    }
}