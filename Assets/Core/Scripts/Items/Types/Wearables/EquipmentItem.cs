/* remove the original class in uMmorpg */
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Equipment", order=0)]
    public class EquipmentItem : UsableItem
    {
        [Header("Equipment")]
        public Gender gender = Gender.Any;
        public EquipmentsCategory category;
        public ushort durability = 20;
        [Header("Bonus")]
        public EquipmentBonus health;
        public EquipmentBonus mana;
        public EquipmentBonus PAtk;
        public EquipmentBonus PDef;
        public EquipmentBonus MAtk;
        public EquipmentBonus MDef;
        public EquipmentFloatBonus critRate;
        public EquipmentFloatBonus critDmg;
        public EquipmentFloatBonus antiCrit;
        public EquipmentFloatBonus block;
        public EquipmentFloatBonus antiBlock;

        public override bool CanUse(Player player, int inventoryIndex)
        {
            if(reqClass == PlayerClass.Any && gender == Gender.Any) 
                return true;
            if(player.classInfo.type != reqClass)
            {
                player.Notify("Unmatched Class", "تخصص غير مناسب");
                return false;
            }
            if(player.model.gender != gender)
            {
                player.Notify("Unmatched Gender", "النوع غير مناسب");
                return false;
            }
            return true;
        }
        public bool CanEquip(Player player, int inventoryIndex, int equipmentIndex)
        {
            return base.CanUse(player, inventoryIndex) && category == (EquipmentsCategory)equipmentIndex;
        }
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            // always call base function too
            base.Use(player, inventoryIndex);
            player.SwapInventoryEquip(inventoryIndex, (int)category);
        }
    }
}