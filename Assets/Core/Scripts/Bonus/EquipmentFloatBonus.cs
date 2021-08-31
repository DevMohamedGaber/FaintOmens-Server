using System;
using UnityEngine;
namespace Game
{
    [Serializable]
    public struct EquipmentFloatBonus
    {
        [Range(0, 1)] public float Base;
        [Range(0, 1)] public float plus;
        public float GetValue(int plus, Quality quality)
        {
            return Base + (this.plus * (float)plus) + Mathf.Floor(Base * ScriptableQuality.dict[(int)quality].equipmentBonusPerc);
        }
        public float GetBonus(int plus, Quality quality)
        {
            return (this.plus * (float)plus) + Mathf.Floor(Base * ScriptableQuality.dict[(int)quality].equipmentBonusPerc);
        }
    }
}