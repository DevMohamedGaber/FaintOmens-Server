using System;
namespace Game
{
    public class WardrobeSystem
    {
        public static async void SwitchVisibility(Player player)
        {
            if(!player.CanTakeAction())
                return;
            player.own.showClothing = !player.own.showClothing;
            await player.RefreshModel();
            player.NextAction(.5d);
        }
        public static void Equip(Player player, ushort id)
        {
            if(!player.CanTakeAction())
                return;
            if(ScriptableWardrobe.dict.TryGetValue(id, out ScriptableWardrobe itemData))
            {
                if(player.own.wardrobe.IndexOf(id) == -1)
                {
                    player.Notify("Wardrobe item isn't active", "الزي غير مفعل");
                    player.Log($"[WardrobeSystem.OnEquip] not-active wardrobeId: {id}");
                    return;
                }

                WardrobeItem cloth = player.own.clothing[(int)itemData.category];
                cloth.id = id;
                player.own.clothing[(int)itemData.category] = cloth;

                if(player.own.showClothing)
                {
                    PlayerModelData modelData = player.model;
                    modelData.AddTo(itemData.category, id);
                    player.model = modelData;
                }
            }
            else
            {
                player.Notify("Unknown wardrobe item", "العتاد غير معروف");
                player.Log($"[WardrobeSystem.OnEquip] invalid wardrobeId: {id}");
            }
            player.NextAction(.5d);
        }
        public static void Unequip(Player player, int index)
        {
            if(!player.CanTakeAction())
                return;
            if(index < 0 || index > player.own.clothing.Count)
            {
                player.Notify("Please select a cloth", "برجاء اختيار الزي");
                player.Log($"[WardrobeSystem.Unequip] index: {index}");
                return;
            }
            if(!player.own.clothing[index].isUsed)
            {
                player.Notify("Please select a cloth", "برجاء اختيار الزي");
                player.Log($"[WardrobeSystem.Unequip] index: {index} isn't used");
                return;
            }
            WardrobeItem wSlot = player.own.clothing[index];
            if(!player.InventoryAdd(wSlot.GetInventoryItem(), 1))
            {
                player.NotifyNotEnoughInventorySpace();
                return;
            }
            wSlot.UnEquip();
            player.own.clothing[index] = wSlot;
            player.NextAction(.5f);
        }
        public static void Synthesize(Player player, int mainIndex, bool isEquiped, int otherIndex, int blessIndex)
        {
            if(!player.CanTakeAction()) // DDoS guard
                return;
            // verify the blessing stone if selected any
            if(blessIndex != -1)
            {
                if( blessIndex < 0 ||
                    blessIndex > player.own.inventory.Count ||
                    player.own.inventory[blessIndex].amount > 0 ||
                    player.own.inventory[blessIndex].item.id != Storage.data.wardrobe.blessingId)
                {
                    player.Notify("Select the bless item to Synthesize");
                    player.Log($"[WardrobeSystem.Synthesize](isEquiped) blessIndex={blessIndex}");
                    return;
                }
            }
            // check the other item (which is common for both cases)
            if(otherIndex < 0 || otherIndex > player.own.inventory.Count || player.own.inventory[otherIndex].amount < 1)
            {
                player.Notify("Select the other item to Synthesize");
                player.Log($"[WardrobeSystem.Synthesize] otherIndex={otherIndex} amount={player.own.inventory[otherIndex].amount}");
                return;
            }
            // if is equiped
            if(isEquiped)
            {
                if(mainIndex < 0 || mainIndex > player.own.clothing.Count || !player.own.clothing[mainIndex].isUsed)
                {
                    player.Notify("Select the main item to Synthesize");
                    player.Log($"[WardrobeSystem.Synthesize](isEquiped) mainIndex = {mainIndex} isUsed = {player.own.clothing[mainIndex].isUsed}");
                    return;
                }
                if(player.own.clothing[mainIndex].plus == Storage.data.wardrobe.max)
                {
                    player.Notify("This clothing reached max enhancment", "الزي وصل لاعلي تحسين ممكن");
                    return;
                }
                if(player.own.inventory[otherIndex].item.data is ClothingItem otherData)
                {
                    //check
                    if(player.own.clothing[mainIndex].data.category != otherData.equipCategory)
                    {
                        player.Notify("Both clothing has to be of the same type", "يجب ان يكون كلا الزيين من نفس النوع");
                        player.Log($"[WardrobeSystem.Synthesize](isEquiped) mainIndex={mainIndex} otherIndex={otherIndex} both of diffrent categories");
                        return;
                    }
                    if(player.own.clothing[mainIndex].plus != player.own.inventory[otherIndex].item.plus)
                    {
                        player.Notify("Both clothing has to have the same enhancment level", "يجب ان يكون كلا الزيين من نفس التطوير");
                        player.Log($"[WardrobeSystem.Synthesize](isEquiped) mainIndex={mainIndex} otherIndex={otherIndex} both of diffrent plus<{player.own.clothing[mainIndex].plus}><{player.own.inventory[otherIndex].item.plus}>");
                        return;
                    }
                    if(player.own.gold < Storage.data.wardrobe.cost[player.own.clothing[mainIndex].plus])
                    {
                        player.NotifyNotEnoughGold();
                        return;
                    }
                    
                    // take the requirements
                    player.UseGold(Storage.data.wardrobe.cost[player.own.clothing[mainIndex].plus]);
                    
                    if(blessIndex != -1)
                    {
                        ItemSlot blessingSlot = player.own.inventory[blessIndex];
                        blessingSlot.DecreaseAmount(1);
                        player.own.inventory[blessIndex] = blessingSlot;
                    }
                    
                    ItemSlot otherSlot = player.own.inventory[otherIndex];
                    otherSlot.DecreaseAmount(1);
                    player.own.inventory[otherIndex] = otherSlot;

                    // Modify according to the randomRate
                    int successRate = 100 - (player.own.clothing[mainIndex].plus * 7) + (blessIndex != -1 ? 50 : 0);
                    if(Utils.random.Next(1, 100) <= successRate)
                    {
                        WardrobeItem mainSlot = player.own.clothing[mainIndex];
                        mainSlot.plus++;
                        player.own.clothing[mainIndex] = mainSlot;
                        player.NotifySuccess(NotifySuccessType.Wardrobe);
                    }
                    else player.NotifyFailure(NotifySuccessType.Wardrobe);
                }
                else
                {
                    player.Notify("Select the other item to Synthesize");
                    player.Log($"[WardrobeSystem.Synthesize] otherIndex = {otherIndex} id = {player.own.inventory[otherIndex].item.id}");
                    return;
                }
            }
            // if isn't equiped
            else
            {
                if(mainIndex < 0 || mainIndex > player.own.inventory.Count || player.own.inventory[mainIndex].amount < 1)
                {
                    player.Notify("Select the main item to Synthesize");
                    player.Log($"[WardrobeSystem.Synthesize] mainIndex = {mainIndex} amount = {player.own.inventory[mainIndex].amount}");
                    return;
                }
                // cashe
                ItemSlot mainSlot = player.own.inventory[mainIndex];
                ItemSlot otherSlot = player.own.inventory[otherIndex];
                // reached max level ?
                if(mainSlot.item.plus == Storage.data.wardrobe.max)
                {
                    player.Notify("This clothing reached max enhancment", "الزي وصل لاعلي تحسين ممكن");
                    return;
                }
                // both items are ClothingItem
                if(mainSlot.item.data is ClothingItem mainData && otherSlot.item.data is ClothingItem otherData)
                {
                    //check
                    if(mainData.equipCategory != otherData.equipCategory)
                    {
                        player.Notify("Both clothing has to be of the same type", "يجب ان يكون كلا الزيين من نفس النوع");
                        player.Log($"[WardrobeSystem.Synthesize] mainIndex={mainIndex} otherIndex={otherIndex} both of diffrent categories");
                        return;
                    }
                    if(mainSlot.item.plus != otherSlot.item.plus)
                    {
                        player.Notify("Both clothing has to have the same enhancment level", "يجب ان يكون كلا الزيين من نفس التطوير");
                        player.Log($"[WardrobeSystem.Synthesize] mainIndex={mainIndex} otherIndex={otherIndex} both of diffrent plus<{mainSlot.item.plus}><{otherSlot.item.plus}>");
                        return;
                    }
                    if(player.own.gold < Storage.data.wardrobe.cost[mainSlot.item.plus])
                    {
                        player.NotifyNotEnoughGold();
                        return;
                    }
                    
                    // take the requirements
                    // gold
                    player.UseGold(Storage.data.wardrobe.cost[mainSlot.item.plus]);
                    // blessing stones(if selected any)
                    if(blessIndex != -1)
                    { 
                        ItemSlot blessingSlot = player.own.inventory[blessIndex];
                        blessingSlot.DecreaseAmount(1);
                        player.own.inventory[blessIndex] = blessingSlot;
                    }
                    // other item
                    otherSlot.DecreaseAmount(1);
                    player.own.inventory[otherIndex] = otherSlot;

                    // Modify according to the randomRate
                    int successRate = 100 - (mainSlot.item.plus * 7) + (blessIndex != -1 ? 50 : 0);
                    if(Utils.random.Next(1, 100) <= successRate)
                    {
                        mainSlot.item.plus++;
                        player.own.inventory[mainIndex] = mainSlot;
                        player.NotifySuccess(NotifySuccessType.Wardrobe); // update ui
                        // TODO: check for achievements and hot events
                    }
                    else player.NotifyFailure(NotifySuccessType.Wardrobe); // update ui
                }
                else
                {
                    player.Notify("Select the proper item to Synthesize", "اختر قطعة مناسبة للتحسين");
                    player.Log($"[WardrobeSystem.Synthesize] otherIndex = {otherIndex} id = {player.own.inventory[otherIndex].item.id}");
                    return;
                }
            }
            player.NextAction(); // DDoS guard
        }
    }
}