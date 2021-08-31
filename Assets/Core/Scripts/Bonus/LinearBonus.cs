namespace Game
{
    [System.Serializable]
    public struct LinearBonus
    {
        public LinearInt health;
        public LinearInt mana;
        public LinearInt pAtk;
        public LinearInt pDef;
        public LinearInt mAtk;
        public LinearInt mDef;
        public LinearFloat block;
        public LinearFloat untiBlock;
        public LinearFloat crit;
        public LinearFloat critDmg;
        public LinearFloat untiCrit;
    }
}