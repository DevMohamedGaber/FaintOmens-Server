using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    public class HotEventsSystem
    {
        public static List<HotEvent> events = new List<HotEvent>();
        public static string eventsIds;
        public static Dictionary<HotEventTypes, bool> checks = new Dictionary<HotEventTypes, bool>();

        public static void ResetChecks() {
            var types = Enum.GetValues(typeof(HotEventTypes));
            foreach(HotEventTypes type in types) {
                checks[type] = false;
            }
        }
        public static HotEventObjective[] DecodeObjectives(string objectivesString, string rewardsString) {
            //objectives formela = "type:amount,type:amount,type:amount"
            //rewards formela = "type:amount-type:amount-type:amount,type:amount-type:amount,type:amount"
            string[] objectives = objectivesString.Split(',');
            string[] rewards = rewardsString.Split(',');

            HotEventObjective[] results = new HotEventObjective[objectives.Length];

            for(int i = 0; i < objectives.Length; i++)
            {
                string[] objectiveData = objectives[i].Split(':');
                string[] rewardsData = rewards[i].Split('-');

                results[i].type = objectiveData[0];
                results[i].amount = Convert.ToInt32(objectiveData[1]);
                results[i].rewards = new HotEventReward[rewardsData.Length];
                for(int rs = 0; rs < rewardsData.Length; rs++)
                {
                    string[] eventRewards = rewardsData[rs].Split(':');
                    results[i].rewards[rs] = new HotEventReward(eventRewards[0], Convert.ToInt32(eventRewards[1]));
                }
            }
            return results;
        }
        public static string[] EncodeObjectives(HotEventObjective[] data) {
            string[] result = new string[2];
            for(int i = 0; i < data.Length; i++) {
                result[0] += $"{data[i].type}:{data[i].amount}";
                if(i < data.Length - 1) result[0] += ",";
                for(int r = 0; r < data[i].rewards.Length; r++) {
                    result[1] += $"{data[i].rewards[r].type}:{data[i].rewards[r].amount}";
                    if(r == data[i].rewards.Length - 1) result[1] += ",";
                    else result[1] += "-";
                }
            }
            return result;
        }
        public static void ResetHotEvents() {
            Database.singleton.LoadHotEvents();
            RestIdsString();
        }
        static void RestIdsString() {
            eventsIds = "(";
            for(int i = 0; i < events.Count; i++) {
                if(i < events.Count - 1)
                    eventsIds += $"{events[i].id},";
                else
                    eventsIds += $"{events[i].id})";
            }
        }
        static int[] EventsIndexByType(HotEventTypes type) {
            List<int> res = new List<int>();
            if(events.Count > 0) {
                for(int i = 0; i < events.Count; ++i) {
                    if(events[i].type == type) {
                        res.Add(i);
                    }
                }
            }
            return res.ToArray();
        }
        public static int EventsCountByType(HotEventTypes type) {
            return events.FindAll(e => e.type == type).Count;
        }
        public static bool IsFulfilled(Player player, int eventIndex, int objectiveIndex) {
            /*HotEventObjective objective = events[eventIndex].objectives[objectiveIndex];
            HotEventProgress progress = player.own.HotEventsProgress[eventIndex];
            
            if(!events[eventIndex].renewable && progress.completeTimes[objectiveIndex] > 0) return false;
            if(events[eventIndex].type == HotEventTypes.LevelUp) {
                return player.level >= objective.amount;
            }
            else if(events[eventIndex].type == HotEventTypes.BR) {
                return player.battlepower >= objective.amount;
            }
            else if(events[eventIndex].type == HotEventTypes.GatherItem) {
                return player.InventoryCountById(Convert.ToInt32(objective.type)) >= objective.amount;
            }
            else {
                return progress.progress >= objective.amount;
            }*/
            return false;
        }
        public static void OnCrafted(Player player, string itemName, int amount) {
            List<HotEvent> craftEvents = events.FindAll(e => e.type == HotEventTypes.Craft);
            if(craftEvents.Count > 0) {
                for (int i = 0; i < craftEvents.Count; i++) {
                    for(int o = 0; o < events[i].objectives.Length; o++) {
                        if(events[i].objectives[o].type == itemName) {
                            /*HotEventProgress progress = player.own.HotEventsProgress[i];
                            progress.progress += amount > 0 ? amount : 0;
                            player.own.HotEventsProgress[i] = progress;*/
                        }
                    }
                }
            }
        }
        public static void OnKilled(Player player, int eventIndex, int objectiveIndex) {

        }
        public static void OnCurrency(Player player, HotEventTypes type, long amount) {
            List<HotEvent> currencyEvents = events.FindAll(e => e.type == type);
            if(currencyEvents.Count > 0) {
                for (int i = 0; i < currencyEvents.Count; i++) {
                    /*HotEventProgress progress = player.own.HotEventsProgress[i];
                    progress.progress += amount > 0 ? (int)amount : 0;
                    player.own.HotEventsProgress[i] = progress;*/
                }
            }
        }
        public static void OnTimeFinished(int eventIndex) {

        }
        public static IEnumerator<WaitForSeconds> ServerUpdater() {
            while(true) {
                ResetHotEvents();
                Debug.Log("Hot Events Has Been Checked.");
                yield return new WaitForSeconds(Utils.SecondsUntilNewDay(-10));
            }
        }
    }
}