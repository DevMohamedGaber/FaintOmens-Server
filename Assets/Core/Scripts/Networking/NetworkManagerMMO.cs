using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mirror;
using Game.Network.Messages;
using Game.ControlPanel;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Game.Network
{
    public class NetworkManagerMMO : NetworkManager
    {
        public Dictionary<NetworkConnection, ulong> lobby = new Dictionary<NetworkConnection, ulong>();
        [Header("Database")]
        public int characterLimit = 4;
        public static int characterNameMaxLength = 16;
        public float saveInterval = 60f; // in seconds
        public override void OnStartServer()
        {
            // handshake packet handlers (in OnStartServer so that reconnecting works)
            NetworkServer.RegisterHandler<CharacterCreate>(OnCharacterCreate);
            NetworkServer.RegisterHandler<CharacterSelect>(OnCharacterSelect);
            NetworkServer.RegisterHandler<CharacterDelete>(OnCharacterDelete);
            NetworkServer.RegisterHandler<ReturnToLobby>(OnReturnToLobby);
            // invoke saving
            InvokeRepeating(nameof(SavePlayers), saveInterval, saveInterval);
        }
        public override void OnStopServer()
        {
            Database.singleton.SaveServerData();
            TribeSystem.SaveAll();
            Database.singleton.SaveGuilds();
            CancelInvoke(nameof(SavePlayers));
        }
        public void ServerSendError(NetworkConnection conn, NetworkError error, NetworkErrorAction action = NetworkErrorAction.None)
        {
            conn.Send(new Error {
                error = error,
                action = action
            });
        }
        
        public override async void OnServerConnect(NetworkConnection conn)
        {
            var msg = await Database.singleton.LoadAvailableCharacters(lobby[conn]);
            conn.Send(msg);
        }
        async void OnCharacterSelect(NetworkConnection conn, CharacterSelect message)
        {
            //double StartOps = System.DateTime.Now.ToOADate(); // Note: for test
            // only while in lobby (aka after handshake and not ingame)
            if (lobby.ContainsKey(conn))
            {
                // validate index
                if(Server.IsPlayerIdWithInServer(message.id))
                {
                    GameObject go = await Database.singleton.CharacterLoad(message.id, lobby[conn]);
                    if(go != null)
                    {
                        // add to client
                        NetworkServer.AddPlayerForConnection(conn, go);
                        Database.singleton.SetCharacterOnline(message.id);
                        lobby.Remove(conn);
                        UIManager.data.homePage.UpdateLobbyCount();
                        // Note: update master server
                    }
                    else
                    {
                        UIManager.data.logsList.Add($"invalid accId: {lobby[conn]} charId: {message.id}");
                        ServerSendError(conn, NetworkError.InvalidCharacterId);
                    }
                }
                else
                {
                    UIManager.data.logsList.Add($"invalid accId: {lobby[conn]} charId: {message.id}");
                    ServerSendError(conn, NetworkError.InvalidCharacterId);
                }
            }
            else
            {
                UIManager.data.logsList.Add("CharacterSelect: not in lobby " + conn);
                ServerSendError(conn, NetworkError.AccountNotInLobby, NetworkErrorAction.Disconnect);
            }
            //print($"({conn}) Selected in: {((double)System.DateTime.Now.ToOADate() - StartOps)}"); // Note: for test
        }
        async void OnCharacterCreate(NetworkConnection conn, CharacterCreate message)
        {
            // guards
            if(!lobby.ContainsKey(conn))
            {
                ServerSendError(conn, NetworkError.AccountNotInLobby, NetworkErrorAction.Disconnect);
                return;
            }
            if(!IsAllowedCharacterName(message.name))
            {
                ServerSendError(conn, NetworkError.NameNotAllawed);
                return;
            }
            if(!ScriptableClass.dict.ContainsKey(message.classId))
            {
                ServerSendError(conn, NetworkError.ChooseClass);
                return;
            }
            if(message.gender != Gender.Male && message.gender != Gender.Female)
            {
                ServerSendError(conn, NetworkError.ChooseGender);
                return;
            }
            if(!TribeSystem.ValidateId(message.tribeId))
            {
                ServerSendError(conn, NetworkError.ChooseTribe);
                return;
            }
            if(Database.singleton.CharactersCount(lobby[conn]) == Storage.data.charactersPerAccount)
            {
                ServerSendError(conn, NetworkError.MaxCharacters);
                return;
            }
            if(Database.singleton.CharacterExists(message.name))
            {
                ServerSendError(conn, NetworkError.NameExists);
                return;
            }
            // create
            ScriptableClass sClass = ScriptableClass.dict[message.classId];
            Player player = Instantiate(sClass.prefab).GetComponent<Player>();
            player.name = message.name;
            player.accId = lobby[conn];
            player.tribeId = message.tribeId;
            player.gender = message.gender;
            if(sClass.defaultEquipments.Length > 0)
            {
                player.equipment.Initiate(Storage.data.player.equipmentCount);
                for(int i = 0; i < sClass.defaultEquipments.Length; i++)
                {
                    player.equipment[(int)((EquipmentItem)sClass.defaultEquipments[i].item.data).category] = sClass.defaultEquipments[i];
                }
            }
            //if(player.defaultItems.Length > 0) {
            //   for(int i = 0; i < player.defaultItems.Length; i++)
            //        player.own.inventory.Add(new ItemSlot(new Item(player.defaultItems[i].item), player.defaultItems[i].amount));
            //}
            player.health = player.healthMax;
            player.mana = player.manaMax;
            Database.singleton.CharacterCreate(player);
            Destroy(player.gameObject);
            // send to select
            var msg = await Database.singleton.LoadAvailableCharacters(player.accId);
            conn.Send(msg);
            UIManager.data.homePage.UpdateCharacterCount();
        }
        async void OnCharacterDelete(NetworkConnection conn, CharacterDelete message)
        {
            // guards
            if(!lobby.ContainsKey(conn))
            {
                ServerSendError(conn, NetworkError.AccountNotInLobby, NetworkErrorAction.Disconnect);
                return;
            }
            if(!Server.IsPlayerIdWithInServer(message.id))
            {
                ServerSendError(conn, NetworkError.InvalidCharacterId);
                return;
            }
            Database.singleton.CharacterDelete(message.id, lobby[conn]);
            var msg = await Database.singleton.LoadAvailableCharacters(lobby[conn]);
            conn.Send(msg);
            UIManager.data.homePage.UpdateCharacterCount(-1);
        }
        async void OnReturnToLobby(NetworkConnection conn, ReturnToLobby message)
        {
            Player player = conn.identity.GetComponent<Player>();
            if(Player.onlinePlayers.ContainsKey(player.id))
            {
                if(!lobby.ContainsKey(conn))
                {
                    float delay = conn.identity != null ? (float)player.remainingLogoutTime : 0;
                    StartCoroutine(DoServerDisconnect(conn, delay)); // its not a disconnection ??
                    lobby[conn] = player.accId;
                    var msg = await Database.singleton.LoadAvailableCharacters(player.accId);
                    conn.Send(msg);
                    UIManager.data.homePage.UpdateLobbyCount();
                }
            }
        }
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            Debug.Log("OnServerDisconnect");
            float delay = 0;
            if (conn.identity != null)
            {
                Player player = conn.identity.GetComponent<Player>();
                delay = (float)player.remainingLogoutTime;
            }
            StartCoroutine(DoServerDisconnect(conn, delay));
        }
        IEnumerator<WaitForSeconds> DoServerDisconnect(NetworkConnection conn, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (conn.identity != null)
            {
                Database.singleton.CharacterLogOff(conn.identity.GetComponent<Player>());
                UIManager.data.homePage.UpdateOnlineCount();
                print("logedOff: " + conn.identity.name);
            }
            else 
            {
                lobby.Remove(conn);
                UIManager.data.homePage.UpdateLobbyCount();
            }
            base.OnServerDisconnect(conn);
        }
        void SavePlayers()
        {
            Database.singleton.CharacterSaveMany(Player.onlinePlayers.Values);
            if (Player.onlinePlayers.Count > 0)
            {
                Debug.Log("saved " + Player.onlinePlayers.Count + " player(s)");
            }
        }
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            Debug.LogWarning("Use the CharacterSelectMsg instead");
        }
        // helpers
        public static bool IsAllowedCharacterName(string characterName)
        {
            return characterName.Length <= characterNameMaxLength && Regex.IsMatch(characterName, @"^[a-zA-Zء-ي0-9_]+$");
        }
        public static bool CheckCharacterName(string characterName)
        {
            return characterName.Length <= characterNameMaxLength &&
                Regex.IsMatch(characterName, @"^[a-zA-Zء-ي0-9_]+$");
        }
        public static void Quit()
        {
    #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
        public override void Awake()
        {
            base.Awake();
            /*foreach(GameObject prefab in spawnPrefabs) {
                Player player = prefab.GetComponent<Player>();
                if(player != null)
                    Storage.data.classPrefabs[player.classInfo.type] = prefab;
            }*/
        }
    }
}