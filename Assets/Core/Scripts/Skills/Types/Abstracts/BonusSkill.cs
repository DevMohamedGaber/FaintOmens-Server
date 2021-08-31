using UnityEngine;
namespace Game
{
    public abstract class BonusSkill : ScriptableSkill
    {
        public LinearInt healthMaxBonus;
        public LinearInt manaMaxBonus;
        public LinearInt damageBonus;
        public LinearInt defenseBonus;
        public LinearFloat blockChanceBonus; // range [0,1]
        public LinearFloat criticalChanceBonus; // range [0,1]
        public LinearFloat healthPercentPerSecondBonus; // 0.1=10%; can be negative too
        public LinearFloat manaPercentPerSecondBonus; // 0.1=10%; can be negative too
        public LinearFloat speedBonus; // can be negative too
        public LinearFloat expBonus;
    }
}
