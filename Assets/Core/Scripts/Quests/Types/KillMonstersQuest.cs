using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Quests/KillMonsters", order=0)]
    public class KillMonstersQuest : ScriptableQuest
    {
        [Header("Fulfillment")]
        public Monster killTarget;
        public uint killAmount;
        public override void OnKilled(Player player, int questIndex, Entity victim)
        {
            // not done yet, and same name as prefab? (hence same monster?)
            Quest quest = player.own.quests[questIndex];
            if (quest.progress < killAmount && victim.name == killTarget.name)
            {
                // increase int field in quest (up to 'amount')
                ++quest.progress;
                player.own.quests[questIndex] = quest;
            }
        }
        public override bool IsFulfilled(Player player, Quest quest)
        {
            return quest.progress >= killAmount;
        }
    }
}
