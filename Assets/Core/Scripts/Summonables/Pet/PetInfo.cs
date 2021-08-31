using System;
namespace Game
{
    [Serializable]
    public struct PetInfo
    {
        public ushort id;
        public SummonableStatus status;
        public byte level;
        public uint experience;
        public Tier tier;
        public byte stars;
        public byte potential;
        public ushort vitality;
        public ushort intelligence;
        public ushort endurance;
        public ushort strength;
        public uint expMax => Storage.data.pet.expMax.Get(level);
        public PetInfo(ushort id)
        {
            this.id = id;
            this.status = SummonableStatus.Saved;
            this.level = 1;
            this.experience = 0;
            this.stars = 0;
            this.tier = Tier.F;
            this.potential = 0;
            this.vitality = 0;
            this.intelligence = 0;
            this.endurance = 0;
            this.strength = 0;
        }
        public void Feed(uint exp)
        {
            if(exp < 1)
                return;
            experience += exp;
            while(experience >= expMax && level < Storage.data.pet.lvlCap)
            {
                experience -= expMax;
                level++;
                vitality += (ushort)(Storage.data.pet.pointPerLvl + potential % Storage.data.pet.potentialToAP);
                intelligence += (ushort)(Storage.data.pet.pointPerLvl + potential % Storage.data.pet.potentialToAP);
                endurance += (ushort)(Storage.data.pet.pointPerLvl + potential % Storage.data.pet.potentialToAP);
                strength += (ushort)(Storage.data.pet.pointPerLvl + potential % Storage.data.pet.potentialToAP);
            }
            if (experience > expMax) // if still too much add max exp
                experience = expMax;
        }
        public ScriptablePet data => ScriptablePet.dict[id];
        // unsummoned data
        public int healthMax
        {
            get
            {
                int result = data.health.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                result += vitality * Storage.data.AP_Vitality;
                return result;
            }
        }
        public int manaMax
        {
            get
            {
                int result = data.mana.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                result += intelligence * Storage.data.AP_Intelligence_MANA;
                return result;
            }
        }
        public int p_atk
        {
            get
            {
                int result = data.pAtk.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                result += strength * Storage.data.AP_Strength_ATK;
                return result;
            }
        }
        public int m_atk
        {
            get
            {
                int result = data.mAtk.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                result += intelligence * Storage.data.AP_Intelligence_ATK;
                return result;
            }
        }
        public int p_def
        {
            get
            {
                int result = data.pDef.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                result += endurance * Storage.data.AP_Endurance + intelligence * Storage.data.AP_Strength_DEF;
                return result;
            }
        }
        public int m_def
        {
            get
            {
                int result = data.mDef.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                result += endurance * Storage.data.AP_Endurance + intelligence * Storage.data.AP_Intelligence_DEF;
                return result;
            }
        }
        public float block
        {
            get
            {
                float result = data.block.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                return result;
            }
        }
        public float antiBlock
        {
            get
            {
                float result = data.untiBlock.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                return result;
            }
        }
        public float critRate
        {
            get
            {
                float result = data.crit.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                return result;
            }
        }
        public float critDmg
        {
            get
            {
                float result = data.critDmg.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                return result;
            }
        }
        public float antiCrit
        {
            get
            {
                float result = data.untiCrit.Get(level);
                result += tier != Tier.F ? (result / 5) * (int)tier : 0;
                return result;
            }
        }
        public uint battlepower 
        {
            get
            {
                return Convert.ToUInt32(
                    healthMax +
                    manaMax +
                    m_atk + 
                    p_atk + 
                    m_def + 
                    p_def + 
                ((block + antiBlock + critRate + critDmg + antiCrit) * 100));
            }
            
        }
    }
}