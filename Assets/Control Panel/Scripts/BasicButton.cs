using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine;
namespace Game.ControlPanel
{
    public class BasicButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] UnityEvent action;
        public UnityAction onClick
        {
            set
            {
                if(value == null)
                {
                    action.RemoveAllListeners();
                }
                else
                {
                    action.SetListener(value);
                }
            }
        }
        public bool hasAction => action != null;
        public virtual void OnInvokeAction()
        {
            if(hasAction)
            {
                action.Invoke();
            }
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            OnInvokeAction();
        }
        public void Show()
        {
            gameObject.SetActive(true);
        }
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}