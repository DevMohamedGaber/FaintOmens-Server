namespace Game
{
    [System.Serializable]
    public struct HotEventProgress
    {
        public int id;
        public int progress;
        public int[] completeTimes;

        public HotEventProgress(int id)
        {
            this.id = id;
            this.progress = 0;
            this.completeTimes = new int[]{};
        }
    }
}