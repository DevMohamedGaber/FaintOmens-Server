using UnityEngine;
namespace Game.Components
{
    [RequireComponent(typeof(BoxCollider))]
    public class CityArea : MonoBehaviour {
        public int id;
        public PlayerSpawnArea _entry;
        public Vector3 entry => _entry.GetValidRandomPosition();
        City data => Storage.data.cities[id];
        /*void OnTriggerEnter(Collider co) {
            Entity entity = co.GetComponentInParent<Entity>();
            if(entity is Player) {
                Player player = entity.GetComponent<Player>();
                if(id > -1) {
                    if(player.level < data.minLvl) {
                        player.TeleportTo(player.tribe.id - 1, Storage.data.cities[player.tribe.id - 1].StartingPoint());
                    }
                }  
            }
        }*/
    }
}