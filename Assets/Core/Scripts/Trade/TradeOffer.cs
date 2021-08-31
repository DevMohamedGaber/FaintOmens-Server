namespace Game
{
    [System.Serializable]
    public struct TradeOffer
    {
        public uint id;
        public TradeOfferContent content;
        public byte confirms;
        public bool accepted;
        public TradeOffer(uint id)
        {
            this.id = id;
            content = new TradeOfferContent();
            confirms = 0;
            accepted = false;
        }
    }
}