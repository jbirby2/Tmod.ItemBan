using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ItemBan
{
    public class ItemBanGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity
        {
            get { return false; }
        }

        public override void OnSpawn(Item item, IEntitySource source)
        {
            this.Mod.Logger.Debug("joestub ItemBanGlobalItem.OnSpawn " + item.ToString());

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var serverConfig = ModContent.GetInstance<ServerConfig>();

                if (item.type == ItemBan.BannedItemType
                                || serverConfig.TypeOfList == "Blacklist" && serverConfig.ItemList.Any(bannedItemDefinition => bannedItemDefinition.Type == item.type)
                                || serverConfig.TypeOfList == "Whitelist" && !serverConfig.ItemList.Any(bannedItemDefinition => bannedItemDefinition.Type == item.type))
                {
                    var worldSystem = ModContent.GetInstance<ItemBanSystem>();
                    if (worldSystem != null)
                        worldSystem.ScheduleDecideBansOnServer();
                }
            }
        }
    }
}
