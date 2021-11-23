using UnityEngine;
namespace Game.StorageData
{
    [System.Serializable]
    public struct Ratios
    {
        public float playerAttackToMoveRange;
        [Header("Attribute points To Stats")]
        public int AP_Vitality;
        public int AP_Strength_ATK;
        public int AP_Strength_DEF;
        public int AP_Intelligence_ATK;
        public int AP_Intelligence_DEF;
        public int AP_Intelligence_MANA;
        public int AP_Endurance;
        [Header("stats to BR")]
        public float hp_br;
        public float mp_br;
        public float pAtk_br;
        public float mAtk_br;
        public float pDef_br;
        public float mDef_br;
        public float block_br;
        public float antiBlock_br;
        public float critRate_br;
        public float critDmg_br;
        public float antiCrit_br;
    }
}