using UnityEngine;
using System.Collections.Generic;
using System;
using Game.Components;
namespace Game.Arena
{
    [Serializable]
    public struct ArenaRoomMultipleData
    {
        public GameObject prefab;
        public int roomsOnAwake;
        public Vector3 nextRoomPosition;
        public float distanceBetweenRooms;
        public List<ArenaRoomSingle> rooms;
        public short GetFreeRoom()
        {
            if(rooms.Count > 0)
            {
                for(short i = 0; i < rooms.Count; i++)
                {
                    if(rooms[i].isFree)
                        return i;
                }
            }
            InstantiateRoom();
            return (short)(rooms.Count - 1);
        }
        public void Prepare()
        {
            if(roomsOnAwake > 0)
            {
                for(int i = 0; i < roomsOnAwake; i++)
                    InstantiateRoom();
            }
        }
        void InstantiateRoom()
        {
            GameObject go = GameObject.Instantiate(prefab, nextRoomPosition, Quaternion.identity);
            go.transform.SetParent(ArenaSystem.manager.transform);
            ArenaRoomSingle room = go.GetComponent<ArenaRoomSingle>();
            room.id = (short)rooms.Count;
            rooms.Add(room);
            nextRoomPosition.y += distanceBetweenRooms;
        }
    }
}