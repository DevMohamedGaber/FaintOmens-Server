using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Recipe", order=0)]
    public class ScriptableRecipe : ScriptableObjectNonAlloc
    {
        public Item result; // item out
        public uint cost;
        public ScriptableItemAndAmount[] ingredients; // items in
        public virtual bool CanCraft(Player player, uint count)
        {
            for(int i = 0; i < ingredients.Length; i++)
            {
                if(ingredients[i].amount > 0 && ingredients[i].item != null)
                {
                    if(player.InventoryCountById(ingredients[i].item.name) < ingredients[i].amount)
                        return false; 
                }
            }
            return true;
        }
        public virtual uint MaxCraftable(Player player)
        {
            uint smallest = 0;
            for(int i = 0; i < ingredients.Length; i++)
            {
                uint itemCount = player.InventoryCountById(ingredients[i].item.name);
                if(itemCount > 0)
                {
                    uint needed = itemCount - (itemCount % ingredients[i].amount);
                    if((needed / ingredients[i].amount) < smallest || smallest == 0) 
                        smallest = needed / ingredients[i].amount;
                }
            }
            return smallest;
        }
        static Dictionary<int, ScriptableRecipe> cache;
        public static Dictionary<int, ScriptableRecipe> dict
        { 
            get
            {
                if (cache == null)
                {// not loaded yet?
                    ScriptableRecipe[] recipes = Resources.LoadAll<ScriptableRecipe>("");// get all ScriptableRecipes in resources
                    List<int> duplicates = recipes.ToList().FindDuplicates(recipe => recipe.name); // check for duplicates, then add to cache
                    if (duplicates.Count == 0)
                    {
                        cache = recipes.ToDictionary(recipe => recipe.name, recipe => recipe);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableRecipes with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}
