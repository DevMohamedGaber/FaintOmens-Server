using UnityEngine;
using System.Linq;
using System.Collections.Generic;
namespace Game.Achievements
{
    public abstract class ScriptableAchievement : ScriptableObjectNonAlloc
    {
        public AchievementTypes type;
        public ScriptableAchievement successor;
        public byte points;
        public ItemSlot[] rewards;
        public virtual void OnAchieved(Player player)
        {
            player.own.achievements.Add(new Achievement((ushort)name));
            // add achievementPoints
            player.own.archive.achievementPoints += points;
            if(player.inprogressAchievements.achievementPoints != null && 
                player.inprogressAchievements.achievementPoints.IsFulfilled(player))
                player.inprogressAchievements.achievementPoints.OnAchieved(player);
        }
        //cash
        static Dictionary<int, ScriptableAchievement> cache;
        public static Dictionary<int, ScriptableAchievement> dict
        {
            get
            {
                if (cache == null)
                {
                    ScriptableAchievement[] items = Resources.LoadAll<ScriptableAchievement>("");
                    List<int> duplicates = items.ToList().FindDuplicates(item => item.name);
                    if (duplicates.Count == 0)
                    {
                        cache = items.ToDictionary(item => item.name, item => item);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableAchievement with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                    }
                }
                return cache;
            }
        }
    }
}