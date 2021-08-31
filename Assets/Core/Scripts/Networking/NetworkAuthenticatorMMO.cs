using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Game.Network.Messages;
using Game.ControlPanel;
namespace Game.Network
{
    public class NetworkAuthenticatorMMO : NetworkAuthenticator
    {
        [Header("Components")]
        public NetworkManagerMMO manager;
        void OnServerLogin(NetworkConnection conn, Login message)
        {
            if(IsAllowedAccountName(message.account))
            {
                ulong accId = Database.singleton.TryLogin(message.account, message.password);// validate account info
                if(accId > 1)
                {
                    // not in lobby and not in world yet?
                    if(!AccountLoggedIn(accId))
                    {
                        manager.lobby[conn] = accId;
                        conn.Send(new LoginSuccess(TribeSystem.tribesIds, Server.timeZone));
                        ServerAccept(conn);// authenticate on server
                        UIManager.data.homePage.UpdateLobbyCount();
                    }
                    else
                    {
                        manager.ServerSendError(conn, NetworkError.AlreadyLogedIn, NetworkErrorAction.Disconnect);
                    }
                }
                else if(accId == 1)
                {
                    manager.ServerSendError(conn, NetworkError.AccountBanned, NetworkErrorAction.Disconnect);
                }
                else
                {
                    manager.ServerSendError(conn, NetworkError.InvalidAccountOrPassword, NetworkErrorAction.Disconnect);
                }
            }
            else
            {
                manager.ServerSendError(conn, NetworkError.InvalidAccount, NetworkErrorAction.Disconnect);
            }
        }
        public bool AccountLoggedIn(ulong accId) {
            if(manager.lobby.ContainsValue(accId))
                return true;
            foreach(Player player in Player.onlinePlayers.Values) {
                if(player.accId == accId)
                    return true;
            }
            return false;
        }
        public override void OnStartServer() {
            NetworkServer.RegisterHandler<Login>(OnServerLogin, false);
        }

        public override void OnServerAuthenticate(NetworkConnection conn)
        {
            // wait for LoginMsg from client
        }

        public bool IsAllowedAccountName(string account)
        {
            // not too long?
            // only contains letters, number and underscore and not empty (+)?
            // (important for database safety etc.)
            return account.Length <= Storage.data.account.usernameMaxLength &&
                Regex.IsMatch(account, @"^[a-zA-Z0-9_]+$");
        }
        // client (useless)
        public override void OnStartClient() {}
        
        public override void OnClientAuthenticate() {}
    }  
}