namespace Game
{
    [System.Serializable]
    public struct ChatSenderInfo
    {
        public static ChatSenderInfo Empty = new ChatSenderInfo();
        public uint id;
        public string name;
        public byte avatar;
        public byte frame;
        public byte vip;
        public ChatSenderInfo(uint id = 0, string name ="", byte avatar = 0, byte frame = 0, byte vip = 0) {
            this.id = id;
            this.name = name;
            this.avatar = avatar;
            this.frame = frame;
            this.vip = vip;
        }
    }
}