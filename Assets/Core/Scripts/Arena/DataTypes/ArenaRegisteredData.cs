namespace Game.Arena
{
    [System.Serializable]
    public struct ArenaRegisteredData
    {
        public uint id;
        public byte level;
        public ArenaRegisteredData(uint id, byte level)
        {
            this.id = id;
            this.level = level;
        }
    }
}