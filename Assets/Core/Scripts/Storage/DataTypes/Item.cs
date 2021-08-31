using UnityEngine;
namespace Game.StorageData
{
    [System.Serializable]
    public struct Item
    {
        [Header("Plus")]
        public byte maxPlus;
        public byte plusDropsAt;
        public ushort[] plusUpItemId;
        public uint[] plusUpCount;
        [SerializeField] ExponentialUInt plusCostGrowth;
        public uint[] plusUpCost;
        public double plusUpSuccessRateReductionPerPlus;
        public double[] plusUpSuccessRate;
        public LuckCharmItem[] plusLuckCharms;
        [Header("Socket")]
        public ushort unlockSocketItemId;
        public uint gemRemovalFee;
        [Header("Quality")]
        public FeedItem[] qualityFeedItems;
        public ushort[] equipmentQualityExpMax;

        public void OnAwake() {
            int i;
            // plus
            // cost
            plusUpCost = new uint[maxPlus];
            for(i = 0; i < maxPlus; i++) {
                plusUpCost[i] = plusCostGrowth.Get(i);
            }
            // success rate
            plusUpSuccessRate = new double[maxPlus];
            for(i = 0; i < maxPlus; i++) {
                plusUpSuccessRate[i] = 1d - (plusUpSuccessRateReductionPerPlus * (double)i);
            }
        }
    }
}