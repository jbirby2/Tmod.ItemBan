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
using Terraria.Localization;

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

        public void UpdateAllWorldBans()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var mod = (ItemBan)this.Mod;
            var clientConfig = ModContent.GetInstance<ClientConfig>();
            var serverConfig = ModContent.GetInstance<ServerConfig>();

            mod.Logger.Debug("Entering ItemBanSystem.decideBansOnServer()");

            foreach (var item in Main.item)
            {
                if (item.active && item.type != ItemID.None)
                {
                    int itemStartType = item.type;

                    mod.UpdateBanStatus(item, clientConfig, serverConfig);

                    if (item.type != itemStartType && Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI);
                }
            }
        }

    }
}
