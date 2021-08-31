using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Quests/Craft", order=999)]
    public class CraftQuest : ScriptableQuest
    {
        [Header("Fulfillment")]
        public ScriptableItem craftItem;
        public uint craftAmount;
        public override void OnCrafted(Player player, int questIndex, ScriptableItem item, uint amount)
        {
            if(item.name == craftItem.name)
            {
                Quest quest = player.own.quests[questIndex];
                if(quest.progress + amount >= craftAmount)
                {
                    quest.progress = craftAmount;
                }
                else
                {
                    quest.progress += amount;
                }
                player.own.quests[questIndex] = quest;
            }
        }
        public override bool IsFulfilled(Player player, Quest quest)
        {
            return craftItem != null && quest.progress == craftAmount;
        }
    }
}