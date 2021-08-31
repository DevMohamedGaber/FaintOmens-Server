using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Points/Exp", order=0)]
    public class ExpOrb : UsableItem
    {
        public uint experience;
        // usage
        public override bool CanUse(Player player, int inventoryIndex)
        {
            return base.CanUse(player, inventoryIndex) && player.level < Storage.data.player.lvlCap;
        }
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);
            
            ItemSlot slot = player.own.inventory[inventoryIndex];
            slot.DecreaseAmount(amount);
            player.own.inventory[inventoryIndex] = slot;

            player.AddExp(experience * (uint)amount);
        }
    }
}