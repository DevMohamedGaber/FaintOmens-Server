using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
namespace Game
{
    public abstract class ScriptableSkill : ScriptableObjectNonAlloc
    {
        [Header("Info")]
        public bool followupDefaultAttack;
        public bool learnDefault; // normal attack etc.
        public bool cancelCastIfTargetDied; // direct hit may want to cancel if target died. buffs doesn't care. etc.

        [Header("Requirements")]
        public ScriptableSkill predecessor; // this skill has to be learned first
        public byte predecessorLevel = 1; // level of predecessor skill that is required
        public string requiredWeaponCategory = ""; // "" = no weapon needed; "Weapon" = requires a weapon, "WeaponSword" = requires a sword weapon, etc.
        public LinearInt requiredLevel; // required player level
        public uint[] requiredSkillExperience;

        [Header("Properties")]
        public byte maxLevel = 1;
        public LinearInt manaCosts;
        public LinearFloat castTime;
        public LinearFloat cooldown;
        public LinearFloat castRange;

        public ushort id => (ushort)name;

        // the skill casting process ///////////////////////////////////////////////
        // 1. self check: alive, enough mana, cooldown ready etc.?
        // (most skills can only be cast while alive. some maybe while dead or only
        //  if we have ammo, etc.)
        public virtual bool CheckSelf(Entity caster, byte skillLevel)
        {
            return caster.health > 0 && caster.mana >= manaCosts.Get(skillLevel);
        }

        // 2. target check: can we cast this skill 'here' or on this 'target'?
        // => e.g. sword hit checks if target can be attacked
        //         skill shot checks if the position under the mouse is valid etc.
        //         buff checks if it's a friendly player, etc.
        // ===> IMPORTANT: this function HAS TO correct the target if necessary,
        //      e.g. for a buff that is cast on 'self' even though we target a NPC
        //      while casting it
        public abstract bool CheckTarget(Entity caster);

        // 3. distance check: do we need to walk somewhere to cast it?
        //    e.g. on a monster that's far away
        //    => returns 'true' if distance is fine, 'false' if we need to move
        // (has corrected target already)
        public abstract bool CheckDistance(Entity caster, byte skillLevel, out Vector3 destination);

        // 4. apply skill: deal damage, heal, launch projectiles, etc.
        // (has corrected target already)
        public abstract void Apply(Entity caster, byte skillLevel);
        
        // caching /////////////////////////////////////////////////////////////////
        public static Dictionary<int, ScriptableSkill> dict;
        public static void LoadAll()
        {
            ScriptableSkill[] items = Resources.LoadAll<ScriptableSkill>("Items");
            // check for duplicates, then add to cache
            List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
            if (duplicates.Count == 0)
            {
                dict = items.ToDictionary(item => item.name, item => item);
            }
            else
            {
                dict = new Dictionary<int, ScriptableSkill>();
                foreach (int duplicate in duplicates)
                    Debug.LogError("Resources folder contains multiple ScriptableSkills with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
            }
        }
        /*static Dictionary<ushort, ScriptableSkill> cache;
        public static Dictionary<ushort, ScriptableSkill> dict
        {
            get
            {
                if (cache == null)
                {
                    ScriptableSkill[] skills = Resources.LoadAll<ScriptableSkill>("");
                    List<int> duplicates = skills.ToList().FindDuplicates(skill => skill.name);
                    if (duplicates.Count == 0)
                    {
                        cache = new Dictionary<ushort, ScriptableSkill>();
                        for(int i = 0; i < skills.Length; i++)
                        {
                            cache[skills[i].id] = skills[i];
                        }
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableSkills with the name " + duplicate + ". If you are using subfolders like 'Warrior/NormalAttack' and 'Archer/NormalAttack', then rename them to 'Warrior/(Warrior)NormalAttack' and 'Archer/(Archer)NormalAttack' instead.");
                    }
                }
                return cache;
            }
        }*/
    }
}