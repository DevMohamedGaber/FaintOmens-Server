namespace Game
{
    [System.Serializable] 
    public struct GuildSkill
    {
        public int baseBonus;
        public int bonusPerLevel;
        public int lvlCost;

        public int Get(int level)
        {
            if(level > 0)
            {
                return baseBonus + (bonusPerLevel * level);
            }
            return 0;
        }
    }
}