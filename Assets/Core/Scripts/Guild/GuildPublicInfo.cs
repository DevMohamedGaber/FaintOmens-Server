namespace Game
{
    [System.Serializable]
    public struct GuildPublicInfo
    {
        public static GuildPublicInfo Empty = new GuildPublicInfo();
        public uint id;
        public string name;
        public Guild data => GuildSystem.guilds[id];
        public GuildPublicInfo(uint id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}