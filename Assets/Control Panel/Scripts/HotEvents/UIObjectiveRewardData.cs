using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Game.ControlPanel
{
    public class UIObjectiveRewardData : MonoBehaviour
    {
        public TMP_InputField type;
        public TMP_InputField amountText;
        int amount => System.Convert.ToInt32(amountText.text);

        public HotEventReward Get() => new HotEventReward(type.text, amount);
        public bool CanGet() => type.text != "" && amount > 0;
        public void OnDelete()
        {
            Destroy(this.gameObject);
            ContentSizeFitter csf = transform.parent.GetComponent<ContentSizeFitter>();
            Canvas.ForceUpdateCanvases();
            csf.enabled = false;
            csf.SetLayoutVertical();
            csf.enabled = true;
            UIManager.data.createNewHotEvent.UpdateLayout();
        }
    }
}