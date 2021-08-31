using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Components;
using Game.Arena;
namespace Game
{
    public class ArenaSystem
    {
        public static ArenaRoomsManager manager;
        #region 1v1
        public static List<ArenaRegisteredData> lobby1v1 = new List<ArenaRegisteredData>();
        public static Dictionary<short, ArenaMatch1v1> matches1v1 = new Dictionary<short, ArenaMatch1v1>();
        public static List<ArenaRankedInfo> ranking1v1 = new List<ArenaRankedInfo>();
        public static void Register1v1(Player player)
        {
            if(!player.CanTakeAction()) // guard
                return;
            // meet level requirement ?
            if(player.level < Storage.data.arena.minLvl)
            {
                player.Notify($"You need to be at level {Storage.data.arena.minLvl} to register in the Arena", $"يجب انت تكون اعلي من مستوي {Storage.data.arena.minLvl} لتسجل بالحلبة");
                return;
            }
            // is free ?
            if(player.IsOccupied())
                return;
            // have enough points to lose
            if(player.own.arena1v1Points < Storage.data.arena.pointsOnLoss)
            {
                player.Notify($"You need to have at least {Storage.data.arena.pointsOnLoss} AP to register", $"يجب انت تملك عل الاقل {Storage.data.arena.pointsOnLoss} لتسجل بالحلبة");
                return;
            }
            // check for a matching opponent
            if(lobby1v1.Count > 0)
            {
                for(int i = 0; i < lobby1v1.Count; i++)
                {
                    if(CheckLevel(player.level, lobby1v1[i].level) && Player.onlinePlayers.TryGetValue(lobby1v1[i].id, out Player player2))
                    {
                        ArenaMatch1v1 match = new ArenaMatch1v1
                        {
                            id = manager.a1v1.GetFreeRoom(),
                            player1 = player,
                            player2 = player2
                        };
                        match.NotifyPlayers();

                        manager.a1v1.rooms[match.id].SetUp(match);
                        matches1v1[match.id] = match;

                        lobby1v1.RemoveAt(i);
                        return;
                    }
                }
            }
            // no match then register
            lobby1v1.Add(new ArenaRegisteredData(player.id, (byte)player.level));
            player.own.occupation = PlayerOccupation.RegisteredArena1v1;
            player.Notify("You've registered in Arena, please wait for a match", "لقد سجلت بالحلبة, برجاء انتظار مباراة");
            player.NextAction();
        }
        public static void UnRegister1vs1(Player player)
        {
            if(!player.CanTakeAction()) // guard
                return;
            // meet level requirement ?
            if(player.level < Storage.data.arena.minLvl)
            {
                player.Notify($"You need to be at level {Storage.data.arena.minLvl} to register in the Arena", $"يجب انت تكون اعلي من مستوي {Storage.data.arena.minLvl} لتسجل بالحلبة");
                return;
            }
            
            if(player.own.occupation != PlayerOccupation.RegisteredArena1v1)
            {
                player.Notify("You're not registered", "انت غير مسجل");
                return;
            }
            if(lobby1v1.Count > 0)
            {
                for(int i = 0; i < lobby1v1.Count; i++)
                {
                    if(lobby1v1[i].id == player.id)
                    {
                        lobby1v1.RemoveAt(i);
                        break;
                    }
                }
            }
            player.own.occupation = PlayerOccupation.None;
            player.Notify("You're now unregistered", "انت الان غير مسجل");
            player.NextAction();
        }
        static bool CheckLevel(int lvl, int opLvl)
        {
            return lvl <= opLvl + Storage.data.arena.lvlDiff && lvl >= opLvl - Storage.data.arena.lvlDiff;
        }
        public static void AcceptMatch1v1(Player player)
        {
            if(player.own.occupation != PlayerOccupation.ReadyArena1v1)
            {
                player.Notify("You're not registered in a match");
                return;
            }
            if(matches1v1.TryGetValue(player.occupationId, out ArenaMatch1v1 match))
            {
                if(match.Accept(player.id))
                {
                    matches1v1[player.occupationId] = match;
                }
                else
                {
                    player.Notify("You're not part of this match");
                    player.Log($"[ArenaSystem.RefuseMatch1v1] Not part of the match p1={match.player1.id} p2={match.player2.id}");
                }
            }
            else
            {
                player.Notify("Invalid match id");
                player.Log($"[ArenaSystem.RefuseMatch1v1] Invalid occupationId={player.occupationId}");
            }
        }
        public static void RefuseMatch1v1(Player player)
        {
            if(player.own.occupation != PlayerOccupation.ReadyArena1v1)
            {
                player.Notify("You're not registered in a match");
                return;
            }
            if(matches1v1.TryGetValue(player.occupationId, out ArenaMatch1v1 match))
            {
                if(match.Refuse(player.id))
                {
                    manager.a1v1.rooms[match.id].isFree = true;
                    matches1v1.Remove(match.id);
                }
                else
                {
                    player.Notify("You're not part of this match");
                    player.Log($"[ArenaSystem.RefuseMatch1v1] Not part of the match p1={match.player1.id} p2={match.player2.id}");
                }
            }
            else
            {
                player.Notify("Invalid match id");
                player.Log($"[ArenaSystem.RefuseMatch1v1] Invalid occupationId={player.occupationId}");
            }
        }
        public static void ConfirmTeleportToMatch1v1(Player player)
        {
            if(matches1v1.TryGetValue(player.occupationId, out ArenaMatch1v1 match))
            {
                match.ConfirmTeleport(player.id);
                matches1v1[match.id] = match;
            }
            else
            {
                player.Notify("Invalid match id");
                player.Log($"[ArenaSystem.ConfirmTeleportToMatch1v1] Invalid occupationId={player.occupationId}");
            }
        }
        public static void AddDamage1v1(Player player, int dmg)
        {
            matches1v1[player.occupationId].AddDamage(player.id, dmg);
            /*if(matches1v1.TryGetValue(player.occupationId, out ArenaMatch1v1 match)) {
                
            }
            else {
                player.Notify("Invalid match id");
                player.Log($"[ArenaSystem.AddDamage1v1] Invalid occupationId={player.occupationId}");
            }*/
        }
        public static void LeaveMatch1v1(Player player) {
            if(player.own.occupation != PlayerOccupation.InMatchArena1v1)
            {
                player.Notify("You're not in a match");
                return;
            }
            if(matches1v1.TryGetValue(player.occupationId, out ArenaMatch1v1 match))
            {
                if(match.LeaveMatch(player.id))
                {
                    matches1v1[match.id] = match;
                }
                else
                {
                    player.Notify("You're not part of this match");
                    player.Log($"[ArenaSystem.RefuseMatch1v1] Not part of the match p1={match.player1.id} p2={match.player2.id}");
                }
            }
            else
            {
                player.Notify("Invalid match id");
                player.Log($"[ArenaSystem.RefuseMatch1v1] Invalid occupationId={player.occupationId}");
            }
        }
        #endregion
    }
}