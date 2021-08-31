/*
LC:
(*) player send invitation to targetPlayer
(*) targetPlayer accepts invitation
(*) show ui to both
(*) each player confirms their offer
(*) each player accepts the offers

TODO:   
    - switch [confirm] to int insted of bool to set max confirmations to 3
    - check if player confirmed before validation and exchange
*/
using System.Collections.Generic;
namespace Game
{
    public class TradeSystem
    {
        static Dictionary<uint, Trade> trades = new Dictionary<uint, Trade>();
        static uint nextId = 1;
        // pre-trade
        public static void Invite(Player player, uint playerId)
        {
            if(!player.CanTakeAction())
                return;
            if(CanTradeWith(player, playerId, out Player targetPlayer))
            {
                targetPlayer.own.tradeInvitations.Add(new TradeInvitation
                {
                    id = player.id,
                    name = player.name,
                    tribeId = player.tribeId,
                    level = (byte)player.level
                });
                player.NextAction(.5d);
            }
        }
        public static void AcceptInvitation(Player player, int index)
        {
            if(player.own.tradeInvitations.Count == 0 || index < 0 || index > player.own.tradeInvitations.Count)
            {
                player.Notify("No Invitations found", "");
                player.Log($"[TradeSystem.AcceptInvitation] No Invitations found, index={index}");
                return;
            }
            if(Player.onlinePlayers.TryGetValue(player.own.tradeInvitations[index].id, out Player targetPlayer))
            {
                player.own.tradeInvitations.RemoveAt(index);
                trades[nextId] = new Trade(player.id, targetPlayer.id);
                player.InitiateTrade(nextId);
                targetPlayer.InitiateTrade(nextId);
                nextId++;
            }
            else
            {
                player.NotifyPlayerOffline();
                player.own.tradeInvitations.RemoveAt(index);
            }
        }
        public static void RefuseInvitation(Player player, int index)
        {
            if(player.own.tradeInvitations.Count == 0 || index < 0 || index > player.own.tradeInvitations.Count)
            {
                player.Notify("No Invitations found", "");
                player.Log($"[TradeSystem.AcceptInvitation] No Invitations found, index={index}");
                return;
            }
            if(Player.onlinePlayers.TryGetValue(player.own.tradeInvitations[index].id, out Player targetPlayer))
            {
                targetPlayer.Notify($"Player {player.name} refused the trade offer");
            }
            player.own.tradeInvitations.RemoveAt(index);
        }
        // while trading
        public static void Confirm(Player player, TradeOfferContent content)
        {
            if(!player.IsTrading()) {
                player.Notify("You're not trading");
                return;
            }
            // validate offer
            if(!content.IsValid(player))
                return;
            // confirm and update it
            if(trades.TryGetValue(player.tradeId, out Trade trade))
            {
                if(!trade.IsPartOfTrade(player.tradeId)) {
                    player.Notify("You're not a part of this trading deal");
                    player.CancelTrade();
                    return;
                }
                if(Player.onlinePlayers.TryGetValue(trade.GetOtherPlayer(player.id), out Player otherPlayer))
                {
                    otherPlayer.TargetShowConfirmedTradeOffer(content.GetItemSlots(player), content.gold, content.diamonds);
                    trade.Confirm(player.id, content);
                    trades[player.tradeId] = trade;
                }
                else
                {
                    Cancel(player);
                }
            }
            else
            {
                player.Notify("You're not trading");
                return;
            }
        }
        public static void Accept(Player player)
        {
            if(!player.IsTrading()) {
                player.Notify("You're not trading");
                return;
            }
            if(trades.TryGetValue(player.tradeId, out Trade trade))
            {
                if(!trade.IsPartOfTrade(player.tradeId)) {
                    player.Notify("You're not a part of this trading deal");
                    player.CancelTrade();
                    return;
                }
                if(trade.IsAccepted(player.id))
                {
                    player.Notify("Aleady Accepted the trade deal");
                    return;
                }
                trade.Accept(player.id);
                if(Player.onlinePlayers.TryGetValue(trade.GetOtherPlayer(player.id), out Player otherPlayer))
                {
                    // if both accepted at this point
                    if(trade.IsBothAccepted)
                    {
                        // validate both offers
                        if(!trade.ValidateOffer(player))
                        {
                            trade.Accept(player.id, false);
                            trades[player.tradeId] = trade;
                            player.Notify("your offer is invalid");
                            return;
                        }
                        if(!trade.ValidateOffer(otherPlayer))
                        {
                            trade.Accept(otherPlayer.id, false);
                            trades[player.tradeId] = trade;
                            otherPlayer.Notify("your offer is invalid");
                            return;
                        }
                        // if both are valid, exchange offers
                        Exchange(player, trade.GetOfferContent(player.id), otherPlayer);
                        Exchange(otherPlayer, trade.GetOfferContent(otherPlayer.id), player);
                        // clean up
                        trades.Remove(player.tradeId);
                        player.CancelTrade();
                        otherPlayer.CancelTrade();
                    }
                    else
                    {
                        trades[player.tradeId] = trade;
                        otherPlayer.TargetAcceptTradeOffer();
                        player.TargetAcceptedMyTradeOffer();
                    }
                }
                else Cancel(player);
            }
            else player.Notify("You're not trading");
        }
        public static void Cancel(Player player)
        {
            if(!player.IsTrading())
            {
                player.Notify("You're not trading");
                return;
            }
            if(trades.TryGetValue(player.tradeId, out Trade trade))
            {
                uint tradeId = player.tradeId;
                player.CancelTrade();
                if(Player.onlinePlayers.TryGetValue(trade.GetOtherPlayer(player.id), out Player otherPlayer))
                {
                    otherPlayer.CancelTrade();
                }
                trades.Remove(tradeId);
            }
            else player.Notify("You're not trading");
        }
        // helpers
        static bool CanTradeWith(Player player, uint playerId, out Player targetPlayer)
        {
            targetPlayer = null;
            if(player.IsFighting())
            {
                player.Notify("Can't trade while fighting", "لا يمكنك المقايضة اثناء معركة");
                return false;
            }
            if(player.InEvent())
            {
                player.Notify("Can't trade while inside an event area", "لا يمكنك المقايضة في منطقة حدث");
                return false;
            }
            if(player.IsTrading())
            {
                player.Notify("You're already trading with other player", "انت يتقايض بالفعل مع لاعب اخر");
                return false;
            }
            if(!Server.IsPlayerIdWithInServer(playerId))
            {
                player.Notify("You're already trading with other player", "انت تقايض بالفعل مع لاعب اخر");
                return false;
            }
            if(Player.onlinePlayers.TryGetValue(playerId, out targetPlayer))
            {
                if(targetPlayer.IsFighting())
                {
                    player.Notify("Target can't trade while fighting", "لا يمكنك المقايضة اثناء معركة");
                    return false;
                }
                if(targetPlayer.InEvent())
                {
                    player.Notify("Target is inside an event area", "لا يمكنك المقايضة في منطقة حدث");
                    return false;
                }
                if(targetPlayer.IsTrading())
                {
                    player.Notify("Target is already trading with other player", "الهدف يتقايض بالفعل مع لاعب اخر");
                    return false;
                }
                return true;
            }
            player.NotifyPlayerOffline();
            return false;
        }
        static void Exchange(Player sender, TradeOfferContent content, Player reciever)
        {
            if(content.gold > 0)
            {
                sender.own.gold -= content.gold;
                reciever.own.gold += content.gold;
            }
            if(content.diamonds > 0)
            {
                sender.own.diamonds -= content.diamonds;
                reciever.own.diamonds += content.diamonds;
            }
            if(content.items.Length > 0)
            {
                for(int i = 0; i < content.items.Length; i++)
                {
                    ItemSlot slot = sender.own.inventory[content.items[i].index];
                    reciever.InventoryAdd(slot.item, content.items[i].amount);
                    slot.DecreaseAmount(content.items[i].amount);
                    sender.own.inventory[content.items[i].index] = slot;
                }
            }
        }
    }
}