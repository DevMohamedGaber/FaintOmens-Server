using UnityEngine;
namespace Game.Achievements
{
    [CreateAssetMenu(menuName = "Custom/Achievements/PlayerLevel", order = 0)]
    public class PlayerLevelAchievement : ScriptableAchievement
    {
        public byte target;
        public bool IsFulfilled(Player player) => player.level >= target;
        public override void OnAchieved(Player player)
        {
            base.OnAchieved(player);
            player.inprogressAchievements.playerLevel = (PlayerLevelAchievement)successor;
        }
    }
}