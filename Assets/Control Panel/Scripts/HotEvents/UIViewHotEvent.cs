using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Game.ControlPanel
{
    public class UIViewHotEvent : MonoBehaviour
    {
        int index;
        public void Show(int index)
        {
            this.index = index;

            gameObject.SetActive(true);
        }
    }
}