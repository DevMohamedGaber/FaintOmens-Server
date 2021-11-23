using UnityEngine;
namespace Game.StorageData
{
    [System.Serializable]
    public struct Pet
    {
        public GameObject petPrefab;
        public byte lvlCap;
        public ExponentialUInt expMax;
        public float expShare;
        public byte starsCap;
        public ushort pointPerLvl;
        public byte potentialToAP;
        public byte potentialMax;
        public ushort trainItemId;
        public ushort starsUpItemId;
        public ushort upgradeItemId;
        public uint[] starUpItemsCount;
        public uint[] upgradeItemsCount;
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
    }
}