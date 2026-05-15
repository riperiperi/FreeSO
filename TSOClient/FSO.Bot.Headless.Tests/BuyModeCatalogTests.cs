/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FSO.Bot.Headless;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Feature integration tests for <see cref="BuyModeHandlers.FindCheapCatalogGuid"/>
/// (shared entry point for both <c>find-cheap-catalog-guid</c> and <c>search-catalog</c>)
/// (freesoexperiment-281a).
///
/// <para>
/// <b>Ground-source-truth requirement (OS CLAUDE.md §10):</b>
/// Tests T1–T4 MUST load the real <c>Content.WorldCatalog</c> via
/// <c>FSO.Content.Content.Init</c>. Any mock or stub of <c>WorldCatalog.All()</c>
/// is an automatic veracity fail per item spec §"Veracity commitment".
/// </para>
///
/// <para>
/// <b>Gate:</b> gated on <c>FSO_INTEGRATION=1</c> because <c>Content.Init</c> requires
/// the TSO game assets at <c>FSO_GAME_LOCATION</c>
/// (default: <c>/home/baron/projects/freeso-experiment/GameAssets/</c>).
/// Skipped in unit-test CI runs that don't mount the asset volume.
/// </para>
///
/// <para>
/// <b>Tests:</b>
/// <list type="bullet">
///   <item>T1: no-args returns ≥1 result + non-empty categories_summary (real catalog load).</item>
///   <item>T2: --category 14 --tier cheap returns only category_id==14 AND price ≤ P33.</item>
///   <item>T3: --limit 1000 returns ≤200 results (server-side clamp).</item>
///   <item>T4: --category 5 returns 0 results (outside whitelist 12..20).</item>
/// </list>
/// </para>
/// </summary>
[Collection("search-catalog-integration")]
public sealed class BuyModeCatalogTests
{
    private static readonly bool IntegrationEnabled =
        Environment.GetEnvironmentVariable("FSO_INTEGRATION") == "1";

    private static string GameLocation
    {
        get
        {
            // basePath must be the TSOClient/ dir (the FSO server's "/game" mount target,
            // see docker/docker-compose.yml). Content.Init looks for tuning.dat directly
            // under basePath; pointing at GameAssets/ (the parent) fails to find it.
            var loc = Environment.GetEnvironmentVariable("FSO_GAME_LOCATION")
                      ?? "/home/baron/projects/freeso-experiment/GameAssets/TSOClient/";
            if (!loc.EndsWith(Path.DirectorySeparatorChar)
                && !loc.EndsWith(Path.AltDirectorySeparatorChar))
                loc += Path.DirectorySeparatorChar;
            return loc;
        }
    }

    // One-time content init shared across all four tests in this collection.
    private static bool _contentInitDone;
    private static readonly object _initLock = new();

    private static bool EnsureContentInit(out string skipReason)
    {
        lock (_initLock)
        {
            if (_contentInitDone) { skipReason = null; return true; }
            try
            {
                FSO.SimAntics.VMContext.InitVMConfig(false);
                FSO.Content.Content.Init(GameLocation, FSO.Content.ContentMode.SERVER);
                _contentInitDone = true;
                skipReason = null;
                return true;
            }
            catch (Exception ex)
            {
                skipReason = $"Content.Init failed — game assets not available: {ex.Message}";
                return false;
            }
        }
    }

    // FindCheapCatalogGuid reads Content.Content.Get() directly — vmHost not accessed.
    private static CommandDispatcher.Response CallSearchCatalog(JsonObject args)
        => BuyModeHandlers.FindCheapCatalogGuid(null, args, verb: "search-catalog");

    // ---- helpers ----

    private static JsonObject PayloadOf(CommandDispatcher.Response resp)
    {
        if (resp.Payload is JsonObject jo) return jo;
        return JsonNode.Parse(JsonSerializer.Serialize(resp.Payload))?.AsObject();
    }

