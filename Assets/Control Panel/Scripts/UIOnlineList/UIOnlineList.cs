using UnityEngine;
namespace Game.ControlPanel
{
    public class UIOnlineList : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_Text counter;
        [SerializeField] GameObject prefab;
        [SerializeField] Transform content;
        public void Refresh()
        {
            counter.text = Player.onlinePlayers.Count.ToString();
            UIUtils.BalancePrefabs(prefab, Player.onlinePlayers.Count, content);
            if(Player.onlinePlayers.Count > 0) 
            {
                int i = 0;
                foreach(Player player in Player.onlinePlayers.Values)
                {
                    UIOnlinePlayerRow row = content.GetChild(i).GetComponent<UIOnlinePlayerRow>();
                    row.info.text = $"[ {player.id} ] [ {player.name} ] [ Lvl.{player.level} ] [ BR:{player.battlepower} ] [ SA:{player.suspiciousActivities} ]";
                    row.btn.onClick.SetListener(() =>
                    {
                        if(Player.onlinePlayers.ContainsKey(player.id))
                        {
                            gameObject.SetActive(false);
                            UIManager.data.onlineProfile.Show(player.id);
                        }
                        else
                        {
                            Refresh();
                        }
                    });
                    i++;
                }
            }
        }
        void OnEnable()
        {
            Refresh();
        }
    }
}