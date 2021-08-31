namespace Game.DatabaseModels
{
    public class Buffs
    {
        public uint id { get; set; }
        public ushort buff { get; set; }
        public byte level { get; set; }
        public float buffTimeEnd { get; set; }
    }
}