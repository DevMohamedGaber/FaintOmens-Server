namespace Game
{
    [System.Serializable]
    public struct GuildInvitation
    {
        public uint id;
        public string name;
        public byte level;
        public uint senderId;
        public string senderName;
    }
}