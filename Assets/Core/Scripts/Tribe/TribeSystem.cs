using System;
using System.Linq;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
namespace Game
{
    public class TribeSystem
    {
        // cashe data
        public static Dictionary<byte, Tribe> tribes = new Dictionary<byte, Tribe>();
        public static Dictionary<byte, List<uint>> OnlineTribesMembers = new Dictionary<byte, List<uint>>();
        public static byte[] tribesIds;
        // rules
        public static TribeRank PromoteMinRank = TribeRank.King; // includes Demote
        public static TribeRank RecallMinRank = TribeRank.Royalty; // call players to where he/she stands

        /*static void BroadcastChanges(byte id, Tribe tribe)
        {
            for(int i = 0; i < OnlineTribesMembers[id].Count; i++)
            {
                if(Player.onlinePlayers.TryGetValue(OnlineTribesMembers[id][i], out Player player))
                {
                    player.tribe = tribe;
                }
            }
            tribes[id] = tribe;
        }*/
        public static bool ValidateId(byte tribeId)
        {
            for(int i = 0; i < tribesIds.Length; i++)
            {
                if(tribesIds[i] == tribeId)
                    return true;
            }
            return false;
        }
        public static bool Recall(Player player)
        {
            if(player.own.tribeRank >= RecallMinRank)
            {
                TribeRecallRequest req = new TribeRecallRequest(player.name, player.own.cityId, player.transform.position);
                int minLvl = Storage.data.cities[player.own.cityId].minLvl;
                List<Player> members = Player.onlinePlayers.Values.Where(p => p.tribeId == player.tribeId && p.level >= minLvl).ToList();
                //for(int i = 0; i < members.Count; i++) Player.onlinePlayers[members[i].id].tribeRecallRequest = req;
                return true;
            }
            return false;
        }
        public static void LoadTribes()
        {
            tribes = Database.singleton.LoadAllTribes();
            tribesIds = tribes.Keys.ToArray();
        }
        public static void Donate(byte id, uint gold, uint diamonds)
        {
            if(tribes.TryGetValue(id, out Tribe tribe))
            {
                tribe.Wealth += gold + (diamonds * 10000);
            }
        }
        public static void EditTroops(byte id, bool decrease = false)
        {
            if(tribes.TryGetValue(id, out Tribe tribe))
            {
                if(decrease) tribe.Troops--;
                else tribe.Troops++;
                tribes[id] = tribe;
            }
        }
        public static void RefreashTotalBR()
        {
            for(int i = 0; i < tribesIds.Length; i++)
            {
                if(tribes[tribesIds[i]].Troops < 1) continue;
                Tribe tribe = tribes[tribesIds[i]];
                tribe.TotalBR = Database.singleton.SumTribeTotalBR(tribesIds[i]);
                tribes[tribesIds[i]] = tribe;
            }
        }
        public static void SaveAll()
        {
            if(tribes.Count > 0)
                RefreashTotalBR();
            Database.singleton.SaveAllTribes();
            Debug.Log("Tribes has been saved.");
        }
        public static IEnumerator<WaitForSeconds> ServerUpdater(float interval)
        {
            while(true)
            {
                yield return new WaitForSeconds(interval);
                SaveAll();
            }
        }
    }
}