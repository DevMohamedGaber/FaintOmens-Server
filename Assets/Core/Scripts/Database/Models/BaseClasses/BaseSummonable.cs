namespace Game.DatabaseModels
{
    public class BaseSummonable
    {
        public uint owner { get; set; }
        public ushort id { get; set; }
        public byte level { get; set; } = 1;
        public uint experience { get; set; } = 0;
        public byte stars { get; set; } = 0;
        public Tier tier { get; set; } = Tier.F;
        public ushort vitality { get; set; } = 0;
        public ushort strength { get; set; } = 0;
        public ushort intelligence { get; set; } = 0;
        public ushort endurance { get; set; } = 0;
        public uint br { get; set; } = 0;
        public SummonableStatus status { get; set; } = SummonableStatus.Saved;
    }
}