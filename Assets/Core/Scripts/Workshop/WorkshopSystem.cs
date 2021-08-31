using UnityEngine;
namespace Game
{
    public class WorkshopSystem
    {
        static Game.StorageData.Item storage => Storage.data.item;
        // Plus
        public static void Plus(Player player, int index, WorkshopOperationFrom from, int luckItem)
        {
            if(!player.CanTakeAction())
                return;
            // check luckItem item
            int luckItemIndex = -1;
            if(luckItem > -1)
            {
                luckItemIndex = player.GetInventoryIndex((ushort)storage.plusLuckCharms[luckItem].name);
                if(luckItemIndex == -1)
                {
                    player.Notify("Selected Luck Charm not found", "");
                    return;
                }
            }
            // select item
            ItemSlot? slot = GetSlot(player, from, index);
            if(slot == null)
            {
                player.Notify("Please select an item to enhance", "برجاء اختيار عتاد لتعليته");
                return;
            }
            ItemSlot itemSlot = slot.Value;
            // item reached max ?
            if(itemSlot.item.plus == storage.maxPlus)
            {
                player.Notify("Item reached max plus", "");
                return;
            }
            // has required gold
            uint reqGold = storage.plusUpCost[itemSlot.item.plus];
            if(player.own.gold < reqGold)
            {
                player.NotifyNotEnoughGold();
                return;
            }
            // has required plus stone
            ushort stoneId = storage.plusUpItemId[(int)System.Math.Floor((int)itemSlot.item.plus / 12d)];
            uint playerStones = player.InventoryCountById(stoneId);
            uint reqStones = storage.plusUpCount[itemSlot.item.plus];
            if(playerStones < reqStones)
            {
                player.Notify("You don't have any of the Materials required.", "");
                return;
            }
            
            // take requirements
            player.InventoryRemove(stoneId, reqStones);
            player.UseGold(reqGold);
            if(luckItemIndex > -1)
            {
                ItemSlot luckCharmSlot = player.own.inventory[luckItemIndex];
                luckCharmSlot.DecreaseAmount(1);
                player.own.inventory[luckItemIndex] = luckCharmSlot;
            }
            // success or failure
            double successRate = storage.plusUpSuccessRate[itemSlot.item.plus];
            if(luckItem > -1)
            {
                successRate += storage.plusLuckCharms[luckItem].amount;
            }
            // success
            if(Utils.random.NextDouble() <= successRate)
            {
                itemSlot.item.plus++;
                player.TargetItemEnhanceSuccess();
                // check achievements, events, quests and royal pass
            }
            // failuer
            else
            {
                player.TargetItemEnhanceFailure();
                if(itemSlot.item.plus >= storage.plusDropsAt)
                    itemSlot.item.plus--;
                // check achievements
            }
            // wrap up
            SaveSlot(player, from, index, itemSlot);
            player.UpdateEquipmentInfo();
            player.NextAction(.5d);
        }
        // Sockets
        public static void UnlockSocket(Player player, int index, WorkshopOperationFrom from, int socketIndex)
        {
            if(!player.CanTakeAction())
                return;
            // select item
            ItemSlot? slot = GetSlot(player, from, index);
            if(slot == null)
            {
                player.Notify("Please select an item to enhance", "برجاء اختيار عتاد لتعليته");
                return;
            }
            ItemSlot itemSlot = slot.Value;
            if(socketIndex < 0 || socketIndex > 3)
            {
                player.Notify("Select a socket");
                return;
            }
            if(itemSlot.item.GetSocket(socketIndex).isOpen)
            {
                player.Notify("The socket is already open");
                return;
            }
            int unlockItemIndex = player.GetInventoryIndex(storage.unlockSocketItemId);
            if(unlockItemIndex == -1)
            {
                player.Notify("You don't have a key");
                return;
            }
            ItemSlot unlockItemSlot = player.own.inventory[unlockItemIndex];
            unlockItemSlot.DecreaseAmount(1);
            player.own.inventory[unlockItemIndex] = unlockItemSlot;
            itemSlot.item.SetSocket(socketIndex, 0);
            // TODO: Add achievment count sockets opened
            SaveSlot(player, from, index, itemSlot);
            player.NextAction(.5d);
        }
        public static void AddGem(Player player, int index, WorkshopOperationFrom from, int socketIndex, int gemIndex)
        {
            if(!player.CanTakeAction())
                return;
            // select item
            ItemSlot? slot = GetSlot(player, from, index);
            if(slot == null)
            {
                player.Notify("Please select an item to enhance", "برجاء اختيار عتاد لتعليته");
                return;
            }
            ItemSlot itemSlot = slot.Value;
            if(socketIndex < 0 || socketIndex > 3)
            {
                player.Notify("Select a socket");
                return;
            }
            Socket socket = itemSlot.item.GetSocket(socketIndex);
            if(!socket.isOpen)
            {
                player.Notify("This socket isn't open");
                return;
            }
            if(socket.id > 0)
            {
                player.Notify("The socket isn't Empty");
                return;
            }
            if(gemIndex < 0 || gemIndex > player.own.inventorySize || player.own.inventory[gemIndex].amount < 1)
            {
                player.TargetNotify("Select a Gem");
                return;
            }
            if(player.own.inventory[gemIndex].item.data is GemItem gem)
            {
                if(itemSlot.item.HasGemWithType(gem.bonusType))
                {
                    player.Notify("You can't put 2 gems with the same bonus type.");
                    return;
                }
                ItemSlot gemItem = player.own.inventory[gemIndex];
                gemItem.DecreaseAmount(1);
                player.own.inventory[gemIndex] = gemItem;

                itemSlot.item.SetSocket(socketIndex, (short)gem.name);

                SaveSlot(player, from, index, itemSlot);
                player.UpdateEquipmentInfo();
                player.NextAction(.5d);
            }
            else player.Notify("Select a Gem to inlay");
        }
        public static void RemoveGem(Player player, int index, WorkshopOperationFrom from, int socketIndex)
        {
            if(!player.CanTakeAction())
                return;
            // have enough gold
            if(player.own.gold < storage.gemRemovalFee)
            {
                player.NotifyNotEnoughGold();
                return;
            }
            // select item
            ItemSlot? slot = GetSlot(player, from, index);
            if(slot == null)
            {
                player.Notify("Please select an item to enhance", "برجاء اختيار عتاد لتعليته");
                return;
            }
            ItemSlot itemSlot = slot.Value;
            if(socketIndex < 0 || socketIndex > 3)
            {
                player.Notify("Select a socket");
                return;
            }
            Socket socket = itemSlot.item.GetSocket(socketIndex);
            if(!socket.isOpen)
            {
                player.Notify("This socket isn't open");
                return;
            }
            if(socket.id == 0)
            {
                player.Notify("The socket is already Empty");
                return;
            }
            if (ScriptableItem.dict.TryGetValue(socket.id, out ScriptableItem gem))
            {
                if(player.InventoryAdd(new Item(gem), 1))
                {
                    player.UseGold(storage.gemRemovalFee);
                    itemSlot.item.SetSocket(socketIndex, 0);

                    SaveSlot(player, from, index, itemSlot);
                    player.UpdateEquipmentInfo();
                    player.NextAction(.5d);
                }
            }
            else
            {
                player.Notify("Insuffitionte gem");
                player.Log("[WorkshopSystem.RemoveGem] invalid gem id=" + socket.id);
            }
        }
        // Quality
        public static void QualityGrowth(Player player, int index, WorkshopOperationFrom from, int feedItem)
        {
            if(!player.CanTakeAction())
                return;
            // select item
            ItemSlot? slot = GetSlot(player, from, index);
            if(slot == null)
            {
                player.Notify("Please select an item to enhance", "برجاء اختيار عتاد لتعليته");
                return;
            }
            ItemSlot itemSlot = slot.Value;
            if(!itemSlot.item.quality.isGrowth)
            {
                player.Notify("Please select a Growth item to Upgrade", "برجاء اختيار عتاد قابل للتطوير لتعليته");
                return;
            }
            if(feedItem < 0 || feedItem >= storage.qualityFeedItems.Length)
            {
                player.Notify("Please select a feed item");
                return;
            }
            int feedItemIndex = player.GetInventoryIndex((ushort)storage.qualityFeedItems[feedItem].name);
            if(feedItemIndex == -1)
            {
                player.Notify("Please select a feed item");
                return;
            }
            ItemSlot feedItemSlot = player.own.inventory[feedItemIndex];
            feedItemSlot.DecreaseAmount(1);
            player.own.inventory[feedItemIndex] = feedItemSlot;

            itemSlot.item.quality.AddExp(storage.qualityFeedItems[feedItem].amount);

            SaveSlot(player, from, index, itemSlot);
            player.NextAction(.5d);
        }
        // Craft
        public static void Craft(Player player, int recipeId, uint amount) {
            if(!player.CanTakeAction())
                return;
            if(amount < 1)
            {
                player.Notify("please select the needed amount");
                return;
            }
            if(ScriptableRecipe.dict.TryGetValue(recipeId, out ScriptableRecipe recipe))
            {
                if(!recipe.CanCraft(player, amount))
                {
                    player.NotifyNotEnoughMaterials();
                    return;
                }
                if(player.own.gold >= (uint)(recipe.cost * amount))
                {
                    player.NotifyNotEnoughGold();
                    return;
                }
                if(player.InventoryAdd(recipe.result, amount))
                {
                    player.UseGold(recipe.cost * (uint)amount);
                    for(int i = 0; i < recipe.ingredients.Length; i++)
                    {
                        player.InventoryRemove(recipe.ingredients[i].item.name, recipe.ingredients[i].amount * amount);
                    }
                    player.QuestsOnCraft(recipe.result.data, amount);
                    player.NextAction(.5d);
                }
            }
            else
            {
                player.Notify("please select the needed recipe");
                player.Log("[WorkshopSystem.Craft] invalid recipe id=" + recipeId);
            }
        }
        // helpers
        static ItemSlot? GetSlot(Player player, WorkshopOperationFrom from, int index)
        {
            if(index < 0)
                return null;
            // from equipments
            if(from == WorkshopOperationFrom.Equipments)
            {
                if(index >= player.equipment.Count || player.equipment[index].isEmpty)
                    return null;
                return player.equipment[index];
            }
            // from accessories
            if(from == WorkshopOperationFrom.Accessories)
            {
                if(index >= player.own.accessories.Count || player.own.accessories[index].isEmpty)
                    return null;
                return player.own.accessories[index];
            }
            // from inventory
            if(index >= player.own.inventory.Count || !player.own.inventory[index].isEquipment)
                return null;
            return player.own.inventory[index];
        }
        static void SaveSlot(Player player, WorkshopOperationFrom from, int index, ItemSlot slot)
        {
            if(from == WorkshopOperationFrom.Equipments)
                player.equipment[index] = slot;
            else if(from == WorkshopOperationFrom.Accessories)
                player.own.accessories[index] = slot;
            else player.own.inventory[index] = slot;
        }
    }
}