using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ItemBan
{
    public class ClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]
        public bool AllowBannedItemsInSinglePlayer;


        public override void OnChanged()
        {
            if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.active)
                Main.LocalPlayer.GetModPlayer<ItemBanPlayer>().ScheduleDecideBans();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var worldSystem = ModContent.GetInstance<ItemBanSystem>();
                if (worldSystem != null)
                    worldSystem.ScheduleDecideBansOnServer();
            }
        }
    }
}
