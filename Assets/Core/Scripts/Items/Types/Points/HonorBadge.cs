using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Points/Honor", order=0)]
    public class HonorBadge : UsableItem
    {
        public uint honor = 1;
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);
            
            if(player.AddHonor(honor * amount, true))
            {
                ItemSlot slot = player.own.inventory[inventoryIndex];
                slot.DecreaseAmount(amount);
                player.own.inventory[inventoryIndex] = slot;
            }
            else player.Notify("Can't get more honor for today", "لا يمكنك اكتساب المزيد من نقاط الشرف لليوم");
        }
    }
}