using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Skills/AreaBuff", order=0)]
    public class AreaBuffSkill : BuffSkill
    {
        static Collider[] hitsBuffer = new Collider[10000];

        public override bool CheckTarget(Entity caster)
        {
            // no target necessary, but still set to self so that LookAt(target)
            // doesn't cause the player to look at a target that doesn't even matter
            caster.target = caster;
            return true;
        }

        public override bool CheckDistance(Entity caster, byte skillLevel, out Vector3 destination)
        {
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
                    candidate.health > 0 && // can't buff dead people
                    candidate.GetType() == caster.GetType()) // only on same type
                {
                    candidates.Add(candidate);
                }
            }

            // apply to all candidates
            foreach (Entity candidate in candidates)
            {
                // add buff or replace if already in there
                candidate.AddOrRefreshBuff(new Buff(this, skillLevel));

                // show effect on target
                //SpawnEffect(caster, candidate);
            }
        }
    }
}
