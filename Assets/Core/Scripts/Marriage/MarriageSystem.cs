namespace Game
{
    public class MarriageSystem
    {
        public static void SendMarriageProposal(Player player, uint sId, MarriageType type)
        {
            if(!player.CanTakeAction()) // DDoS guard
                return;
            if(player.IsMarried())
            {
                player.Notify("You're already married", "انت بالفعل متزوج");
                return;
            }
            if(!Server.IsPlayerIdWithInServer(sId))
            {
                player.Notify("Invalid player ID", "رقم تعريفي غير صحيح");
                player.Log("[CmdSendMarriageProposal] Invalid player ID: " + sId);
                return;
            }
            if(Player.onlinePlayers.TryGetValue(sId, out Player target))
            {
                if(target.gender == player.gender)
                {
                    player.Notify("Can't marry from same gender", "لا يمكنك الزواج من نفس النوع");
                    return;
                }
                if(target.tribeId != player.tribeId)
                {
                    player.Notify("Can't marry from another tribe", "لا يمكنك الزواج من عشيرة اخري");
                    return;
                }
                if(target.IsMarried())
                {
                    player.Notify("Player is already married", "اللاعب متزوج بالفعل");
                    return;
                }
                if(target.own.marriageProposals.Has(player.id))
                {
                    player.Notify("You've already sent a proposal", "تم ارسال دعوة بالفعل");
                    return;
                }
                if( (type == MarriageType.Common && player.own.b_diamonds < Storage.data.marriage.commonFee) ||
                    (type == MarriageType.Noble && player.own.diamonds < Storage.data.marriage.nobleFee) ||
                    (type == MarriageType.Royal && player.own.diamonds < Storage.data.marriage.royalFee) )
                {
                    player.NotifyNotEnoughDiamonds();
                    return;
                }
                // send proposal
                target.own.marriageProposals.Add(new MarriageProposal {
                    id = player.id,
                    name = player.name,
                    avatar = player.avatar,
                    level = (byte)player.level,
                    classInfo = player.classInfo,
                    br = player.battlepower,
                    guildName = player.guild.name,
                    type = type
                });
                player.Notify("Proposal has been sent", "تم ارسال الدعوة");
                player.NextAction(.5); // DDoS guard
            }
            else player.NotifyPlayerOffline();
        }
        public static void RefuseMarriageProposal(Player player, int index)
        {
            if(!player.CanTakeAction()) // DDoS guard
                return;
            if(index < 0 || index > player.own.marriageProposals.Count)
            {
                player.Notify("Please select a proposal", "برجاء اختيار دعوة");
                player.Log("[MarriageSystem.RefuseMarriageProposal] invalid index= " + index);
                return;
            }
            if(Player.onlinePlayers.ContainsKey(player.own.marriageProposals[index].id))
            {
                Player.onlinePlayers[player.own.marriageProposals[index].id].Notify(player.name + " has refused your marriage proposal", player.name + " قد رفض طلب زواجك");
            }
            player.own.marriageProposals.RemoveAt(index);
            player.NextAction(.5); // DDoS guard
        }
        public static void AcceptMarriageProposal(Player player, int index)
        {
            if(!player.CanTakeAction()) // DDoS guard
                return;
            if(index < 0 || index > player.own.marriageProposals.Count)
            {
                player.Notify("Please select a proposal", "برجاء اختيار دعوة");
                player.Log("[MarriageSystem.AcceptMarriageProposal] invalid index= " + index);
                return;
            }
            if(player.IsMarried())
            {
                player.Notify("You're already married", "انت بالفعل متزوج");
                player.Log("[MarriageSystem.AcceptMarriageProposal] already married to: " + player.own.marriage.spouse);
                return;
            }
            if(Player.onlinePlayers.TryGetValue(player.own.marriageProposals[index].id, out Player target))
            {
                if(target.gender == player.gender)
                {
                    player.Notify("Can't marry from same gender", "لا يمكنك الزواج من نفس النوع");
                    player.Log("[CmdAcceptMarriageProposal] same gender proposal from ID: " + target.id);
                    return;
                }
                if(target.IsMarried())
                {
                    player.Notify("Player is already married", "اللاعب متزوج بالفعل");
                    player.own.marriageProposals.RemoveAt(index);
                    return;
                }
                MarriageProposal proposal = player.own.marriageProposals[index];
                if( (proposal.type == MarriageType.Common && target.own.b_diamonds < Storage.data.marriage.commonFee) ||
                    (proposal.type == MarriageType.Noble && target.own.diamonds < Storage.data.marriage.nobleFee) ||
                    (proposal.type == MarriageType.Royal && target.own.diamonds < Storage.data.marriage.royalFee) )
                {
                    Player.onlinePlayers[proposal.id].Notify("Please Prepare the marriage fee", "برجاء تحضير قيمة الزواج");
                    player.Notify("Target doesn't have enough diamonds", "الهدف لا يملك الماس كافية");
                    return;
                }
                byte mLvl = 1;
                if(proposal.type == MarriageType.Common)
                {
                    target.UseBDiamonds(Storage.data.marriage.commonFee);
                }
                else if(proposal.type == MarriageType.Noble)
                {
                    target.UseDiamonds(Storage.data.marriage.nobleFee);
                    mLvl = 3;
                }
                else if(proposal.type == MarriageType.Royal)
                {
                    target.UseDiamonds(Storage.data.marriage.royalFee);
                    mLvl = 6;
                }
                player.own.marriage = new Marriage
                {
                    level = mLvl,
                    spouse = proposal.id,
                    spouseName = target.name,
                    spouseLevel = (byte)target.level,
                    spouseOnline = true
                };
                target.own.marriage = new Marriage
                {
                    level = mLvl,
                    spouse = player.id,
                    spouseName = player.name,
                    spouseLevel = (byte)player.level,
                    spouseOnline = true
                };
                string husband = player.gender == Gender.Male ? player.name : target.name;
                string wife = player.gender == Gender.Female ? player.name : target.name;
                foreach(Player p in Player.onlinePlayers.Values)
                {
                    p.TargetAnnounceMarriage(husband, wife);
                }
                player.own.marriageProposals.Clear();
            }
            else
            {
                player.NotifyPlayerOffline();
            }
        }
    }
}