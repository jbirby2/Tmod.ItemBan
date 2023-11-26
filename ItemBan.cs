using MonoMod.Core.Platforms;
using System;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;

namespace ItemBan
{
	public class ItemBan : Mod
	{
        public static int BannedItemType { get; private set; }
        public static List<Func<Item, bool>> BanDeciders = new List<Func<Item, bool>>();

        public override void PostSetupContent()
        {
            BannedItemType = ModContent.ItemType<BannedItem>();
        }

        public override object Call(params object[] args)
        {
            if (args == null || args.Length == 0)
                throw new ArgumentException(nameof(args) + " cannot be null or empty", nameof(args));
            else if (!(args[0] is string))
                throw new ArgumentException("First parameter must be a string containing a command", nameof(args));

            switch (((string)args[0]).Trim().ToUpper())
            {
                case "REGISTERBANDECIDER":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Func<Item, bool>))
                        throw new ArgumentException("Second argument must be a Func<Item, bool>", nameof(args));

                    var newDecider = (Func<Item, bool>)args[1];

                    if (!BanDeciders.Contains(newDecider))
                        BanDeciders.Add(newDecider);

                    return null;

                case "UNREGISTERBANDECIDER":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Func<Item, bool>))
                        throw new ArgumentException("Second argument must be a Func<Item, bool>", nameof(args));

                    var decider = (Func<Item, bool>)args[1];

                    if (BanDeciders.Contains(decider))
                        BanDeciders.Remove(decider);

                    return null;

                default:
                    throw new InvalidOperationException("Unrecognized command \"" + args[0] + "\"");
            }
        }

        public void ApplyRulesToPlayerInventory()
        {
            if (Main.netMode == NetmodeID.Server || !Main.LocalPlayer.active)
                return;

            bool allowBannedItemsInSinglePlayer = ModContent.GetInstance<ClientConfig>().AllowBannedItemsInSinglePlayer;
            var modPlayer = Main.LocalPlayer.GetModPlayer<ItemBanPlayer>();

            bool needResync = false;
            foreach (var item in modPlayer.GetAllActiveItems())
            {
                // If any of the BanDeciders decide that the item is banned, then it's banned.
                bool isItemBanned = false;
                foreach (var decide in BanDeciders)
                {
                    isItemBanned = decide(item);

                    if (isItemBanned)
                        break;
                }

                bool allowBannedItem = (Main.netMode == NetmodeID.SinglePlayer && allowBannedItemsInSinglePlayer);

                if (item.type == ItemBan.BannedItemType)
                {
                    if (!isItemBanned || allowBannedItem)
                    {
                        ChangeBackToOriginalItem(item);
                        needResync = true;
                    }
                }
                else
                {
                    if (isItemBanned && !allowBannedItem)
                    {
                        ChangeToBannedItem(item);
                        needResync = true;
                    }
                }
            }

            if (needResync && Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, Main.myPlayer);
        }

        public void ChangeToBannedItem(Item item)
        {
            if (!item.active || item.type == ItemID.None || item.type == BannedItemType)
                throw new Exception("Cannot change this item into an BannedItem: " + item.ToString());

            Logger.Debug("Changing item " + item.Name + " (" + item.type.ToString() + ")");

            var originalType = item.type;
            var originalStack = item.stack;
            var originalPrefix = item.prefix;
            var originalData = item.SerializeData();

            item.ChangeItemType(ModContent.ItemType<BannedItem>());

            var bannedItem = (BannedItem)item.ModItem;
            bannedItem.OriginalType = originalType;
            bannedItem.OriginalStack = originalStack;
            bannedItem.OriginalPrefix = originalPrefix;
            bannedItem.OriginalData = originalData;
        }

        public void ChangeBackToOriginalItem(Item item)
        {
            if (!item.active || item.type != BannedItemType)
                throw new Exception("Cannot change this item from an BannedItem back to its original type: " + item.ToString());

            Logger.Debug("Changing back item " + item.Name + " (" + item.type.ToString() + ")");

            var bannedItem = (BannedItem)item.ModItem;

            var originalType = bannedItem.OriginalType;
            var originalStack = bannedItem.OriginalStack;
            var originalPrefix = bannedItem.OriginalPrefix;
            var originalData = bannedItem.OriginalData;

            item.ChangeItemType(originalType);
            item.stack = originalStack;
            item.Prefix(originalPrefix);
            ItemIO.Load(item, originalData);
        }
    }
}