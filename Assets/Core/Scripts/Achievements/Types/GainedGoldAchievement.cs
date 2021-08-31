using UnityEngine;
namespace Game.Achievements
{
    [CreateAssetMenu(menuName = "Custom/Achievements/GainedGold", order = 0)]
    public class GainedGoldAchievement : ScriptableAchievement
    {
        public ulong target;
        public bool IsFulfilled(Player player) => player.own.archive.gainedGold >= target;
        public override void OnAchieved(Player player)
        {
            base.OnAchieved(player);
            player.inprogressAchievements.gainedGold = (GainedGoldAchievement)successor;
        }
    }
}