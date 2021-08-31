namespace Game
{
    [System.Serializable]
    public struct GuildJoinInfo
    {
        public uint id;
        public string name;
        public string masterName;
        public byte level;
        public byte requiredLevel;
        public byte membersCount;
        public byte capacity;
        public bool autoAccept;
    }
}