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
        // search-catalog is the full convention op; find-cheap-catalog-guid keeps its old
        // response shape for backward compatibility with existing integration tests. Both names
        // route to FindCheapCatalogGuid; the verb parameter determines the response shape.
        dispatcher.Register("search-catalog", (args, ct) => Task.FromResult(FindCheapCatalogGuid(vmHost, args, verb: "search-catalog")));
        dispatcher.Register("list-catalog-categories", (args, ct) => Task.FromResult(ListCatalogCategories(vmHost, args)));
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

    // ---- list-catalog-categories (freesoexperiment-2d1) ----

    /// <summary>
    /// Category name map for the roommate-buyer whitelist (categories 12..20).
    /// Taken from FSO.IDE/ResourceBrowser/XMLEntryEditor.cs — the authoritative IDE label
    /// table for buy-panel object categories. Single English label per ID; no i18n.
    /// </summary>
    private static readonly Dictionary<int, string> RoomieWhiteListCategoryNames = new()
    {
        { 12, "Seating" },
        { 13, "Surfaces" },
        { 14, "Appliances" },
        { 15, "Entertainment" },
        { 16, "Skill and Job Objects" },
        { 17, "Decorative" },
        { 18, "Miscellaneous" },
        { 19, "Lighting" },
        { 20, "Pets and Pet Objects" },
    };

    /// <summary>
    /// Return the category index for the roommate-buyer whitelist (categories 12..20).
    /// One entry per category where at least one non-blacklisted, DisableLevel==0 item
    /// exists. Categories with zero qualifying items are omitted from the response.
    /// Output schema: <c>[{category_id, category_name, count}]</c> sorted by category_id.
    /// No args; pure local-catalog read, no PDU emission.
    /// </summary>
    internal static CommandDispatcher.Response ListCatalogCategories(HeadlessVMHost vmHost, JsonObject args)
    {
        try
        {
            var content = Content.Content.Get();
            var catalog = content.WorldCatalog;
            if (catalog == null)
                return CommandDispatcher.Response.Fail("list-catalog-categories: catalog not initialised");

            // Blacklist and whitelist mirror FindCheapCatalogGuid / VMNetBuyObjectCmd.
            var blacklist = new HashSet<uint> { 0x24C95F99u };
            var allowedCategories = new HashSet<int> { 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            var all = catalog.All();
            if (all == null || all.Count == 0)
                return CommandDispatcher.Response.Fail("list-catalog-categories: catalog empty");

            var counts = all
                .Where(i => i.DisableLevel == 0)
                .Where(i => allowedCategories.Contains((int)i.Category))
                .Where(i => !blacklist.Contains(i.GUID))
                .GroupBy(i => (int)i.Category)
                .Where(g => g.Any())
                .OrderBy(g => g.Key)
                .Select(g => (object)new
                {
                    category_id   = g.Key,
                    category_name = RoomieWhiteListCategoryNames.TryGetValue(g.Key, out var name) ? name : $"unknown({g.Key})",
                    count         = g.Count(),
                })
                .ToList();

            return CommandDispatcher.Response.Success(new
            {
                verb       = "list-catalog-categories",
                categories = counts,
            });
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"list-catalog-categories: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ---- search-catalog / find-cheap-catalog-guid (freesoexperiment-281a) ----

    /// <summary>
    /// Tier-bin thresholds computed lazily at first call from the live catalog snapshot.
    /// Thresholds are snapshot-relative: recomputed when the bot reboots with a different
    /// content snapshot. cheap = price ≤ P33; expensive = price > P67; moderate = in-between.
    /// </summary>
    private static int _tierP33 = -1;
    private static int _tierP67 = -1;
    private static readonly object _tierLock = new();

    private static (int p33, int p67) GetTierThresholds()
    {
        if (_tierP33 >= 0) return (_tierP33, _tierP67);
        lock (_tierLock)
        {
            if (_tierP33 >= 0) return (_tierP33, _tierP67);
            var content = Content.Content.Get();
            var catalog = content.WorldCatalog;
            var bl      = new HashSet<uint> { 0x24C95F99u };
            var allowed = new HashSet<int>  { 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            var prices  = catalog.All()
                .Where(i => i.Price > 0 && i.DisableLevel == 0
                         && allowed.Contains((int)i.Category) && !bl.Contains(i.GUID))
                .Select(i => (int)i.Price)
                .OrderBy(p => p)
                .ToList();
            if (prices.Count < 2) { _tierP33 = 0; _tierP67 = int.MaxValue; }
            else
            {
                _tierP33 = prices[(int)(prices.Count * 0.33)];
                _tierP67 = prices[(int)(prices.Count * 0.67)];
            }
            return (_tierP33, _tierP67);
        }
    }

    /// <summary>
    /// Category slug → ID map (subset of RoomieWhiteList 12..20). OrdinalIgnoreCase.
    /// Unrecognised slugs resolve to -1 in <see cref="ParseCategory"/>.
    /// </summary>
    private static readonly Dictionary<string, int> CategorySlugMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "seating",               12 }, { "seat",   12 }, { "chairs", 12 }, { "sofas",  12 },
        { "surfaces",              13 }, { "tables", 13 },
        { "appliances",            14 }, { "appliance", 14 },
        { "entertainment",         15 },
        { "skill-and-job-objects", 16 }, { "skill",  16 }, { "job",    16 },
        { "decorative",            17 }, { "decor",  17 },
        { "miscellaneous",         18 }, { "misc",   18 },
        { "lighting",              19 }, { "lights", 19 },
        { "pets-and-pet-objects",  20 }, { "pets",   20 },
    };

    /// <summary>
    /// Parse the <c>category</c> arg (int 12..20 OR string slug).
    /// Returns null (absent), valid category id (12..20), or -1 (out-of-whitelist → 0 results).
    /// </summary>
    private static int? ParseCategory(JsonObject args)
    {
        var node = args["category"];
        if (node == null) return null;
        if (node is JsonValue v)
        {
            if (v.TryGetValue<long>(out var n))
                return (n >= 12 && n <= 20) ? (int)n : -1;
            if (v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
            {
                if (CategorySlugMap.TryGetValue(s.Trim(), out var id)) return id;
                if (int.TryParse(s.Trim(), out var ni)) return (ni >= 12 && ni <= 20) ? ni : -1;
                return -1;
            }
        }
        return null;
    }

    /// <summary>
    /// Scan <see cref="Content.WorldCatalog"/> for non-blacklisted, user-placeable items
    /// in the server's roommate whitelist (categories 12..20) with optional filters.
    ///
    /// <para>
    /// Shared entry point for two dispatcher ops:
    /// <list type="bullet">
    ///   <item><c>find-cheap-catalog-guid</c> (default, legacy): old response shape
    ///     <c>{verb, max_price, count, candidates:[{guid, price, name, category}]}</c>
    ///     preserved for backward compat with verb-buymode.sh integration test.</item>
    ///   <item><c>search-catalog</c>: new shape
    ///     <c>{verb, count, results:[{guid_hex, guid_decimal, name, price, category_id,
    ///     category_name}], categories_summary:[{category_id, category_name, count}]}</c>.</item>
    /// </list>
    /// </para>
    ///
    /// New filter args (all optional for both ops):
    /// <c>name</c> (case-insensitive substring), <c>category</c> (int 12..20 or slug),
    /// <c>tier</c> ("cheap"/"moderate"/"expensive" — P33/P67, snapshot-relative),
    /// <c>min_price</c>, <c>max_price</c>,
    /// <c>limit</c> (clamped to [1,200]; default 25 for search-catalog, 5 for find-cheap).
    /// No PDU emission. Pure local-catalog read.
    /// </summary>
    internal static CommandDispatcher.Response FindCheapCatalogGuid(
        HeadlessVMHost vmHost, JsonObject args, string verb = "find-cheap-catalog-guid")
    {
        bool isSearchCatalog = verb == "search-catalog";

        int limitRaw = (int)((long?)args["limit"] ?? (isSearchCatalog ? 25L : 5L));
        // Server-side clamp: limit must be in [1, 200].
        int limit    = Math.Clamp(limitRaw, 1, 200);

        // max_price: find-cheap-catalog-guid defaults to 500 (backward compat);
        // search-catalog defaults to no cap (int.MaxValue).
        long rawMax  = (long?)args["max_price"] ?? (isSearchCatalog ? (long)int.MaxValue : 500L);
        int maxPrice = (int)Math.Min(rawMax, int.MaxValue);
        int minPrice = (int)((long?)args["min_price"] ?? 0L);

        string nameFilter    = (string?)args["name"];
        int?   categoryFilter = ParseCategory(args);
        string tierFilter    = ((string?)args["tier"])?.Trim().ToLowerInvariant();

        try
        {
            var content = Content.Content.Get();
            var catalog = content.WorldCatalog;
            if (catalog == null)
                return CommandDispatcher.Response.Fail($"{verb}: catalog not initialised");

            // Safety filters: blacklist + roommate whitelist (categories 12..20) + DisableLevel==0.
            // NON-NEGOTIABLE: mirrors VMNetBuyObjectCmd server-side gating so agents never
            // discover items the server will silently drop on the buy command.
            var blacklist         = new HashSet<uint> { 0x24C95F99u };
            var allowedCategories = new HashSet<int>  { 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            var all = catalog.All();
            if (all == null || all.Count == 0)
                return CommandDispatcher.Response.Fail($"{verb}: catalog empty");

            // Tier thresholds computed lazily (snapshot-relative — reset on bot reboot).
            int p33 = 0, p67 = int.MaxValue;
            if (!string.IsNullOrEmpty(tierFilter))
                (p33, p67) = GetTierThresholds();

            IEnumerable<FSO.Content.Interfaces.ObjectCatalogItem> query = all
                .Where(i => i.DisableLevel == 0)
                .Where(i => allowedCategories.Contains((int)i.Category))
                .Where(i => !blacklist.Contains(i.GUID))
                .Where(i => i.Price >= (uint)minPrice)
                .Where(i => i.Price <= (uint)maxPrice);

            // Category filter (null = all whitelisted; -1 = out-of-range → empty).
            if (categoryFilter.HasValue)
            {
                if (categoryFilter.Value == -1)
                    query = Enumerable.Empty<FSO.Content.Interfaces.ObjectCatalogItem>();
                else
                    query = query.Where(i => (int)i.Category == categoryFilter.Value);
            }

            // Name filter (case-insensitive substring match on item Name).
            if (!string.IsNullOrEmpty(nameFilter))
                query = query.Where(i => (i.Name ?? "").Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

            // Tier filter.
            if (!string.IsNullOrEmpty(tierFilter))
            {
                query = tierFilter switch
                {
                    "cheap"     => query.Where(i => i.Price <= (uint)p33),
                    "expensive" => query.Where(i => i.Price >  (uint)p67),
                    "moderate"  => query.Where(i => i.Price >  (uint)p33 && i.Price <= (uint)p67),
                    _           => query, // unrecognised tier — no-op
                };
            }

            var filtered = query.OrderBy(i => i.Price).ToList();

            if (isSearchCatalog)
            {
                // New shape: guid_hex/guid_decimal/category_name + categories_summary.
                var results = filtered
                    .Take(limit)
                    .Select(i => (object)new
                    {
                        guid_hex      = "0x" + i.GUID.ToString("X8"),
                        guid_decimal  = (long)(uint)i.GUID,
                        name          = i.Name ?? "",
                        price         = (int)i.Price,
                        category_id   = (int)i.Category,
                        category_name = RoomieWhiteListCategoryNames.TryGetValue((int)i.Category, out var cn1)
                                        ? cn1 : $"unknown({(int)i.Category})",
                    })
                    .ToList();

                // categories_summary: full category landscape with same safety filters but
                // WITHOUT name/tier/price filters — shows what exists before the agent filters.
                var catSummary = all
                    .Where(i => i.DisableLevel == 0)
                    .Where(i => allowedCategories.Contains((int)i.Category))
                    .Where(i => !blacklist.Contains(i.GUID))
                    .GroupBy(i => (int)i.Category)
                    .Where(g => g.Any())
                    .Select(g => (object)new
                    {
                        category_id   = g.Key,
                        category_name = RoomieWhiteListCategoryNames.TryGetValue(g.Key, out var cn2)
                                        ? cn2 : $"unknown({g.Key})",
                        count         = g.Count(),
                    })
                    .OrderBy(o => ((dynamic)o).category_id)
                    .ToList();

                return CommandDispatcher.Response.Success(new
                {
                    verb               = "search-catalog",
                    count              = results.Count,
                    results,
                    categories_summary = catSummary,
                });
            }
            else
            {
                // Legacy find-cheap-catalog-guid shape — backward compat for verb-buymode.sh.
                var sorted = filtered
                    .Take(limit)
                    .Select(i => (object)new
                    {
                        guid     = (long)i.GUID,
                        price    = (long)i.Price,
                        name     = i.Name ?? "",
                        category = (int)i.Category,
                    })
                    .ToList();

                return CommandDispatcher.Response.Success(new
                {
                    verb      = "find-cheap-catalog-guid",
                    max_price = maxPrice,
                    count     = sorted.Count,
                    candidates = sorted,
                });
            }
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"{verb}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
