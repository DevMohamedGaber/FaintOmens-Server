using System;
namespace Game
{
    [Serializable]
    public struct Team
    {
        public uint id;
        public TeamMember[] members;
        public float[] bonuses;
        public uint leaderId;
        public ExperiaceShareType share;
        public bool IsFull => members != null && members.Length == Storage.data.team.capacity;
        public Team(uint id = 0, uint leaderId = 0u)
        {
            this.id = id;
            this.leaderId = leaderId;
            this.members = new TeamMember[]{};
            this.bonuses = new float[]{};
            this.share = ExperiaceShareType.Equal;
        }
        public void Add(TeamMember member)
        {
            Array.Resize(ref members, members.Length + 1);
            members[members.Length - 1] = member;
            ReDistributeBonuses();
        }
        public bool Contains(uint memberId)
        {
            if (members != null)
                for(int i = 0; i < members.Length; i++) {
                    if(members[i].id == memberId)
                        return true;
                }
            return false;
        }
        public bool HasOnline()
        {
            for(int i = 0; i < members.Length; i++) {
                if(members[i].online)
                    return true;
            }
            return false;
        }
        public void ReDistributeBonuses()
        {
            bonuses = new float[members.Length];
            if(members.Length > 1)
            {
                for(int i = 0; i < members.Length; i++)
                {
                    float totalBonus = Storage.data.team.bonusPerMemeber * members.Length;
                    for(int j = 0; j < members.Length; j++)
                    {
                        if(j != i && members[j].online)
                        {
                            byte friendLevel = members[i].data.own.friends.GetFriendLevel(members[j].id);
                            if(friendLevel > 0)
                                totalBonus += friendLevel * Storage.data.team.bonusPerFriendLevel;
                        }
                    }
                    bonuses[i] = totalBonus;
                }
            }
            else {
                bonuses = new float[]{0};
            }
        }
        public float GetBonus(uint mId)
        {
            if(members.Length > 1) {
                for(int i = 0; i < members.Length; i++)
                {
                    if(members[i].id == mId)
                        return bonuses[i];
                }
            }
            return 0f;
        }
    }
}