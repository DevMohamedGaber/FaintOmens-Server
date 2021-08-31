using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game
{

    [CreateAssetMenu(menuName = "Custom/Bonus/ScriptableTotalGemLevels", order = 0)]
    public class ScriptableTotalGemLevels : ScriptableObjectNonAlloc
    {
        [Header("Info")]
        public int totalLevels;
        public Bonus bonus;

        public static ScriptableTotalGemLevels GetBonus(int level)
        {
            ScriptableTotalGemLevels result = null;
            if(cache.Count > 0)
            {
                foreach (ScriptableTotalGemLevels item in cache.Values)
                {
                    if(item.totalLevels < level)
                    {
                        result = item;
                    }
                    else break;
                }
            }
            return result;
        }

        //cash
        static Dictionary<int, ScriptableTotalGemLevels> cache = new Dictionary<int, ScriptableTotalGemLevels>();
        public static Dictionary<int, ScriptableTotalGemLevels> dict
        {
            get
            {
                if(cache == null)
                {
                    ScriptableTotalGemLevels[] items = Resources.LoadAll<ScriptableTotalGemLevels>("");
                    List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
                    if(duplicates.Count == 0)
                    {
                        cache = items.ToDictionary(item => item.name, item => item);
                    }
                    else
                    {
                        foreach(int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableTotalGemLevels with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}