    // ---- T1: no-args returns results + categories_summary ----

    /// <summary>
    /// T1 (feature): <c>search-catalog</c> with no args returns ≥1 result
    /// and a non-empty categories_summary. Verifies real WorldCatalog load.
    /// </summary>
    [SkippableFact]
    public void T1_NoArgs_ReturnsResultsAndCategoriesSummary()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var resp = CallSearchCatalog(new JsonObject());

        Assert.True(resp.Ok, $"search-catalog no-args failed: {resp.Error}");
        var payload = PayloadOf(resp);
        Assert.NotNull(payload);
        Assert.Equal("search-catalog", (string)payload["verb"]);

        // results: non-empty
        var results = payload["results"]?.AsArray();
        Assert.NotNull(results);
        Assert.True(results.Count >= 1, $"expected ≥1 result; got {results.Count}");

        foreach (var item in results)
        {
            var obj = item.AsObject();
            Assert.NotNull(obj["guid_hex"]);
            Assert.NotNull(obj["guid_decimal"]);
            Assert.NotNull(obj["name"]);
            Assert.NotNull(obj["price"]);
            Assert.NotNull(obj["category_id"]);
            Assert.NotNull(obj["category_name"]);
            Assert.StartsWith("0x", (string)obj["guid_hex"], StringComparison.OrdinalIgnoreCase);
        }

        // categories_summary: non-empty
        var catSummary = payload["categories_summary"]?.AsArray();
        Assert.NotNull(catSummary);
        Assert.True(catSummary.Count >= 1, $"expected ≥1 category in categories_summary; got {catSummary.Count}");

