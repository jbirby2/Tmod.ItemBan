using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            ((ItemBan)this.Mod).DecideBans();
        }
    }
}
