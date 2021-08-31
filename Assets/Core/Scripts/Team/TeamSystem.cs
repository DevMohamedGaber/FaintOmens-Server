using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game
{
    public class TeamSystem
    {
        public static Dictionary<uint, Team> teams = new Dictionary<uint, Team>();
        public static uint nextTeamId = (uint)(Server.number * 10000000);
        public static Team emptyTeam = new Team();
        public static void FormTeam(Player creator)
        {
            Team team = new Team(nextTeamId++, creator.id);
            team.Add(new TeamMember(creator, true));
            BroadcastChanges(team);// broadcast and save in dict
        }
        public static void JoinTeam(uint teamId, Player player)
        {
            teams[teamId].Add(new TeamMember(player, true));
            BroadcastChanges(teams[teamId]);
        }
        public static void KickFromTeam(Player leader, uint memberId)
        {
            if(teams.TryGetValue(leader.teamId, out Team team))
            {
                if(team.leaderId != leader.id)
                {
                    leader.Notify("You're not the leader", "انت لست قائد الفريق");
                    return;
                }
                if(!team.Contains(memberId))
                {
                    leader.Notify("Player isn't in the team", "اللاعب غير مسجل بالفريق");
                    return;
                }
                team.members = team.members.Where(member => member.id != memberId).ToArray();
                BroadcastChanges(team);
                BroadcastTo(memberId, emptyTeam); // clear for kicked person
                if(Player.onlinePlayers.TryGetValue(memberId, out Player kicked))
                {
                    kicked.Notify("You've been kicked out of the team", "تم طردك من الفريق");
                }
            }
            else leader.Notify("Team not found", "هذا الفريق غير موجود");
        }
        public static void LeaveTeam(uint teamId, uint memberId)
        {
            if(teams.TryGetValue(teamId, out Team team))
            {
                if(!team.Contains(memberId))
                {
                    Player.onlinePlayers[memberId]?.Notify("Player isn't in the team", "اللاعب غير مسجل بالفريق");
                    return;
                }
                if(team.leaderId == memberId)
                {
                    if(team.members.Length > 1)
                    {
                        for(int i = 0; i < team.members.Length; i++)
                        {
                            if(team.members[i].id != memberId)
                            {
                                team.leaderId = team.members[i].id;
                                break;
                            }
                        }
                    }
                    else
                    {
                        DisbandTeam(team);
                        return;
                    }
                }
                team.members = team.members.Where(member => member.id != memberId).ToArray();
                team.ReDistributeBonuses();
                BroadcastChanges(team);
                BroadcastTo(memberId, emptyTeam); // clear for kicked person
            }
            else Player.onlinePlayers[memberId]?.Notify("team not found", "هذا الفريق غير موجود");
        }
        public static void DisbandTeam(Player leader)
        {
            if(teams.TryGetValue(leader.teamId, out Team team))
            {
                if(team.leaderId != leader.id)
                {
                    leader.Notify("Only the team leader can Disband it", "قائد الفريق فقط يمكنه حل الفريق");
                    return;
                }
                DisbandTeam(team);
            }
            else leader.Notify("Team not found", "هذا الفريق غير موجود");
        }
        public static void DisbandTeam(Team team)
        {
            for(int i = 0; i < team.members.Length; i++)
            {
                if(Player.onlinePlayers.TryGetValue(team.members[i].id, out Player player))
                {
                    player.teamId = 0;
                    player.own.team = emptyTeam;
                    player.Notify("Team has been disbanded", "تم حل الفريق");
                }
            }
            Database.singleton.RemoveTeam(team.id);
            teams.Remove(team.id);
        }
        public static void SetMemberOnline(uint teamId, uint playerId, bool online = true)
        {
            if(teams.TryGetValue(teamId, out Team team))
            {
                for(int i = 0; i < team.members.Length; i++)
                {
                    if(team.members[i].id == playerId)
                    {
                        team.members[i].online = online;
                        break;
                    }
                }
                BroadcastChanges(team);
            }
        }
        public static void SetMemberLevel(Player player)
        {
            if(teams.TryGetValue(player.teamId, out Team team))
            {
                for(int i = 0; i < team.members.Length; i++)
                {
                    if(team.members[i].id == player.id)
                    {
                        team.members[i].level = (byte)player.level;
                        break;
                    }
                }
                BroadcastChanges(team);
            }
        }
        static void BroadcastChanges(Team team)
        {
            for(int i = 0; i < team.members.Length; i++)
                BroadcastTo(team.members[i].id, team);
            teams[team.id] = team;
        }
        static void BroadcastTo(uint memberId, Team team)
        {
            if(Player.onlinePlayers.TryGetValue(memberId, out Player player))
            {
                player.teamId = team.id;
                player.own.team = team;
            }
        }
        static void CleanCashe()
        {
            foreach(Team team in teams.Values)
            {
                if(!team.HasOnline())
                    teams.Remove(team.id);
            }
        }
        public static IEnumerator<WaitForSeconds> ServerUpdater(float interval)
        {
            while(true)
            {
                yield return new WaitForSeconds(interval);
                if(teams.Count > 0)
                {
                    Database.singleton.SaveTeams();
                    CleanCashe();
                }
            }
        }
        /*public static void ChangeTeamShare(Player leader, ExperiaceShareType newShare) {
            if (teams.TryGetValue(leader.teamId, out Team team)) {
                if(team.leaderId != leader.id) {
                    leader.Notify("You're not in a team", "انت لست بفريق");
                    return;
                }
                if(team.share == newShare) {
                    leader.Notify("Aleady selected", "مختار بالفعل");
                    return;
                }
                team.share = newShare;
                BroadcastChanges(team);
            }
        }*/
    }
}