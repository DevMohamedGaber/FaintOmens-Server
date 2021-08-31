using UnityEngine;
using System.Collections.Generic;
using System;
namespace Game
{
    [Serializable]
    public struct Item {
        public ushort id;
        public ItemQualityData quality;
        public byte plus;
        public Socket socket1;
        public Socket socket2;
        public Socket socket3;
        public Socket socket4;
        public ushort durability;
        public bool bound;

        public int maxStack => data.maxStack;
        public Item(ScriptableItem data) {
            id = Convert.ToUInt16(data.name);
            quality = new ItemQualityData();
            plus = 0;
            socket1 = new Socket();
            socket2 = new Socket();
            socket3 = new Socket();
            socket4 = new Socket();
            bound = true;
            durability = data is EquipmentItem ? 
                        (ushort)((((EquipmentItem)data).durability + (plus * 10)) * ((int)quality.current + 1)) : (ushort)1;
        }
        public ScriptableItem data
        {
            get
            {
                if (!ScriptableItem.dict.ContainsKey(id))
                    throw new KeyNotFoundException("There is no ScriptableItem with ID=" + id + ". Make sure that all ScriptableItems are in the Resources folder so they are loaded properly.");
                return ScriptableItem.dict[id];
            }
        }
        #region For Equipments
        public EquipmentItem dataEquip => (EquipmentItem)data;
        // bonus
        public int health => dataEquip.health.GetValue(plus, quality.current);
        public int mana => dataEquip.mana.GetValue(plus, quality.current);
        public int pAtk => dataEquip.PAtk.GetValue(plus, quality.current);
        public int mAtk => dataEquip.MAtk.GetValue(plus, quality.current);
        public int pDef => dataEquip.PDef.GetValue(plus, quality.current);
        public int mDef => dataEquip.MDef.GetValue(plus, quality.current);
        public float critRate => dataEquip.critRate.GetValue(plus, quality.current);
        public float critDmg => dataEquip.critDmg.GetValue(plus, quality.current);
        public float block => dataEquip.block.GetValue(plus, quality.current);
        #endregion
        #region Socket
        public int GetTotalGemLevels()
        {
            int result = 0;
            if(socket1.id > 0)
                result += (int)socket1.data.level;
            if(socket2.id > 0)
                result += (int)socket2.data.level;
            if(socket3.id > 0)
                result += (int)socket3.data.level;
            if(socket4.id > 0)
                result += (int)socket4.data.level;
            return result;
        }
        public Socket GetSocket(int index)
        {
            if(index == 1) return socket2;
            if(index == 2) return socket3;
            if(index == 3) return socket4;
            return socket1;
        }
        public void SetSocket(int index, short socketId)
        {
            if(index == 0) socket1.id = socketId;
            if(index == 1) socket2.id = socketId;
            if(index == 2) socket3.id = socketId;
            if(index == 3) socket4.id = socketId;
        }
        public bool HasGemWithType(BonusType bonusType)
        {
            if((socket1.id > 0 && socket1.type == bonusType) || (socket2.id > 0 && socket2.type == bonusType) 
            || (socket3.id > 0 && socket3.type == bonusType) || (socket4.id > 0 && socket4.type == bonusType))
                return true;
            return false;
        }
        public float GetSocketOfType(BonusType bonusType)
        {
            if(socket1.id > 0 && socket1.type == bonusType)
                return socket1.bonus;
            if(socket2.id > 0 && socket2.type == bonusType)
                return socket2.bonus;
            if(socket3.id > 0 && socket3.type == bonusType)
                return socket3.bonus;
            if(socket4.id > 0 && socket4.type == bonusType)
                return socket4.bonus;
            return 0f;
        }
        #endregion
        #region Durability
        public void ApplyDamage(int dmg)
        {
            
        }
        public uint GetRepairDurabilityCost()
        {
            return (uint)((MaxDurability() - durability) * data.minLevel * plus * ((int)quality.current + 1u));
        }
        public ushort MaxDurability()
        {
            return data is EquipmentItem ? (ushort)
                        ((dataEquip.durability + ( plus * 10)) * ((int)quality.current + 1)) : (ushort)0;
        }
        #endregion
    }
}