using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Points/Flower", order=0)]
    public class PopularityFlower : UsableItem
    {
        public uint points;
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);
            
            ItemSlot slot = player.own.inventory[inventoryIndex];
            slot.DecreaseAmount(amount);
            player.own.inventory[inventoryIndex] = slot;

            player.AddPopularity(points * amount);
        }
    }
}