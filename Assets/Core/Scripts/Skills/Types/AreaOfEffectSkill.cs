using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Skills/AOE", order=0)]
    public class AreaOfEffectSkill : DamageSkill
    {
        public LinearFloat effectRange;
        // OverlapSphereNonAlloc array to avoid allocations.
        // -> static so we don't create one per skill
        // -> this is worth it because skills are casted a lot!
        // -> should be big enough to work in just about all cases
        static Collider[] hitsBuffer = new Collider[1000];
        public override bool CheckTarget(Entity caster) => caster.target != null && caster.CanAttack(caster.target);
        public override bool CheckDistance(Entity caster, byte skillLevel, out Vector3 destination)
        {
            // target still around?
            if (caster.target != null) {
                destination = Utils.ClosestPoint(caster.target, caster.transform.position);
                return Utils.ClosestDistance(caster, caster.target) <= castRange.Get(skillLevel);
            }
            destination = caster.transform.position;
            return false;
        }

        public override void Apply(Entity caster, byte skillLevel)
        {
            HashSet<Entity> candidates = new HashSet<Entity>();
            // find all entities of same type in castRange around the caster
            int hits = Physics.OverlapSphereNonAlloc(caster.transform.position, castRange.Get(skillLevel), hitsBuffer);
            for(int i = 0; i < hits; ++i)
            {
                Collider co = hitsBuffer[i];
                Entity candidate = co.GetComponentInParent<Entity>();
                if (candidate != null && candidate.health > 0 && candidate.GetType() == caster.GetType())
                {
                    candidates.Add(candidate);
                }
            }

            // apply to all candidates
            foreach (Entity candidate in candidates)
            {
                caster.DealDamageAt(candidate,
                caster.p_atk + damage.Get(skillLevel)/* + caster.MyAtkType * damageIncreace.Get(skillLevel)*/,
                stunChance.Get(skillLevel),
                stunTime.Get(skillLevel));
            }
        }
    }
}
