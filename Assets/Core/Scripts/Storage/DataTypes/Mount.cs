namespace Game.StorageData
{
    [System.Serializable]
    public struct Mount
    {
        public byte lvlCap;
        public ExponentialUInt expMax;
        public float expShare;
        public byte starsCap;
        public ushort pointPerLvl;
        public ushort starsUpItemId;
        public ushort upgradeItemId;
        public uint[] starUpItemsCount;
        public uint[] upgradeItemsCount;
        public FeedItem[] feeds;
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
    }
}