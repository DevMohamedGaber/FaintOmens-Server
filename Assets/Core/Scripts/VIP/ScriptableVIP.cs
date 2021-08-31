using UnityEngine;
using System.Linq;
using System.Collections.Generic;
namespace Game
{
    [CreateAssetMenu(menuName = "ScriptableVIP", order = 999)]
    public class ScriptableVIP : ScriptableObjectNonAlloc
    {
        public int nextLevelpoints;
        public int bonusHonor;
        public VIPReward firstReward;
        public VIPReward weeklyReward;
        public int assistPets;
        public int assistMounts;
        public ScriptableQuest[] quests;
        public int questsQuota;

        //cash
        static Dictionary<int, ScriptableVIP> cache;
        public static Dictionary<int, ScriptableVIP> dict
        {
            get
            {
                // not loaded yet?
                if (cache == null)
                {
                    // get all ScriptableItems in resources
                    ScriptableVIP[] items = Resources.LoadAll<ScriptableVIP>("");
                    // check for duplicates, then add to cache
                    List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
                    if (duplicates.Count == 0)
                    {
                        cache = items.ToDictionary(item => item.name, item => item);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableVIP with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}