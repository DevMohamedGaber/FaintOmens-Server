using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
namespace Game.ControlPanel
{
    public class UIGiveItem : MonoBehaviour
    {
        [SerializeField] InputField playerIdText;
        [SerializeField] InputField itemIdText;
        [SerializeField] InputField plus;
        [SerializeField] Dropdown quality;
        [SerializeField] Dropdown qualityMax;
        [SerializeField] InputField socket1;
        [SerializeField] InputField socket2;
        [SerializeField] InputField socket3;
        [SerializeField] InputField socket4;
        [SerializeField] InputField amountText;
        [SerializeField] Toggle bound;
        [SerializeField] Button submit;
        [SerializeField] TMP_Text errorTxt;

        public void OnSubmit() {
            if(itemIdText.text == "") {
                errorTxt.text = "fill the item id";
                return;
            }
            if(playerIdText.text == "") {
                errorTxt.text = "fill the player id";
                return;
            }
            if(amountText.text == "" || amountText.text == "0") {
                errorTxt.text = "fill the amount";
                return;
            }
            uint playerId = Convert.ToUInt32((string)playerIdText.text);
            ushort itemId = Convert.ToUInt16((string)itemIdText.text);
            uint amount = Convert.ToUInt32((string)amountText.text);
            if(Player.onlinePlayers.TryGetValue(playerId, out Player player) && playerId > 0 && itemId > 0 
                && amount > 0 && ScriptableItem.dict.ContainsKey(itemId)) {
                Item item = new Item {
                    id = itemId,
                    plus = Convert.ToByte((string)plus.text),
                    quality = new ItemQualityData((Quality)quality.value, (Quality)qualityMax.value),
                    socket1 = new Socket(Convert.ToInt16((string)socket1.text)),
                    socket2 = new Socket(Convert.ToInt16((string)socket2.text)),
                    socket3 = new Socket(Convert.ToInt16((string)socket3.text)),
                    socket4 = new Socket(Convert.ToInt16((string)socket4.text)),
                    bound = bound.isOn
                };
                item.durability = item.MaxDurability();
                player.InventoryAdd(item, amount);
                Reset();
            }
            else {
                // send mail to database
            }
            //else UINotifications.list.Add("ال ID يابنى ركز هتضيعنا");
        }
        public void Show(uint id) {
            if(Server.IsPlayerIdWithInServer(id)) {
                playerIdText.text = id.ToString();
                gameObject.SetActive(true);
            }
        }
        void Reset() {
            itemIdText.text = "";
            plus.text = "0";
            quality.value = 0;
            socket1.text = "-1";
            socket2.text = "-1";
            socket3.text = "-1";
            socket4.text = "-1";
            amountText.text = "1";
            bound.isOn = true;
        }
        void OnDisable() {
            Reset();
            playerIdText.text = "";
        }
    }
}