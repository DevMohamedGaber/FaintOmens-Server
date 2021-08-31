namespace Game
{
    [System.Serializable]
    public struct TradeOfferContent
    {
        public static TradeOfferContent Empty = new TradeOfferContent();
        public IndexedAmount[] items;
        public uint gold;
        public uint diamonds;
        public bool IsValid(Player player)
        {
            if(gold > 0 && player.own.gold < gold)
            {
                player.NotifyNotEnoughGold();
                return false;
            }
            if(diamonds > 0 && player.own.diamonds < diamonds)
            {
                player.NotifyNotEnoughDiamonds();
                return false;
            }
            if(items.Length > 0)
            {
                for(int i = 0; i < items.Length; i++)
                {
                    if(items[i].index < 0 || items[i].index > player.own.inventorySize)
                    {
                        player.Notify("invalid item selected");
                        return false;
                    }
                    ItemSlot slot = player.own.inventory[items[i].index];
                    if(slot.isEmpty)
                    {
                        player.Notify("invalid item selected");
                        return false;
                    }
                    if(slot.item.bound)
                    {
                        player.Notify("Cant trade bound items");
                        return false;
                    }
                    if(slot.amount < items[i].amount)
                    {
                        player.Notify("you have selected invalid amount of an item");
                        return false;
                    }
                }
            }
            return true;
        }
        public ItemSlot[] GetItemSlots(Player player)
        {
            ItemSlot[] result = new ItemSlot[items.Length];
            if(items.Length > 0)
            {
                for(int i = 0; i < items.Length; i++)
                {
                    result[i].item = player.own.inventory[items[i].index].item;
                    result[i].amount = items[i].amount;
                }
            }
            return result;
        }
    }
}