using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Mount", order=0)]
    public class MountItem : UsableItem
    {
        public ushort mountId;
        // usage
        public override bool CanUse(Player player, int inventoryIndex)
        {
            return base.CanUse(player, inventoryIndex) && !player.own.mounts.Has(mountId);
        }
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);
            
            MountSystem.Activate(player, (ushort)name);
        }
    }
}