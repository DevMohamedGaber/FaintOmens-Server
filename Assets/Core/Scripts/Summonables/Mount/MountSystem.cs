namespace Game
{
    public class MountSystem
    {
        public static void Deploy(Player player, ushort mountId)
        {
            if(!player.CanTakeAction())
                return;
            if(!ScriptableMount.dict.ContainsKey(mountId))
            {
                player.Notify("Select a valid Mount", "اختر راكب صحيح");
                player.Log("[MountSystem.SetActive] invalid id=" + mountId);
                return;
            }
            if(!player.own.mounts.Has(mountId))
            {
                player.Notify("Mount isn't active", "الراكب غير مفعل");
                player.Log("[MountSystem.SetActive] inactive id=" + mountId);
                return;
            }
            if(player.mount.id != 0)
            {
                for(int i = 0; i < player.own.mounts.Count; i++)
                {
                    if(player.own.mounts[i].id == player.mount.id)
                    {
                        Mount active = player.own.mounts[i];
                        active.status = SummonableStatus.Saved;
                        player.own.mounts[i] = active;
                        break;
                    }
                }
            }
            for(int i = 0; i < player.own.mounts.Count; i++)
            {
                if(player.own.mounts[i].id == mountId)
                {
                    Mount newActive = player.own.mounts[i];
                    newActive.status = SummonableStatus.Deployed;
                    player.own.mounts[i] = newActive;
                    player.selectedMountIndex = i;
                    break;
                }
            }
            ActiveMount info = player.mount;
            info.id = mountId;
            player.mount = info;
            player.NextAction();
        }
        public static void Recall(Player player)
        {
            if(!player.CanTakeAction())
                return;
            if(player.mount.id == 0)
            {
                player.Notify("Mount isn't active", "الراكب غير مفعل");
            }
            if(player.mount.id != 0)
            {
                for(int i = 0; i < player.own.mounts.Count; i++)
                {
                    if(player.own.mounts[i].id == player.mount.id)
                    {
                        Mount active = player.own.mounts[i];
                        active.status = SummonableStatus.Saved;
                        player.own.mounts[i] = active;
                        ActiveMount info = player.mount;
                        info.id = 0;
                        info.mounted = false;
                        player.mount = info;
                        return;
                    }
                }
            }
        }
        public static void Feed(Player player, ushort mountId, int selectedFeed, uint amount)
        {
            if(!player.CanTakeAction())
                return;
            int index = player.own.mounts.FindIndex(mount => mount.id == mountId);
            if(index == -1)
            {
                player.Notify("Mount isn't activated", "الراكب غير مفعل");
                return;
            }
            if(selectedFeed < 0 || selectedFeed > Storage.data.mount.feeds.Length)
            {
                player.Notify("Please Select a Feed Item", "برجاء اختيار طعام للمرافق");
                return;
            }

            FeedItem item = Storage.data.mount.GetFeed(selectedFeed);
            uint ownAmount = player.InventoryCountById(item.name);
            amount = ownAmount < amount ? ownAmount : amount;

            Mount info = player.own.mounts[index];
            uint feeded = 0;
            while(feeded < amount)
            {
                if(info.level < Storage.data.mount.lvlCap)
                {
                    info.Feed(item.amount);
                    feeded++;
                }
                else break;
            }
            player.InventoryRemove(item.name, feeded);
            player.own.mounts[index] = info;
            player.NextAction(.5);
        }
        public static void Activate(Player player, ushort itemId)
        {
            if(!player.CanTakeAction())
                return;
            int index = player.GetInventoryIndex(itemId);
            if(index > -1)
            {
                ItemSlot itemSlot = player.own.inventory[index];
                if(itemSlot.amount < 1)
                {
                    player.NotifyNotEnoughMaterials();
                    return;
                }
                if(itemSlot.item.data is MountItem itemData && itemData.CanUse(player, index))
                {
                    player.own.mounts.Add(new Mount(itemData.mountId));
                    ItemSlot slot = player.own.inventory[index];
                    slot.DecreaseAmount(1);
                    player.own.inventory[index] = slot;
                    // Note: update mount Collection Achievement
                    //itemData.Use(player, index);
                    player.NextAction(.5);
                }
            }
            else
            {
                player.Notify("Mount Card Not Found", "لا يوجد بطاقة مرافق");
            }
        }
        public static void Upgrade(Player player, ushort id) // upgrade tier
        { 
            if(!player.CanTakeAction())
                return;
            int index = player.own.mounts.FindIndex(p => p.id == id);
            if(index == -1)
            {
                player.Notify("Mount isn't active", "المرافق غير مفعل");
                return;
            }
            Mount mount = player.own.mounts[index];
            if(mount.tier == mount.data.maxTire)
            {
                player.Notify("Mount reached max Tire", "وصل المرافق لاعلي تطوير");
                return;
            }
            uint availableItems = player.InventoryCountById(Storage.data.mount.upgradeItemId);
            uint neededItems = Storage.data.mount.upgradeItemsCount[(int)mount.tier];
            if(availableItems < neededItems)
            {
                player.NotifyNotEnoughMaterials();
                return;
            }
            player.InventoryRemove(Storage.data.mount.upgradeItemId, neededItems);
            mount.tier++;
            player.own.mounts[index] = mount;
            player.NextAction();
        }
        public static void StarUp(Player player, ushort id)
        {
            // upgrade stars
            if(!player.CanTakeAction())
                return;
            int index = player.own.mounts.FindIndex(m => m.id == id);
            if(index == -1)
            {
                player.Notify("Mount isn't active", "الراكب غير مفعل");
                return;
            }
            Mount mount = player.own.mounts[index];
            if(mount.stars == Storage.data.mount.starsCap)
            {
                player.Notify("Mount reached max stars", "وصل الراكب لاعلي عدد نجوم");
                return;
            }
            uint availableItems = player.InventoryCountById(Storage.data.mount.starsUpItemId);
            uint neededItems = Storage.data.mount.starUpItemsCount[mount.stars];
            if(availableItems < neededItems)
            {
                player.NotifyNotEnoughMaterials();
                return;
            }
            player.InventoryRemove(Storage.data.mount.starsUpItemId, neededItems);
            mount.stars++;
            player.own.mounts[index] = mount;
            player.NextAction();
        }
        public static void Train(Player player, ushort id, byte type, ushort count = 1)
        {
            if(!player.CanTakeAction())
                return;
            if(type < 0 || type > 3)
            {
                player.Notify("Select an attribute to train", "");
                return;
            }
            int index = player.own.mounts.FindIndex(m => m.id == id);
            if(index == -1)
            {
                player.Notify("Mount isn't active", "الراكب غير مفعل");
                return;
            }
            uint invCount = player.InventoryCountById(Storage.data.mount.trainItemId);
            if(invCount < 1)
            {
                player.Notify("Not Enough mount training item", "");
                return;
            }
            if(invCount < count)
            {
                count = (ushort)invCount;
            }

            Mount mount = player.own.mounts[index];

            if(type == 0 && mount.training.vitality.level < mount.level)
            {
                mount.training.vitality.AddExp((ushort)(Storage.data.mount.trainingExpPerItem * count));
            }
            else if(type == 1 && mount.training.strength.level < mount.level)
            {
                mount.training.strength.AddExp((ushort)(Storage.data.mount.trainingExpPerItem * count));
            }
            else if(type == 2 && mount.training.intelligence.level < mount.level)
            {
                mount.training.intelligence.AddExp((ushort)(Storage.data.mount.trainingExpPerItem * count));
            }
            else if(type == 3 && mount.training.endurance.level < mount.level)
            {
                mount.training.endurance.AddExp((ushort)(Storage.data.mount.trainingExpPerItem * count));
            }
            
            player.own.mounts[index] = mount;
            player.InventoryRemove(Storage.data.mount.trainItemId);
            player.NextAction();
        }
    }
}