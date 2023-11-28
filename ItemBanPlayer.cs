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

namespace ItemBan
{
    public class ItemBanPlayer : ModPlayer
    {
        private List<int> lastUpdateInventoryTypes = new List<int>();

        public override void PreUpdate()
        {
            triggerOnInventorySlotChanged();
        }

        public override void OnEnterWorld()
        {
            ((ItemBan)this.Mod).DecideBans();
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
                        mod.TriggerOnInventorySlotChanged(allItems[i]);
                }
            }

            // save the list for next update
            lastUpdateInventoryTypes = inventoryTypes;
        }
    }
}
