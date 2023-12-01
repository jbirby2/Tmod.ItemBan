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

        internal static List<Action<Item>> OnInventorySlotChangedCallbacks = new List<Action<Item>>();
        internal static List<Func<Item, bool>> OnDecideBanCallbacks = new List<Func<Item, bool>>();
        internal static List<Func<Item, object>> OnItemPreBanCallbacks = new List<Func<Item, object>>();
        internal static List<Action<Item, object>> OnItemPostBanCallbacks = new List<Action<Item, object>>();
        internal static List<Action> OnClientBansCompleteCallbacks = new List<Action>();
        internal static List<Action> OnServerBansCompleteCallbacks = new List<Action>();


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

                case "DECIDEBANSONCLIENT":
                    if (args.Length != 1)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));

                    if (Main.LocalPlayer.active)
                        Main.LocalPlayer.GetModPlayer<ItemBanPlayer>().ScheduleDecideBans();

                    return null;

                case "DECIDEBANSONSERVER":
                    if (args.Length != 1)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));

                    var worldSystem = ModContent.GetInstance<ItemBanSystem>();
                    if (worldSystem != null)
                        worldSystem.ScheduleDecideBansOnServer();

                    return null;

                case "ONINVENTORYSLOTCHANGED":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action<Item>))
                        throw new ArgumentException("Second argument must be a Action<Item>", nameof(args));

                    var newSlotChangedCallback = (Action<Item>)args[1];

                    if (!OnInventorySlotChangedCallbacks.Contains(newSlotChangedCallback))
                        OnInventorySlotChangedCallbacks.Add(newSlotChangedCallback);

                    return null;

                case "OFFINVENTORYSLOTCHANGED":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action<Item>))
                        throw new ArgumentException("Second argument must be a Action<Item>)", nameof(args));

                    var slotChangedCallback = (Action<Item>)args[1];

                    if (OnInventorySlotChangedCallbacks.Contains(slotChangedCallback))
                        OnInventorySlotChangedCallbacks.Remove(slotChangedCallback);

                    return null;

                case "ONDECIDEBAN":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Func<Item, bool>))
                        throw new ArgumentException("Second argument must be a Func<Item, bool>", nameof(args));

                    var newDecideCallback = (Func<Item, bool>)args[1];

                    if (!OnDecideBanCallbacks.Contains(newDecideCallback))
                        OnDecideBanCallbacks.Add(newDecideCallback);

                    return null;

                case "OFFDECIDEBAN":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Func<Item, bool>))
                        throw new ArgumentException("Second argument must be a Func<Item, bool>", nameof(args));

                    var decideCallback = (Func<Item, bool>)args[1];

                    if (OnDecideBanCallbacks.Contains(decideCallback))
                        OnDecideBanCallbacks.Remove(decideCallback);

                    return null;

                case "ONITEMBAN":
                    if (args.Length != 3)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Func<Item, object>))
                        throw new ArgumentException("Second argument must be a Func<Item, object>", nameof(args));
                    else if (!(args[2] is Action<Item, object>))
                        throw new ArgumentException("Third argument must be a Action<Item, object>", nameof(args));

                    var newPreBanCallback = (Func<Item, object>)args[1];
                    if (!OnItemPreBanCallbacks.Contains(newPreBanCallback))
                        OnItemPreBanCallbacks.Add(newPreBanCallback);

                    var newPostBanCallback = (Action<Item, object>)args[2];
                    if (!OnItemPostBanCallbacks.Contains(newPostBanCallback))
                        OnItemPostBanCallbacks.Add(newPostBanCallback);

                    return null;

                case "OFFITEMBAN":
                    if (args.Length != 3)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Func<Item, object>))
                        throw new ArgumentException("Second argument must be a Func<Item, object>", nameof(args));
                    else if (!(args[2] is Action<Item, object>))
                        throw new ArgumentException("Third argument must be a Action<Item, object>", nameof(args));

                    var preBanCallback = (Func<Item, object>)args[1];
                    if (OnItemPreBanCallbacks.Contains(preBanCallback))
                        OnItemPreBanCallbacks.Remove(preBanCallback);

                    var postBanCallback = (Action<Item, object>)args[2];
                    if (OnItemPostBanCallbacks.Contains(postBanCallback))
                        OnItemPostBanCallbacks.Remove(postBanCallback);

                    return null;

                case "ONCLIENTBANSCOMPLETE":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action))
                        throw new ArgumentException("Second argument must be a Action", nameof(args));

                    var newClientBansCompleteCallback = (Action)args[1];

                    if (!OnClientBansCompleteCallbacks.Contains(newClientBansCompleteCallback))
                        OnClientBansCompleteCallbacks.Add(newClientBansCompleteCallback);

                    return null;

                case "OFFCLIENTBANSCOMPLETE":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action))
                        throw new ArgumentException("Second argument must be a Action", nameof(args));

                    var clientBansCompleteCallback = (Action)args[1];

                    if (OnClientBansCompleteCallbacks.Contains(clientBansCompleteCallback))
                        OnClientBansCompleteCallbacks.Remove(clientBansCompleteCallback);

                    return null;

                case "ONSERVERBANSCOMPLETE":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action))
                        throw new ArgumentException("Second argument must be a Action", nameof(args));

                    var newServerBansCompleteCallback = (Action)args[1];

                    if (!OnServerBansCompleteCallbacks.Contains(newServerBansCompleteCallback))
                        OnServerBansCompleteCallbacks.Add(newServerBansCompleteCallback);

                    return null;

                case "OFFSERVERBANSCOMPLETE":
                    if (args.Length != 2)
                        throw new ArgumentException("Invalid number of arguments for this command", nameof(args));
                    else if (!(args[1] is Action))
                        throw new ArgumentException("Second argument must be a Action", nameof(args));

                    var serverBansCompleteCallback = (Action)args[1];

                    if (OnServerBansCompleteCallbacks.Contains(serverBansCompleteCallback))
                        OnServerBansCompleteCallbacks.Remove(serverBansCompleteCallback);

                    return null;

                default:
                    throw new InvalidOperationException("Unrecognized command \"" + args[0] + "\"");
            }
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

        public void DecideBan(Item item, ClientConfig clientConfig, ServerConfig serverConfig)
        {
            // For any items currently in the player's inventory that have already been changed to BannedItems, change them back now.
            // The code below is about to re-decide whether this item should still be banned.
            if (item.type == ItemBan.BannedItemType)
                ChangeBackToOriginalItem(item);

            Logger.Debug("Deciding ban for " + item.ToString());

            bool isItemBanned = false;

            if (serverConfig.BannedItems.Any(bannedItemDefinition => bannedItemDefinition.Type == item.type))
                isItemBanned = true;
            else
            {
                // If any of the callbacks decide that the item is banned, then it's banned.
                foreach (var decideCallback in ItemBan.OnDecideBanCallbacks)
                {
                    isItemBanned = decideCallback(item);

                    if (isItemBanned)
                        break;
                }
            }

            bool allowBannedItem = (Main.netMode == NetmodeID.SinglePlayer && clientConfig.AllowBannedItemsInSinglePlayer);

            if (isItemBanned && !allowBannedItem)
            {
                Logger.Debug("Banning item " + item.ToString());

                var preBanStates = new object[OnItemPreBanCallbacks.Count];
                for (int i = 0; i < OnItemPreBanCallbacks.Count; i++)
                {
                    preBanStates[i] = OnItemPreBanCallbacks[i](item);
                }

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

                for (int i = 0; i < OnItemPostBanCallbacks.Count; i++)
                {
                    OnItemPostBanCallbacks[i](item, preBanStates[i]);
                }
            }
        }
    }
}