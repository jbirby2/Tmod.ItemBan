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
            ScheduleDecideBans();
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

            // If the player currently has a chest open, then return those items too
            if (this.Player.chest > -1)
            {
                foreach (var item in Main.chest[this.Player.chest].item)
                {
                    allItems.Add(item);
                }
            }

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

                        if (item.active)
                        {
                            // Any time a new BannedItem enters Player inventory, re-decide whether or not it still needs to be banned.
                            if (item.type == ItemBan.BannedItemType)
                                ScheduleDecideBans();

                            // joestub: other ItemBan logic goes here
                        }

                        foreach (var callback in ItemBan.OnInventorySlotChangedCallbacks)
                        {
                            callback(item);
                        }
                    }
                }
            }
            else
            {
                // If the total number of inventory slots changed, then redecide the bans just to be safe.
                // This usually happens when the player opens or closes a chest.
                ScheduleDecideBans();
            }

            // save the list for next update
            lastUpdateInventoryTypes = inventoryTypes;
        }

        private void decideBans()
        {
            if (Main.netMode == NetmodeID.Server || !this.Player.active)
                return;

            var mod = (ItemBan)this.Mod;
            var clientConfig = ModContent.GetInstance<ClientConfig>();

            mod.Logger.Debug("Entering ItemBanPlayer.decideBans()");

            bool needsSync = false;
            foreach (var item in GetAllActiveItems())
            {
                int itemStartType = item.type;

                mod.DecideBan(item, clientConfig);

                if (item.type != itemStartType)
                    needsSync = true;
            }

            foreach (var bansCompleteCallback in ItemBan.OnClientBansCompleteCallbacks)
            {
                bansCompleteCallback();
            }

            if (needsSync && Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, this.Player.whoAmI);
        }
    }
}
