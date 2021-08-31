namespace Game.DatabaseModels
{
    public class GuildMembers
    {
        [SQLite.PrimaryKey]
        public uint id { get; set; }
        [SQLite.Indexed]
        public uint guildId { get; set; }
        public GuildRank rank { get; set; }
        public uint contribution { get; set; }
    }
}