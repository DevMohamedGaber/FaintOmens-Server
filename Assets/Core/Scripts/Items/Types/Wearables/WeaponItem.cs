using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Weapon", order=0)]
    public class WeaponItem : EquipmentItem
    {
        [Header("Weapon Info")]
        public WeaponType weaponType;
        [Header("Mastery")]
        public ScriptableSkill reqSkill;
        public byte reqSkillLevel;
        public override bool CanUse(Player player, int inventoryIndex)
        {
            if(!base.CanUse(player, inventoryIndex))
                return false;
            if(reqSkill != null)
            {
                int index = player.skills.IndexOf(reqSkill.id);
                if(index > -1)
                {
                    if(player.skills[index].level < reqSkillLevel)
                    {
                        player.Notify("Insufficint Skill level", "مستوي مهارة غير مناسب");
                        return false;
                    }
                    return true;
                }
                player.Notify("The required skill is missing", "المهارة المطلوبة غير موجودة");
                return false;
            }
            return true;
        }
    }
}