namespace Game
{
    [System.Serializable]
    public struct TeleportNPCOffer
    {
        public int city;
        public uint cost;
        public TeleportNPCOffer(int city = -1, uint cost = 0)
        {
            this.city = city;
            this.cost = cost;
        }
    }
}