namespace Game
{
    [System.Serializable]
    public struct TeamMember
    {
        public uint id;
        public string name;
        public byte level;
        public byte avatar;
        public bool online;
        public Player data => Player.onlinePlayers[id];
        public TeamMember(Player member, bool online)
        {
            this.id = member.id;
            this.name = member.name;
            this.level = (byte)member.level;
            this.avatar = member.avatar;
            this.online = online;
        }
    }
}