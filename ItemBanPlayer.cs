using MonoMod.Core.Platforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.Localization;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using log4net.Repository.Hierarchy;

namespace ItemBan
{
    public class ItemBanPlayer : ModPlayer
    {
        private List<int> lastUpdateInventoryTypes = new List<int>();
        private bool needToDecideBans = false;

        public override void PreUpdate()
        {
            triggerOnInventorySlotChanged();

            if (needToDecideBans)
            {
                decideBans();
                needToDecideBans = false;
            }
        }

        public override void OnEnterWorld()
        {
            needToDecideBans = true;
        }

        public List<Item> GetAllItems()
        {
            var allItems = new List<Item>();
            allItems.AddRange(this.Player.inventory);
            allItems.AddRange(this.Player.bank.item);
            allItems.AddRange(this.Player.bank2.item);
            allItems.AddRange(this.Player.bank3.item);
            allItems.AddRange(this.Player.bank4.item);
            allItems.AddRange(this.Player.armor);
            allItems.AddRange(this.Player.dye);
            allItems.AddRange(this.Player.miscEquips);
            allItems.AddRange(this.Player.miscDyes);
            allItems.Add(this.Player.trashItem);
            return allItems;
        }

        public List<Item> GetAllActiveItems()
        {
            return GetAllItems().Where(item => item.active && item.type != ItemID.None).ToList();
        }

        public void ScheduleDecideBans()
        {
            needToDecideBans = true;
        }

        // private

        private void triggerOnInventorySlotChanged()
        {
            if (Main.netMode == NetmodeID.Server || !this.Player.active)
                return;

            var mod = (ItemBan)this.Mod;
            var allItems = GetAllItems();

            // Build the list of current inventory types
            var inventoryTypes = new List<int>();
            foreach (var item in allItems)
            {
                inventoryTypes.Add(item.type);
            }

            // Compare to the list of inventory types from the previous Update and call TriggerOnInventorySlotChanged for any slot that has changed
            if (inventoryTypes.Count == lastUpdateInventoryTypes.Count)
            {
                for (int i = 0; i < inventoryTypes.Count; i++)
                {
                    if (inventoryTypes[i] != lastUpdateInventoryTypes[i])
                    {
                        var item = allItems[i];

                        mod.Logger.Debug("joestub TriggerOnInventorySlotChanged " + item.ToString());

                        if (item.active)
                        {
                            // Any time a new BannedItem enters Player inventory, re-decide whether or not it still needs to be banned.
                            if (item.type == ItemBan.BannedItemType)
                                needToDecideBans = true;

                            // joestub: other ItemBan logic goes here
                        }

                        foreach (var callback in ItemBan.OnInventorySlotChangedCallbacks)
                        {
                            callback(item);
                        }
                    }
                }
            }

            // save the list for next update
            lastUpdateInventoryTypes = inventoryTypes;
        }

        private void decideBans()
        {
            if (Main.netMode == NetmodeID.Server || !this.Player.active)
                return;

            var mod = (ItemBan)this.Mod;
            bool allowBannedItemsInSinglePlayer = ModContent.GetInstance<ClientConfig>().AllowBannedItemsInSinglePlayer;

            mod.Logger.Debug("joestub entering loop");

            foreach (var item in GetAllActiveItems())
            {
                mod.Logger.Debug("joestub looping for " + item.ToString());

                // For any items currently in the player's inventory that have already been changed to BannedItems, change them back now.
                // The code below is about to re-decide whether this item should still be banned.
                if (item.type == ItemBan.BannedItemType)
                    mod.ChangeBackToOriginalItem(item);

                // If any of the callbacks decide that the item is banned, then it's banned.
                bool isItemBanned = false;
                foreach (var decideCallback in ItemBan.OnDecideBanCallbacks)
                {
                    isItemBanned = decideCallback(item);

                    if (isItemBanned)
                        break;
                }

                bool allowBannedItem = (Main.netMode == NetmodeID.SinglePlayer && allowBannedItemsInSinglePlayer);

                if (isItemBanned && !allowBannedItem)
                {
                    mod.Logger.Debug("Banning item " + item.ToString());

                    var originalItemClone = item.Clone();
                    var originalType = item.type;
                    var originalStack = item.stack;
                    var originalPrefix = item.prefix;
                    var originalData = item.SerializeData();

                    item.ChangeItemType(ModContent.ItemType<BannedItem>());

                    var bannedItem = (BannedItem)item.ModItem;
                    bannedItem.OriginalType = originalType;
                    bannedItem.OriginalStack = originalStack;
                    bannedItem.OriginalPrefix = originalPrefix;
                    bannedItem.OriginalData = originalData;

                    foreach (var bannedCallback in ItemBan.OnItemBannedCallbacks)
                    {
                        bannedCallback(item, originalItemClone);
                    }
                }
            }

            foreach (var bansCompleteCallback in ItemBan.OnBansCompleteCallbacks)
            {
                bansCompleteCallback();
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, this.Player.whoAmI);
        }
    }
}
