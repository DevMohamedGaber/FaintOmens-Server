using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Skills/AreaHeal", order=0)]
    public class AreaHealSkill : HealSkill
    {
        // OverlapSphereNonAlloc array to avoid allocations.
        // -> static so we don't create one per skill
        // -> this is worth it because skills are casted a lot!
        // -> should be big enough to work in just about all cases
        static Collider[] hitsBuffer = new Collider[10000];

        public override bool CheckTarget(Entity caster) {
            // no target necessary, but still set to self so that LookAt(target)
            // doesn't cause the player to look at a target that doesn't even matter
            caster.target = caster;
            return true;
        }

        public override bool CheckDistance(Entity caster, byte skillLevel, out Vector3 destination) {
            // can cast anywhere
            destination = caster.transform.position;
            return true;
        }

        public override void Apply(Entity caster, byte skillLevel)
        {
            // candidates hashset to be 100% sure that we don't apply an area skill
            // to a candidate twice. this could happen if the candidate has more
            // than one collider (which it often has).
            HashSet<Entity> candidates = new HashSet<Entity>();

            // find all entities of same type in castRange around the caster
            int hits = Physics.OverlapSphereNonAlloc(caster.transform.position, castRange.Get(skillLevel), hitsBuffer);
            for (int i = 0; i < hits; ++i)
            {
                Collider co = hitsBuffer[i];
                Entity candidate = co.GetComponentInParent<Entity>();
                if (candidate != null &&
                    candidate.health > 0 && // can't heal dead people
                    candidate.GetType() == caster.GetType()) // only on same type
                {
                    candidates.Add(candidate);
                }
            }

            // apply to all candidates
            foreach (Entity candidate in candidates)
            {
                candidate.health += healsHealth.Get(skillLevel);
                candidate.mana += healsMana.Get(skillLevel);

                // show effect on candidate
                //SpawnEffect(caster, candidate);
            }
        }
    }
}
