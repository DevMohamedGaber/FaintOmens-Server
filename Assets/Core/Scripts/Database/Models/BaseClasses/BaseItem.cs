namespace Game.DatabaseModels
{
    public class BaseItem
    {
        public ushort id { get; set; }
        public byte plus { get; set; } = 0;
        public Quality quality { get; set; } = 0;
        public Quality qualityMax { get; set; } = 0;
        public ushort progress { get; set; } = 0;
        public short socket1 { get; set; } = -1;
        public short socket2 { get; set; } = -1;
        public short socket3 { get; set; } = -1;
        public short socket4 { get; set; } = -1;
        public ushort durability { get; set; } = 0;
        public uint amount { get; set; } = 1;
        public bool bound { get; set; } = false;
    }
}