namespace Game
{
    [System.Serializable]
    public struct GuildShopItem
    {
        public ScriptableItem item;
        public uint cost;
        public byte level;
    }
}