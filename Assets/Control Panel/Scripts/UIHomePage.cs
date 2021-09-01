using UnityEngine;
using TMPro;
using Game.Network;
namespace Game.ControlPanel
{
    public class UIHomePage : MonoBehaviour
    {
        [SerializeField] TMP_Text onlineCount;
        [SerializeField] TMP_Text lobbyCount;
        [SerializeField] TMP_Text charactersCount;
        [SerializeField] NetworkManagerMMO manager;
        public void ClearLobby()
        {
            if(manager.lobby.Count > 0)
            {
                manager.lobby.Clear();
                lobbyCount.text = "0";
            }
        }
        public void UpdateOnlineCount()
        {
            onlineCount.text = Player.onlinePlayers.Count.ToString();
        }
        public void UpdateLobbyCount()
        {
            lobbyCount.text = manager.lobby.Count.ToString();
        }
        public void UpdateCharacterCount(sbyte value = 1)
        {
            int count = charactersCount.text.ToInt();
            count += value;
            charactersCount.text = count.ToString();
        }
        public void Show()
        {
            gameObject.SetActive(true);
        }
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        void Awake()
        {
            charactersCount.text = Database.singleton.GetCharactersCount().ToString();
        }
    }
}