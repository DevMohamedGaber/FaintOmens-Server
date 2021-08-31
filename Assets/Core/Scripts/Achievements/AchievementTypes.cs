namespace Game.Achievements
{
    [System.Serializable]
    public enum AchievementTypes : byte
    {
        AchievementPoints,
        PlayerLevel,
        GainedGold,
        UsedGold,
        GainedDiamonds,
        UsedDiamonds,
        GainedBDiamonds,
        UsedBDiamonds,
        Popularity,
        Honor
    }
}