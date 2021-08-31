using UnityEngine;
using Mirror;
namespace Game.Components
{
    public class ChatComponent : NetworkBehaviourNonAlloc
    {
        public Player player;
        ChatSenderInfo playerInfo => new ChatSenderInfo(player.id, name, player.avatar, player.frame, player.own.vip.level);
        //public int maxLength = 70;
        bool IsValid(string msg) {
            if(string.IsNullOrWhiteSpace(msg)) {
                player.TargetNotify("No Message");
                return false;
            }
            if(msg.Length > Storage.data.player.chatMsgMaxLength) {
                player.TargetNotify("Message is More than 70 characters");
                return false;
            }
            return true;
        }
        static bool _IsValid(string msg) {
            if(string.IsNullOrWhiteSpace(msg) || msg.Length > Storage.data.player.chatMsgMaxLength)
                return false;
            return true;
        }
        [Command] void CmdMsgWorld(string message) {
            if (!IsValid(message)) return;
            ChatMessage msg = new ChatMessage(playerInfo, ChatChannels.World, message);
            foreach(Player onlinePlayer in Player.onlinePlayers.Values) {
                onlinePlayer.chat.TargetMsgGeneral(msg);
            }
        }
        [Command] void CmdMsgTribe(string message) {
            if (!IsValid(message) || player.tribeId > 0) return;
            ChatMessage msg = new ChatMessage(playerInfo, ChatChannels.Tribe, message);
            for(int i = 0; i < TribeSystem.OnlineTribesMembers[player.tribeId].Count; i++) {
                if(Player.onlinePlayers.TryGetValue(TribeSystem.OnlineTribesMembers[player.tribeId][i], out Player onlinePlayer)) {
                    onlinePlayer.chat.TargetMsgGeneral(msg);
                }
            }
        }
        [Command] void CmdMsgGuild(string message) {
            if (!IsValid(message) || !player.InGuild()) return;
            ChatMessage msg = new ChatMessage(playerInfo, ChatChannels.Guild, message);
            for (int i = 0; i < GuildSystem.members[player.guild.id].Length; i++) {
                if(Player.onlinePlayers.TryGetValue(GuildSystem.members[player.guild.id][i].id, out Player onlinePlayer)) {
                    onlinePlayer.chat.TargetMsgGeneral(msg);
                }
            }
        }
        [Command] void CmdMsgLocal(string message) {
            if (!IsValid(message)) return;
            // it's local chat, so let's send it to all observers via ClientRpc
            RpcMsgLocal(new ChatMessage(playerInfo, ChatChannels.Local, message));
        }
        [Command] void CmdMsgTeam(string message) {
            if (!IsValid(message) || player.teamId < 1) return;
            ChatMessage msg = new ChatMessage(playerInfo, ChatChannels.Team, message);
            for(int i = 0; i < player.own.team.members.Length; i++) {
                if(Player.onlinePlayers.TryGetValue(player.own.team.members[i].id, out Player onlinePlayer)) {
                    onlinePlayer.chat.TargetMsgGeneral(msg);
                }
            }
        }
        [Command] void CmdMsgWhisper(uint playerId, string message) {
            if (!IsValid(message)) return;
            if(!Server.IsPlayerIdWithInServer(playerId)) {
                player.TargetNotify("Please put a valid player id");
                return;
            }
            if (Player.onlinePlayers.TryGetValue(playerId, out Player onlinePlayer)) {
                ChatMessage msg = new ChatMessage(playerInfo, ChatChannels.Whisper, message);
                onlinePlayer.chat.TargetMsgGeneral(msg);
                TargetMsgGeneral(msg);
            }
            else player.TargetNotify("This player is offline");
        }
        [Server] public void SendSystemMsgToMe(string message) {
            if (!IsValid(message)) return;
            TargetMsgGeneral(new ChatMessage(ChatSenderInfo.Empty, ChatChannels.System, message));
        }
        [Server] public static void SendSystemMsg(string message) {
            if (!_IsValid(message)) return;
            ChatMessage msg = new ChatMessage(ChatSenderInfo.Empty, ChatChannels.System, message);
            foreach(Player onlinePlayer in Player.onlinePlayers.Values)
                onlinePlayer.chat.TargetMsgGeneral(msg);
        }
        [Server] public static void SendSystemMsgTo(uint id, string message) {
            if (!_IsValid(message)) return;
            if(Player.onlinePlayers.TryGetValue(id, out Player target))
                target.chat.TargetMsgGeneral(new ChatMessage(ChatSenderInfo.Empty, ChatChannels.System, message));
        }
        [TargetRpc] public void TargetMsgGeneral(ChatMessage msg) {}
        [ClientRpc] public void RpcMsgLocal(ChatMessage msg) {}
    }
}