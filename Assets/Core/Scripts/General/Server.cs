using System;
namespace Game
{
    public static class Server
    {
        public static ushort number = 1;
        public static string name;
        public static string ip;
        public static ushort port = 7777;
        public static DateTime createdAt;
        public static string timeZone;//"Africa/Cairo";
        public static bool IsPlayerIdWithInServer(uint id)
        {
            return id > number * 10000000 && id < (number + 1) * 10000000;
        }
    }
}
