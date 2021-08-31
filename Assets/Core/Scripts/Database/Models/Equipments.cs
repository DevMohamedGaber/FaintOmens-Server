namespace Game.DatabaseModels
{
    public class Equipments : BaseItem
    {
        public uint owner { get; set; }
        public int slot { get; set; }
    }
}