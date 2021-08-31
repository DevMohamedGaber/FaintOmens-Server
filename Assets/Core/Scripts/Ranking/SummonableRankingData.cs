namespace Game
{
    [System.Serializable]
    public struct SummonableRankingData
    {
        //public int id; //NOTE: add when (preview pet/mount/equipments) is added
        public ushort prefab;
        public Tier tier;
        public uint value;
        public uint ownerId;
        public string ownerName;
    }
}