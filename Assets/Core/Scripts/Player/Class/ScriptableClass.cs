using UnityEngine;
using System.Linq;
using System.Collections.Generic;
namespace Game
{
    [CreateAssetMenu(menuName = "Custom/ScriptableClass", order = 999)]
    public class ScriptableClass : ScriptableObjectNonAlloc
    {
        public GameObject prefab;
        public SubclassInfo[] subs;
        public ItemSlot[] defaultEquipments;
        public Quality quality;
        public ushort[] initialAPs = new ushort[4];
        public ScriptableQuality qualityData => ScriptableQuality.dict[(int)quality];
        //cash
        static Dictionary<PlayerClass, ScriptableClass> cache;
        public static Dictionary<PlayerClass, ScriptableClass> dict
        {
            get
            {
                // not loaded yet?
                if (cache == null)
                {
                    // get all ScriptableItems in resources
                    ScriptableClass[] items = Resources.LoadAll<ScriptableClass>("");

                    // check for duplicates, then add to cache
                    List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
                    if(duplicates.Count == 0)
                    {
                        cache = items.ToDictionary(item => (PlayerClass)item.name, item => item);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableClass with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
        void OnValidate()
        {
            if(defaultEquipments == null)
            {
                defaultEquipments = new ItemSlot[]{};
            }
        }
    }
}