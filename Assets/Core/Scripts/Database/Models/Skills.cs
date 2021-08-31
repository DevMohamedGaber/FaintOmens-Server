namespace Game.DatabaseModels
{
    public class Skills
    {
        public uint id { get; set; }
        public ushort skill { get; set; }
        public byte level { get; set; }
        public uint experience { get; set; } = 0;
    }
}