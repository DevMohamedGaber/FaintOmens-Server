using UnityEngine;
using Mirror;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Skills/TargetDamage", order=0)]
    public class TargetDamageSkill : DamageSkill
    {
        public override bool CheckTarget(Entity caster)
        {   // target exists, alive, not self, ok type?
            return caster.target != null && caster.CanAttack(caster.target);
        }

        public override bool CheckDistance(Entity caster, byte skillLevel, out Vector3 destination)
        {   // target still around?
            if (caster.target != null)
            {
                destination = Utils.ClosestPoint(caster.target, caster.transform.position);
                return Utils.ClosestDistance(caster, caster.target) <= castRange.Get(skillLevel);
            }
            destination = caster.transform.position;
            return false;
        }
        public override void Apply(Entity caster, byte skillLevel)
        {
            caster.DealDamageAt(caster.target,
                caster.p_atk + damage.Get(skillLevel)/* + caster.MyAtkType * damageIncreace.Get(skillLevel)*/,
                stunChance.Get(skillLevel),
                stunTime.Get(skillLevel));
            //SpawnEffect(caster, caster.target);
        }
    }
}