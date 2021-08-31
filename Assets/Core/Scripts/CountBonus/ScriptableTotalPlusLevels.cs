using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game
{

    [CreateAssetMenu(menuName = "Custom/Bonus/ScriptableTotalPlusLevels", order = 0)]
    public class ScriptableTotalPlusLevels : ScriptableObjectNonAlloc
    {
        [Header("Info")]
        public int totalLevels;
        public Bonus bonus;

        public static ScriptableTotalPlusLevels GetBonus(int level)
        {
            ScriptableTotalPlusLevels result = null;
            if(cache.Count > 0)
            {
                foreach(ScriptableTotalPlusLevels item in cache.Values)
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
        static Dictionary<int, ScriptableTotalPlusLevels> cache = new Dictionary<int, ScriptableTotalPlusLevels>();
        public static Dictionary<int, ScriptableTotalPlusLevels> dict
        {
            get
            {
                if(cache == null)
                {
                    ScriptableTotalPlusLevels[] items = Resources.LoadAll<ScriptableTotalPlusLevels>("");
                    List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
                    if(duplicates.Count == 0)
                    {
                        cache = items.ToDictionary(item => item.name, item => item);
                    }
                    else
                    {
                        foreach(int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableTotalPlusLevels with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}