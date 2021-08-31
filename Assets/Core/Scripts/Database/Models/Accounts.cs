using SQLite;
using System;
namespace Game.DatabaseModels
{
    public class Accounts
    {
        [PrimaryKey, AutoIncrement]
        public ulong? id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public DateTime created { get; set; }
        public DateTime lastlogin { get; set; }
        public bool banned { get; set; }
    }
}