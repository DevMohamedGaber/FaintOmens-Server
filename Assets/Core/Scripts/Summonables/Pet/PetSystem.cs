using UnityEngine;
using Mirror;
namespace Game
{
    public class PetSystem
    {
        public static void Summon(Player player, ushort id)
        {
            if(!player.CanTakeAction())
                return;
            int index = player.own.pets.FindIndex(p => p.id == id);
            if(index == -1)
            {
                player.Notify("Pet isn't activated", "المرافق غير مفعل");
                return;
            }
            // set as deployed
            PetInfo petinfo = player.own.pets[index];
            petinfo.status = SummonableStatus.Deployed;
            player.own.pets[index] = petinfo;

            if(player.activePet != null)
            {
                // set as saved
                PetInfo deployedPet = player.own.pets[player.activePet.dataIndex];
                deployedPet.status = SummonableStatus.Saved;
                player.own.pets[player.activePet.dataIndex] = deployedPet;
            }
            else
            {
                GameObject go = GameObject.Instantiate(Storage.data.pet.petPrefab, player.petDestination, Quaternion.identity);
                NetworkServer.Spawn(go);
                player.activePet = go.GetComponent<Pet>();
                player.activePet.owner = player;
            }
            player.activePet.SetData(index);
            //player.health += pet.healthMax;
            //player.mana += pet.manaMax;
            player.NextAction();
        }
        public static void Unsummon(Player player)
        {
            if(!player.CanTakeAction())
                return;
            if (player.CanUnsummonPet())
            {
                PetInfo info = player.own.pets[player.activePet.dataIndex];
                info.status = SummonableStatus.Saved;
                player.own.pets[player.activePet.dataIndex] = info;
                NetworkServer.Destroy(player.activePet.gameObject);
            }
        }
        public static void Feed(Player player, ushort petId, int selectedFeed, uint amount)
        {
            if(!player.CanTakeAction())
                return;
            int index = player.own.pets.FindIndex(pet => pet.id == petId);
            if(index == -1)
            {
                player.Notify("Pet isn't activated", "المرافق غير مفعل");
                return;
            }
            if(selectedFeed < 0 || selectedFeed > Storage.data.pet.feeds.Length)
            {
                player.Notify("Please Select a Feed Item", "برجاء اختيار طعام للمرافق");
                return;
            }

            FeedItem item = Storage.data.pet.GetFeed(selectedFeed);
            uint ownAmount = player.InventoryCountById(item.name);
            amount = ownAmount < amount ? ownAmount : amount;

            PetInfo info = player.own.pets[index];
            uint feeded = 0;
            while(feeded < amount)
            {
                if(info.level < Storage.data.pet.lvlCap)
                {
                    info.Feed(item.amount);
                    feeded++;
                }
                else break;
            }
            player.InventoryRemove(item.name, feeded);
            player.own.pets[index] = info;
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
                if(itemSlot.amount < 1){
                    player.NotifyNotEnoughMaterials();
                    return;
                }
                if(itemSlot.item.data is PetItem itemData && itemData.CanUse(player, index))
                {
                    player.own.pets.Add(new PetInfo(itemData.petId));
                    ItemSlot slot = player.own.inventory[index];
                    slot.DecreaseAmount(1);
                    player.own.inventory[index] = slot;
                    // Note: update Pet Collection Achievement
                    //itemData.Use(player, index);
                    player.NextAction(.5);
                }
            }
            else player.Notify("Pet Card Not Found", "لا يوجد بطاقة مرافق");
        }
        public static void Upgrade(Player player, ushort id) // upgrade tier
        {
            if(!player.CanTakeAction())
                return;
            int index = player.own.pets.FindIndex(p => p.id == id);
            if(index == -1)
            {
                player.Notify("Pet isn't active", "المرافق غير مفعل");
                return;
            }
            PetInfo pet = player.own.pets[index];
            if(pet.tier == pet.data.maxTire)
            {
                player.Notify("Pet reached max Tire", "وصل المرافق لاعلي تطوير");
                return;
            }
            uint availableItems = player.InventoryCountById(Storage.data.pet.upgradeItemId);
            uint neededItems = Storage.data.pet.upgradeItemsCount[(int)pet.tier];
            if(availableItems < neededItems)
            {
                player.NotifyNotEnoughMaterials();
                return;
            }
            player.InventoryRemove(Storage.data.pet.upgradeItemId, neededItems);
            pet.tier++;
            player.own.pets[index] = pet;
            if(player.activePet != null && player.activePet.data.id == id)
            {
                player.activePet.UpdateTier();
            }
            player.NextAction();
        }
        public static void StarUp(Player player, ushort id) // upgrade stars
        {
            if(!player.CanTakeAction())
                return;
            int index = player.own.pets.FindIndex(p => p.id == id);
            if(index == -1)
            {
                player.Notify("Pet isn't active", "المرافق غير مفعل");
                return;
            }
            PetInfo pet = player.own.pets[index];
            if(pet.stars == Storage.data.pet.starsCap)
            {
                player.Notify("Pet reached max stars", "وصل المرافق لاعلي عدد نجوم");
                return;
            }
            uint availableItems = player.InventoryCountById(Storage.data.pet.starsUpItemId);
            uint neededItems = Storage.data.pet.starUpItemsCount[pet.stars];
            if(availableItems < neededItems)
            {
                player.NotifyNotEnoughMaterials();
                return;
            }
            player.InventoryRemove(Storage.data.pet.starsUpItemId, neededItems);
            pet.stars++;
            player.own.pets[index] = pet;
            player.NextAction();
        }
        public static void Train(Player player, ushort id) // upgrade potential
        {
            if(!player.CanTakeAction())
                return;
            // is pet active ?
            int index = player.own.pets.FindIndex(p => p.id == id);
            if(index == -1)
            {
                player.Notify("Pet isn't active", "المرافق غير مفعل");
                return;
            }
            // cache for later use
            PetInfo pet = player.own.pets[index];
            // reached full potential ?
            if(pet.potential == Storage.data.pet.potentialMax)
            {
                player.Notify("Pet reached max potential", "المرافق وصل لاعلي طموح له");
                return;
            }
            // check training item
            int itemIndex = player.GetInventoryIndex(Storage.data.pet.trainItemId);
            if(itemIndex == -1 || player.own.inventory[itemIndex].amount < 1)
            {
                player.Notify("Training Permit not found", "اذن التدريب غير موجود");
                return;
            }
            // take the training item
            ItemSlot slot = player.own.inventory[itemIndex];
            slot.DecreaseAmount(1u);
            player.own.inventory[itemIndex] = slot;
            // if success
            if(Utils.random.Next(-50, 120) > pet.potential)
            {
                byte inc = (byte)Utils.random.Next(1, 3);

                pet.potential += inc;
                if(pet.potential > Storage.data.pet.potentialMax)
                    pet.potential = Storage.data.pet.potentialMax;
                
                player.own.pets[index] = pet;
                player.Notify($"Pet potential increced by {inc} points", $"طموح المرافق زاد بمقدار {inc} نقاط");
            }
            // if failed
            else player.Notify("Training failed", "فشل تجريب المرافق");
        }
    }
}