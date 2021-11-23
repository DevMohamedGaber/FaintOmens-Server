namespace Game.StorageData
{
    [System.Serializable]
    public struct Mount
    {
        public byte lvlCap;
        public ExponentialUInt expMaxRate;
        public ExponentialUShort trainingExpMaxRate;
        public ushort trainingExpPerItem;
        public float expShare;
        public byte starsCap;
        public ushort pointPerLvl;
        public ushort starsUpItemId;
        public ushort upgradeItemId;
        public ushort trainItemId;
        public uint[] starUpItemsCount;
        public uint[] upgradeItemsCount;
        public uint[] expMax;
        public ushort[] trainingExpMax;
        public FeedItem[] feeds;
        public float savedBonus;
        public FeedItem GetFeed(int id)
        {
            if(feeds.Length > 0)
            {
                for(int i = 0; i < feeds.Length; i++)
                {
                    if(feeds[i].name == id)
                        return feeds[i];
                }
            }
            return feeds[0];
        }
        public void OnAwake()
        {
            int i;
            trainingExpMax = new ushort[lvlCap - 1];
            for (i = 0; i < trainingExpMax.Length; i++)
            {
                trainingExpMax[i] = trainingExpMaxRate.Get(i + 1);
            }
            expMax = new uint[lvlCap - 1];
            for (i = 0; i < expMax.Length; i++)
            {
                expMax[i] = expMaxRate.Get(i + 1);
            }
        }
    }
}