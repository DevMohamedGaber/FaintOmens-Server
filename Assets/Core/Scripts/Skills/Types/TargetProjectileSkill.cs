/*using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName="Custom/Skills/TargetProjectile", order=0)]
public class TargetProjectileSkill : DamageSkill
{
    [Header("Projectile")]
    public ProjectileSkillEffect projectile;*/ // Arrows, Bullets, Fireballs, ...

    /*bool HasRequiredWeaponAndAmmo(Entity caster)
    {
        int weaponIndex = caster.GetEquippedWeaponIndex();
        if (weaponIndex != -1)
        {
            // no ammo required, or has that ammo equipped?
            WeaponItem itemData = (WeaponItem)caster.equipment[weaponIndex].item.data;
            return itemData.requiredAmmo == null ||
                   caster.GetEquipmentIndexByName(itemData.requiredAmmo.name) != -1;
        }
        return false;
    }

    void ConsumeRequiredWeaponsAmmo(Entity caster)
    {
        int weaponIndex = caster.GetEquippedWeaponIndex();
        if (weaponIndex != -1)
        {
            // no ammo required, or has that ammo equipped?
            WeaponItem itemData = (WeaponItem)caster.equipment[weaponIndex].item.data;
            if (itemData.requiredAmmo != null)
            {
                int ammoIndex = caster.GetEquipmentIndexByName(itemData.requiredAmmo.name);
                if (ammoIndex != 0)
                {
                    // reduce it
                    ItemSlot slot = caster.equipment[ammoIndex];
                    --slot.amount;
                    caster.equipment[ammoIndex] = slot;
                }
            }
        }
    }*/

    /*public override bool CheckSelf(Entity caster, byte skillLevel)
    {
        // check base and ammo
        return base.CheckSelf(caster, skillLevel)/* &&
               HasRequiredWeaponAndAmmo(caster);
    }

    public override bool CheckTarget(Entity caster)
    {
        // target exists, alive, not self, oktype?
        return caster.target != null && caster.CanAttack(caster.target);
    }

    public override bool CheckDistance(Entity caster, byte skillLevel, out Vector3 destination)
    {
        // target still around?
        if (caster.target != null)
        {
            destination = Utils.ClosestPoint(caster.target, caster.transform.position);
            return Utils.ClosestDistance(caster, caster.target) <= castRange.Get(skillLevel);
        }
        destination = caster.transform.position;
        return false;
    }
    public override void Apply(Entity caster, byte skillLevel) {
        //ConsumeRequiredWeaponsAmmo(caster);// consume ammo if needed
        if (projectile != null) {
            GameObject go = Instantiate(projectile.gameObject, caster.collider.bounds.center, caster.transform.rotation);
            ProjectileSkillEffect effect = go.GetComponent<ProjectileSkillEffect>();
            effect.target = caster.target;
            effect.caster = caster;
            //effect.damage = caster.attack + damage.Get(skillLevel);
            effect.stunChance = stunChance.Get(skillLevel) - caster.target.untiStunChance;
            effect.stunTime = stunTime.Get(skillLevel);
            NetworkServer.Spawn(go);
        }
        else Debug.LogWarning(name + ": missing projectile");
    }
}
*/