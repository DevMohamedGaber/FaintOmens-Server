using UnityEngine;
namespace Game
{
    public abstract class BuffSkill : BonusSkill
    {
        public LinearFloat buffTime = new LinearFloat{baseValue=60};
        [Tooltip("Some buffs should remain after death, e.g. exp scrolls.")]
        public bool remainAfterDeath;
    }
}
