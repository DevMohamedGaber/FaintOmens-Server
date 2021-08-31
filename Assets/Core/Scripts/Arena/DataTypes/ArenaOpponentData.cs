namespace Game.Arena
{
    [System.Serializable]
    public struct ArenaOpponentData
    {
        public string Name;
        public byte level;
        public PlayerClassData classInfo;
        public Gender gender;
        public byte tribeId;
        public byte avatar;
    }
}