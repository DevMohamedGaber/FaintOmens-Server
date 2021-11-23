using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
namespace Game.ControlPanel
{
    public class UIOnlineProfile : MonoBehaviour
    {
        [SerializeField] float updateInterval = 5f;
        [SerializeField] TMP_Text Name;
        [SerializeField] TMP_Text accId;
        [SerializeField] TMP_Text idText;
        [SerializeField] TMP_Text level;
        [SerializeField] TMP_Text br;
        [SerializeField] TMP_Text vip;
        [SerializeField] TMP_Text gold;
        [SerializeField] TMP_Text diamonds;
        [SerializeField] TMP_Text bdiamonds;
        [SerializeField] TMP_Text todayHonor;
        [SerializeField] TMP_Text totalHonor;
        [SerializeField] TMP_Text createdAt;
        [SerializeField] TMP_Text SA;
        [SerializeField] UIGivePlayer givePlayer;
        [SerializeField] UIGiveItem giveItem;
        uint id;
        void UpdateData() {
            if(Player.onlinePlayers.ContainsKey(id))
            {
                Player player = Player.onlinePlayers[id];
                Name.text = "Name: " + player.name;
                accId.text = "AccID: " + player.accId.ToString();
                idText.text = "ID:" + player.id.ToString();
                level.text = "Level: " + player.level.ToString();
                br.text = "BR: " + player.battlepower.ToString();
                vip.text = "VIP: " + player.own.vip.level.ToString();
                gold.text = "Gold: " + player.own.gold.ToString();
                diamonds.text = "Diamonds: " + player.own.diamonds.ToString();
                bdiamonds.text = "B.Diamonds: " + player.own.b_diamonds.ToString();
                createdAt.text = "Created: " + DateTime.FromOADate(player.own.createdAt);
                SA.text = $"S_Acts: <color={(player.suspiciousActivities > 0 ? "red" : "white")}>{player.suspiciousActivities}</color>";
            }
            else
            {
                gameObject.SetActive(false);
                UIManager.data.homePage.Show();
                //UIManager.data.offlineProfile.Show(id);
            }
        }
        public void Show(uint id)
        {
            this.id = id;
            givePlayer.id = id;
            gameObject.SetActive(true);
        }
        public void OnShowGiveItem()
        {
            giveItem.Show(id);
        }
        public void OnKick()
        {
            Destroy(Player.onlinePlayers[id]);
        }
        public void OnSwapGender()
        {
            Gender oldGen = Player.onlinePlayers[id].model.gender;
            Player.onlinePlayers[id].model.gender = oldGen == Gender.Male ? Gender.Female : Gender.Male;
        }
        void OnEnable()
        {
            InvokeRepeating("UpdateData", 0, updateInterval);
        }
        void OnDisable()
        {
            CancelInvoke("UpdateData");
            id = 0;
        }
    }
}