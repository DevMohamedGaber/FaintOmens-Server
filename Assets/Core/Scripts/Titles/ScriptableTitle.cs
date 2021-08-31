using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Title", order = 0)]
    public class ScriptableTitle : ScriptableObjectNonAlloc {
        // bonus
        public ActivationBonus hp;
        public ActivationBonus mp;
        public ActivationBonus pAtk;
        public ActivationBonus mAtk;
        public ActivationBonus pDef;
        public ActivationBonus mDef;
        public ActivationFloatBonus block;
        public ActivationFloatBonus antiBlock;
        public ActivationFloatBonus critRate;
        public ActivationFloatBonus critDmg;
        public ActivationFloatBonus antiCrit;
        public ActivationFloatBonus antiStun;

        //cash
        static Dictionary<int, ScriptableTitle> cache;
        public static Dictionary<int, ScriptableTitle> dict
        {
            get
            {
                // not loaded yet?
                if (cache == null)
                {
                    // get all ScriptableItems in resources
                    ScriptableTitle[] items = Resources.LoadAll<ScriptableTitle>("");

                    // check for duplicates, then add to cache
                    List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
                    if (duplicates.Count == 0)
                    {
                        cache = items.ToDictionary(item => item.name, item => item);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableItems with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}