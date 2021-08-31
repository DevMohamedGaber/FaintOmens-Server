using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
namespace GameServer.ControlPanel
{
    public class UICloseWindow : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject window;
        [SerializeField] private GameObject nextWindow;
        [SerializeField] private UnityEvent action;
        public void OnPointerClick(PointerEventData eventData)
        {
            if(action != null)
                action.Invoke();
            if(window != null)
                window.SetActive(false);
            if(nextWindow != null)
                nextWindow.SetActive(true);
        }
    }
}