using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Components;
namespace Game
{
    [Serializable]
    public struct City
    {
        public int minLvl;
        //public CityStatus status;
        public List<Npc> teleportNPCs;
        public CityArea prefab;
        public Vector3 StartingPoint() => prefab.entry;
    }
}
