using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Game.Network;
namespace Game.ControlPanel
{
    public class UIGivePlayer : MonoBehaviour
    {
        public uint id;
        bool isOnline => Player.onlinePlayers.ContainsKey(id);
        public void OnChangeName(TMP_InputField input)
        {
            if(isOnline && NetworkManagerMMO.IsAllowedCharacterName((string)input.text))
            {
                Player.onlinePlayers[id].ChangeName((string)input.text);
                input.text = "";
            }
        }
        public void OnAddGold(TMP_InputField input)
        {
            uint amount = Convert.ToUInt32((string)input.text);
            if(isOnline && amount > 0)
            {
                Player.onlinePlayers[id].AddGold(amount);
                input.text = "0";
            }
        }
        public void OnAddDiamonds(TMP_InputField input)
        {
            uint amount = Convert.ToUInt32((string)input.text);
            if(isOnline && amount > 0)
            {
                Player.onlinePlayers[id].AddDiamonds(amount);
                input.text = "0";
            }
        }
        public void OnAddBDiamonds(TMP_InputField input)
        {
            uint amount = Convert.ToUInt32((string)input.text);
            if(isOnline && amount > 0)
            {
                Player.onlinePlayers[id].AddBDiamonds(amount);
                input.text = "0";
            }
        }
    }
}