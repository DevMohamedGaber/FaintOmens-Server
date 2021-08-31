using UnityEngine;
using Mirror;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Skills/Passive", order=0)]
    public class PassiveSkill : BonusSkill
    {
        public override bool CheckTarget(Entity caster)
        {
            return false;
        }
        public override bool CheckDistance(Entity caster, byte skillLevel, out Vector3 destination)
        {
            destination = caster.transform.position;
            return false;
        }
        public override void Apply(Entity caster, byte skillLevel) {}
    }
}
