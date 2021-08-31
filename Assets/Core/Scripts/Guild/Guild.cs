using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
[Serializable]
    public struct Guild {
        public static Guild Empty = new Guild();
        public uint id;
        public string Name;
        public byte tribeId;
        public byte level;
        public uint exp;
        public bool AutoAccept;
        public byte JoinLevel;
        public uint br;
        public byte membersCount;
        public string notice;
        public string masterName;
        public GuildAssets assets;
        public GuildBuildings buildings;
        uint experienceMax => Storage.data.guild.expMax[level - 1];
        public int capacity => Storage.data.guild.capacity + (Storage.data.guild.capacityIncresePerLevel * buildings.hall);
        public bool IsFull() => membersCount == capacity;
        public int MemberIndex(uint memberId)
        {
            for(int i = 0; i < GuildSystem.members[id].Length; i++)
            {
                if(GuildSystem.members[id][i].id == memberId)
                {
                    return i;
                }
            }
            return -1;
        }
        public bool CanLeave(uint requesterId)
        {
            return GuildSystem.members[id] != null &&
                    Array.FindIndex(GuildSystem.members[id], (m) => m.id == requesterId && m.rank != GuildRank.Master) != -1;
        }
        public bool CanKick(uint requesterId, uint targetId)
        {
            if (GuildSystem.members[id] != null)
            {
                int requesterIndex = Array.FindIndex(GuildSystem.members[id], (m) => m.id == requesterId);
                int targetIndex = Array.FindIndex(GuildSystem.members[id], (m) => m.id == targetId);

                if(requesterIndex != -1 && targetIndex != -1)
                {
                    GuildMember requester = GuildSystem.members[id][requesterIndex];
                    GuildMember target = GuildSystem.members[id][targetIndex];

                    return requester.rank >= Storage.data.guild.kickMinRank &&
                        requesterId != targetId &&
                        target.rank != GuildRank.Master &&
                        target.rank < requester.rank;
                }
            }
            return false;
        }
        public bool CanTerminate(uint requesterId)
        {
            return GuildSystem.members[id] != null &&
                Array.FindIndex(GuildSystem.members[id], (m) => m.id == requesterId && m.rank == GuildRank.Master) != -1;
        }
        public bool CanNotify(uint requesterId)
        {
            return GuildSystem.members[id] != null &&
                Array.FindIndex(GuildSystem.members[id], (m) => m.id == requesterId && m.rank >= Storage.data.guild.notifyMinRank) != -1;
        }
        public bool CanInvite(uint requesterId, uint targetId)
        {
            return GuildSystem.members[id] != null &&
                GuildSystem.members[id].Length < Storage.data.guild.capacity &&
                requesterId != targetId &&
                Array.FindIndex(GuildSystem.members[id], (m) => m.id == requesterId && m.rank >= Storage.data.guild.inviteMinRank) != -1;
        }
        public bool CanPromote(uint requesterId, uint targetId)
        {
            if(GuildSystem.members[id] != null)
            {
                int requesterIndex = MemberIndex(requesterId);
                int targetIndex = MemberIndex(targetId);
                if(requesterIndex != -1 && targetIndex != -1)
                {
                    GuildMember requester = GuildSystem.members[id][requesterIndex];
                    GuildMember target = GuildSystem.members[id][targetIndex];
                    return requester.rank >= Storage.data.guild.promoteMinRank &&
                        requesterId != targetId &&
                        target.rank + 1 < requester.rank;
                }
            }
            return false;
        }

        // can 'requester' demote 'target'?
        // => not in GuildSystem because it needs to be available on the client too
        public bool CanDemote(uint requesterId, uint targetId)
        {
            if (GuildSystem.members[id] != null)
            {
                int requesterIndex = MemberIndex(requesterId);
                int targetIndex = MemberIndex(targetId);
                if(requesterIndex != -1 && targetIndex != -1)
                {
                    GuildMember requester = GuildSystem.members[id][requesterIndex];
                    GuildMember target = GuildSystem.members[id][targetIndex];
                    return requester.rank >= Storage.data.guild.promoteMinRank &&
                        requesterId != targetId &&
                        target.rank > GuildRank.Member;
                }
            }
            return false;
        }
    }
}