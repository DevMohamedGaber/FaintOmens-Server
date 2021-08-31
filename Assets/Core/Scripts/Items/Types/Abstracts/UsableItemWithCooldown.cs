using UnityEngine;
namespace Game
{
    public abstract class UsableItemWithCooldown : UsableItem
    {
        [Header("Cooldown")]
        public float cooldown; // potion usage interval, etc.
        [Tooltip("Cooldown category can be used if different potion items should share the same cooldown. Cooldown applies only to this item name if empty.")]
    #pragma warning disable CS0649 // Field never assigned to
        [SerializeField] string _cooldownCategory; // leave empty for itemname based cooldown. fill in for category.
    #pragma warning restore CS0649 // Field never assigned to
        public string cooldownCategory =>
            // defaults to per-item-name cooldown if empty. otherwise category.
            string.IsNullOrWhiteSpace(_cooldownCategory) ? name.ToString() : _cooldownCategory;
        public override bool CanUse(Player player, int inventoryIndex)
        {
            return base.CanUse(player, inventoryIndex) && player.GetItemCooldown(cooldownCategory) == 0;
        }
    }
}