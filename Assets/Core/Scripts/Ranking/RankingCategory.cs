namespace Game
{
    [System.Serializable]
    public enum RankingCategory : byte
    {
        // player
        PlayerBR,
        PlayerLevel,
        PlayerHonor,

        //guild
        GuildBR,
        GuildLevel,

        //tribe
        TribeBR,
        TribeWins,

        //summonables
        PetBR,
        PetLvl,
        MountBR,
        MountLvl 
    }
}