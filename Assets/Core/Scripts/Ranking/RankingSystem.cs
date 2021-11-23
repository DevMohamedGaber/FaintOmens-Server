using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Game
{
    public class RankingSystem
    {
        // players
        public static RankingBasicData[] playerRankingBR;
        public static RankingBasicData[] playerRankingLvl;
        public static RankingBasicData[] playerRankingHnr;
        // guilds
        public static RankingBasicData[] guildRankingLvl;
        public static RankingBasicData[] guildRankingBR;
        // tribes
        public static RankingBasicData[] tribeRankingBR;
        public static RankingBasicData[] tribeRankingWins;
        // pets
        public static SummonableRankingData[] petRankingBR;
        public static SummonableRankingData[] petRankingLvl;
        //mounts
        public static SummonableRankingData[] mountRankingBR;
        public static SummonableRankingData[] mountRankingLvl;
        public static IEnumerator<WaitForSeconds> ServerUpdater(float interval)
        {
            while(true)
            {
                Refresh();
                yield return new WaitForSeconds(interval);
            }
        }
        public static async void Refresh()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            List<Task> tasks = new List<Task>();
            // players
            tasks.Add(Task.Run(() => {
                playerRankingBR = Database.singleton.LoadRankingPlayerBR();
            }));
            tasks.Add(Task.Run(() => {
                playerRankingLvl = Database.singleton.LoadRankingPlayerLvl();
            }));
            tasks.Add(Task.Run(() => {
                playerRankingHnr = Database.singleton.LoadRankingPlayerHnr();
            }));
            // guilds
            tasks.Add(Task.Run(() => {
                guildRankingLvl = Database.singleton.LoadRankingGuildLvl();
            }));
            tasks.Add(Task.Run(() => {
                guildRankingBR = Database.singleton.LoadRankingGuildBR();
            }));
            // tribes
            tasks.Add(Task.Run(() => {
                tribeRankingBR = Database.singleton.LoadRankingTribeBR();
            }));
            tasks.Add(Task.Run(() => {
                tribeRankingWins = Database.singleton.LoadRankingTribeWins();
            }));
            // pets
            tasks.Add(Task.Run(() => {
                petRankingBR = Database.singleton.LoadRankingPetBR();
            }));
            tasks.Add(Task.Run(() => {
                petRankingLvl = Database.singleton.LoadRankingPetLvl();
            }));
            // mounts
            tasks.Add(Task.Run(() => {
                mountRankingBR = Database.singleton.LoadRankingMountBR();
            }));
            tasks.Add(Task.Run(() => {
                mountRankingLvl = Database.singleton.LoadRankingMountLvl();
            }));
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            Debug.Log($"Ranking Refreshed in: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}