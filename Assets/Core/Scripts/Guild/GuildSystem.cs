using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
namespace Game
{
    public class GuildSystem {
        public static Dictionary<uint, Guild> guilds = new Dictionary<uint, Guild>();// loaded guilds
        public static Dictionary<uint, GuildMember[]> members = new Dictionary<uint, GuildMember[]>();
        public static Dictionary<uint, int> elitesCount = new Dictionary<uint, int>();
        public static Dictionary<uint, bool> hasVice = new Dictionary<uint, bool>();
        public static Dictionary<uint, GuildJoinRequest[]> joinRequests = new Dictionary<uint, GuildJoinRequest[]>();
        public static Dictionary<byte, GuildJoinInfo[]> guildsJoinInfo = new Dictionary<byte, GuildJoinInfo[]>();//guilds in tribe
        public static uint nextGuildId = (uint)(Server.number * 10000);

        #region Create/Join/Leave
        public static void CreateGuild(Player player, string guildName)
        {
            if(player.level < Storage.data.guild.minJoinLevel)
            {
                player.Notify("You didn't reach the required Level yet.");
                return;
            }
            if(player.InGuild())
            {
                player.Notify("you are already in a guild.");
                return;
            }
            if(player.own.gold < Storage.data.guild.creationPriceGold)
            {
                player.Notify("you don't have enough gold.");
            }
            if(!IsValidGuildName(guildName))
            {
                player.Notify("This name isn't valid.");
                return;
            }
            // create
            if(guilds.Count > 0)
            {
                foreach (Guild item in guilds.Values)
                {
                    if(item.Name == guildName)
                        player.TargetNotify("This name is already used.");
                        return;
                }
            }
            Guild guild = new Guild
            {
                id = nextGuildId,
                Name = guildName,
                tribeId = player.tribeId,
                level = 1,
                JoinLevel = (byte)Storage.data.guild.minJoinLevel,
                masterName = player.name,
                br = player.battlepower,
                membersCount = 1
            };
            guilds[nextGuildId] = guild;
            members[nextGuildId] = new GuildMember[]{};
            AddMemberToGuild(new GuildMember
            {
                id = player.id,
                Name = player.name,
                level = (byte)player.level,
                classInfo = player.classInfo,
                br = player.battlepower,
                rank = GuildRank.Master,
                online = 0
            }, nextGuildId);
            player.own.guildRank = GuildRank.Master;
            BroadcastChanges(guild);
            player.UseGold(Storage.data.guild.creationPriceGold);
            player.TargetJoinedGuild();
            player.guild = new GuildPublicInfo(guild.id, guild.Name);
            AddJoinInfo(guild); // add guild join info
            joinRequests[guild.id] = new GuildJoinRequest[]{}; // add to requests
            nextGuildId++;
        }
        public static bool JoinGuild(uint guildId, GuildMember member)
        {
            if (guilds.TryGetValue(guildId, out Guild guild))
            {
                // add to members
                AddMemberToGuild(member, guildId);
                if(!Player.onlinePlayers.ContainsKey(member.id))
                {
                    Database.singleton.AddOrRemovePlayerFromGuild(member.id, guildId);
                }
                else
                {
                    Player.onlinePlayers[member.id].guild = new GuildPublicInfo(guildId, guild.Name);
                }
                guild.membersCount++;
                guilds[guildId] = guild;
                // broadcast and save
                BroadcastChanges(guild);
                ModifiedMemberCountInJoinInfo(guild.id, guild.tribeId);
                RemoveJoinRequest(guildId, member.id);
                StartPlayerUpdateBR(member.id, guildId);
                return true;
            }
            return false;
        }
        public static bool AddToGuild(uint guildId, uint requester, Player newMember)
        {
            // guild exists, requester can invite member?
            if (guilds.TryGetValue(guildId, out Guild guild) && guild.CanInvite(requester, newMember.id))
            {
                // add to members
                AddMemberToGuild(new GuildMember
                {
                    id = newMember.id,
                    Name = newMember.name,
                    level = (byte)newMember.level,
                    classInfo = newMember.classInfo,
                    br = newMember.battlepower,
                    rank = GuildRank.Member,
                    online = Player.onlinePlayers.ContainsKey(newMember.id) ? 0 : 0
                }, guildId);
                ModifiedMemberCountInJoinInfo(guild.id, guild.tribeId);
                RemoveJoinRequest(guildId, newMember.id);
                // broadcast and save
                newMember.guild = new GuildPublicInfo(guildId, guild.Name);
                BroadcastChanges(guild);
                StartPlayerUpdateBR(newMember.id, guildId);
                return true;
            }
            return false;
        }
        public static void SendJoinRequest(Player player, uint guildId)
        {
            if(guilds.TryGetValue(guildId, out Guild guild))
            {
                if(guild.AutoAccept) {
                    GuildMember member = new GuildMember
                    {
                        id = player.id,
                        Name = player.name,
                        level = (byte)player.level,
                        classInfo = player.classInfo,
                        br = player.battlepower,
                        rank = GuildRank.Member,
                        online = Player.onlinePlayers.ContainsKey(player.id) ? 0 : 0
                    };
                    if(JoinGuild(guildId, member))
                        player.TargetJoinedGuild();
                }
                else
                {
                    if(joinRequests[guildId].Length > 0)
                    {
                        for (int i = 0; i < joinRequests[guildId].Length; i++)
                        {
                            if(joinRequests[guildId][i].id == player.id)
                            {
                                player.TargetNotify("You've already sent a request.");
                                return;
                            }
                        }
                    }
                    AddJoinRequest(new GuildJoinRequest
                    {
                        id = player.id,
                        name = player.name,
                        level = (byte)player.level,
                        br = player.battlepower,
                        sent = DateTime.Now.ToOADate()
                    }
                    , guildId);
                    player.TargetNotify("Request has been sent.");
                }
            }
        }
        public static void TerminateGuild(uint guildId, uint requester)
        {
            // guild exists and member can terminate?
            if (guilds.TryGetValue(guildId, out Guild guild) && guild.CanTerminate(requester))
            {
                // remove guild from database
                Database.singleton.RemoveGuild(guild.id);
                // clear for person that terminated
                for(int i = 0; i < members[guildId].Length; i++)
                {
                    BroadcastTo(members[guildId][i].id, Guild.Empty);
                }
                // remove from 'guilds' so it doesn't grow forever
                guilds.Remove(guildId);
                members.Remove(guildId);
                //if(guildsJoinInfo.ContainsKey(guildId))
                //    guildsJoinInfo.Remove(guildId);
                if(joinRequests.ContainsKey(guildId))
                    joinRequests.Remove(guildId);
            }
        }
        static void AddMemberToGuild(GuildMember newMember, uint guildId)
        {
            GuildMember[] tmp = members[guildId];
            Array.Resize(ref tmp, tmp.Length + 1);
            tmp[tmp.Length - 1] = newMember;
            members[guildId] = tmp;
        }
        static void RemoveMemberFromGuild(uint memberId, uint guildId)
        {
            for (int i = 0; i < members[guildId].Length; i++)
            {
                if(members[guildId][i].id == memberId)
                {
                    GuildMember[] tmp = members[guildId];
                    tmp[i] = tmp[tmp.Length - 1];
                    Array.Resize(ref tmp, tmp.Length - 1);
                    members[guildId] = tmp;
                    return;
                }
            }
        }
        public static void AddJoinInfo(Guild guild)
        {
            GuildJoinInfo[] tmp = guildsJoinInfo[guild.tribeId];
            Array.Resize(ref tmp, tmp.Length + 1);
            tmp[tmp.Length - 1] = new GuildJoinInfo
            {
                id = guild.id,
                name = guild.Name,
                masterName = guild.masterName,
                level = guild.level,
                requiredLevel = guild.JoinLevel,
                membersCount = (byte)members[guild.id].Length,
                capacity = (byte)guild.capacity
            };
            guildsJoinInfo[guild.tribeId] = tmp;
        }
        public static void RemoveJoinInfo(Guild guild)
        {
            if(guildsJoinInfo[guild.tribeId].Length < 1)
                return;
            for (int i = 0; i < guildsJoinInfo[guild.tribeId].Length; i++)
            {
                if(guildsJoinInfo[guild.tribeId][i].id == guild.id)
                {
                    GuildJoinInfo[] tmp = guildsJoinInfo[guild.tribeId];
                    tmp[i] = tmp[tmp.Length - 1];
                    Array.Resize(ref tmp, tmp.Length - 1);
                    guildsJoinInfo[guild.tribeId] = tmp;
                    return;
                }
            }
        }
        static void ModifiedMemberCountInJoinInfo(uint guildId, byte tribeId)
        {
            int infoIndex = -1;
            for (int i = 0; i < guildsJoinInfo[tribeId].Length; i++)
            {
                if(guildsJoinInfo[tribeId][i].id == guildId)
                    infoIndex = i;
                    break;
            }
            if(infoIndex != -1)
            {
                if(members[guildId].Length < guilds[guildId].capacity)
                {
                    guildsJoinInfo[tribeId][infoIndex].membersCount = (byte)members[guildId].Length;
                }
                else
                {
                    RemoveJoinInfo(guilds[guildId]);
                }
            }
            else if(members[guildId].Length < guilds[guildId].capacity)
            {
                AddJoinInfo(guilds[guildId]);
            }
        }
        static void AddJoinRequest(GuildJoinRequest request, uint guildId)
        {
            GuildJoinRequest[] tmp = joinRequests[guildId];
            Array.Resize(ref tmp, tmp.Length + 1);
            tmp[tmp.Length - 1] = request;
            joinRequests[guildId] = tmp;
        }
        public static void RemoveJoinRequest(uint guildId, uint memberId)
        { // if any
            if(joinRequests[guildId].Length < 1) return;
            for(int i = 0; i < joinRequests[guildId].Length; i++)
            {
                if(joinRequests[guildId][i].id == memberId) {
                    GuildJoinRequest[] tmp = joinRequests[guildId];
                    tmp[i] = tmp[tmp.Length - 1];
                    Array.Resize(ref tmp, tmp.Length - 1);
                    joinRequests[guildId] = tmp;
                    return;
                }
            }
        }
        public static void LeaveGuild(uint guildId, uint member, bool kicked = true, bool UpdateDatabase = true)
        {
            // guild exists and member can leave?
            if (guilds.TryGetValue(guildId, out Guild guild) && guild.CanLeave(member))
            {
                GuildMember memberData = GetMemberById(guildId, member);
                if(memberData.rank == GuildRank.Master)
                    return;
                else if(memberData.rank == GuildRank.Vice)
                {
                    hasVice[guildId] = false;
                }
                else if(memberData.rank == GuildRank.Elite)
                {
                    elitesCount[guildId]--;
                }
                RemoveMemberFromGuild(member, guildId);

                if(Player.onlinePlayers.TryGetValue(member, out Player player))
                {
                    player.guild = GuildPublicInfo.Empty;
                    if(!kicked)
                    {
                        player.TargetNotify("You have left the guild.");
                    }
                    else
                    {
                        player.TargetNotify("You have been kicked out of the guild.");
                    }
                }
                else if(UpdateDatabase)
                {
                    Database.singleton.AddOrRemovePlayerFromGuild(member, 0);
                }
                guild.membersCount--;
                guilds[guildId] = guild;
                // remove from list
                ModifiedMemberCountInJoinInfo(guild.id, guild.tribeId);
                BroadcastChanges(guild);// broadcast and save
            }
        }
        #endregion
        #region In Guild Actions
        public static void DecreaseMemberContribution(Player player, uint amount)
        {
            AddMemberContribution(player.guild.id, player.id, (uint)(-amount));
            //members[guild.id][guild.MemberIndex(player.id)].contribution -= amount;
            player.UseGuildContribution(amount);
        }
        public static void IncreaceMemberContribution(Player player, uint amount)
        {
            if (guilds.TryGetValue(player.guild.id, out Guild guild))
            {
                members[guild.id][guild.MemberIndex(player.id)].contribution += amount;
                player.AddGuildContribution(amount);
                guild.assets.wealth += amount;
                BroadcastChanges(guild);
            }
        }
        public static void OnLearnedSkill(Player player, int skillIndex)
        {
            byte lvl = player.own.guildSkills[skillIndex];
            if(lvl == (byte)Storage.data.guild.maxLevel)
            {
                player.Notify("Skill already reached max level", "المهارة وصلت لاعلي مستوي بالفعل");
                return;
            }
            if(player.own.guild.buildings.academy < lvl + 1)
            {
                player.Notify($"Academy level {lvl + 1} is required", $"مطلوب اكاديمية مستوي {lvl + 1}");
                return;
            }
            if(ScriptableGuildSkill.dict.TryGetValue(skillIndex, out ScriptableGuildSkill skill))
            {
                if(player.own.guildContribution < skill.cost[lvl])
                {
                    player.Notify("You don't have enough Contribution to the guild", "لا تملك مشاركة كافية في النقابة");
                    return;
                }
                AddMemberContribution(player.guild.id, player.id, (uint)(-skill.cost[lvl]));
                player.UseGuildContribution(skill.cost[lvl]);
                player.own.guildSkills[skillIndex] = (byte)(lvl + 1);
                player.Notify($"Skill have been Upgraded to Level {lvl + 1}", $"تم تطوير المهاروة الي المستوى {lvl + 1}");
            }
            else
            {
                player.Notify("Please select a skill", "برجاء اختيار مهارة");
                player.Log($"[CmdLearnGuildSkill] invalid skill index = {skillIndex}");
            }
        }
        public static void AddMemberContribution(uint guildId, uint memberId, uint amount)
        {
            if(members.ContainsKey(guildId))
            {
                for (int i = 0; i < members[guildId].Length; i++)
                {
                    if(members[guildId][i].id == memberId)
                    {
                        members[guildId][i].contribution += amount;
                        return;
                    }
                }
            }
        }
        public static void SetMemberLevel(uint guildId, uint memberId, byte newLevel)
        {
            if(members.ContainsKey(guildId))
            {
                for (int i = 0; i < members[guildId].Length; i++)
                {
                    if(members[guildId][i].id == memberId)
                    {
                        members[guildId][i].level = newLevel;
                        return;
                    }
                }
            }
        }
        public static bool Recall(Player player)
        {
            if(!player.InGuild() || !members.ContainsKey(player.guild.id))
                return false;
            uint guildId = player.guild.id;
            GuildMember member = GetMemberById(guildId, player.id);
            if(member.rank >= Storage.data.guild.recallMinRank)
            {
                GuildRecallRequest req = new GuildRecallRequest(player.id, player.name, player.cityId, player.transform.position);
                for (int i = 0; i < members[guildId].Length; i++)
                {
                    if(Player.onlinePlayers.TryGetValue(members[guildId][i].id, out Player target))
                    {
                        target.SetGuildRecallRequest(req);
                    }
                }
                return true;
            }
            return false;
        }
        // adminstration
        public static bool SetGuildNotice(uint guildId, uint requester, string notice)
        {
            // guild exists, member can notify, notice not too long?
            if (guilds.TryGetValue(guildId, out Guild guild) &&
                guild.CanNotify(requester) &&
                notice.Length <= Storage.data.guild.maxNoticeLength)
            {
                // set notice and reset next time
                guild.notice = notice;

                // broadcast and save
                BroadcastChanges(guild);
                Debug.Log(requester + " changed guild notice to: " + guild.notice);
                return true;
            }
            return false;
        }
        public static void KickFromGuild(uint guildId, uint requester, uint member)
        {
            // guild exists, requester can kick member?
            if (guilds.TryGetValue(guildId, out Guild guild) && guild.CanKick(requester, member))
            {
                // reuse Leave function
                LeaveGuild(guildId, member, true);
                Debug.Log(requester + " kicked " + member + " from guild: " + guildId);
            }
        }
        public static void SetGuildOnline(uint guildId, uint member, double online)
        {
            if (members.ContainsKey(guildId))
            {
                for (int i = 0; i < members[guildId].Length; i++)
                {
                    if(members[guildId][i].id == member)
                    {
                        members[guildId][i].online = online;
                    }
                }
            }
        }
        public static void AcceptJoinRequest(uint guildId, uint requesterId, Player accepter)
        {
            if(members.ContainsKey(guildId))
            {
                if(Player.onlinePlayers.TryGetValue(requesterId, out Player player))
                {
                    JoinGuild(guildId, new GuildMember
                    {
                        id = player.id,
                        Name = player.name,
                        level = (byte)player.level,
                        classInfo = player.classInfo,
                        br = player.battlepower,
                        rank = GuildRank.Member,
                        online = 0
                    });
                    accepter.Notify(player.name + " has been added to the guild", $"تم قبول {player.name} بالتحالف");
                }
                else
                {
                    if(Database.singleton.CheckIfPlayerNotInGuild(requesterId))
                    {
                        GuildMember? info = Database.singleton.GetJoinInfoOfflinePlayer(requesterId);
                        if(info != null)
                        {
                            JoinGuild(guildId, info.Value);
                            accepter.TargetNotify("player has been added to the guild.");
                        }
                        else
                        {
                            RemoveJoinRequest(guildId, requesterId);
                            accepter.TargetNotify("Couldn't find this player.");
                        }
                    }
                    else
                    {
                        RemoveJoinRequest(guildId, requesterId);
                        accepter.TargetNotify("This player in already in a guild.");
                    }
                }
            }
        }
        public static void PromoteMember(uint guildId, uint requester, uint member)
        {
            if (members.ContainsKey(guildId) && guilds[guildId].CanPromote(requester, member))
            {
                for (int i = 0; i < members[guildId].Length; i++)
                {
                    if(members[guildId][i].id == member) {
                        GuildRank newRank = members[guildId][i].rank++;
                        // check if promotable
                        if(newRank == GuildRank.Master)
                        {
                            Player.onlinePlayers[requester].TargetNotify("You can only Transfar the Master Position.");
                            return;
                        }
                        else if(newRank == GuildRank.Elite && elitesCount[guildId] == Storage.data.guild.maxElites)
                        {
                            Player.onlinePlayers[requester].TargetNotify("Reached max Elites.");
                            return;
                        }
                        else if(newRank == GuildRank.Vice && hasVice[guildId])
                        {
                            Player.onlinePlayers[requester].TargetNotify("The Guild already have a Vice Master.");
                            return;
                        }
                        // update count cache
                        if(newRank == GuildRank.Elite)
                        {
                            elitesCount[guildId]++;
                        }
                        if(newRank == GuildRank.Vice)
                        {
                            hasVice[guildId] = true;
                        }
                        // update and broadcast
                        
                        BroadcastTo(member, guilds[guildId]);
                        //else Database.singleton.UpdateCharacterGuildInfo(guildId, member);
                        return;
                    }
                }
            }
        }
        public static void DemoteMember(uint guildId, uint requester, uint member)
        {
            if (members.ContainsKey(guildId) && guilds[guildId].CanDemote(requester, member))
            {
                for (int i = 0; i < members[guildId].Length; i++)
                {
                    if(members[guildId][i].id == member)
                    {
                        members[guildId][i].rank--;
                        if(Player.onlinePlayers.ContainsKey(member))
                        {
                            BroadcastTo(member, guilds[guildId]);
                        }
                        else
                        {
                            Database.singleton.UpdateCharacterGuildInfo(guildId, member);
                        }
                        return;
                    }
                }
            }
        }
        public static void TransfarMastership(uint guildId, uint masterId, uint newMasterId)
        {
            if(members.ContainsKey(guildId) && masterId != newMasterId)
            {
                int masterIndex = -1;
                int newMasterIndex = -1;
                int i = 0;
                foreach(GuildMember member in members[guildId])
                {
                    if(masterIndex > -1 && newMasterIndex > -1)
                        break;
                    if(member.id == masterId)
                    {
                        if(member.rank != GuildRank.Master)
                        {
                            Player.onlinePlayers[masterId].Notify("you are not master.");
                            return;
                        }
                        masterIndex = i;
                        continue;
                    }
                    if(member.id == newMasterId)
                    {
                        newMasterIndex = i;
                    }

                    i++;
                }
                if(masterIndex < 0 && newMasterIndex < 0)
                {
                    Player.onlinePlayers[masterId].Notify("you can't transfar Master Rank to that player.");
                    return;
                }
                if(Player.onlinePlayers.TryGetValue(members[guildId][newMasterIndex].id, out Player newMaster))
                {
                    members[guildId][masterIndex].rank = GuildRank.Member;
                    members[guildId][newMasterIndex].rank = GuildRank.Master;
                    BroadcastTo(masterId, guilds[guildId]);
                    BroadcastTo(newMasterId, guilds[guildId]);
                    Player.onlinePlayers[masterId].Notify("Master rank has been transfared.");
                    newMaster.TargetNotify("you have been Promoted to be the Master of your guild.");
                }
                else
                {
                    Player.onlinePlayers[masterId].Notify("this player is not online.");
                }
            }
        }
        public static void UpgradeHall(Player upgrader)
        {
            if(guilds.TryGetValue(upgrader.guild.id, out Guild guild))
            {
                if(guild.buildings.hall == Storage.data.guild.maxLevel)
                {
                    upgrader.Notify("Hall already reached max level", "وصلت القاعة لاعلي مستوي بالفعل");
                    return;
                }
                if(guild.level < guild.buildings.hall + 1)
                {
                    upgrader.Notify($"Guild level {guild.buildings.hall + 1} is required", $"يلزم مستوي النقابة {guild.buildings.hall + 1}");
                    return;
                }
                GuildAssets req = Storage.data.guild.hallUpgradeReqs[guild.buildings.hall];
                if(guild.assets.wealth < req.wealth)
                {
                    upgrader.Notify("Didn't meet the Wealth required to upgrade the Hall", "لم تحقق الثروة المطلوبة لتطوير القاعة");
                    return;
                }
                if(guild.assets.wood < req.wood)
                {
                    upgrader.Notify("Didn't meet the Wood required to upgrade the Hall", "لم تحقق الخشب المطلوبة لتطوير القاعة");
                    return;
                }
                if(guild.assets.stone < req.stone)
                {
                    upgrader.Notify("Didn't meet the Stone required to upgrade the Hall", "لم تحقق الحجارة المطلوبة لتطوير القاعة");
                    return;
                }
                if(guild.assets.iron < req.iron)
                {
                    upgrader.Notify("Didn't meet the Iron required to upgrade the Hall", "لم تحقق الحديد المطلوبة لتطوير القاعة");
                    return;
                }
                if(guild.assets.food < req.food)
                {
                    upgrader.Notify("Didn't meet the Food required to upgrade the Hall", "لم تحقق الطعام المطلوبة لتطوير القاعة");
                    return;
                }
                
                guild.buildings.hall++;
                guild.assets.wealth -= req.wealth;
                guild.assets.wood -= req.wood;
                guild.assets.stone -= req.stone;
                guild.assets.iron -= req.iron;
                guild.assets.food -= req.food;

                BroadcastChanges(guild);
                upgrader.Notify("You've upgraded Guild Hall", "لقد قمت بتطوير قاعة النقابة");
                // TODO: send system chat massege to all guild members
            } 
            else upgrader.Notify("Couldn't find guild data", "لم نستطع ايجاد بيانات النقابة");
        }
        #endregion
        #region Manage
        static bool IsValidGuildName(string guildName)
        {
            return guildName.Length <= Storage.data.guild.maxNameLength &&
                Regex.IsMatch(guildName, @"^[a-zA-Z0-9_]+$");
        }
        static void BroadcastTo(uint member, Guild guild)
        {
            if (Player.onlinePlayers.TryGetValue(member, out Player player))
            {
                //if(guild.id > 0)
                //    player.guild.myInfo = GetMemberById(guild.id, member);
                player.own.guild = guild;
            }
        }
        static void BroadcastChanges(Guild guild)
        {
            for (int i = 0; i < members[guild.id].Length; i++)
            {
                BroadcastTo(members[guild.id][i].id, guild);
            }
            guilds[guild.id] = guild;
        }
        public static GuildMember GetMemberById(uint guildId, uint memberId)
        {
            if(members.ContainsKey(guildId))
            {
                for(int i = 0; i < members[guildId].Length; i++)
                {
                    if(members[guildId][i].id == memberId)
                        return members[guildId][i];
                }
            }
            return default;
        }
        public static GuildMember GetGuildMaster(uint guildId)
        {
            if(members.ContainsKey(guildId)) {
                for (int i = 0; i < members[guildId].Length; i++)
                {
                    if(members[guildId][i].rank == GuildRank.Master)
                        return members[guildId][i];
                }
            }
            return default;
        }
        public static void LoadGuilds()
        {
            Database.singleton.LoadAllGuildsInfo();
            Debug.Log(guilds.Count + " Guilds has been loaded.");
        }
        static void StartPlayerUpdateBR(uint playerId, uint guildId)
        {
            if(Player.onlinePlayers.ContainsKey(playerId) && guilds.ContainsKey(guildId))
            {
                Player.onlinePlayers[playerId].StartUpdatePlayerBRInGuildInfo();
            }
        }
        public static IEnumerator<WaitForSeconds> UpdatePlayerBRInGuildInfo(uint playerId, uint guildId)
        {
            while(true) {
                yield return new WaitForSeconds(Storage.data.UpdateBRInGuildIntervalInMins);
                // update
                if(members.ContainsKey(guildId))
                {
                    for (int i = 0; i < members[guildId].Length; i++)
                    {
                        if(members[guildId][i].id == playerId)
                        {
                            members[guildId][i].br = Player.onlinePlayers[playerId].battlepower;
                            break;
                        }
                    }
                }
                /*if(guilds.TryGetValue(guildId, out Guild guild)) {
                    for (int i = 0; i < guild.members.Length; i++) {
                        if(guild.members[i].id == playerId) {
                            guild.members[i].br = 
                            break;
                        }
                    }
                    BroadcastChanges(guild);
                }*/
            }
        }
        public static IEnumerator<WaitForSeconds> ServerUpdater(float interval)
        {
            yield return new WaitForSeconds(interval);
            if(guilds.Count > 0)
                Database.singleton.SaveGuilds();
        }
        #endregion
    }
}