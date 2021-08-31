namespace Game
{
    [System.Serializable]
    public struct SubclassInfo
    {
        public byte reqLevel;
        public uint reqBR;
        public byte reqMilitaryRank;
        public ScriptableItemAndAmount[] UpgradeItems;
        public ScriptableQuest startQuest;
        //Bonus
        public ExponentialInt hp;
        public ExponentialInt mp;
        public ExponentialInt pAtk;
        public ExponentialInt pDef;
        public ExponentialInt mAtk;
        public ExponentialInt mDef;
        public ExponentialFloat block;
        public ExponentialFloat untiBlock;
        public ExponentialFloat crit;
        public ExponentialFloat critDmg;
        public ExponentialFloat untiCrit;
    }
}