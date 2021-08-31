using UnityEngine;
using Mirror;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Pet", order=0)]
    public class PetItem : UsableItem
    {
        public ushort petId;
        // usage
        public override bool CanUse(Player player, int inventoryIndex)
        {
            return base.CanUse(player, inventoryIndex) && !player.own.pets.Has(petId);
        }
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);

            PetSystem.Activate(player, (ushort)name);
        }
    }
}