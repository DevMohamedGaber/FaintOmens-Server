using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace Game
{
    [CreateAssetMenu(menuName = "Custom/Set")]
    public class ScriptableSet : ScriptableObjectNonAlloc
    {
        [Header("2 Equipments")]
        public Bonus first;
        [Header("5 Equipments")]
        public Bonus second;
        [Header("8 Equipments")]
        public Bonus third;
        // cache
        static Dictionary<int, ScriptableSet> cache;
        public static Dictionary<int, ScriptableSet> dict
        {
            get
            {
                if (cache == null)
                {// not loaded yet?
                    ScriptableSet[] sets = Resources.LoadAll<ScriptableSet>("");// get all ScriptableSet in resources
                    List<int> duplicates = sets.ToList().FindDuplicates(set => set.name); // check for duplicates, then add to cache
                    if (duplicates.Count == 0)
                    {
                        cache = sets.ToDictionary(set => set.name, set => set);
                    }
                    else
                    {
                        for(int i = 0; i < duplicates.Count; i++)
                            Debug.LogError($"Resources folder contains multiple ScriptableSet with ID: {duplicates[i]}");
                    }
                }
                return cache;
            }
        }
    }
}