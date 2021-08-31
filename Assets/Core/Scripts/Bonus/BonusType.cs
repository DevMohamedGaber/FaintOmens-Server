namespace Game
{
    [System.Serializable]
    public enum BonusType : byte
    {
        hp,
        mp,
        pAtk,
        mAtk,
        pDef,
        mDef,
        crit,
        critRate,
        untiCrit,
        block,
        untiBlock,
        speed
    }
}