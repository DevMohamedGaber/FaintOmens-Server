namespace Game.DatabaseModels
{
    public class Accessories : BaseItem
    {
        public uint owner { get; set; }
        public int slot { get; set; }
    }
}