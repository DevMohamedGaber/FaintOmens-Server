namespace Game
{
    [System.Serializable]
    public struct TeamInvitation
    {
        public uint id;
        public uint senderId;
        public string senderName;
        public byte senderLevel;
    }
}