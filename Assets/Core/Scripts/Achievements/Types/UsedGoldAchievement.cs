using UnityEngine;
namespace Game.Achievements
{
    [CreateAssetMenu(menuName = "Custom/Achievements/UsedGold", order = 0)]
    public class UsedGoldAchievement : ScriptableAchievement
    {
        public ulong target;
        public bool IsFulfilled(Player player) => player.own.archive.usedGold >= target;
        public override void OnAchieved(Player player)
        {
            base.OnAchieved(player);
            player.inprogressAchievements.usedGold = (UsedGoldAchievement)successor;
        }
    }
}