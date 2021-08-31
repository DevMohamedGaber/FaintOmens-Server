using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
namespace Game.ControlPanel
{
    public class UICreateNewHotEvent : MonoBehaviour
    {
        [SerializeField] TMP_InputField arName;
        [SerializeField] TMP_InputField enName;
        [SerializeField] TMP_InputField arDesc;
        [SerializeField] TMP_InputField enDesc;
        [SerializeField] TMP_InputField startDate;
        [SerializeField] TMP_InputField endDate;
        [SerializeField] TMP_Dropdown types;
        [SerializeField] Toggle renewable;
        [SerializeField] Transform content;
        [SerializeField] GameObject objPrefab;
        [SerializeField] ContentSizeFitter csf;

        public void OnSubmit()
        {
            if(CanCreate())
            {
                HotEvent evnt = new HotEvent();
                evnt.name = new string[] { arName.text, enName.text };
                evnt.description = new string[] { arDesc.text, enDesc.text };
                evnt.startsAt = DateTime.Parse(startDate.text).ToOADate();
                evnt.endsAt = DateTime.Parse(endDate.text).ToOADate();
                Debug.Log(DateTime.Parse(startDate.text));
                evnt.type = (HotEventTypes)types.value;
                evnt.renewable = renewable.isOn;
                evnt.objectives = new HotEventObjective[content.childCount];
                for(int i = 0; i < content.childCount; i++)
                {
                    UIObjectiveDataInputs data = content.GetChild(i).GetComponent<UIObjectiveDataInputs>();
                    if(!data.CanGet())
                        return;
                    evnt.objectives[i] = data.Get();
                }
                Database.singleton.CreateHotEvent(evnt);
            }
        }
        bool CanCreate()
        {
            return  arName.text != "" && enName.text != "" &&
                    arDesc.text != "" && enDesc.text != "" &&
                    startDate.text != "" && endDate.text != "" && content.childCount > 0;
        }
        public void AddNewObjective()
        {
            Instantiate(objPrefab, content, false);
            UpdateLayout();
        }
        public void UpdateLayout()
        {
            Invoke("UpdateLayoutAction", 1f);
        }
        void UpdateLayoutAction()
        {
            Canvas.ForceUpdateCanvases();
            csf.enabled = false;
            csf.SetLayoutVertical();
            csf.enabled = true;
        }
        void ResetInputs()
        {
            arName.text = "";
            enName.text = "";
            arDesc.text = "";
            enDesc.text = "";
            startDate.text = DateTime.Now.ToString("MM-dd-yyyy") + " 12:00 AM";
            endDate.text = DateTime.Now.ToString("MM-dd-yyyy") + " 12:00 AM";
            types.value = 0;
            renewable.isOn = false;
        }
        void OnEnable()
        {
            ResetInputs();
        }
        void Awake()
        {
            foreach(HotEventTypes type in Enum.GetValues(typeof(HotEventTypes)))
            {
                types.options.Add(new TMP_Dropdown.OptionData(type.ToString()));
            }
            types.RefreshShownValue();
        }
    }
}