namespace Game.DatabaseModels
{
    public class Achievements
    {
        public uint owner { get; set; }
        public ushort id { get; set; }
        public bool claimed { get; set; } = false;
    }
}