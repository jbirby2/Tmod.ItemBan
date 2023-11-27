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
        public override void OnEnterWorld()
        {
            ((ItemBan)this.Mod).DecideBans();
        }

        public override bool OnPickup(Item item)
        {
            this.Mod.Logger.Debug("joestub ItemBanPlayer.OnPickup");

            if (item.type == ItemBan.BannedItemType)
                ((ItemBan)this.Mod).DecideBans();
    
            return true;
        }

        public IEnumerable<Item> GetAllActiveItems()
        {
            var allItems = new List<Item>();
            allItems.AddRange(this.Player.inventory.Where(item => item.active && item.type != ItemID.None));
            allItems.AddRange(this.Player.bank.item.Where(item => item.active && item.type != ItemID.None));
            allItems.AddRange(this.Player.bank2.item.Where(item => item.active && item.type != ItemID.None));
            allItems.AddRange(this.Player.bank3.item.Where(item => item.active && item.type != ItemID.None));
            allItems.AddRange(this.Player.bank4.item.Where(item => item.active && item.type != ItemID.None));
            allItems.AddRange(this.Player.armor.Where(item => item.active && item.type != ItemID.None));
            allItems.AddRange(this.Player.dye.Where(item => item.active && item.type != ItemID.None));
            allItems.AddRange(this.Player.miscEquips.Where(item => item.active && item.type != ItemID.None));
            allItems.AddRange(this.Player.miscDyes.Where(item => item.active && item.type != ItemID.None));
            if (this.Player.trashItem.active && this.Player.trashItem.type != ItemID.None)
                allItems.Add(this.Player.trashItem);

            return allItems;
        }
    }
}