        foreach (var entry in catSummary)
        {
            var obj = entry.AsObject();
            Assert.NotNull(obj["category_id"]);
            Assert.NotNull(obj["category_name"]);
            Assert.NotNull(obj["count"]);
        }
    }

    // ---- T2: category + tier filter ----

    /// <summary>
    /// T2 (feature): <c>search-catalog --category 14 --tier cheap</c> returns only items
    /// with category_id==14 AND price ≤ P33 (computed inline from the live catalog).
    /// The P33 threshold is derived from the same dataset the handler uses — no hardcoding.
    /// </summary>
    [SkippableFact]
    public void T2_CategoryAndTierCheap_ReturnsOnlyMatchingItems()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        // Compute P33 inline from the live catalog (same dataset as the handler).
        var catalog = FSO.Content.Content.Get().WorldCatalog;
        Assert.NotNull(catalog);

        var allowedCategories = new HashSet<int> { 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var blacklist = new HashSet<uint> { 0x24C95F99u };

        var allPrices = catalog.All()
            .Where(i => i.Price > 0 && i.DisableLevel == 0
                     && allowedCategories.Contains((int)i.Category)
                     && !blacklist.Contains(i.GUID))
            .Select(i => (int)i.Price)
            .OrderBy(p => p)
            .ToList();

        Assert.True(allPrices.Count >= 3,
            $"catalog has too few whitelisted items ({allPrices.Count}) to compute P33");

        int p33 = allPrices[(int)(allPrices.Count * 0.33)];

        var args = new JsonObject
        {
            ["category"] = JsonValue.Create(14L),
            ["tier"]     = JsonValue.Create("cheap"),
            ["limit"]    = JsonValue.Create(200L),
        };
        var resp = CallSearchCatalog(args);

        Assert.True(resp.Ok, $"search-catalog --category 14 --tier cheap failed: {resp.Error}");
        var payload = PayloadOf(resp);
        Assert.NotNull(payload);

        var results = payload["results"]?.AsArray();
        Assert.NotNull(results);

        foreach (var item in results)
        {
            var obj   = item.AsObject();
            var catId = (int)obj["category_id"];
            var price = (int)obj["price"];
            Assert.Equal(14, catId);
            Assert.True(price <= p33,
                $"tier=cheap item has price {price} > P33={p33}: {(string)obj["name"]}");
        }
    }

    // ---- T3: limit clamp ----

    /// <summary>
    /// T3 (negative): <c>search-catalog --limit 1000</c> returns ≤200 results.
    /// Verifies server-side limit clamp of max 200 (item spec §Constraints).
    /// </summary>
    [SkippableFact]
    public void T3_LimitOver200_ClampsTo200()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var args = new JsonObject { ["limit"] = JsonValue.Create(1000L) };
        var resp = CallSearchCatalog(args);

        Assert.True(resp.Ok, $"search-catalog --limit 1000 failed: {resp.Error}");
        var payload = PayloadOf(resp);
        Assert.NotNull(payload);

        var results = payload["results"]?.AsArray();
        Assert.NotNull(results);
        Assert.True(results.Count <= 200,
            $"limit clamp failed: got {results.Count} results, expected ≤200");
    }

    // ---- T4: category outside whitelist returns 0 results ----

    /// <summary>
    /// T4 (negative): <c>search-catalog --category 5</c> (outside whitelist 12..20)
    /// returns 0 results. Verifies the safety filter is preserved.
    /// </summary>
    [SkippableFact]
    public void T4_CategoryOutsideWhitelist_ReturnsZeroResults()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var args = new JsonObject { ["category"] = JsonValue.Create(5L) };
        var resp = CallSearchCatalog(args);

        // Succeeds with 0 results (category 5 is valid int but outside whitelist).
        Assert.True(resp.Ok, $"search-catalog --category 5 failed unexpectedly: {resp.Error}");
        var payload = PayloadOf(resp);
        Assert.NotNull(payload);

        var results = payload["results"]?.AsArray();
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    // ---- T5–T8: --guid_hex catalog ↔ instance reverse lookup (freesoexperiment-fe1) ----
    //
    // TSO multi-tile objects have two GUIDs: the catalog GUID (master OBJD; what
    // buy-object expects) and one or more instance GUIDs (subordinate OBJDs; what
    // query-lot-objects reports for placed BaseObjects). Pre-fe1, --guid_hex only
    // resolved the catalog GUID; pasting an instance GUID got count:0 and forced
    // brute enumeration. T5–T8 lock in the bidirectional contract.
    //
    // T5: catalog GUID still resolves (regression for -289).
    // T6: instance GUID resolves to the same catalog entry (the fix).
    // T7: malformed/unknown GUID returns count:0.
    // T8: instance_guids array contains both the catalog GUID and ≥1 subordinate
    //     GUID for a known multitile item.

    // Catalog GUID for "Bed - Double - Castle" — verified on the live workshop catalog
    // (2026-05-15). This is the master OBJD's GUID; what buy-object accepts.
    private const string BedCastleCatalogGuid = "0x17579980";
    // BaseObject's OBJD GUID for the same bed when placed — verified via
    // query-lot-objects on workshop lot 2 (master bedroom).
    private const string BedCastleInstanceGuid = "0x1478FD75";

    /// <summary>
    /// T5 (regression — preserves -289 behaviour): passing the catalog GUID returns
    /// the matching catalog entry with both <c>guid_hex</c> (catalog) and
    /// <c>instance_guid_hex</c> (echoes the input) populated.
    /// </summary>
    [SkippableFact]
    public void T5_GuidHexCatalogGuid_ReverseLookupReturnsEntry()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var args = new JsonObject { ["guid_hex"] = JsonValue.Create(BedCastleCatalogGuid) };
        var resp = CallSearchCatalog(args);

        Assert.True(resp.Ok, $"search-catalog --guid_hex {BedCastleCatalogGuid} failed: {resp.Error}");
        var payload = PayloadOf(resp);
        Assert.NotNull(payload);
        Assert.Equal(1, (int)payload["count"]);

        var item = payload["results"]?.AsArray()[0]?.AsObject();
        Assert.NotNull(item);
        Assert.Equal(BedCastleCatalogGuid, (string)item["guid_hex"], ignoreCase: true);
        Assert.Equal(BedCastleCatalogGuid, (string)item["instance_guid_hex"], ignoreCase: true);
        Assert.Contains("Bed", (string)item["name"]);
        Assert.Contains("Castle", (string)item["name"]);
    }

    /// <summary>
    /// T6 (the fix — freesoexperiment-fe1): passing the instance GUID
    /// (subordinate OBJD GUID returned by query-lot-objects) resolves to the
    /// same catalog entry as the catalog-GUID query. <c>guid_hex</c> reports the
    /// catalog GUID; <c>instance_guid_hex</c> echoes the user's input.
    /// </summary>
    [SkippableFact]
    public void T6_GuidHexInstanceGuid_ReverseLookupReturnsCatalogEntry()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var args = new JsonObject { ["guid_hex"] = JsonValue.Create(BedCastleInstanceGuid) };
        var resp = CallSearchCatalog(args);

        Assert.True(resp.Ok, $"search-catalog --guid_hex {BedCastleInstanceGuid} failed: {resp.Error}");
        var payload = PayloadOf(resp);
        Assert.NotNull(payload);
        Assert.Equal(1, (int)payload["count"]);

        var item = payload["results"]?.AsArray()[0]?.AsObject();
        Assert.NotNull(item);
        Assert.Equal(BedCastleCatalogGuid, (string)item["guid_hex"], ignoreCase: true);
        Assert.Equal(BedCastleInstanceGuid, (string)item["instance_guid_hex"], ignoreCase: true);
        Assert.Contains("Bed", (string)item["name"]);
        Assert.Contains("Castle", (string)item["name"]);
    }

    /// <summary>
    /// T7 (negative): a clearly-fake GUID misses both the catalog-direct lookup
    /// and the instance reverse map, returning <c>count:0</c>.
    /// </summary>
    [SkippableFact]
    public void T7_GuidHexUnknown_ReturnsZero()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var args = new JsonObject { ["guid_hex"] = JsonValue.Create("0xDEADBEEF") };
        var resp = CallSearchCatalog(args);

        Assert.True(resp.Ok, $"search-catalog --guid_hex 0xDEADBEEF failed: {resp.Error}");
        var payload = PayloadOf(resp);
        Assert.NotNull(payload);
        Assert.Equal(0, (int)payload["count"]);
        Assert.Empty(payload["results"].AsArray());
    }

    /// <summary>
    /// T8: the <c>instance_guids</c> array enumerates every OBJD GUID in the
    /// catalog item's iff that belongs to the same multitile group (master +
    /// subordinates). For a multitile bed this must contain the catalog GUID
    /// AND the instance GUID we observed on workshop lot 2.
    /// </summary>
    [SkippableFact]
    public void T8_GuidHex_InstanceGuidsArrayContainsBothCatalogAndSubordinate()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var args = new JsonObject { ["guid_hex"] = JsonValue.Create(BedCastleCatalogGuid) };
        var resp = CallSearchCatalog(args);

        Assert.True(resp.Ok, $"search-catalog --guid_hex {BedCastleCatalogGuid} failed: {resp.Error}");
        var payload = PayloadOf(resp);
        var item = payload["results"]?.AsArray()[0]?.AsObject();
        Assert.NotNull(item);

        var instanceGuids = item["instance_guids"]?.AsArray()
            ?.Select(n => (string)n)
            .ToList();
        Assert.NotNull(instanceGuids);
        Assert.Contains(BedCastleCatalogGuid, instanceGuids, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(BedCastleInstanceGuid, instanceGuids, StringComparer.OrdinalIgnoreCase);
        Assert.True(instanceGuids.Count >= 2,
            $"multitile bed should have ≥2 entries in instance_guids; got {instanceGuids.Count}");
    }
}
