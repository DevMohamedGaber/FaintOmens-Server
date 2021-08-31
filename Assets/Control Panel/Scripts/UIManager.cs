using UnityEngine;
using UnityEngine.UI;
namespace Game.ControlPanel
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager data;
        [Header("Windows")]
        public UIStartOfflinePage offlinePage;
        public UIHomePage homePage;
        public ServerInitialization initializer;
        public UIOnlineProfile onlineProfile;
        public UIOfflineProfile offlineProfile;
        public UICreateNewHotEvent createNewHotEvent;
        public UILogsList logsList;
        [Header("Prefabs")]
        public GameObject objRewardInput;

        void Awake() {
            if(data == null)
                data = this;
        }
    }
}