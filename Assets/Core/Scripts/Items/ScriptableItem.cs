using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/General", order=0)]
    public class ScriptableItem : ScriptableObjectNonAlloc
    {
        [Header("Base Stats")]
        public int maxStack = 999;
        public long buyPrice;
        public long sellPrice;
        public long itemMallPrice;
        public bool sellable;
        public bool tradable;
        public bool destroyable;
        [Header("Custom Stats")]
        public PlayerClass reqClass = PlayerClass.Any;
        public byte minLevel = 1;
        public Quality quality;
        // cache
        public static Dictionary<int, ScriptableItem> dict;
        public static void LoadAll()
        {
            ScriptableItem[] items = Resources.LoadAll<ScriptableItem>("Items");
            // check for duplicates, then add to cache
            List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
            if (duplicates.Count == 0)
            {
                dict = items.ToDictionary(item => item.name, item => item);
            }
            else
            {
                dict = new Dictionary<int, ScriptableItem>();
                foreach (int duplicate in duplicates)
                    Debug.LogError("Resources folder contains multiple ScriptableItems with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
            }
        }
        /*static Dictionary<int, ScriptableItem> cache;
        public static Dictionary<int, ScriptableItem> dict
        {
            get
            {
                // not loaded yet?
                if (cache == null)
                {
                    // get all ScriptableItems in resources
                    ScriptableItem[] items = Resources.LoadAll<ScriptableItem>("");

                    // check for duplicates, then add to cache
                    List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
                    if (duplicates.Count == 0)
                    {
                        cache = items.ToDictionary(item => item.name, item => item);
                        Debug.Log(cache.Count + " item loaded");
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableItems with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }*/

        void OnValidate()
        { // make sure that the sell price <= buy price to avoid exploitation
            sellPrice = System.Math.Min(sellPrice, buyPrice);
        }
    }
}
