/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.Content;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model.Platform;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Build-buy-catalog family command handlers (freesoexperiment-304). Eight ops over
/// catalog/object-placement: buy-object, place-from-inventory, move-object, delete-object,
/// send-to-inventory, list-object-for-sale, buy-listed-object, upgrade-object. All PDUs ride
/// the <b>lot</b> socket via <see cref="HeadlessVMHost.Driver"/> (VMClientDriver.SendCommand).
///
/// <para>
/// <b>OQ-8 resolution (docs/design/verb-catalog.md:145)</b>: there is no build/buy mode-entry
/// PDU. The UI has local <c>UIBuildMode</c>/<c>UIBuyMode</c> panels but all wire-level gating
/// is enforced server-side inside each <c>VMNet*Cmd.Verify()</c> via
/// <c>PlatformState.Validator.GetPurchaseMode(...)</c>. A caller without build-roommate+
/// permissions gets a silent drop (no error PDU back), not a rejection. The sidecar observes
/// the effect (or absence) via the next perception frame.
/// </para>
///
/// <para>
/// <b>Owner gating in the handler</b>. Per wave-2b veracity lesson and
/// <see cref="PropertyHandlers.CheckOwner"/>, we enforce a deterministic refuse path BEFORE the
/// PDU goes out: every mutation checks <c>vm.TSOState.OwnerID == MyAvatarPersistId</c>. The
/// server would also refuse, but a local refuse gives the agent a synchronous error it can see
/// without waiting for a server timeout. Exception: <c>buy-listed-object</c> requires a visitor
/// (must NOT be owner), so its guard is inverted; <c>list-object-for-sale</c> and
/// <c>send-to-inventory</c>/<c>upgrade-object</c> require own-object — we check
/// <c>VMTSOObjectState.OwnerID == MyAvatarPersistId</c> on the target.
/// </para>
///
/// <para>
/// <b>Thread safety</b>: every pre-PDU check reads VM state the tick thread mutates
/// (OwnerID, VMTSOObjectState, GetObjectByPersist). All reads route through
/// <see cref="HeadlessVMHost.RunUnderTickLock{T}"/>; outbound PDUs use Driver.SendCommand which
/// is lock-protected per the wave-2a contract.
/// </para>
///
/// <para>
/// <b>delete-object refund note (doc contradiction)</b>: docs/design/verb-catalog.md:53 claims
/// "Money is not refunded (use send-to-inventory or sell-back for refund paths)". The
/// <c>VMNetDeleteObjectCmd.Execute</c> source (tso.simantics/NetPlay/Model/Commands) DOES call
/// <c>GlobalLink.PerformTransaction</c> to refund <c>obj.MultitileGroup.Price</c> when Mode ==
/// DeleteMode.Delete. Wire-level truth wins over the catalog comment. Filed as a follow-up
/// catalog-doc amendment.
/// </para>
///
/// <para>
/// <b>find-cheap-catalog-guid</b> is a test-support helper (not in the verb catalog). It scans
/// <c>Content.WorldCatalog</c> for the cheapest non-blacklisted object in the user-placement
/// whitelist and returns its GUID + price + name. Used by the integration test to pick a cheap
/// throwaway for non-destructive buy/delete cycles without the test hardcoding a GUID.
/// </para>
/// </summary>
public static class BuyModeHandlers
{
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));

        dispatcher.Register("buy-object",            (args, ct) => Task.FromResult(BuyObject(vmHost, args)));
        dispatcher.Register("place-from-inventory",  (args, ct) => Task.FromResult(PlaceFromInventory(vmHost, args)));
        dispatcher.Register("move-object",           (args, ct) => Task.FromResult(MoveObject(vmHost, args)));
        dispatcher.Register("delete-object",         (args, ct) => Task.FromResult(DeleteObject(vmHost, args)));
        dispatcher.Register("send-to-inventory",     (args, ct) => Task.FromResult(SendToInventory(vmHost, args)));
        dispatcher.Register("list-object-for-sale",  (args, ct) => Task.FromResult(ListObjectForSale(vmHost, args)));
        dispatcher.Register("buy-listed-object",     (args, ct) => Task.FromResult(BuyListedObject(vmHost, args)));
        dispatcher.Register("upgrade-object",        (args, ct) => Task.FromResult(UpgradeObject(vmHost, args)));
        dispatcher.Register("find-cheap-catalog-guid", (args, ct) => Task.FromResult(FindCheapCatalogGuid(vmHost, args)));
    }

    // ---- helpers ----

    private static (bool isOwner, uint ownerId) CheckLotOwner(HeadlessVMHost vmHost)
    {
        return vmHost.RunUnderTickLock(() =>
        {
            var vm = vmHost.VM;
            if (vm == null) return (false, 0u);
            var lotState = vm.TSOState as VMTSOLotState;
            if (lotState == null) return (false, 0u);
            var ownerId = lotState.OwnerID;
            return (ownerId == vmHost.MyAvatarPersistId && ownerId != 0, ownerId);
        });
    }

    private static (bool isOwnerOfObj, uint objectOwner, short objectId, uint objectPid) CheckObjectOwnership(
        HeadlessVMHost vmHost, uint persistId)
    {
        return vmHost.RunUnderTickLock(() =>
        {
            var vm = vmHost.VM;
            if (vm == null) return (false, 0u, (short)0, 0u);
            var obj = vm.GetObjectByPersist(persistId);
            if (obj == null) return (false, 0u, (short)0, 0u);
            var state = obj.TSOState as VMTSOObjectState;
            if (state == null) return (false, 0u, obj.ObjectID, persistId);
            var owner = state.OwnerID;
            return (owner == vmHost.MyAvatarPersistId && owner != 0, owner, obj.ObjectID, persistId);
        });
    }

    private static Direction ParseDir(JsonNode node)
    {
        if (node == null) return Direction.NORTHWEST;
        // Accept either a numeric byte (0x80 = NORTHWEST) or a string name.
        if (node is JsonValue v)
        {
            if (v.TryGetValue<long>(out var n))
                return (Direction)checked((byte)(n & 0xFF));
            if (v.TryGetValue<string>(out var s) && !string.IsNullOrEmpty(s))
            {
                if (Enum.TryParse<Direction>(s, ignoreCase: true, out var d))
                    return d;
            }
        }
        return Direction.NORTHWEST;
    }

    private static PurchaseMode ParseMode(JsonNode node)
    {
        if (node == null) return PurchaseMode.Normal;
        if (node is JsonValue v && v.TryGetValue<string>(out var s))
        {
            if (s.Equals("Donate", StringComparison.OrdinalIgnoreCase)) return PurchaseMode.Donate;
            if (s.Equals("Normal", StringComparison.OrdinalIgnoreCase)) return PurchaseMode.Normal;
        }
        return PurchaseMode.Normal;
    }

    // ---- buy-object ----

    internal static CommandDispatcher.Response BuyObject(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
        {
            return CommandDispatcher.Response.Fail($"buy-object: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");
        }
        var guidArg = (long?)args["guid"];
        if (!guidArg.HasValue || guidArg.Value == 0)
            return CommandDispatcher.Response.Fail("buy-object requires guid (uint, non-zero catalog GUID)");
        var xArg = (long?)args["x"];
        var yArg = (long?)args["y"];
        if (!xArg.HasValue || !yArg.HasValue)
            return CommandDispatcher.Response.Fail("buy-object requires x, y (subtile coords, 16 = tile 1)");

        var cmd = new VMNetBuyObjectCmd
        {
            GUID = checked((uint)guidArg.Value),
            x = checked((short)xArg.Value),
            y = checked((short)yArg.Value),
            level = checked((sbyte)((long?)args["level"] ?? 1)),
            dir = ParseDir(args["dir"]),
            Mode = ParseMode(args["mode"]),
            TargetUpgradeLevel = checked((byte)((long?)args["target_upgrade_level"] ?? 0)),
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "buy-object",
            guid = (long)cmd.GUID,
            x = (int)cmd.x,
            y = (int)cmd.y,
            level = (int)cmd.level,
            dir = (int)(byte)cmd.dir,
            mode = cmd.Mode.ToString(),
        });
    }

    // ---- place-from-inventory ----

    internal static CommandDispatcher.Response PlaceFromInventory(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
        {
            return CommandDispatcher.Response.Fail($"place-from-inventory: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");
        }
        var pidArg = (long?)args["object_persist_id"];
        if (!pidArg.HasValue || pidArg.Value == 0)
            return CommandDispatcher.Response.Fail("place-from-inventory requires object_persist_id (uint, non-zero)");
        var xArg = (long?)args["x"];
        var yArg = (long?)args["y"];
        if (!xArg.HasValue || !yArg.HasValue)
            return CommandDispatcher.Response.Fail("place-from-inventory requires x, y");

        var cmd = new VMNetPlaceInventoryCmd
        {
            ObjectPID = checked((uint)pidArg.Value),
            x = checked((short)xArg.Value),
            y = checked((short)yArg.Value),
            level = checked((sbyte)((long?)args["level"] ?? 1)),
            dir = ParseDir(args["dir"]),
            Mode = ParseMode(args["mode"]),
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "place-from-inventory",
            object_persist_id = (long)cmd.ObjectPID,
            x = (int)cmd.x,
            y = (int)cmd.y,
            level = (int)cmd.level,
            dir = (int)(byte)cmd.dir,
        });
    }

    // ---- move-object ----

    internal static CommandDispatcher.Response MoveObject(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
        {
            return CommandDispatcher.Response.Fail($"move-object: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");
        }
        var idArg = (long?)args["target_object_id"];
        if (!idArg.HasValue || idArg.Value == 0)
            return CommandDispatcher.Response.Fail("move-object requires target_object_id (short, non-zero)");
        var xArg = (long?)args["x"];
        var yArg = (long?)args["y"];
        if (!xArg.HasValue || !yArg.HasValue)
            return CommandDispatcher.Response.Fail("move-object requires x, y");

        var cmd = new VMNetMoveObjectCmd
        {
            ObjectID = checked((short)idArg.Value),
            x = checked((short)xArg.Value),
            y = checked((short)yArg.Value),
            level = checked((sbyte)((long?)args["level"] ?? 1)),
            dir = ParseDir(args["dir"]),
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "move-object",
            target_object_id = (int)cmd.ObjectID,
            x = (int)cmd.x,
            y = (int)cmd.y,
            level = (int)cmd.level,
            dir = (int)(byte)cmd.dir,
        });
    }

    // ---- delete-object ----

    internal static CommandDispatcher.Response DeleteObject(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
        {
            return CommandDispatcher.Response.Fail($"delete-object: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");
        }
        var idArg = (long?)args["target_object_id"];
        if (!idArg.HasValue || idArg.Value == 0)
            return CommandDispatcher.Response.Fail("delete-object requires target_object_id (short, non-zero)");

        var cmd = new VMNetDeleteObjectCmd
        {
            ObjectID = checked((short)idArg.Value),
            CleanupAll = (bool?)args["cleanup_all"] ?? false,
            Mode = DeleteMode.Delete,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "delete-object",
            target_object_id = (int)cmd.ObjectID,
            cleanup_all = cmd.CleanupAll,
        });
    }

    // ---- send-to-inventory ----

    internal static CommandDispatcher.Response SendToInventory(HeadlessVMHost vmHost, JsonObject args)
    {
        // send-to-inventory requires the caller to OWN the object (not just the lot).
        var pidArg = (long?)args["target_object_persist_id"];
        if (!pidArg.HasValue || pidArg.Value == 0)
            return CommandDispatcher.Response.Fail("send-to-inventory requires target_object_persist_id (uint, non-zero)");

        var (isOwnerOfObj, owner, objectId, objPid) = CheckObjectOwnership(vmHost, (uint)pidArg.Value);
        if (objectId == 0 && owner == 0)
            return CommandDispatcher.Response.Fail($"send-to-inventory: object persist_id={pidArg} not found in local VM");
        if (!isOwnerOfObj)
            return CommandDispatcher.Response.Fail($"send-to-inventory: caller is not the object owner (object_owner={owner}, me={vmHost.MyAvatarPersistId})");

        var cmd = new VMNetSendToInventoryCmd
        {
            ObjectPID = (uint)pidArg.Value,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "send-to-inventory",
            target_object_persist_id = (long)cmd.ObjectPID,
        });
    }

    // ---- list-object-for-sale ----

    internal static CommandDispatcher.Response ListObjectForSale(HeadlessVMHost vmHost, JsonObject args)
    {
        var pidArg = (long?)args["target_object_persist_id"];
        if (!pidArg.HasValue || pidArg.Value == 0)
            return CommandDispatcher.Response.Fail("list-object-for-sale requires target_object_persist_id (uint, non-zero)");
        var priceArg = (long?)args["new_price"];
        if (!priceArg.HasValue)
            return CommandDispatcher.Response.Fail("list-object-for-sale requires new_price (int; 0+ to list at that price, -1 to delist)");

        var (isOwnerOfObj, owner, _, _) = CheckObjectOwnership(vmHost, (uint)pidArg.Value);
        if (!isOwnerOfObj)
            return CommandDispatcher.Response.Fail($"list-object-for-sale: caller is not the object owner (object_owner={owner}, me={vmHost.MyAvatarPersistId})");

        var cmd = new VMNetAsyncPriceCmd
        {
            ObjectPID = (uint)pidArg.Value,
            NewPrice = checked((int)priceArg.Value),
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "list-object-for-sale",
            target_object_persist_id = (long)cmd.ObjectPID,
            new_price = cmd.NewPrice,
        });
    }

    // ---- buy-listed-object ----

    internal static CommandDispatcher.Response BuyListedObject(HeadlessVMHost vmHost, JsonObject args)
    {
        // buy-listed-object requires the caller to NOT own the object (visitor/guest path).
        var pidArg = (long?)args["target_object_persist_id"];
        if (!pidArg.HasValue || pidArg.Value == 0)
            return CommandDispatcher.Response.Fail("buy-listed-object requires target_object_persist_id (uint, non-zero)");

        var (isOwnerOfObj, owner, objectId, _) = CheckObjectOwnership(vmHost, (uint)pidArg.Value);
        if (objectId == 0 && owner == 0)
            return CommandDispatcher.Response.Fail($"buy-listed-object: object persist_id={pidArg} not found in local VM");
        if (isOwnerOfObj)
            return CommandDispatcher.Response.Fail("buy-listed-object: caller cannot buy their own object (use place-from-inventory or move-object instead)");

        var cmd = new VMNetAsyncSaleCmd
        {
            ObjectPID = (uint)pidArg.Value,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "buy-listed-object",
            target_object_persist_id = (long)cmd.ObjectPID,
        });
    }

    // ---- upgrade-object ----

    internal static CommandDispatcher.Response UpgradeObject(HeadlessVMHost vmHost, JsonObject args)
    {
        var pidArg = (long?)args["target_object_persist_id"];
        if (!pidArg.HasValue || pidArg.Value == 0)
            return CommandDispatcher.Response.Fail("upgrade-object requires target_object_persist_id (uint, non-zero)");
        var lvlArg = (long?)args["target_upgrade_level"];
        if (!lvlArg.HasValue || lvlArg.Value <= 0)
            return CommandDispatcher.Response.Fail("upgrade-object requires target_upgrade_level (> 0)");

        var (isOwnerOfObj, owner, _, _) = CheckObjectOwnership(vmHost, (uint)pidArg.Value);
        if (!isOwnerOfObj)
            return CommandDispatcher.Response.Fail($"upgrade-object: caller is not the object owner (object_owner={owner}, me={vmHost.MyAvatarPersistId})");

        var cmd = new VMNetUpgradeCmd
        {
            ObjectPID = (uint)pidArg.Value,
            TargetUpgradeLevel = checked((byte)lvlArg.Value),
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "upgrade-object",
            target_object_persist_id = (long)cmd.ObjectPID,
            target_upgrade_level = (int)cmd.TargetUpgradeLevel,
        });
    }

    // ---- find-cheap-catalog-guid (test helper) ----

    /// <summary>
    /// Scan <see cref="Content.WorldCatalog"/> for cheap, non-blacklisted, user-placeable items
    /// AND in the server's roommate whitelist (categories 12..20 per
    /// <c>VMNetBuyObjectCmd.RoomieWhiteList</c>) so the server won't silently drop the buy.
    /// Not a verb-catalog op — a test-support utility that lets the integration test pick a
    /// cheap throwaway GUID at runtime instead of hardcoding one (different TSO asset snapshots
    /// have different catalog contents). No PDU emission.
    /// Args: max_price (int, default 500), limit (int, default 5).
    /// </summary>
    internal static CommandDispatcher.Response FindCheapCatalogGuid(HeadlessVMHost vmHost, JsonObject args)
    {
        int maxPrice = (int)((long?)args["max_price"] ?? 500L);
        int limit = (int)((long?)args["limit"] ?? 5L);

        try
        {
            var content = Content.Content.Get();
            var catalog = content.WorldCatalog;
            if (catalog == null)
                return CommandDispatcher.Response.Fail("find-cheap-catalog-guid: catalog not initialised");

            // Blacklist from VMNetBuyObjectCmd.
            var blacklist = new HashSet<uint> { 0x24C95F99u };
            // Roommate whitelist (VMNetBuyObjectCmd.RoomieWhiteList): categories 12..20.
            // Items outside this set are silently dropped by GetPurchaseMode for a
            // BuildBuyRoommate caller, so the helper must restrict to these.
            var allowedCategories = new HashSet<int> { 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            var all = catalog.All();
            if (all == null || all.Count == 0)
                return CommandDispatcher.Response.Fail("find-cheap-catalog-guid: catalog empty");

            var sorted = all
                .Where(i => i.Price > 0 && i.Price <= maxPrice)
                .Where(i => i.DisableLevel == 0)
                .Where(i => allowedCategories.Contains((int)i.Category))
                .Where(i => !blacklist.Contains(i.GUID))
                .OrderBy(i => i.Price)
                .Take(limit)
                .Select(i => (object)new
                {
                    guid = (long)i.GUID,
                    price = (long)i.Price,
                    name = i.Name ?? "",
                    category = (int)i.Category,
                })
                .ToList();

            return CommandDispatcher.Response.Success(new
            {
                verb = "find-cheap-catalog-guid",
                max_price = maxPrice,
                count = sorted.Count,
                candidates = sorted,
            });
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"find-cheap-catalog-guid: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
