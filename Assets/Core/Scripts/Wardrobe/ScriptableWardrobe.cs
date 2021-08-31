using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace Game
{
    [CreateAssetMenu(menuName = "Custom/ScriptableWardrobe", order = 0)]
    public class ScriptableWardrobe : ScriptableObjectNonAlloc
    {
        [Header("General")]
        public ClothingCategory category;
        public ushort itemId;
        [Header("Bonus")]
        public LinearInt hp;
        public LinearInt mp;
        public LinearInt atk;
        public LinearInt def;
        public static bool HasWardropItem(SyncListWardrop wardropList, short itemId)
        {
            if(wardropList.Count > 0)
            {
                for(int i = 0; i < wardropList.Count; i++)
                {
                    if(wardropList[i].id == itemId)
                        return true;
                }
            }
            return false;
        }

        //cash
        static Dictionary<int, ScriptableWardrobe> cache;
        public static Dictionary<int, ScriptableWardrobe> dict
        {
            get
            {
                if(cache == null)
                {
                    ScriptableWardrobe[] items = Resources.LoadAll<ScriptableWardrobe>("");
                    List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
                    if(duplicates.Count == 0)
                    {
                        cache = items.ToDictionary(item => item.name, item => item);
                    }
                    else
                    {
                        foreach(int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableWardrobe with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}