using UnityEngine;
using Game.Arena;
namespace Game.Components
{
    public class ArenaRoomsManager : MonoBehaviour
    {
        public ArenaRoomSingleData a1v1;
        public ArenaRoomMultipleData a3v3;
        public ArenaRoomMultipleData a5v5;
        void Awake()
        {
            ArenaSystem.manager = this;
            a1v1.Prepare();
            a3v3.Prepare();
            a5v5.Prepare();
        }
    }
}