using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName = "ScriptableMilitaryRank", order = 0)]
    public class ScriptableMilitaryRank : ScriptableObjectNonAlloc
    {
        [Header("Promotion")]
        public int level;
        public int honor;
        public int monsterPoints;
        public ScriptableMilitaryRank next;
        [Header("Bonus")]
        public int hp;
        public int mp;
        public int hpRec;
        public int mpRec;
        public int atk;
        public int def;
        public float block;
        public float untiBlock;
        public float crit;
        public float critDmg;
        public float untiCrit;
        public float untiStun;
        public float speed;

        // cache
        static Dictionary<int, ScriptableMilitaryRank> cache;
        public static Dictionary<int, ScriptableMilitaryRank> dict
        {
            get
            {
                if (cache == null)
                {// not loaded yet?
                    ScriptableMilitaryRank[] qualities = Resources.LoadAll<ScriptableMilitaryRank>("");
                    List<int> duplicates = qualities.ToList().FindDuplicates(mr => (int)mr.name);
                    if (duplicates.Count == 0)
                    {
                        cache = qualities.ToDictionary(mr => (int)mr.name, mr => mr);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableMilitaryRank with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}