namespace Game
{
    [System.Serializable]
    public struct PlayerClassData
    {
        public PlayerClass type;
        public byte rank;

        public ScriptableClass data => ScriptableClass.dict[type];
        public SubclassInfo sub => data.subs[rank];
        public int hp => sub.hp.Get(rank);
        public int mp => sub.mp.Get(rank);
        public int pAtk => sub.pAtk.Get(rank);
        public int pDef => sub.pDef.Get(rank);
        public int mAtk => sub.mAtk.Get(rank);
        public int mDef => sub.mDef.Get(rank);
        public float block => sub.block.Get(rank);
        public float antiBlock => sub.untiBlock.Get(rank);
        public float critRate => sub.crit.Get(rank);
        public float critDmg => sub.critDmg.Get(rank);
        public float antiCrit => sub.untiCrit.Get(rank);
        public PlayerClassData(PlayerClass type, byte rank)
        {
            this.type = type;
            this.rank = rank;
        }
    }
}