namespace Game.StorageData
{
    [System.Serializable]
    public struct Player
    {
        public byte lvlCap;
        public ushort dailyHonor;
        [UnityEngine.SerializeField] ExponentialUInt expMaxDynamic;
        public uint[] expMax;
        public ushort pointsPerLevel;
        public byte maxInventorySize;
        public int chatMsgMaxLength;
        public ushort inplaceReviveItemId;
        public byte freeInplaceRevives;
        public uint inplaceReviveCost;
        public byte equipmentCount;
        public byte accessoriesCount;
        public int skillExpToLevelDiff;
        public int maxRandomSpawnIteration;
        public float combatLogoutDelay;
        public void OnAwake()
        {
            expMax = new uint[lvlCap];
            for(int i = 0; i < lvlCap; i++)
            {
                expMax[i] = expMaxDynamic.Get(i + 1);
            }
        }
    }
}