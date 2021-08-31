using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Clothing", order=999)]
    public class ClothingItem : UsableItem
    {
        [Header("Wardrobe")]
        public ClothingCategory equipCategory;
        public ushort wardrobeId;
        // usage
        /*public override bool CanUse(Player player, int inventoryIndex) {
            return base.CanUse(player, inventoryIndex) && !player.own.wardrop.Has(wardropId);
        }*/
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);

            if(player.wardrobe[(int)equipCategory].isUsed)
            {
                Item oldItem = new Item {
                    id = player.wardrobe[(int)equipCategory].data.itemId, 
                    plus = player.wardrobe[(int)equipCategory].plus
                };
                if(!player.InventoryAdd(oldItem, 1))
                {
                    player.NotifyNotEnoughInventorySpace();
                    return;
                }
            }
            if(player.own.wardrobe.IndexOf(wardrobeId) == -1)
                player.own.wardrobe.Add(wardrobeId);
            
            WardrobeItem newItem = player.wardrobe[(int)equipCategory];
            newItem.id = wardrobeId;
            newItem.plus = (byte)player.own.inventory[inventoryIndex].item.plus;
            player.wardrobe[(int)equipCategory] = newItem;

            ItemSlot slot = player.own.inventory[inventoryIndex];
            slot.DecreaseAmount(1);
            player.own.inventory[inventoryIndex] = slot;
        }
    }
}