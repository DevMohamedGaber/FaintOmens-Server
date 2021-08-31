using UnityEngine;
using UnityEngine.EventSystems;
namespace GameServer.ControlPanel
{
    public class UIOpenWindow : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] GameObject window;
        public void OnPointerClick(PointerEventData eventData)
        {
            if(window != null)
                window.SetActive(true);
        }
    }
}