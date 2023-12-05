using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Microsoft.Xna.Framework;

namespace ItemBan
{
    public class BannedItem : ModItem
    {
        public string BannedByModName = "";
        public int OriginalType = 0;
        public int OriginalStack = 0;
        public int OriginalPrefix = 0;
        public TagCompound OriginalData = new TagCompound();

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 0;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 1;
            Item.value = 0;
        }

        public override bool CanReforge()
        {
            return false;
        }

        public override bool CanResearch()
        {
            return false;
        }

        public override bool CanStack(Item source)
        {
            return false;
        }

        public override bool CanStackInWorld(Item source)
        {
            return false;
        }

        public override void SaveData(TagCompound tag)
        {
            if (!String.IsNullOrWhiteSpace(BannedByModName))
                tag["BannedByModName"] = BannedByModName;

            tag["OriginalType"] = OriginalType;
            tag["OriginalStack"] = OriginalStack;
            tag["OriginalPrefix"] = OriginalPrefix;
            tag["OriginalData"] = OriginalData;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("BannedByModName"))
                BannedByModName = tag.GetString("BannedByModName");

            OriginalType = tag.GetInt("OriginalType");
            OriginalStack = tag.GetInt("OriginalStack");
            OriginalPrefix = tag.GetInt("OriginalPrefix");
            OriginalData = tag.GetCompound("OriginalData");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(BannedByModName);
            writer.Write(OriginalType);
            writer.Write(OriginalStack);
            writer.Write(OriginalPrefix);
            TagIO.Write(OriginalData, writer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BannedByModName = reader.ReadString();
            OriginalType = reader.ReadInt32();
            OriginalStack = reader.ReadInt32();
            OriginalPrefix = reader.ReadInt32();
            OriginalData = TagIO.Read(reader);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(this.Mod, "OriginalItem", Lang.GetItemNameValue(OriginalType) + (OriginalStack < 2 ? "" : " [" + OriginalStack.ToString() + "]")));

            if (!String.IsNullOrWhiteSpace(BannedByModName))
                tooltips.Add(new TooltipLine(this.Mod, "BannedByModName", Language.GetTextValue("Mods.ItemBan.Custom.BannedBy", BannedByModName)));
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            try
            {
                Main.instance.LoadItem(OriginalType);
                spriteBatch.Draw(TextureAssets.Item[OriginalType].Value, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            catch (Exception ex)
            {
                this.Mod.Logger.Error("Exception in BannedItem.PostDrawInInventory", ex);
            }
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            try
            {
                // Most of the code below is vanilla code taken from Main.Draw()

                Main.instance.LoadItem(OriginalType);

                var texture = TextureAssets.Item[OriginalType].Value;
                var frame = texture.Frame();

                Vector2 vector = frame.Size() / 2f;
                Vector2 vector2 = new Vector2((float)(Item.width / 2) - vector.X, Item.height - frame.Height);
                Vector2 vector3 = Item.position - Main.screenPosition + vector + vector2;

                Color color = Lighting.GetColor(Item.Center.ToTileCoordinates());
                Color currentColor = Item.GetAlpha(color);

                float num = Item.velocity.X * 0.2f;

                spriteBatch.Draw(texture, vector3, frame, currentColor, num, vector, scale, SpriteEffects.None, 0f);
            }
            catch (Exception ex)
            {
                this.Mod.Logger.Error("Exception in BannedItem.PostDrawInWorld", ex);
            }
        }

    }
}
