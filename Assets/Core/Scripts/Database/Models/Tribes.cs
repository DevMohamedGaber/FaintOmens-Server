namespace Game.DatabaseModels
{
    public class Tribes 
    {
        [SQLite.PrimaryKey]
        public byte id { get; set; }
        public byte rank { get; set; } = 0;
        public ulong wealth { get; set; } = 0;
        public uint totalBR { get; set; } = 0;
        public uint troops { get; set; } = 0;
        public ushort wins { get; set; } = 0;
    }
}