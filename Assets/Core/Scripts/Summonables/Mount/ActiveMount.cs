namespace Game
{
    [System.Serializable]
    public struct ActiveMount
    {
        public ushort id;
        public bool mounted;

        public bool canMount => id > 0;

        public ActiveMount(ushort id = 0)
        {
            this.id = id;
            this.mounted = false;
        }
    }
}