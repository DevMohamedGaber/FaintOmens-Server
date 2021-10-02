namespace Game
{
    [System.Serializable]
    public struct MountTrainingAttribute
    {
        public byte _level;
        public ushort exp;

        public int level
        {
            set
            {
                _level = (byte)value;
            }
            get
            {
                return (int)_level;
            }
        }
        ushort expMax => Storage.data.mount.trainingExpMax[level];

        public MountTrainingAttribute(byte level, ushort exp)
        {
            this._level = level;
            this.exp = exp;
        }
        public void AddExp(ushort expValue)
        {
            if(exp < 1)
                return;
            exp += expValue;
            while(exp >= expMax)
            {
                exp -= expMax;
                _level++;
            }
        }
    }
}