using System;
using UnityEngine;
using Mirror;
namespace Game
{
    public class Npc : NetworkBehaviourNonAlloc
    {
        [Header("Teleportation")]
        public TeleportNPCOffer[] teleports;

        [Header("Booth")]
        public string boothName;
        public GameObject BoothGameObject;

        void Start() {
            NetworkServer.Spawn(gameObject);
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            //health = healthMax;
            //mana = manaMax;
        }
    }
}