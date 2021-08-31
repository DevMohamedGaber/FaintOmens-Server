namespace Game
{
    [System.Serializable]
    public struct ItemQualityData
    {
        public Quality current;
        public Quality max;
        public ushort progress;
        public bool isGrowth => current < max;
        public ushort expMax => isGrowth ? Storage.data.item.equipmentQualityExpMax[(int)current] : (ushort)0;
        public ScriptableQuality data => ScriptableQuality.dict[(int)current];
        public ItemQualityData(Quality current, Quality max, ushort progress = 0)
        {
            this.current = current;
            this.max = max;
            this.progress = progress;
        }
        public void AddExp(ushort amount)
        {
            if(amount > 0)
            {
                progress += amount;
                while(isGrowth && progress > expMax)
                {
                    progress -= expMax;
                    current++;
                }
            }
        }
        public void Reset()
        {
            current = Quality.Normal;
            max = Quality.Normal;
            progress = 0;
        }
    }
}