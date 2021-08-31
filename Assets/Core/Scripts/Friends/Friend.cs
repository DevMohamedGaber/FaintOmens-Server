namespace Game
{
    [System.Serializable]
    public struct Friend
    {
        public uint id;
        public string name;
        public PlayerClassData classInfo;
        public byte avatar;
        public byte level;
        public byte tribe;
        public uint br;
        public byte friendship;
        public double lastOnline;
        public bool IsOnline()
        {
            return lastOnline == 0;
        }
    }
}