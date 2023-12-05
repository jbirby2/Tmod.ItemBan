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
        public bool updateBanOnNextTick = false;

        public override bool InstancePerEntity
        {
            get { return true; }
        }

        public override void PostUpdate(Item item)
        {
            if (updateBanOnNextTick)
            {
                var serverConfig = ModContent.GetInstance<ServerConfig>();
                var clientConfig = ModContent.GetInstance<ClientConfig>();

                int itemStartType = item.type;

                ((ItemBan)this.Mod).UpdateBanStatus(item, clientConfig, serverConfig);

                if (item.type != itemStartType && Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI);

                updateBanOnNextTick = false;
            }
        }

        public override void OnSpawn(Item item, IEntitySource source)
        {
            this.Mod.Logger.Debug("joestub ItemBanGlobalItem.OnSpawn " + item.ToString());

            if (Main.netMode != NetmodeID.MultiplayerClient)
                updateBanOnNextTick = true;
        }
    }
}
