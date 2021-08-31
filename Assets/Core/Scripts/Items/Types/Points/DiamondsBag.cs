using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Points/Diamonds", order=0)]
    public class DiamondsBag : UsableItem
    {
        public uint diamonds = 1;
        public bool bound;
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);
            
            ItemSlot slot = player.own.inventory[inventoryIndex];
            slot.DecreaseAmount(amount);
            player.own.inventory[inventoryIndex] = slot;

            if(bound)   player.AddBDiamonds(diamonds * (uint)amount);
            else        player.AddDiamonds(diamonds * (uint)amount);
        }
    }
}