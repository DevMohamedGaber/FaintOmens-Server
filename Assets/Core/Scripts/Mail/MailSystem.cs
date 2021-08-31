using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    public class MailSystem
    {
        public static void Send(Mail mail, uint recieverId) {
            if(!Server.IsPlayerIdWithInServer(recieverId)) return;
            if(Player.onlinePlayers.TryGetValue(recieverId, out Player player)) {
                mail.sentAt = DateTime.Now.ToOADate();
                player.own.mailBox.Insert(0, mail);
            }
            else
                Database.singleton.SaveMail(mail, recieverId);
        }
        public static void BroadcastToOnline(Mail mail) {
            if(Player.onlinePlayers.Count > 0) {
                mail.sentAt = DateTime.Now.ToOADate();
                foreach(Player player in Player.onlinePlayers.Values)
                    player.own.mailBox.Insert(0, mail);
            }
        }
        public static void BroadcastToAll(Mail mail) {
            BroadcastToOnline(mail);
            Database.singleton.BroadcastMailToAllOfflinePlayers(mail);
        }
        public static IEnumerator<WaitForSeconds> ServerUpdater(float interval) {
            while(true) {
                Database.singleton.ClearOutDatedMails();
                yield return new WaitForSeconds(interval);
            }
        }
    }
}