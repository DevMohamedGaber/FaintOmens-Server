namespace Game
{
    [System.Serializable]
    public struct MarriageProposal
    {
        public uint id;
        public string name;
        public byte avatar;
        public byte level;
        public PlayerClassData classInfo;
        public uint br;
        public string guildName;
        public MarriageType type;
    }
}