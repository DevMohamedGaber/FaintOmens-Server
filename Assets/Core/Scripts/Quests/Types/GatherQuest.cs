using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Quests/GatherItem", order=0)]
    public class GatherQuest : ScriptableQuest
    {
        [Header("Fulfillment")]
        public ScriptableItem gatherItem;
        public uint gatherAmount;
        public override bool IsFulfilled(Player player, Quest quest)
        {
            return gatherItem != null && player.InventoryCount(new Item(gatherItem)) >= gatherAmount;
        }
        public override void OnCompleted(Player player, Quest quest)
        {
            base.OnCompleted(player, quest);
            
            if (gatherItem != null)
            {
                player.InventoryRemove(gatherItem.name, gatherAmount);
            }
        }
    }
}