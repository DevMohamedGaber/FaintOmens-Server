namespace Game
{
    [System.Serializable]
    public struct MailItemSlot
    {
        public Item item;
        public uint amount;
        [UnityEngine.HideInInspector] public bool recieved;
        public MailItemSlot(Item item, uint amount, bool recieved = false)
        {
            this.item = item;
            this.amount = amount;
            this.recieved = recieved;
        }
        public MailItemSlot(ItemSlot data, bool recieved = false)
        {
            item = data.item;
            amount = data.amount;
            this.recieved = recieved;
        }
    }
}