using MonoMod.Core.Platforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ItemBan
{
    public class ItemBanSystem : ModSystem
    {
        public override void PreSaveAndQuit()
        {
            var mod = (ItemBan)this.Mod;
            var modPlayer = Main.LocalPlayer.GetModPlayer<ItemBanPlayer>();

            foreach (var item in modPlayer.GetAllActiveItems())
            {
                if (item.type == ItemBan.BannedItemType)
                {
                    // Change all of the player's BannedItems back to their original states whenever the player leaves the world
                    mod.ChangeBackToOriginalItem(item);
                }
            }
        }
    }
}
