//using UnityEngine.Purchasing;
//using UnityEngine.Purchasing.Security;
namespace Game.Purchase
{
    [System.Serializable]
    public struct Package
    {
        public string name;
        //public ProductType type;
        public float price;
        public float discount;
        public uint diamonds;
        public uint b_diamonds;
        public uint gold;
        public ItemSlot[] items;
    }
}