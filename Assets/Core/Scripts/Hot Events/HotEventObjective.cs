namespace Game
{
    [System.Serializable]
    public struct HotEventObjective
    {
        public string type;
        public int amount;
        public HotEventReward[] rewards;
    }
}