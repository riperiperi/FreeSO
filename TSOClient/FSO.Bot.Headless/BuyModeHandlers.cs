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
        dispatcher.Register("search-catalog",          (args, ct) => Task.FromResult(FindCheapCatalogGuid(vmHost, args, verb: "search-catalog")));
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

    // ---- catalog search (find-cheap-catalog-guid + search-catalog) ----

    // Blacklist from VMNetBuyObjectCmd — never return this GUID.
    private static readonly HashSet<uint> CatalogBlacklist = new() { 0x24C95F99u };

    // Roommate whitelist (VMNetBuyObjectCmd.RoomieWhiteList): categories 12..20.
    // Items outside this set are silently dropped by GetPurchaseMode for a
    // BuildBuyRoommate caller, so helpers must restrict to these.
    private static readonly HashSet<int> AllowedCategories = new() { 12, 13, 14, 15, 16, 17, 18, 19, 20 };

    // Human-readable names for the allowed categories.
    private static readonly Dictionary<int, string> RoomieWhiteListCategoryNames = new()
    {
        { 12, "seating" },
        { 13, "surfaces" },
        { 14, "appliances" },
        { 15, "entertainment" },
        { 16, "skill" },
        { 17, "decorative" },
        { 18, "misc" },
        { 19, "lighting" },
        { 20, "pets" },
    };

    // Slug-to-category mapping for search-catalog --category <slug>.
    private static readonly Dictionary<string, int> CategorySlugMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "seating",       12 },
        { "surfaces",      13 },
        { "appliances",    14 },
        { "entertainment", 15 },
        { "skill",         16 },
        { "decorative",    17 },
        { "misc",          18 },
        { "lighting",      19 },
        { "pets",          20 },
    };

    // Tier bins: P33 / P67 percentiles over the whitelisted catalog, computed lazily.
    private static int _tierP33 = -1;
    private static int _tierP67 = -1;
    private static readonly object _tierLock = new();

    private static (int p33, int p67) EnsureTierBins()
    {
        lock (_tierLock)
        {
            if (_tierP33 >= 0) return (_tierP33, _tierP67);

            var content = Content.Content.Get();
            var catalog = content?.WorldCatalog;
            if (catalog == null) return (int.MaxValue, int.MaxValue);

            var prices = catalog.All()
                .Where(i => i.Price > 0 && i.DisableLevel == 0
                         && AllowedCategories.Contains((int)i.Category)
                         && !CatalogBlacklist.Contains(i.GUID))
                .Select(i => (int)i.Price)
                .OrderBy(p => p)
                .ToList();

            if (prices.Count == 0) return (int.MaxValue, int.MaxValue);

            _tierP33 = prices[(int)(prices.Count * 0.33)];
            _tierP67 = prices[(int)(prices.Count * 0.67)];
            return (_tierP33, _tierP67);
        }
    }

    /// <summary>
    /// Parses a category arg that may be an integer (12-20) or a slug name ("seating", etc.).
    /// Returns -1 if absent, 0 if parse failed (to be treated as invalid by the caller).
    /// </summary>
    private static int ParseCategoryArg(JsonNode node)
    {
        if (node == null) return -1;
        if (node is JsonValue v)
        {
            if (v.TryGetValue<long>(out var n)) return (int)n;
            if (v.TryGetValue<string>(out var s) && !string.IsNullOrEmpty(s))
            {
                if (CategorySlugMap.TryGetValue(s, out var catId)) return catId;
                if (int.TryParse(s, out var parsed)) return parsed;
            }
        }
        return 0;
    }

    /// <summary>
    /// Backs both <c>find-cheap-catalog-guid</c> (legacy shape) and <c>search-catalog</c>
    /// (new shape with filters). The <paramref name="verb"/> parameter selects the response
    /// shape; all safety filters (blacklist, DisableLevel==0, AllowedCategories) are always
    /// applied regardless of verb.
    ///
    /// <para><b>find-cheap-catalog-guid</b> args: max_price (int, default 500), limit (int, default 5).
    /// Response: <c>{verb, max_price, count, candidates:[{guid, price, name, category}]}</c></para>
    ///
    /// <para><b>search-catalog</b> args: name (string), category (int|slug), tier (string),
    /// min_price (int), max_price (int), limit (int, default 50, max 200).
    /// Response: <c>{verb, count, results:[{guid_hex, guid_decimal, name, price, category_id, category_name}],
    /// categories_summary:[{category_id, category_name, count}]}</c></para>
    /// </summary>
    internal static CommandDispatcher.Response FindCheapCatalogGuid(
        HeadlessVMHost vmHost, JsonObject args, string verb = "find-cheap-catalog-guid")
    {
        bool isSearchCatalog = verb == "search-catalog";

        try
        {
            var content = Content.Content.Get();
            var catalog = content?.WorldCatalog;
            if (catalog == null)
                return CommandDispatcher.Response.Fail($"{verb}: catalog not initialised");

            var all = catalog.All();
            if (all == null || all.Count == 0)
                return CommandDispatcher.Response.Fail($"{verb}: catalog empty");

            // Always-on safety filters.
            var baseQuery = all
                .Where(i => i.Price > 0 && i.DisableLevel == 0
                         && AllowedCategories.Contains((int)i.Category)
                         && !CatalogBlacklist.Contains(i.GUID));

            if (!isSearchCatalog)
            {
                // ---- legacy find-cheap-catalog-guid ----
                int maxPrice = (int)((long?)args["max_price"] ?? 500L);
                int limit = (int)((long?)args["limit"] ?? 5L);

                var candidates = baseQuery
                    .Where(i => i.Price <= maxPrice)
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
                    count = candidates.Count,
                    candidates,
                });
            }

            // ---- search-catalog ----
            // guid_hex: reverse-lookup a single catalog entry (freesoexperiment-289).
            // Bypasses the whitelist + blacklist so any GUID seen via query-lot-objects can
            // be resolved (objects like 0x1478FD75 are present on lots but outside the
            // roommate-purchaseable whitelist; full enumeration misses them).
            string guidHexFilter = args["guid_hex"] is JsonValue gv && gv.TryGetValue<string>(out var gs) ? gs : null;
            if (!string.IsNullOrEmpty(guidHexFilter))
            {
                var raw = guidHexFilter.Trim();
                if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                    raw.StartsWith("0X", StringComparison.Ordinal))
                    raw = raw.Substring(2);
                if (!uint.TryParse(raw, System.Globalization.NumberStyles.HexNumber,
                        System.Globalization.CultureInfo.InvariantCulture, out var guidParsed))
                {
                    return CommandDispatcher.Response.Fail(
                        $"search-catalog: malformed guid_hex '{guidHexFilter}' (expected 32-bit hex, optionally 0x-prefixed)");
                }

                // Bypass safety filters for reverse-lookup; agents asking about a GUID they
                // observed on a tile need an answer even if the item is non-purchaseable.
                var matchList = all
                    .Where(i => i.GUID == guidParsed)
                    .Select(i => (object)new
                    {
                        guid_hex     = "0x" + i.GUID.ToString("X8"),
                        guid_decimal = (long)(uint)i.GUID,
                        name         = i.Name ?? "",
                        price        = (int)i.Price,
                        category_id  = (int)i.Category,
                        category_name = RoomieWhiteListCategoryNames.TryGetValue((int)i.Category, out var cnGuid) ? cnGuid : "",
                    })
                    .ToList();

                var matchCatSummary = matchList.Count == 0
                    ? (object)Array.Empty<object>()
                    : new[]
                    {
                        new
                        {
                            category_id   = (int)((dynamic)matchList[0]).category_id,
                            category_name = (string)((dynamic)matchList[0]).category_name,
                            count         = 1,
                        }
                    };

                return CommandDispatcher.Response.Success(new
                {
                    verb               = "search-catalog",
                    count              = matchList.Count,
                    results            = matchList,
                    categories_summary = matchCatSummary,
                });
            }

            // Category filter: integer or slug.
            int catFilter = ParseCategoryArg(args["category"]);
            if (catFilter == 0)
                return CommandDispatcher.Response.Fail($"search-catalog: unrecognised category value: {args["category"]}");
            // catFilter == -1 → no filter (all categories)
            if (catFilter > 0 && !AllowedCategories.Contains(catFilter))
            {
                // Caller asked for a category outside the whitelist — return 0 results, not an error.
                return CommandDispatcher.Response.Success(new
                {
                    verb = "search-catalog",
                    count = 0,
                    results = Array.Empty<object>(),
                    categories_summary = Array.Empty<object>(),
                });
            }

            // Name filter.
            string nameFilter = args["name"] is JsonValue nv && nv.TryGetValue<string>(out var ns) ? ns : null;

            // Tier filter: cheap (≤P33), mid (P33..P67), expensive (>P67).
            string tierFilter = args["tier"] is JsonValue tv && tv.TryGetValue<string>(out var ts) ? ts : null;
            int? tierMin = null;
            int? tierMax = null;
            if (!string.IsNullOrEmpty(tierFilter))
            {
                var (p33, p67) = EnsureTierBins();
                if (tierFilter.Equals("cheap", StringComparison.OrdinalIgnoreCase))
                    tierMax = p33;
                else if (tierFilter.Equals("mid", StringComparison.OrdinalIgnoreCase) ||
                         tierFilter.Equals("medium", StringComparison.OrdinalIgnoreCase))
                { tierMin = p33 + 1; tierMax = p67; }
                else if (tierFilter.Equals("expensive", StringComparison.OrdinalIgnoreCase))
                    tierMin = p67 + 1;
                else
                    return CommandDispatcher.Response.Fail($"search-catalog: unknown tier '{tierFilter}'; use cheap|mid|expensive");
            }

            int? minPrice = args["min_price"] != null ? (int)(long)args["min_price"] : (int?)null;
            int? searchMaxPrice = args["max_price"] != null ? (int)(long)args["max_price"] : (int?)null;

            // Server-side limit clamp: max 200.
            int limitRaw = (int)((long?)args["limit"] ?? 50L);
            int searchLimit = Math.Clamp(limitRaw, 1, 200);

            var filtered = baseQuery;
            if (catFilter > 0)
                filtered = filtered.Where(i => (int)i.Category == catFilter);
            if (!string.IsNullOrEmpty(nameFilter))
                filtered = filtered.Where(i => (i.Name ?? "").Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
            if (tierMin.HasValue)
                filtered = filtered.Where(i => (int)i.Price >= tierMin.Value);
            if (tierMax.HasValue)
                filtered = filtered.Where(i => (int)i.Price <= tierMax.Value);
            if (minPrice.HasValue)
                filtered = filtered.Where(i => (int)i.Price >= minPrice.Value);
            if (searchMaxPrice.HasValue)
                filtered = filtered.Where(i => (int)i.Price <= searchMaxPrice.Value);

            var resultList = filtered
                .OrderBy(i => i.Price)
                .Take(searchLimit)
                .Select(i => (object)new
                {
                    guid_hex     = "0x" + i.GUID.ToString("X8"),
                    guid_decimal = (long)(uint)i.GUID,
                    name         = i.Name ?? "",
                    price        = (int)i.Price,
                    category_id  = (int)i.Category,
                    category_name = RoomieWhiteListCategoryNames.TryGetValue((int)i.Category, out var cn) ? cn : "",
                })
                .ToList();

            // categories_summary: all categories present in the full (base+cat+name+tier+price) result set
            // before the Take limit — but we want them from the unrestricted-by-limit query so it
            // reflects the dataset, not just the page. Recompute from filtered (no Take).
            var catSummary = filtered
                .GroupBy(i => (int)i.Category)
                .Select(g => (object)new
                {
                    category_id   = g.Key,
                    category_name = RoomieWhiteListCategoryNames.TryGetValue(g.Key, out var cn2) ? cn2 : "",
                    count         = g.Count(),
                })
                .OrderBy(o => ((dynamic)o).category_id)
                .ToList();

            return CommandDispatcher.Response.Success(new
            {
                verb    = "search-catalog",
                count   = resultList.Count,
                results = resultList,
                categories_summary = catSummary,
            });
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"{verb}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ---- list-catalog-categories ----

    /// <summary>
    /// Returns the set of whitelisted catalog categories (12..20) that have at least one
    /// purchaseable, non-blacklisted item in <see cref="Content.WorldCatalog"/>, along with
    /// item counts. Useful for agents to discover what categories are browsable before
    /// calling <c>search-catalog --category</c>.
    /// </summary>
    internal static CommandDispatcher.Response ListCatalogCategories(HeadlessVMHost vmHost, JsonObject args)
    {
        try
        {
            var content = Content.Content.Get();
            var catalog = content?.WorldCatalog;
            if (catalog == null)
                return CommandDispatcher.Response.Fail("list-catalog-categories: catalog not initialised");

            var all = catalog.All();
            if (all == null || all.Count == 0)
                return CommandDispatcher.Response.Fail("list-catalog-categories: catalog empty");

            var categories = all
                .Where(i => i.Price > 0 && i.DisableLevel == 0
                         && AllowedCategories.Contains((int)i.Category)
                         && !CatalogBlacklist.Contains(i.GUID))
                .GroupBy(i => (int)i.Category)
                .Select(g => (object)new
                {
                    category_id   = g.Key,
                    category_name = RoomieWhiteListCategoryNames.TryGetValue(g.Key, out var cn) ? cn : "",
                    slug          = RoomieWhiteListCategoryNames.TryGetValue(g.Key, out var slug) ? slug : "",
                    count         = g.Count(),
                })
                .OrderBy(o => ((dynamic)o).category_id)
                .ToList();

            return CommandDispatcher.Response.Success(new
            {
                verb       = "list-catalog-categories",
                count      = categories.Count,
                categories,
            });
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"list-catalog-categories: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
