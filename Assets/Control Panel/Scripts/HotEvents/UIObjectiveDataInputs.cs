using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Game.ControlPanel
{
    public class UIObjectiveDataInputs : MonoBehaviour
    {
        public TMP_InputField type;
        public TMP_InputField amountText;
        public Transform rewards;
        [SerializeField] ContentSizeFitter csf;
        int amount => Convert.ToInt32(amountText.text);
        public HotEventObjective Get()
        {
            HotEventObjective result = new HotEventObjective();
            result.type = type.text;
            result.amount = amount;
            result.rewards = new HotEventReward[rewards.childCount];
            for(int i = 0; i < rewards.childCount; i++)
            {
                result.rewards[i] = rewards.GetChild(i).GetComponent<UIObjectiveRewardData>().Get();
            }
            return result;
        }
        public bool CanGet()
        {
            return amount > 0 && rewards.childCount > 0 && IsRewardsValid();
        }
        bool IsRewardsValid()
        {
            for(int i = 0; i < rewards.childCount; i++)
            {
                if(!rewards.GetChild(i).GetComponent<UIObjectiveRewardData>().CanGet())
                    return false;
            }
            return true;
        }
        public void AddRewardField()
        {
            Instantiate(UIManager.data.objRewardInput, rewards, false);
            UpdateLayout();
            UIManager.data.createNewHotEvent.UpdateLayout();
        }
        public void OnDelete()
        {
            Destroy(gameObject);
            UIManager.data.createNewHotEvent.UpdateLayout();
        }
        public void UpdateLayout()
        {
            Canvas.ForceUpdateCanvases();
            csf.enabled = false;
            csf.SetLayoutVertical();
            csf.enabled = true;
        }
    }
}