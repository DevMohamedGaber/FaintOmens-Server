using System;
using SQLite;
namespace Game.DatabaseModels
{
    public class Server
    {
        [PrimaryKey]
        public ushort number { get; set; }
        public string name { get; set; }
        public string timezone { get; set; } = TimeZoneInfo.Local.Id;
        public ushort port { get; set; } = 7777;
        public DateTime createdAt { get; set; }
    }
}