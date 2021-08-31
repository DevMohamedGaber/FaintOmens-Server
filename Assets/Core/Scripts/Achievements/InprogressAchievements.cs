namespace Game.Achievements
{
    [System.Serializable]
    public struct InprogressAchievements
    {
        public AchievementPointsAchievement achievementPoints;
        public PlayerLevelAchievement playerLevel;
        public GainedGoldAchievement gainedGold;
        public UsedGoldAchievement usedGold;
        public HonorAchievement honor;
        public PopularityAchievement popularity;

        public InprogressAchievements(Player player)
        {
            int achPnts = -1;
            int pLvl = -1;
            int gGold = -1;
            int uGold = -1;
            int hnr = -1;
            int pplrty = -1;
            // get last achieved
            if(player.own.achievements.Count > 0)
            {
                SyncListAchievements data = player.own.achievements;
                for(int i = 0; i < data.Count; i++)
                {
                    switch(data[i].data.type)
                    {
                        case AchievementTypes.AchievementPoints:
                            achPnts = data[i].id;
                            break;
                        case AchievementTypes.PlayerLevel:
                            pLvl = data[i].id;
                            break;
                        case AchievementTypes.GainedGold:
                            gGold = data[i].id;
                            break;
                        case AchievementTypes.UsedGold: 
                            uGold = data[i].id;
                            break;
                        case AchievementTypes.Honor:
                            hnr = data[i].id;
                            break;
                        case AchievementTypes.Popularity:
                            pplrty = data[i].id;
                            break;
                    }
                }
            }
            // set inprogress achievement
            achievementPoints = achPnts != -1 ? (AchievementPointsAchievement)ScriptableAchievement.dict[achPnts].successor : Storage.data.achievements.achievPnts;
            playerLevel = pLvl != -1 ? (PlayerLevelAchievement)ScriptableAchievement.dict[pLvl].successor : Storage.data.achievements.playerLevel;
            gainedGold = gGold != -1 ? (GainedGoldAchievement)ScriptableAchievement.dict[gGold].successor : Storage.data.achievements.gainedGold;
            usedGold = uGold != -1 ? (UsedGoldAchievement)ScriptableAchievement.dict[uGold].successor : Storage.data.achievements.usedGold;
            honor = hnr != -1 ? (HonorAchievement)ScriptableAchievement.dict[hnr].successor : Storage.data.achievements.honor;
            popularity = pplrty != -1 ? (PopularityAchievement)ScriptableAchievement.dict[pplrty].successor : Storage.data.achievements.popularity;
        
            //check if current is fullfiled or not
            
        }
    }
}