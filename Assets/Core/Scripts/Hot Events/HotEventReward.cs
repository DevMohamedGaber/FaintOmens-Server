namespace Game
{
    [System.Serializable]
    public struct HotEventReward
    {
        public string type;
        public int amount;
        public HotEventReward(string type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }
    }
}