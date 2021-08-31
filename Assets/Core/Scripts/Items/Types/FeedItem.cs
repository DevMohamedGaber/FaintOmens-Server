using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Feed", order=0)]
    public class FeedItem : ScriptableItem
    {
        public ushort amount;
    }
}
