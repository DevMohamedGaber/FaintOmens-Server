using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName = "ScriptableQuality", order = 999)]
    public class ScriptableQuality : ScriptableObjectNonAlloc
    {
        public float equipmentBonusPerc;

        public static bool isUpgradable(Quality quality)
        {
            return quality < Quality.Legendary && (int)quality > -1;
        }

        // cache
        static Dictionary<int, ScriptableQuality> cache;
        public static Dictionary<int, ScriptableQuality> dict
        {
            get
            {
                if (cache == null) // not loaded yet?
                {
                    ScriptableQuality[] qualities = Resources.LoadAll<ScriptableQuality>("");
                    List<int> duplicates = qualities.ToList().FindDuplicates(quality => quality.name);
                    if (duplicates.Count == 0){
                        cache = qualities.ToDictionary(quality => quality.name, quality => quality);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableQuality with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}