using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="uMMORPG Item/Gem", order=999)]
    public class GemItem : ScriptableItem
    {
        [Header("Gem Info")]
        public byte level = 1;
        public BonusType bonusType;
        public float bonus;
        public bool isFloated;
    }
}