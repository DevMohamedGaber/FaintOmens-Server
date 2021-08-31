using UnityEngine;
namespace Game.Achievements
{
    [CreateAssetMenu(menuName = "Custom/Achievements/Honor", order = 0)]
    public class HonorAchievement : ScriptableAchievement
    {
        public uint target;
        public bool IsFulfilled(Player player) => player.own.TotalHonor >= target;
        public override void OnAchieved(Player player)
        {
            base.OnAchieved(player);
            player.inprogressAchievements.honor = (HonorAchievement)successor;
        }
    }
}