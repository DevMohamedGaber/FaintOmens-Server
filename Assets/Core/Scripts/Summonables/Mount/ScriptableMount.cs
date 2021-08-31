using UnityEngine;
using System.Linq;
using System.Collections.Generic;
namespace Game
{
    [CreateAssetMenu(menuName = "Custom/Summonables/Mount", order = 0)]
    public class ScriptableMount : ScriptableObjectNonAlloc
    {
        public int ActivateItemId;
        public DamageType damageType;
        public Tier maxTire;
        public LinearInt health = new LinearInt{ baseValue=100 };
        public LinearInt mana = new LinearInt{ baseValue=100 };
        public LinearInt pAtk = new LinearInt{ baseValue=5 };
        public LinearInt pDef = new LinearInt{ baseValue=1 };
        public LinearInt mAtk = new LinearInt{ baseValue=5 };
        public LinearInt mDef = new LinearInt{ baseValue=1 };
        public LinearFloat block = new LinearFloat();
        public LinearFloat untiBlock = new LinearFloat();
        public LinearFloat crit = new LinearFloat();
        public LinearFloat critDmg = new LinearFloat();
        public LinearFloat untiCrit = new LinearFloat();
        public LinearFloat speed = new LinearFloat();

        static Dictionary<ushort, ScriptableMount> cache;
        public static Dictionary<ushort, ScriptableMount> dict
        { 
            get
            {
                if (cache == null) // not loaded yet?
                {
                    ScriptableMount[] mounts = Resources.LoadAll<ScriptableMount>("");
                    List<int> duplicates = mounts.ToList().FindDuplicates(mount => mount.name);
                    if (duplicates.Count == 0)
                    {
                        cache = mounts.ToDictionary(mount => (ushort)mount.name, mount => mount);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableMount with ID:" + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}