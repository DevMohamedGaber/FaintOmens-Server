using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace Game.ControlPanel
{
    public class UILogsList : MonoBehaviour
    {
        [SerializeField] float updateInverval = 5f;
        [SerializeField] Transform content;
        [SerializeField] ScrollRect slider;
        [SerializeField] GameObject prefab;
        List<string> logs = new List<string>();
        static bool needScroll = true;
        public void Add(string log)
        {
            logs.Add(log);
            needScroll = true;
            if(gameObject.activeSelf)
            {
                UpdateData();
            }
        }
        void UpdateData()
        {
            UIUtils.BalancePrefabs(prefab, logs.Count, content);
            if(logs.Count > 0) {
                for(int i = 0; i < logs.Count; i++)
                {
                    content.GetChild(i).GetComponent<TMPro.TMP_Text>().text = logs[i];
                }
                if(needScroll)
                {
                    needScroll = false;
                    ScrollDown();
                }
            }
        }
        void ScrollDown()
        {
            Canvas.ForceUpdateCanvases();// update first so we don't ignore recently added messages, then scroll
            slider.verticalNormalizedPosition = 0;
        }
    }
}