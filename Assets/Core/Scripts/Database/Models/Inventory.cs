namespace Game.DatabaseModels
{
    public class Inventory : BaseItem
    {
        public uint owner { get; set; }
        public int slot { get; set; }
    }
}