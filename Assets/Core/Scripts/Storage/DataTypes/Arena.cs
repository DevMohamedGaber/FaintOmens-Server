namespace Game.StorageData
{
    [System.Serializable]
    public struct Arena
    {
        public byte minLvl;
        public byte lvlDiff;
        public float cancelIfNotReadyTime;
        public float endIfNotReadyTime;
        public float startTime;
        public int matchDurationInMins;
        public float wrapUpTime;
        public byte pointsOnWin;
        public byte pointsOnLoss;
        public ushort dailyPoints;
    }
}