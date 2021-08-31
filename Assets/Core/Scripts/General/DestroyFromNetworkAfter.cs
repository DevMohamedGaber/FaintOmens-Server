using UnityEngine;
namespace Game.Components
{
    public class DestroyFromNetworkAfter : NetworkBehaviourNonAlloc
    {
        [SerializeField] float after;
        void Hide()
        {
            Mirror.NetworkServer.Destroy(gameObject);
        }
        void OnEnable()
        {
            Invoke("Hide", after);
        }
    }
}
