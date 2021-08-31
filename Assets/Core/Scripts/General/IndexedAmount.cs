namespace Game
{
    [System.Serializable]
    public struct IndexedAmount
    {
        public int index;
        public uint amount;
        public IndexedAmount(int index, uint amount)
        {
            this.index = index;
            this.amount = amount;
        }
    }
}