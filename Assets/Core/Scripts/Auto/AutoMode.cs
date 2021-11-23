namespace Game
{
    [System.Serializable]
    public struct AutoMode
    {
        public bool on;
        public int lastskill;
        public float followDistance;
        public bool collectGold;
        public bool collectitems;
        public double hpRecovery;
        public double manaRecovery;
        public string[] hpRecoveryPotions;
        public string[] manaRecoveryPotions;
    }
}