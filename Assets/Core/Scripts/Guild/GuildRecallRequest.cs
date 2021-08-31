using System;
using UnityEngine;
namespace Game
{
    [Serializable]
    public struct GuildRecallRequest
    {
        public static readonly GuildRecallRequest Empty = new GuildRecallRequest();
        public uint id;
        public string name;
        public byte city;
        public Vector3 position;

        public GuildRecallRequest(uint id, string name, byte city, Vector3 position)
        {
            this.id = id;
            this.name = name;
            this.city = city;
            this.position = position;
        }
    }
}