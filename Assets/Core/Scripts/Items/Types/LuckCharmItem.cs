using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    [CreateAssetMenu(menuName="Custom/Items/LuckCharm", order=0)]
    public class LuckCharmItem : ScriptableItem
    {
        public double amount = .1d;
    }
}