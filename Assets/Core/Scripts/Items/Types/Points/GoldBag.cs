using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Points/Gold", order=0)]
    public class GoldBag : UsableItem
    {
        public uint gold = 1;
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);
            
            ItemSlot slot = player.own.inventory[inventoryIndex];
            slot.DecreaseAmount(amount);
            player.own.inventory[inventoryIndex] = slot;

            player.AddGold(gold * amount);
        }
    }
}