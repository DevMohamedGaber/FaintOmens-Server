using Game.Achievements;
namespace Game.StorageData
{
    [System.Serializable]
    public struct DefaultAchievements
    {
        public AchievementPointsAchievement achievPnts;
        public PlayerLevelAchievement playerLevel;
        public GainedGoldAchievement gainedGold;
        public UsedGoldAchievement usedGold;
        public HonorAchievement honor;
        public PopularityAchievement popularity;
    }
}