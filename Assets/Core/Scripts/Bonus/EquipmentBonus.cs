using System;
namespace Game
{
    [Serializable]
    public struct EquipmentBonus
    {
        public int Base;
        public int plus;
        public int GetValue(int plus, Quality quality)
        {
            return Base + (this.plus * plus) + (int)Math.Floor(Base * ScriptableQuality.dict[(int)quality].equipmentBonusPerc);
        }
        public int GetBonus(int plus, Quality quality)
        {
            return (this.plus * plus) + (int)Math.Floor(Base * ScriptableQuality.dict[(int)quality].equipmentBonusPerc);
        }
    }
}