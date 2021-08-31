namespace Game
{
    [System.Serializable]
    public struct GuildJoinRequest
    {
        public uint id;
        public string name;
        public byte level;
        public uint br;
        public double sent;
    }
}