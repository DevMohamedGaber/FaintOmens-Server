namespace Game.StorageData
{
    [System.Serializable]
    public struct Guild
    {
        public int minJoinLevel;
        public int capacity;
        public int capacityIncresePerLevel;
        public int maxLevel;
        public int skillsMaxLevel;
        public int maxElites;
        public uint creationPriceGold;
        public uint goldToContribution;
        public uint diamondToContribution;
        public int maxNoticeLength;
        public int maxNameLength;
        public ExponentialUInt expMaxDynamic;
        public uint[] expMax;
        public GuildAssets[] hallUpgradeReqs;
        public GuildRank inviteMinRank;
        public GuildRank kickMinRank;
        public GuildRank promoteMinRank;
        public GuildRank recallMinRank;
        public GuildRank notifyMinRank;
        public void OnAwake()
        {
            expMax = new uint[maxLevel];
            for(int i = 0; i < maxLevel; i++)
            {
                expMax[i] = expMaxDynamic.Get(i);
            }
        }
    }
}