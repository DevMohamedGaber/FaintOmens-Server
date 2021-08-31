using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace Game
{
    public abstract class ScriptableQuest : ScriptableObjectNonAlloc
    {
        [Header("General")]
        public Vector3 location;

        [Header("Requirements")]
        public byte requiredLevel; // player.level
        public ScriptableQuest predecessor; // this quest has to be completed first
        public ScriptableQuest successor; // this quest has to be completed first

        [Header("Rewards")]
        public uint rewardGold;
        public uint rewardExperience;
        public ItemSlot[] rewardItems;

        // events
        public virtual void OnCrafted(Player player, int questIndex, ScriptableItem item, uint amount) {}
        public virtual void OnCompleted(Player player, Quest quest)
        {
            if(successor != null)
            {
                player.own.quests.Add(new Quest(successor));
            }
        }
        public virtual void OnKilled(Player player, int questIndex, Entity victim) {}
        public virtual void OnLocation(Player player, int questIndex, Collider location) {}
        public abstract bool IsFulfilled(Player player, Quest quest);
        // cache
        static Dictionary<ushort, ScriptableQuest> cache;
        public static Dictionary<ushort, ScriptableQuest> dict
        {
            get
            {
                if (cache == null) // not loaded yet?
                {
                    // get all ScriptableQuests in resources
                    ScriptableQuest[] quests = Resources.LoadAll<ScriptableQuest>("");

                    // check for duplicates, then add to cache
                    List<int> duplicates = quests.ToList().FindDuplicates(quest => quest.name);
                    if (duplicates.Count == 0)
                    {
                        cache = quests.ToDictionary(quest => (ushort)quest.name, quest => quest);
                    }
                    else
                    {
                        foreach (int duplicate in duplicates)
                            Debug.LogError("Resources folder contains multiple ScriptableQuests with the name " + duplicate + ". If you are using subfolders like 'Warrior/BeginnerQuest' and 'Archer/BeginnerQuest', then rename them to 'Warrior/(Warrior)BeginnerQuest' and 'Archer/(Archer)BeginnerQuest' instead.");
                    }
                }
                return cache;
            }
        }
    }
}