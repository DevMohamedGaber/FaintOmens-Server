using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace Game
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] Player player;
        PlayerOwnData own => player.own;

        public async Task<uint> GetBattleRate()
        {
            List<Task<uint>> tasks = new List<Task<uint>>();
            // stats
            tasks.Add(Task.Run(() => (uint)(GetHealth() * Storage.data.ratios.hp_br))); // health
            tasks.Add(Task.Run(() => (uint)(GetMana() * Storage.data.ratios.mp_br))); // mana
            // attack
            tasks.Add(Task.Run(() => (uint)(GetPAtk() * Storage.data.ratios.pAtk_br))); // p_atk
            tasks.Add(Task.Run(() => (uint)(GetMAtk() * Storage.data.ratios.mAtk_br))); // m_atk
            // defense
            tasks.Add(Task.Run(() => (uint)(GetPDef() * Storage.data.ratios.pDef_br))); // p_def
            tasks.Add(Task.Run(() => (uint)(GetMDef() * Storage.data.ratios.mDef_br))); // m_def
            // block
            tasks.Add(Task.Run(() => (uint)(GetBlock() * Storage.data.ratios.block_br))); // block
            tasks.Add(Task.Run(() => (uint)(GetAntiBlock() * Storage.data.ratios.antiBlock_br))); // anti block
            // critical
            tasks.Add(Task.Run(() => (uint)(GetCritRate() * Storage.data.ratios.critRate_br))); // crit rate
            tasks.Add(Task.Run(() => (uint)(GetCritDmg() * Storage.data.ratios.critDmg_br))); // crit dmg
            tasks.Add(Task.Run(() => (uint)(GetAntiCrit() * Storage.data.ratios.antiCrit_br))); // anti crit

            var taskResults = await Task.WhenAll(tasks);
            uint result = 0;

            for(int i = 0; i < taskResults.Length; i++)
            {
                result += taskResults[i];
            }

            return result;
        }

        #region Health
        public async Task<int> GetHealthAsync()
        {
            return await Task.Run(() => GetHealth());
        }
        public int GetHealth()
        {
            int i, result = (own.vitality * Storage.data.ratios.AP_Vitality) + (player.classInfo.hp * player.level);
            // skills & buffs
            for(i = 0; i < player.skills.Count; i++)
            {
                if(player.skills[i].level > 0 && player.skills[i].data is PassiveSkill)
                {
                    result += ((PassiveSkill)player.skills[i].data).healthMaxBonus.Get(player.skills[i].level);
                }
            }
            if(player.buffs.Count > 0)
            {
                for(i = 0; i < player.buffs.Count; i++)
                {
                    result += player.buffs[i].healthMaxBonus;
                }
            }
            // gear
            for(i = 0; i < own.equipment.Count; i++)
            {
                if(own.equipment[i].amount > 0 && own.equipment[i].item.data != null)
                {
                    result += (int)own.equipment[i].item.GetSocketOfType(BonusType.hp) + own.equipment[i].item.health;
                }
            }
            for(i = 0; i < own.accessories.Count; i++)
            {
                if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                {
                    result += (int)own.accessories[i].item.GetSocketOfType(BonusType.hp) + own.accessories[i].item.health;
                }
            }
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.healthMax;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += own.pets[i].healthMax / 2;
                    }
                }
            }
            
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].healthMax / 2 : own.mounts[i].healthMax;
                }
            }
            
            if(own.militaryRank > 0)
            {
                result += ScriptableMilitaryRank.dict[own.militaryRank].hp;
            }
            
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.hp.active : title.hp.notActive;
                    }
                }
            }

            if(player.InGuild())
            {
                result += ScriptableGuildSkill.dict[0].Get(own.guildSkills[0]) * Storage.data.ratios.AP_Vitality;
            }

            return result;
        }
        public int GetHealthRecovery()
        {
            int result = 0;

            return result;
        }
        #endregion
        #region Mana
        public async Task<int> GetManaAsync()
        {
            return await Task.Run(() => GetMana());
        }
        public int GetMana()
        {
            int i, result = (own.intelligence * Storage.data.ratios.AP_Intelligence_MANA) 
                            + (player.classInfo.mp * player.level);
            // gear
            for(i = 0; i < own.equipment.Count; i++)
            {
                if(own.equipment[i].amount > 0 && own.equipment[i].item.data != null)
                {
                    result += own.equipment[i].item.mana;
                }
            }
            for(i = 0; i < own.accessories.Count; i++)
            {
                if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                {
                    result += own.accessories[i].item.mana;
                }
            }
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.manaMax;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += (int)(own.pets[i].manaMax * Storage.data.pet.savedBonus);
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved ? own.mounts[i].manaMax / 2 : own.mounts[i].manaMax;
                }
            }
            
            if(own.militaryRank > 0)
            {
                result += ScriptableMilitaryRank.dict[own.militaryRank].mp;
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.mp.active : title.mp.notActive;
                    }
                }
            }
            if(player.InGuild())
            {
                result += ScriptableGuildSkill.dict[2].Get(own.guildSkills[2]) * Storage.data.ratios.AP_Intelligence_MANA;
            }
            return result;
        }
        public int GetManaRecovery()
        {
            int result = 0;

            return result;
        }
        #endregion
        
        #region Physical Attack
        public async Task<int> GetPAtkAsync()
        {
            return await Task.Run(() => GetPAtk());
        }
        public int GetPAtk()
        {
            int i, result = (own.strength * Storage.data.ratios.AP_Strength_ATK) + (player.classInfo.pAtk * player.level);
            // skills
            for(i = 0; i < player.skills.Count; i++)
            {
                if(player.skills[i].level > 0 && player.skills[i].data is PassiveSkill)
                {
                    result += ((PassiveSkill)player.skills[i].data).damageBonus.Get(player.skills[i].level);
                }
            }
            if(player.buffs.Count > 0)
            {
                for(i = 0; i < player.buffs.Count; i++)
                {
                    result += player.buffs[i].damageBonus;
                }
            }
            // gear
            for(i = 0; i < own.equipment.Count; i++)
            {
                if(own.equipment[i].amount > 0 && own.equipment[i].item.data != null)
                {
                    result += (int)own.equipment[i].item.GetSocketOfType(BonusType.pAtk) + own.equipment[i].item.pAtk;
                }
            }
            for(i = 0; i < own.accessories.Count; i++)
            {
                if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                {
                    result += (int)own.accessories[i].item.GetSocketOfType(BonusType.pAtk) + own.accessories[i].item.pAtk;
                }
            }
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.p_atk;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += (int)(own.pets[i].p_atk * Storage.data.pet.savedBonus);
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved
                            ? (int)(own.mounts[i].p_atk * Storage.data.mount.savedBonus)
                            : own.mounts[i].p_atk;
                }
            }
            
            if(player.classType == DamageType.Physical)
            {
                if(own.militaryRank > 0)
                {
                    result += ScriptableMilitaryRank.dict[own.militaryRank].atk;
                }
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.pAtk.active : title.pAtk.notActive;
                    }
                }
            }
            if(player.InGuild())
            {
                result += ScriptableGuildSkill.dict[1].Get(own.guildSkills[1]) * Storage.data.ratios.AP_Strength_ATK;
            }
            return result;
        }
        #endregion
        #region Magical Attack
        public async Task<int> GetMAtkAsync()
        {
            return await Task.Run(() => GetMAtk());
        }
        public int GetMAtk()
        {
            int i, result = (own.intelligence * Storage.data.ratios.AP_Intelligence_ATK)
                            + (player.classInfo.mAtk * player.level);
            // skills
            for(i = 0; i < player.skills.Count; i++)
            {
                if(player.skills[i].level > 0 && player.skills[i].data is PassiveSkill)
                {
                    result += ((PassiveSkill)player.skills[i].data).damageBonus.Get(player.skills[i].level);
                }
            }
            if(player.buffs.Count > 0)
            {
                for(i = 0; i < player.buffs.Count; i++)
                {
                    result += player.buffs[i].damageBonus;
                }
            }
            // gear
            for(i = 0; i < own.equipment.Count; i++)
            {
                if(own.equipment[i].amount > 0 && own.equipment[i].item.data != null)
                {
                    result += (int)own.equipment[i].item.GetSocketOfType(BonusType.mAtk) + own.equipment[i].item.mAtk;
                }
            }
            for(i = 0; i < own.accessories.Count; i++)
            {
                if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                {
                    result += (int)own.accessories[i].item.GetSocketOfType(BonusType.mAtk) + own.accessories[i].item.mAtk;
                }
            }
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.m_atk;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += (int)(own.pets[i].m_atk * Storage.data.pet.savedBonus);
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved
                            ? (int)(own.mounts[i].m_atk * Storage.data.mount.savedBonus)
                            : own.mounts[i].m_atk;
                }
            }
            
            if(player.classType == DamageType.Magical)
            {
                if(own.militaryRank > 0)
                {
                    result += ScriptableMilitaryRank.dict[own.militaryRank].atk;
                }
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.mAtk.active : title.mAtk.notActive;
                    }
                }
            }
            if(player.InGuild())
            {
                result += ScriptableGuildSkill.dict[2].Get(own.guildSkills[2]) * Storage.data.ratios.AP_Intelligence_ATK;
            }
            
            return result;
        }
        #endregion
    
        #region Physical Defense
        public async Task<int> GetPDefAsync()
        {
            return await Task.Run(() => GetPDef());
        }
        public int GetPDef()
        {
            int i, result = (player.classInfo.pDef * player.level) + (own.endurance * Storage.data.ratios.AP_Endurance) 
                            + (own.strength * Storage.data.ratios.AP_Strength_DEF);
            // skills
            for(i = 0; i < player.skills.Count; i++)
            {
                if(player.skills[i].level > 0 && player.skills[i].data is PassiveSkill)
                {
                    result += ((PassiveSkill)player.skills[i].data).defenseBonus.Get(player.skills[i].level);
                }
            }
            if(player.buffs.Count > 0)
            {
                for(i = 0; i < player.buffs.Count; i++)
                {
                    result += player.buffs[i].defenseBonus;
                }
            }
            // gear
            for(i = 0; i < own.equipment.Count; i++)
            {
                if(own.equipment[i].amount > 0 && own.equipment[i].item.data != null)
                {
                    result += (int)own.equipment[i].item.GetSocketOfType(BonusType.pDef) + own.equipment[i].item.pDef;
                }
            }
            for(i = 0; i < own.accessories.Count; i++)
            {
                if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                {
                    result += (int)own.accessories[i].item.GetSocketOfType(BonusType.pDef) + own.accessories[i].item.pDef;
                }
            }
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.p_def;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += (int)(own.pets[i].p_def * Storage.data.pet.savedBonus);
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved 
                            ? (int)(own.mounts[i].p_def * Storage.data.mount.savedBonus)
                            : own.mounts[i].p_def;
                }
            }

            if(player.classType == DamageType.Physical)
            {
                if(own.militaryRank > 0)
                {
                    result += ScriptableMilitaryRank.dict[own.militaryRank].def;
                }
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.pDef.active : title.pDef.notActive;
                    }
                }
            }
            
            if(player.InGuild())
            {
                result += ScriptableGuildSkill.dict[1].Get(own.guildSkills[1]) * Storage.data.ratios.AP_Strength_DEF
                        + ScriptableGuildSkill.dict[3].Get(own.guildSkills[3]) * Storage.data.ratios.AP_Endurance;
            }

            return result;
        }
        #endregion
        #region Magical Defense
        public async Task<int> GetMDefAsync()
        {
            return await Task.Run(() => GetMDef());
        }
        public int GetMDef()
        {
            int i, result = (player.classInfo.mDef * player.level) + (own.endurance * Storage.data.ratios.AP_Endurance) 
                        + (own.intelligence * Storage.data.ratios.AP_Intelligence_DEF);
            // skills
            for(i = 0; i < player.skills.Count; i++)
            {
                if(player.skills[i].level > 0 && player.skills[i].data is PassiveSkill)
                {
                    result += ((PassiveSkill)player.skills[i].data).defenseBonus.Get(player.skills[i].level);
                }
            }
            if(player.buffs.Count > 0)
            {
                for(i = 0; i < player.buffs.Count; i++)
                {
                    result += player.buffs[i].defenseBonus;
                }
            }
            // gear
            for(i = 0; i < own.equipment.Count; i++)
            {
                if(own.equipment[i].amount > 0 && own.equipment[i].item.data != null)
                {
                    result += (int)own.equipment[i].item.GetSocketOfType(BonusType.mDef) + own.equipment[i].item.mDef;
                }
            }
            for(i = 0; i < own.accessories.Count; i++)
            {
                if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                {
                    result += (int)own.accessories[i].item.GetSocketOfType(BonusType.mDef) + own.accessories[i].item.mDef;
                }
            }
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.m_def;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += (int)(own.pets[i].m_def * Storage.data.pet.savedBonus);
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved
                            ? (int)(own.mounts[i].m_def * Storage.data.mount.savedBonus)
                            : own.mounts[i].m_def;
                }
            }
            
            if(player.classType == DamageType.Magical)
            {
                if(own.militaryRank > 0)
                {
                    result += ScriptableMilitaryRank.dict[own.militaryRank].def;
                }
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.mDef.active : title.mDef.notActive;
                    }
                }
            }
            
            if(player.InGuild())
            {
                result += ScriptableGuildSkill.dict[2].Get(own.guildSkills[2]) * Storage.data.ratios.AP_Intelligence_DEF
                        + ScriptableGuildSkill.dict[3].Get(own.guildSkills[3]) * Storage.data.ratios.AP_Endurance;
            }
            return result;
        }
        #endregion

        #region Block
        public async Task<float> GetBlockAsync()
        {
            return await Task.Run(() => GetBlock());
        }
        public float GetBlock()
        {
            int i;
            float result = (float)(player.classInfo.block * player.level);
            // skills
            for(i = 0; i < player.skills.Count; i++)
            {
                if(player.skills[i].level > 0 && player.skills[i].data is PassiveSkill)
                {
                    result += ((PassiveSkill)player.skills[i].data).blockChanceBonus.Get(player.skills[i].level);
                }
            }
            if(player.buffs.Count > 0)
            {
                for(i = 0; i < player.buffs.Count; i++)
                {
                    result += player.buffs[i].blockChanceBonus;
                }
            }
            // gear
            for(i = 0; i < own.equipment.Count; i++)
            {
                if(own.equipment[i].amount > 0 && own.equipment[i].item.data != null)
                {
                    result += own.equipment[i].item.GetSocketOfType(BonusType.block) + own.equipment[i].item.block;
                }
            }
            for(i = 0; i < own.accessories.Count; i++)
            {
                if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                {
                    result += own.accessories[i].item.GetSocketOfType(BonusType.block) + own.accessories[i].item.block;
                }
            }
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.blockChance;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += own.pets[i].block * Storage.data.pet.savedBonus;
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved
                            ? own.mounts[i].blockChance * Storage.data.mount.savedBonus
                            : own.mounts[i].blockChance;
                }
            }
            
            if(own.militaryRank > 0)
            {
                result += ScriptableMilitaryRank.dict[own.militaryRank].block;
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.block.active : title.block.notActive;
                    }
                }
            }
            
            if(player.InGuild())
            {
                result += (float)ScriptableGuildSkill.dict[5].Get(own.guildSkills[5]) * Storage.data.guild.blockSkillPerLvl;
            }

            return result;
        }
        #endregion
        #region Anti-Block
        public async Task<float> GetAntiBlockAsync()
        {
            return await Task.Run(() => GetBlock());
        }
        public float GetAntiBlock()
        {
            int i;
            float result = (float)(player.classInfo.antiBlock * player.level);
            if(player.activePet != null)
            {
                result += player.activePet.untiBlockChance;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += own.pets[i].antiBlock * Storage.data.pet.savedBonus;
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved 
                            ? own.mounts[i].untiBlockChance * Storage.data.mount.savedBonus
                            : own.mounts[i].untiBlockChance;
                }
            }
            if(own.militaryRank > 0)
            {
                result += ScriptableMilitaryRank.dict[own.militaryRank].untiBlock;
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.antiBlock.active : title.antiBlock.notActive;
                    }
                }
            }
            return result;
        }
        #endregion
    
        #region CritRate
        public async Task<float> GetCritRateAsync()
        {
            return await Task.Run(() => GetCritRate());
        }
        public float GetCritRate()
        {
            int i;
            float result = (float)(player.classInfo.critRate * player.level);
            // skills
            for(i = 0; i < player.skills.Count; i++)
            {
                if(player.skills[i].level > 0 && player.skills[i].data is PassiveSkill)
                {
                    result += ((PassiveSkill)player.skills[i].data).criticalChanceBonus.Get(player.skills[i].level);
                }
            }
            if(player.buffs.Count > 0)
            {
                for(i = 0; i < player.buffs.Count; i++)
                {
                    result += player.buffs[i].criticalChanceBonus;
                }
            }
            // gear
            for(i = 0; i < own.equipment.Count; i++)
            {
                if(own.equipment[i].amount > 0 && own.equipment[i].item.data != null)
                {
                    result += own.equipment[i].item.GetSocketOfType(BonusType.crit) + own.equipment[i].item.critRate;
                }
            }
            for(i = 0; i < own.accessories.Count; i++)
            {
                if(own.accessories[i].amount > 0 && own.accessories[i].item.data != null)
                {
                    result += own.accessories[i].item.GetSocketOfType(BonusType.crit) + own.accessories[i].item.critRate;
                }
            }
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.critRate;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += own.pets[i].critRate * Storage.data.pet.savedBonus;
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved 
                            ? own.mounts[i].criticalChance * Storage.data.mount.savedBonus
                            : own.mounts[i].criticalChance;
                }
            }
            
            if(own.militaryRank > 0)
            {
                result += ScriptableMilitaryRank.dict[own.militaryRank].crit;
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.critRate.active : title.critRate.notActive;
                    }
                }
            }
            if(player.InGuild())
            {
                result += (float)ScriptableGuildSkill.dict[4].Get(own.guildSkills[4]) * Storage.data.guild.critRateSkillPerLvl;
            }
            return result;
        }
        #endregion
        #region CritDmg
        public async Task<float> GetCritDmgAsync()
        {
            return await Task.Run(() => GetCritDmg());
        }
        public float GetCritDmg()
        {
            int i;
            float result = player.classInfo.critDmg;
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.critDmg;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += own.pets[i].critDmg * Storage.data.pet.savedBonus;
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved
                            ? own.mounts[i].criticalRate * Storage.data.mount.savedBonus
                            : own.mounts[i].criticalRate;
                }
            }

            if(own.militaryRank > 0)
            {
                result += ScriptableMilitaryRank.dict[own.militaryRank].critDmg;
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.critDmg.active : title.critDmg.notActive;
                    }
                }
            }
            return result;
        }
        #endregion
        #region AntiCrit
        public async Task<float> GetAntiCritAsync()
        {
            return await Task.Run(() => GetAntiCrit());
        }
        public float GetAntiCrit()
        {
            int i;
            float result = player.classInfo.antiCrit;
            // summonables
            if(player.activePet != null)
            {
                result += player.activePet.antiCrit;
            }
            if(own.pets.Count > 0)
            {
                for(i = 0; i < own.pets.Count; i++)
                {
                    if(own.pets[i].status == SummonableStatus.Saved)
                    {
                        result += own.pets[i].antiCrit * Storage.data.pet.savedBonus;
                    }
                }
            }
            if(own.mounts.Count > 0)
            {
                for(i = 0; i < own.mounts.Count; i++)
                {
                    result += own.mounts[i].status == SummonableStatus.Saved
                            ? own.mounts[i].untiCriticalChance * Storage.data.mount.savedBonus
                            : own.mounts[i].untiCriticalChance;
                }
            }

            if(own.militaryRank > 0)
            {
                result += ScriptableMilitaryRank.dict[own.militaryRank].untiCrit;
            }
            if(own.titles.Count > 0)
            {
                for(i = 0; i < own.titles.Count; i++)
                {
                    if(ScriptableTitle.dict.TryGetValue(own.titles[i], out ScriptableTitle title))
                    {
                        result += own.titles[i] == player.activeTitle ? title.antiCrit.active : title.antiCrit.notActive;
                    }
                }
            }
            return result;
        }
        #endregion
    }
}