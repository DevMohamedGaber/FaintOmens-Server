using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    [Serializable]
    public struct Mail
    {
        [HideInInspector] public uint id;
        [HideInInspector] public uint sender;
        [HideInInspector] public string senderName;
        public MailCategory category;
        public string subject;
        [TextArea(0, 30)] public string content;
        [HideInInspector] public bool opened;
        public Currencies currency;
        [HideInInspector] public double sentAt;
        public MailItemSlot[] items;

        public Mail(bool opened = false)
        {
            this.id = 0;
            this.category = MailCategory.System;
            this.sender = 0;
            this.senderName = "";
            this.subject = "";
            this.content = "";
            this.opened = false;
            this.currency = new Currencies();
            this.sentAt = 0;
            this.items = new MailItemSlot[]{};
        }
        public bool IsEmpty()
        {
            if(!currency.recieved)
                return false;
            if(items.Length > 0) {
                for(int i = 0; i < items.Length; i++)
                {
                    if(!items[i].recieved)
                        return false;
                }
            }
            return true;
        }
        public void AddItem(MailItemSlot item)
        {
            Array.Resize(ref items, items.Length + 1);
            items[items.Length + 1] = item;
        }
        public void DefineItems(ItemSlot[] itemsList)
        {
            items = new MailItemSlot[itemsList.Length];
            for(int i = 0; i < items.Length; i++) {
                items[i] = new MailItemSlot(itemsList[i]);
            }
        }
        public void DefineItems(List<ItemSlot> itemsList)
        {
            items = new MailItemSlot[itemsList.Count];
            for(int i = 0; i < items.Length; i++)
            {
                items[i] = new MailItemSlot(itemsList[i]);
            }
        }
    }
}