using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace Game
{
    [Serializable]
    public struct Buff
    {
        public ushort id;
        public byte level;
        public double buffTimeEnd; // server time. double for long term precision.
        public BuffSkill data
        {
            get
            {
                if (!ScriptableSkill.dict.ContainsKey(id))
                    throw new KeyNotFoundException("There is no ScriptableSkill with id=" + id + ". Make sure that all ScriptableSkills are in the Resources folder so they are loaded properly.");
                return (BuffSkill)ScriptableSkill.dict[id];
            }
        }
        public int name => data.name;
        public float buffTime => data.buffTime.Get(level);
        public bool remainAfterDeath => data.remainAfterDeath;
        public int healthMaxBonus => data.healthMaxBonus.Get(level);
        public int manaMaxBonus => data.manaMaxBonus.Get(level);
        public int damageBonus => data.damageBonus.Get(level);
        public int defenseBonus => data.defenseBonus.Get(level);
        public float blockChanceBonus => data.blockChanceBonus.Get(level);
        public float criticalChanceBonus => data.criticalChanceBonus.Get(level);
        public float healthPercentPerSecondBonus => data.healthPercentPerSecondBonus.Get(level);
        public float manaPercentPerSecondBonus => data.manaPercentPerSecondBonus.Get(level);
        public float speedBonus => data.speedBonus.Get(level);
        public int maxLevel => data.maxLevel;

        public Buff(BuffSkill data, byte level)
        {
            this.id = data.id;
            this.level = level;
            this.buffTimeEnd = NetworkTime.time + data.buffTime.Get(level); // start buff immediately
        }

        public float BuffTimeRemaining()
        {
            return NetworkTime.time >= buffTimeEnd ? 0 : (float)(buffTimeEnd - NetworkTime.time);
        }
    }
}

    