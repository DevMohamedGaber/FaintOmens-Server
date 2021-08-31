using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace Game
{
    [CreateAssetMenu(menuName = "ScriptableTribe", order = 999)]
    public class ScriptableTribe : ScriptableObjectNonAlloc
    {
        public string Name;
        public Sprite flag;
        [TextArea(1, 30)] public string desciption;

        static Dictionary<int, ScriptableTribe> cache;
        public static Dictionary<int, ScriptableTribe> dict
        {
            get
            {
                if (cache == null)
                {// not loaded yet?
                    ScriptableTribe[] tribes = Resources.LoadAll<ScriptableTribe>("");
                    List<int> duplicates = tribes.ToList().FindDuplicates(tribe => tribe.name);
                    if (duplicates.Count == 0)
                    {
                        cache = tribes.ToDictionary(tribe => tribe.name, tribe => tribe);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableTribe with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}