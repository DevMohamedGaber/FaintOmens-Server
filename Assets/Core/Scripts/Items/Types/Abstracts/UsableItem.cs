using UnityEngine;
namespace Game
{
    public abstract class UsableItem : ScriptableItem
    {
        public virtual bool CanUse(Player player, int inventoryIndex)
        {
            return player.level >= minLevel;
        }
        public virtual void Use(Player player, int inventoryIndex, uint amount = 1) {}
        public virtual void OnUsed(Player player) {}
    }
}