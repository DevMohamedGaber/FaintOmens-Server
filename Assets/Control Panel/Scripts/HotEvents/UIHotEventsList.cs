using System;
using UnityEngine;
using UnityEngine.UI;
namespace Game.ControlPanel
{
    public class UIHotEventsList : MonoBehaviour
    {
        [SerializeField] Transform content;
        [SerializeField] GameObject prefab;
        [SerializeField] UIViewHotEvent editWindow;
        public void UpdateData()
        {
            UIUtils.BalancePrefabs(prefab, HotEventsSystem.events.Count, content);
            if(HotEventsSystem.events.Count > 0)
            {
                for(int i = 0; i < HotEventsSystem.events.Count; i++)
                {
                    int iCopy = i;
                    content.GetChild(i).GetComponent<UIHotEventRow>().Set(HotEventsSystem.events[i], () => OnEdit(iCopy));
                }
            }
        }
        void OnEnable()
        {
            UpdateData();
        }
        void OnEdit(int index)
        {
            editWindow.Show(index);
        }
    }
}