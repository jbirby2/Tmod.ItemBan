using MonoMod.Core.Platforms;
using System;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Data;

namespace ItemBan
{
	public class ItemBan : Mod
	{
        public static int BannedItemType { get; private set; }
        private static List<Func<Item, bool>> onDecideBanCallbacks = new List<Func<Item, bool>>();
        private static List<Action<Item>> onInventorySlotChangedCallbacks = new List<Action<Item>>();
        private static List<Action<Item, Item>> onItemBannedCallbacks = new List<Action<Item, Item>>();
        private static List<Action> onBansCompleteCallbacks = new List<Action>();


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
                case "GETBANNEDITEMTYPE":
                    if (args.Length != 1)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));

                    return BannedItemType;

                case "DECIDEBANS":
                    if (args.Length != 1)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));

                    DecideBans();

                    return null;

                case "ONINVENTORYSLOTCHANGED":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action<Item>))
                        throw new ArgumentException("Second argument must be a Action<Item>", nameof(args));

                    var newSlotChangedCallback = (Action<Item>)args[1];

                    if (!onInventorySlotChangedCallbacks.Contains(newSlotChangedCallback))
                        onInventorySlotChangedCallbacks.Add(newSlotChangedCallback);

                    return null;

                case "OFFINVENTORYSLOTCHANGED":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action<Item>))
                        throw new ArgumentException("Second argument must be a Action<Item>)", nameof(args));

                    var slotChangedCallback = (Action<Item>)args[1];

                    if (onInventorySlotChangedCallbacks.Contains(slotChangedCallback))
                        onInventorySlotChangedCallbacks.Remove(slotChangedCallback);

                    return null;

                case "ONDECIDEBAN":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Func<Item, bool>))
                        throw new ArgumentException("Second argument must be a Func<Item, bool>", nameof(args));

                    var newDecideCallback = (Func<Item, bool>)args[1];

                    if (!onDecideBanCallbacks.Contains(newDecideCallback))
                        onDecideBanCallbacks.Add(newDecideCallback);

                    return null;

                case "OFFDECIDEBAN":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Func<Item, bool>))
                        throw new ArgumentException("Second argument must be a Func<Item, bool>", nameof(args));

                    var decideCallback = (Func<Item, bool>)args[1];

                    if (onDecideBanCallbacks.Contains(decideCallback))
                        onDecideBanCallbacks.Remove(decideCallback);

                    return null;

                case "ONITEMBANNED":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action<Item, Item>))
                        throw new ArgumentException("Second argument must be a Action<Item, Item>", nameof(args));

                    var newBannedCallback = (Action<Item, Item>)args[1];

                    if (!onItemBannedCallbacks.Contains(newBannedCallback))
                        onItemBannedCallbacks.Add(newBannedCallback);

                    return null;

                case "OFFITEMBANNED":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action<Item, Item>))
                        throw new ArgumentException("Second argument must be a Action<Item, Item>", nameof(args));

                    var bannedCallback = (Action<Item, Item>)args[1];

                    if (onItemBannedCallbacks.Contains(bannedCallback))
                        onItemBannedCallbacks.Remove(bannedCallback);

                    return null;

                case "ONBANSCOMPLETE":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action))
                        throw new ArgumentException("Second argument must be a Action", nameof(args));

                    var newBansCompleteCallback = (Action)args[1];

                    if (!onBansCompleteCallbacks.Contains(newBansCompleteCallback))
                        onBansCompleteCallbacks.Add(newBansCompleteCallback);

                    return null;

                case "OFFBANSCOMPLETE":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action))
                        throw new ArgumentException("Second argument must be a Action", nameof(args));

                    var bansCompleteCallback = (Action)args[1];

                    if (onBansCompleteCallbacks.Contains(bansCompleteCallback))
                        onBansCompleteCallbacks.Remove(bansCompleteCallback);

                    return null;

                default:
                    throw new InvalidOperationException("Unrecognized command \"" + args[0] + "\"");
            }
        }

        public void TriggerOnInventorySlotChanged(Item item)
        {
            Logger.Debug("joestub TriggerOnInventorySlotChanged " + item.ToString());

            if (item.active)
            {
                if (item.type == ItemBan.BannedItemType)
                    DecideBans();
            }

            foreach (var callback in onInventorySlotChangedCallbacks)
            {
                callback(item);
            }
        }

        public void DecideBans()
        {
            if (Main.netMode == NetmodeID.Server || !Main.LocalPlayer.active)
                return;

            bool allowBannedItemsInSinglePlayer = ModContent.GetInstance<ClientConfig>().AllowBannedItemsInSinglePlayer;
            var modPlayer = Main.LocalPlayer.GetModPlayer<ItemBanPlayer>();

            Logger.Debug("joestub entering loop");

            foreach (var item in modPlayer.GetAllActiveItems())
            {
                Logger.Debug("joestub looping for " + item.ToString());

                // For any items currently in the player's inventory that have already been changed to BannedItems, change them back now.
                // The code below is about to re-decide whether this item should still be banned.
                if (item.type == ItemBan.BannedItemType)
                    ChangeBackToOriginalItem(item);

                // If any of the callbacks decide that the item is banned, then it's banned.
                bool isItemBanned = false;
                foreach (var decideCallback in onDecideBanCallbacks)
                {
                    isItemBanned = decideCallback(item);

                    if (isItemBanned)
                        break;
                }

                bool allowBannedItem = (Main.netMode == NetmodeID.SinglePlayer && allowBannedItemsInSinglePlayer);

                if (isItemBanned && !allowBannedItem)
                {
                    Logger.Debug("Banning item " + item.ToString());

                    var originalItemClone = item.Clone();
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

                    foreach (var bannedCallback in onItemBannedCallbacks)
                    {
                        bannedCallback(item, originalItemClone);
                    }
                }
            }

            foreach (var bansCompleteCallback in onBansCompleteCallbacks)
            {
                bansCompleteCallback();
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, Main.myPlayer);
        }

        public void ChangeBackToOriginalItem(Item item)
        {
            if (!item.active || item.type != BannedItemType)
                throw new Exception("Cannot change this item from an BannedItem back to its original type: " + item.ToString());

            var bannedItem = (BannedItem)item.ModItem;

            var originalType = bannedItem.OriginalType;
            var originalStack = bannedItem.OriginalStack;
            var originalPrefix = bannedItem.OriginalPrefix;
            var originalData = bannedItem.OriginalData;

            item.ChangeItemType(originalType);
            item.stack = originalStack;
            item.Prefix(originalPrefix);
            ItemIO.Load(item, originalData);

            Logger.Debug("Changed back item " + item.ToString());
        }
    }
}