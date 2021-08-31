using System.Text;
using UnityEngine;
using System;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/Title", order=999)]
    public class TitleItem : UsableItem
    {
        public int titleId;
        public override bool CanUse(Player player, int inventoryIndex)
        {
            return player.own.titles.FindIndex(t => t == titleId) == -1;
        }
        public override void Use(Player player, int inventoryIndex, uint amount = 1)
        {
            base.Use(player, inventoryIndex);
            player.CmdActivateTitle(inventoryIndex);
        }
    }
}