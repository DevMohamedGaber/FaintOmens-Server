using System;
using UnityEngine;
using UnityEngine.UI;
namespace Game.ControlPanel
{
    public class UIHotEventRow : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_Text info;
        [SerializeField] Button button;
        public void Set(HotEvent evnt, UnityEngine.Events.UnityAction onEdit)
        {
            info.text = $"[{evnt.id}] [{evnt.name[1]}] [Objs: {evnt.objectives.Length}] [From: {DateTime.FromOADate(evnt.startsAt).ToString("dd/MM")}] [To: {DateTime.FromOADate(evnt.endsAt).ToString("dd/MM")}]";
            button.onClick.SetListener(onEdit);
        }
    }
}