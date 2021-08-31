using UnityEngine;
namespace Game.Achievements
{
    [CreateAssetMenu(menuName = "Custom/Achievements/Popularity", order = 0)]
    public class PopularityAchievement : ScriptableAchievement
    {
        public uint target;
        public bool IsFulfilled(Player player) => player.own.popularity >= target;
        public override void OnAchieved(Player player)
        {
            base.OnAchieved(player);
            player.inprogressAchievements.popularity = (PopularityAchievement)successor;
        }
    }
}