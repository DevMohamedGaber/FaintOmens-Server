using System.Text;
using UnityEngine;
namespace Game
{
    public enum RecallTypes : byte {
        Guild,
        Tribe
    }
    [CreateAssetMenu(menuName="Custom/Items/Recall", order=999)]
    public class RecallItem : UsableItem
    {
        [Header("info")]
        public RecallTypes recallType;

        // usage
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            // always call base function too
            base.Use(player, inventoryIndex);

            if(recallType == RecallTypes.Tribe) {
                if(TribeSystem.Recall(player)) 
                    RemoveItem(player, inventoryIndex);
            }
            if(recallType == RecallTypes.Guild) {
                if(GuildSystem.Recall(player)) 
                    RemoveItem(player, inventoryIndex);
            }
        }
        protected void RemoveItem(Player player, int inventoryIndex) {
            ItemSlot slot = player.own.inventory[inventoryIndex];
            slot.DecreaseAmount(1);
            player.own.inventory[inventoryIndex] = slot;
        }
    }
}