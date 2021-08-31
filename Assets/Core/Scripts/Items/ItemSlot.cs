using UnityEngine;
namespace Game
{
    [System.Serializable]
    public struct ItemSlot
    {
        public Item item;
        public uint amount;
        public bool isEmpty => amount < 1 || item.id < 1;
        public bool isEquipment => !isEmpty && item.data is EquipmentItem;
        public ItemSlot(Item item, uint amount = 1)
        {
            this.item = item;
            this.amount = amount;
        }
        // helper functions to increase/decrease amount more easily
        // -> returns the amount that we were able to increase/decrease by
        public uint DecreaseAmount(uint reduceBy)
        {
            // as many as possible
            uint limit = (uint)Mathf.Clamp(reduceBy, 0, amount);
            amount -= limit;
            return limit;
        }
        public uint IncreaseAmount(uint increaseBy)
        {
            // as many as possible
            uint limit = (uint)Mathf.Clamp(increaseBy, 0, item.maxStack - amount);
            amount += limit;
            return limit;
        }
    }
}
