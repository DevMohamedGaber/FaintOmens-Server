using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="uMMORPG Quest/Location Quest", order=999)]
    public class LocationQuest : ScriptableQuest
    {
        [Header("Fulfillment")]
        public string locationName;
        public override void OnLocation(Player player, int questIndex, Collider location)
        {
            if (location.name == name.ToString())
            {
                Quest quest = player.own.quests[questIndex];
                quest.progress = 1;
                player.own.quests[questIndex] = quest;
            }
        }
        public override bool IsFulfilled(Player player, Quest quest)
        {
            return quest.progress == 1;
        }
    }
}