using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Mirror;
using Game.Network;
using Game.Components;
namespace Game.ControlPanel
{
    public class ServerInitialization : MonoBehaviour
    {
        [SerializeField] bool spawnMonsterAreas;
        [Header("Refresh Interval In Minutes")]
        [SerializeField] int tribeRI = 90;
        [SerializeField] int RankingRI = 180;
        [SerializeField] int teamRI = 120;
        [SerializeField] int GuildRI = 60;
        [Header("Refresh Interval In Hours")]
        [SerializeField] int MailRI = 12;
        NetworkManagerMMO manager;
        public void StartServer()
        {
            manager = GameObject.Find("NetworkManager").GetComponent<NetworkManagerMMO>();
            manager.StartServer();
            Task.Run(() => {
                SpawnAllNetworkIdentities();
            });
            LoadDBCashe();
            InitiateUpdaters();
            LoadAllScriptables();
        }
        public void StopServer()
        {
            manager.StopServer();
            StopCoroutine(TribeSystem.ServerUpdater(tribeRI * 60f));
            StopCoroutine(RankingSystem.ServerUpdater(RankingRI * 60f));
            StopCoroutine(TeamSystem.ServerUpdater(teamRI * 60f));
            StopCoroutine(GuildSystem.ServerUpdater(GuildRI * 60f));
            StopCoroutine(MailSystem.ServerUpdater(MailRI * 60f * 60f));
        }
        void InitiateUpdaters()
        {
            //StartCoroutine(HotEventsSystem.ServerUpdater());
            StartCoroutine(TribeSystem.ServerUpdater(tribeRI * 60f));
            StartCoroutine(RankingSystem.ServerUpdater(RankingRI * 60f));
            StartCoroutine(TeamSystem.ServerUpdater(teamRI * 60f));
            StartCoroutine(GuildSystem.ServerUpdater(GuildRI * 60f));
            StartCoroutine(MailSystem.ServerUpdater(MailRI * 60f * 60f));
            Debug.Log("All Updaters has been initialized.");
        }
        void LoadDBCashe()
        {
            GuildSystem.LoadGuilds();
            Database.singleton.SetNextGuildId();
            Database.singleton.SetNextTeamId();
        }
        async void SpawnAllNetworkIdentities()
        {
            MonstersSpawnArea[] spawnPoints = GameObject.FindObjectsOfType<MonstersSpawnArea>();
            int i;
            if(spawnMonsterAreas && spawnPoints.Length > 0) {
                for(i = 0; i < spawnPoints.Length; i++)
                {
                    spawnPoints[i].Init();
                }
            }
            Debug.Log("All NPCs/Monsters/Bosses has been spawned.");
        }
        void LoadAllScriptables() {
            ScriptableItem.LoadAll();
            ScriptableSkill.LoadAll();

            Debug.Log($"Loaded Scriptables: \n Items: {ScriptableItem.dict.Count} \n Skills: {ScriptableSkill.dict.Count}");
        }
        void Start() {
            Application.targetFrameRate = 30;
        }
    }
}