/*using UnityEngine;
using Mirror;
[RequireComponent(typeof(CapsuleCollider))] // aggro area trigger
public class SkillApplyOnImpact : SkillEffect, CustomSkillEffect {
    public LinearInt damage = new LinearInt{baseValue=1};
    public LinearFloat stunChance; // range [0,1]
    public LinearFloat stunTime; // in seconds
    public bool destroyOnFirstImpact;
    public int level { get; set; }
    void OnTriggerEnter(Collider co) {
        Entity entity = co.GetComponentInParent<Entity>();
        if(entity != null && entity != caster) {
            if(target != null && entity == target && entity.health > 0) {
                Apply(entity);
            }
            else if((entity is Player || entity is Monster) && entity.health > 0) {
                Apply(entity);
            }
        }
    }
    void Apply(Entity entity) {
        int casterAtk = (caster.classType == PlayerType.Physical ? caster.p_atk : caster.m_atk);
        caster.DealDamageAt(entity, casterAtk + damage.Get(level), stunChance.Get(level), stunTime.Get(level));
        if(destroyOnFirstImpact)
            NetworkServer.Destroy(gameObject);
    }
}
public interface CustomSkillEffect {
    int level { get; set; }
}*/