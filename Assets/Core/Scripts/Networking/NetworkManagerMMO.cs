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
            print("OnCharacterSelect");
            // only while in lobby (aka after handshake and not ingame)
            if (!lobby.ContainsKey(conn))
            {
                UIManager.data.logsList.Add("CharacterSelect: not in lobby " + conn);
                ServerSendError(conn, NetworkError.AccountNotInLobby, NetworkErrorAction.Disconnect);
                return;
            }
            // validate id
            if(!Server.IsPlayerIdWithInServer(message.id))
            {
                UIManager.data.logsList.Add($"invalid accId: {lobby[conn]} charId: {message.id}");
                ServerSendError(conn, NetworkError.InvalidCharacterId);
                return; 
            }
            
            GameObject go = GameObject.Instantiate(ScriptableClass.dict[PlayerClass.Warrior].prefab);
            bool loaded = await Database.singleton.CharacterLoad(message.id, lobby[conn], go.GetComponent<Player>());

            if(loaded)
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
            if(Database.singleton.CharactersCount(lobby[conn]) == Storage.data.account.charactersCount)
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
            Database.singleton.CharacterCreate(message, lobby[conn]);
            // send to select
            var msg = await Database.singleton.LoadAvailableCharacters(lobby[conn]);
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
                    await DoServerDisconnect(conn, delay, player.transform.position); // its not a disconnection ??
                    lobby[conn] = player.accId;
                    var msg = await Database.singleton.LoadAvailableCharacters(player.accId);
                    conn.Send(msg);
                    UIManager.data.homePage.UpdateLobbyCount();
                }
            }
        }
        public override async void OnServerDisconnect(NetworkConnection conn)
        {
            Debug.Log("OnServerDisconnect");
            float delay = 0;
            if (conn.identity != null)
            {
                Player player = conn.identity.GetComponent<Player>();
                delay = (float)player.remainingLogoutTime;
                await DoServerDisconnect(conn, delay, player.transform.position);
                base.OnServerDisconnect(conn);
            }
        }
        async Task DoServerDisconnect(NetworkConnection conn, float remainingLogoutTime, Vector3 position)
        {
            int delay = (int)(remainingLogoutTime * 1000);
            if(delay > 0)
            {
                await Task.Delay(delay);
            }
            if (conn.identity != null)
            {
                await Database.singleton.CharacterLogOff(conn.identity.GetComponent<Player>(), position);
                UIManager.data.homePage.UpdateOnlineCount();
                print("logedOff: " + conn.identity.name);
            }
            else 
            {
                lobby.Remove(conn);
                UIManager.data.homePage.UpdateLobbyCount();
            }
        }
        async void SavePlayers()
        {
            if (Player.onlinePlayers.Count > 0)
            {
                await Database.singleton.CharacterSaveMany(Player.onlinePlayers.Values);
                Debug.Log($"saved {Player.onlinePlayers.Count} players at {System.DateTime.Now}");
            }
        }
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            Debug.LogWarning("Use the CharacterSelectMsg instead");
        }
        // helpers
        public static bool IsAllowedCharacterName(string characterName)
        {
            return characterName.Length <= Storage.data.account.usernameMaxLength && Regex.IsMatch(characterName, @"^[a-zA-Zء-ي0-9_]+$");
        }
        public static bool CheckCharacterName(string characterName)
        {
            return characterName.Length <= Storage.data.account.usernameMaxLength &&
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
    }
}