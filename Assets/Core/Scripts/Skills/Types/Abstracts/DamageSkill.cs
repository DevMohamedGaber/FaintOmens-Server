using UnityEngine;
namespace Game
{
    public abstract class DamageSkill : ScriptableSkill
    {
        [Header("Damage")]
        public LinearInt damage = new LinearInt{baseValue=1};
        public LinearFloat stunChance; // range [0,1]
        public LinearFloat stunTime; // in seconds
        public LinearFloat damageIncreace = new LinearFloat{baseValue=0f};
    }
}