using UnityEngine;
namespace Game.Achievements
{
    [CreateAssetMenu(menuName = "Custom/Achievements/AchievementPoints", order = 0)]
    public class AchievementPointsAchievement : ScriptableAchievement
    {
        public ushort target;
        public bool IsFulfilled(Player player) => player.own.archive.achievementPoints >= target;
        public override void OnAchieved(Player player)
        {
            base.OnAchieved(player);
            player.inprogressAchievements.achievementPoints = (AchievementPointsAchievement)successor;
        }
    }
}