using UnityEngine;
using Mirror;
using Game.ControlPanel;
namespace Game
{
    public class Loot : NetworkBehaviourNonAlloc
    {
        [SyncVar] public uint owner;
        [SyncVar] public ItemSlot item;
        public bool IsEmpty()
        {
            return item.amount == 0 || item.item.id == 0;
        }
        public override void OnStartServer()
        {
            Invoke("AllowLootForAll", Storage.data.lootAllowAll);
        }
        [Server] void AllowLootForAll()
        {
            owner = 0;
            Invoke("DestroySelf", Storage.data.lootDestroySelf);
        }
        [Server] void DestroySelf()
        {
            NetworkServer.Destroy(this.gameObject);
        }
        [Command] public void CmdClaim(uint playerId)
        {
            if(IsEmpty())
            {
                DestroySelf();
                return;
            }
            if(Player.onlinePlayers.TryGetValue(playerId, out Player player))
            {
                if(owner == 0 || owner == playerId)
                {
                    if(player.InventoryAdd(item.item, item.amount))
                        DestroySelf();
                }
                else
                {
                    player.Notify("This loot belongs to another player", "هذه الغنيمة خاصة بلاعب اخر");
                }
            }
            else
            {
                UIManager.data.logsList.Add($"[Loot.CmdCollect] Someone sent id: {playerId}");
            }
        }
    }
}