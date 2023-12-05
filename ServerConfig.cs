using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Terraria.ModLoader.Config;
using System.ComponentModel;

namespace ItemBan
{
    public class ServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DrawTicks]
        [OptionStrings(new string[] { "Blacklist", "Whitelist" })]
        [DefaultValue("Blacklist")]
        public string TypeOfList;

        [JsonDefaultListValue("{}")]
        public List<ItemDefinition> ItemList = new List<ItemDefinition>();


        public override void OnChanged()
        {
            if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.active)
                Main.LocalPlayer.GetModPlayer<ItemBanPlayer>().UpdateAllBans();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var worldSystem = ModContent.GetInstance<ItemBanSystem>();
                if (worldSystem != null)
                    worldSystem.UpdateAllWorldBans();
            }
        }
    }
}
