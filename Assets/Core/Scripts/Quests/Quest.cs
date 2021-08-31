using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    [Serializable]
    public struct Quest
    {
        public ushort id;
        public QuestType type;
        public Quality quality;
        public uint progress;
        public bool completed;

        public ItemSlot[] rewardItems => data.rewardItems;
        public int successor => data.successor != null ? data.successor.name : 0;
        public uint rewardGold => data.rewardGold;
        public int requiredLevel => data.requiredLevel;
        public int predecessor => data.predecessor != null ? data.predecessor.name : 0;
        public uint rewardExperience => data.rewardExperience;
        public ScriptableQuest data
        {
            get
            {
                if (!ScriptableQuest.dict.ContainsKey(id))
                    throw new KeyNotFoundException("There is no ScriptableQuest with hash=" + id + ". Make sure that all ScriptableQuests are in the Resources folder so they are loaded properly.");
                return ScriptableQuest.dict[id];
            }
        }
        
        public Quest(ScriptableQuest data)
        {
            id = (ushort)data.name;
            progress = 0;
            completed = false;
            type = QuestType.General;
            quality = 0;
        }
        // events
        public void OnKilled(Player player, int questIndex, Entity victim)
        {
            data.OnKilled(player, questIndex, victim);
        }
        public void OnLocation(Player player, int questIndex, Collider location)
        {
            data.OnLocation(player, questIndex, location);
        }

        // completion
        public bool IsFulfilled(Player player)
        {
            return data.IsFulfilled(player, this);
        }
        public void OnCompleted(Player player)
        {
            data.OnCompleted(player, this);
        }
    }
}