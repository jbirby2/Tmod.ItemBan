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
        private bool updateAllBansNextTick = false;
        
        public override void PreUpdate()
        {
            if (Main.netMode == NetmodeID.Server || !this.Player.active)
                return;

            var mod = (ItemBan)this.Mod;
            var serverConfig = ModContent.GetInstance<ServerConfig>();
            var clientConfig = ModContent.GetInstance<ClientConfig>();

            var allItems = GetAllItems();

            // build inventoryTypes
            var inventoryTypes = new List<int>();
            foreach (var item in allItems)
            {
                inventoryTypes.Add(item.type);
            }

            bool needsSync = false;
            for (int i = 0; i < inventoryTypes.Count; i++)
            {
                bool updateBan = false;

                if (inventoryTypes.Count != lastUpdateInventoryTypes.Count || inventoryTypes[i] != lastUpdateInventoryTypes[i])
                {
                    updateBan = true;

                    foreach (var callback in ItemBan.OnInventorySlotChangedCallbacks)
                    {
                        callback(allItems[i]);
                    }
                }
                else if (updateAllBansNextTick)
                {
                    updateBan = true;
                }

                if (updateBan)
                {
                    var item = allItems[i];

                    if (item.active && item.type != ItemID.None)
                    {
                        int itemStartType = item.type;

                        mod.UpdateBanStatus(item, clientConfig, serverConfig);

                        if (item.type != itemStartType)
                        {
                            needsSync = true;
                            inventoryTypes[i] = item.type; // since the item type has changed since inventoryTypes was built, update it
                        }
                    }
                }
            }

            if (needsSync && Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, this.Player.whoAmI);

            lastUpdateInventoryTypes = inventoryTypes;
            updateAllBansNextTick = false;
        }

        public override void OnEnterWorld()
        {
            lastUpdateInventoryTypes.Clear();
            updateAllBansNextTick = true;
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

        public void UpdateAllBans()
        {
            updateAllBansNextTick = true;
        }

    }
}
