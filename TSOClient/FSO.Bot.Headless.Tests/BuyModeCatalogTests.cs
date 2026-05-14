/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Bot.Headless;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Feature integration tests for <see cref="BuyModeHandlers.FindCheapCatalogGuid"/>
/// (the shared entry point for both <c>find-cheap-catalog-guid</c> and
/// <c>search-catalog</c>) (freesoexperiment-281a).
///
/// <para>
/// <b>Ground-source-truth requirement (OS CLAUDE.md §10):</b>
/// These tests MUST load the real <c>Content.WorldCatalog</c> via
/// <c>FSO.Content.Content.Init</c>. Any mock of <c>WorldCatalog.All()</c> or a fake
/// catalog response is an automatic veracity fail (item spec §"Veracity commitment").
/// </para>
///
/// <para>
/// <b>Gate:</b> gated on <c>FSO_INTEGRATION=1</c> (same as ChargenHandlerTests) because
/// <c>Content.Init</c> requires the TSO game assets at
/// <c>FSO_GAME_LOCATION</c> (default: <c>/home/baron/projects/freeso-experiment/GameAssets/</c>).
/// Skipped in unit-test CI runs that don't mount the asset volume.
/// </para>
///
/// <para>
/// <b>Tests:</b>
/// <list type="bullet">
///   <item>T1: <c>search-catalog</c> with no args returns ≥1 result + non-empty categories_summary.</item>
///   <item>T2: <c>search-catalog --category 14 --tier cheap</c> returns only items with
///     category_id==14 AND price ≤ P33 (computed inline from live catalog).</item>
///   <item>T3: <c>search-catalog --limit 1000</c> returns ≤200 results (clamp verification).</item>
///   <item>T4: <c>search-catalog --category 5</c> returns 0 results (outside whitelist 12..20).</item>
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
            var loc = Environment.GetEnvironmentVariable("FSO_GAME_LOCATION")
                      ?? "/home/baron/projects/freeso-experiment/GameAssets/";
            if (!loc.EndsWith(Path.DirectorySeparatorChar)
                && !loc.EndsWith(Path.AltDirectorySeparatorChar))
                loc += Path.DirectorySeparatorChar;
            return loc;
        }
    }

    // One-time content init shared across all four tests in this collection.
    // Lazy so it only runs when tests actually execute (not on skip path).
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

    /// <summary>
    /// Allocates a fake HeadlessVMHost whose VM is null (search-catalog reads Content
    /// directly, not through the VM — no tick lock required). Providing a null vmHost
    /// doesn't matter here; the catalog read path goes through Content.Content.Get(),
    /// not through vmHost.VM.
    /// </summary>
    private static CommandDispatcher.Response CallSearchCatalog(JsonObject args)
    {
        // BuyModeHandlers.FindCheapCatalogGuid reads Content.Content.Get() directly.
        // It does NOT access vmHost.VM — pass null safely for catalog-only tests.
        return BuyModeHandlers.FindCheapCatalogGuid(null, args);
    }

    // ---- T1: no-args returns results + categories_summary ----

    /// <summary>
    /// T1 (feature): <c>search-catalog</c> with no args returns ≥1 result
    /// and a non-empty categories_summary, verifying real WorldCatalog load.
    /// </summary>
    [SkippableFact]
    public void T1_NoArgs_ReturnsResultsAndCategoriesSummary()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var resp = CallSearchCatalog(new JsonObject());

        Assert.True(resp.Ok, $"search-catalog no-args failed: {resp.Error}");

        var payload = resp.Payload as JsonObject
                      ?? JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(resp.Payload))?.AsObject();
        Assert.NotNull(payload);

        // verb field
        Assert.Equal("search-catalog", (string)payload["verb"]);

        // results: non-empty list
        var results = payload["results"]?.AsArray();
        Assert.NotNull(results);
        Assert.True(results.Count >= 1, $"expected ≥1 result; got {results.Count}");

        // Each result must have the required shape fields
        foreach (var item in results)
        {
            var obj = item.AsObject();
            Assert.NotNull(obj["guid_hex"]);
            Assert.NotNull(obj["guid_decimal"]);
            Assert.NotNull(obj["name"]);
            Assert.NotNull(obj["price"]);
            Assert.NotNull(obj["category_id"]);
            Assert.NotNull(obj["category_name"]);
            // guid_hex must start with "0x"
            Assert.StartsWith("0x", (string)obj["guid_hex"], StringComparison.OrdinalIgnoreCase);
        }

        // categories_summary: non-empty
        var catSummary = payload["categories_summary"]?.AsArray();
        Assert.NotNull(catSummary);
        Assert.True(catSummary.Count >= 1, $"expected ≥1 category in categories_summary; got {catSummary.Count}");

        // Each summary entry must have category_id, category_name, count
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

        // Compute P33 inline from the live catalog (same dataset the handler uses).
        var content = FSO.Content.Content.Get();
        var catalog = content.WorldCatalog;
        Assert.NotNull(catalog);

        var allowedCategories = new HashSet<int> { 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var blacklist = new HashSet<uint> { 0x24C95F99u };

        var allPrices = catalog.All()
            .Where(i => i.Price > 0)
            .Where(i => i.DisableLevel == 0)
            .Where(i => allowedCategories.Contains((int)i.Category))
            .Where(i => !blacklist.Contains(i.GUID))
            .Select(i => (int)i.Price)
            .OrderBy(p => p)
            .ToList();

        Assert.True(allPrices.Count >= 3, $"catalog has too few whitelisted items ({allPrices.Count}) to compute P33");

        int p33 = allPrices[(int)(allPrices.Count * 0.33)];

        var args = new JsonObject
        {
            ["category"] = JsonValue.Create(14L),
            ["tier"] = JsonValue.Create("cheap"),
            ["limit"] = JsonValue.Create(200L),
        };
        var resp = CallSearchCatalog(args);

        Assert.True(resp.Ok, $"search-catalog --category 14 --tier cheap failed: {resp.Error}");

        var payload = resp.Payload as JsonObject
                      ?? JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(resp.Payload))?.AsObject();
        Assert.NotNull(payload);

        var results = payload["results"]?.AsArray();
        Assert.NotNull(results);

        // category 14 might have no cheap items on some asset snapshots — skip content
        // assertion when empty but do NOT skip if results exist.
        foreach (var item in results)
        {
            var obj = item.AsObject();
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

        var payload = resp.Payload as JsonObject
                      ?? JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(resp.Payload))?.AsObject();
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

        // The handler should succeed with 0 results (not a hard error),
        // because category=5 is a valid integer — just outside the whitelist.
        Assert.True(resp.Ok, $"search-catalog --category 5 failed unexpectedly: {resp.Error}");

        var payload = resp.Payload as JsonObject
                      ?? JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(resp.Payload))?.AsObject();
        Assert.NotNull(payload);

        var results = payload["results"]?.AsArray();
        Assert.NotNull(results);
        Assert.Empty(results);
    }
}

/// <summary>
/// Feature integration tests for <see cref="BuyModeHandlers.ListCatalogCategories"/>
/// (freesoexperiment-2d1).
///
/// <para>
/// <b>Ground-source-truth requirement (OS CLAUDE.md §10):</b>
/// Tests load the real <c>Content.WorldCatalog</c> via <c>FSO.Content.Content.Init</c>.
/// No mocking of catalog data is acceptable (item spec §"Veracity commitment").
/// </para>
///
/// <para>
/// <b>Gate:</b> gated on <c>FSO_INTEGRATION=1</c> because <c>Content.Init</c> requires
/// TSO game assets at <c>FSO_GAME_LOCATION</c>.
/// </para>
///
/// <para>
/// <b>Tests:</b>
/// <list type="bullet">
///   <item>T1: <c>list-catalog-categories</c> returns ≥1 category, every category_id ∈ {12..20},
///     and the sum of counts equals the independent catalog count.</item>
///   <item>T2: a category with zero qualifying items is omitted from the response (not returned with count=0).</item>
/// </list>
/// </para>
/// </summary>
[Collection("list-catalog-categories-integration")]
public sealed class ListCatalogCategoriesTests
{
    private static readonly bool IntegrationEnabled =
        Environment.GetEnvironmentVariable("FSO_INTEGRATION") == "1";

    private static string GameLocation
    {
        get
        {
            var loc = Environment.GetEnvironmentVariable("FSO_GAME_LOCATION")
                      ?? "/home/baron/projects/freeso-experiment/GameAssets/";
            if (!loc.EndsWith(Path.DirectorySeparatorChar)
                && !loc.EndsWith(Path.AltDirectorySeparatorChar))
                loc += Path.DirectorySeparatorChar;
            return loc;
        }
    }

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

    // ---- T1: returns ≥1 category, all IDs ∈ 12..20, counts are consistent ----

    /// <summary>
    /// T1 (feature): <c>list-catalog-categories</c> returns ≥1 category.
    /// Every returned <c>category_id</c> ∈ {12..20}. The sum of all counts equals
    /// the independent catalog count (ground-source-truth assertion).
    /// </summary>
    [SkippableFact]
    public void T1_ReturnsCategoriesWithConsistentCounts()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var resp = BuyModeHandlers.ListCatalogCategories(null, new JsonObject());

        Assert.True(resp.Ok, $"list-catalog-categories failed: {resp.Error}");

        var payload = resp.Payload as JsonObject
                      ?? JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(resp.Payload))?.AsObject();
        Assert.NotNull(payload);

        Assert.Equal("list-catalog-categories", (string)payload["verb"]);

        var categories = payload["categories"]?.AsArray();
        Assert.NotNull(categories);
        Assert.True(categories.Count >= 1, $"expected ≥1 category; got {categories.Count}");

        var allowedIds = new HashSet<int> { 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        int sumFromResponse = 0;
        foreach (var entry in categories)
        {
            var obj = entry.AsObject();
            var catId = (int)obj["category_id"];
            Assert.True(allowedIds.Contains(catId),
                $"category_id {catId} is outside the roommate whitelist 12..20");
            Assert.False(string.IsNullOrEmpty((string)obj["category_name"]),
                $"category_id {catId} has empty category_name");
            var count = (int)obj["count"];
            Assert.True(count > 0,
                $"category_id {catId} returned with count=0; zero-count categories must be omitted");
            sumFromResponse += count;
        }

        // Ground-source-truth: compute expected count independently from live catalog.
        var content = FSO.Content.Content.Get();
        var catalog = content.WorldCatalog;
        var blacklist = new HashSet<uint> { 0x24C95F99u };
        int expectedTotal = catalog.All()
            .Where(i => i.DisableLevel == 0)
            .Where(i => allowedIds.Contains((int)i.Category))
            .Where(i => !blacklist.Contains(i.GUID))
            .Count();

        Assert.Equal(expectedTotal, sumFromResponse);
    }

    // ---- T2: zero-count category is omitted ----

    /// <summary>
    /// T2 (negative): a category with zero qualifying items must be omitted from the response.
    /// Verified by asserting that every returned entry has count > 0, AND that the response
    /// contains no duplicate category_ids (which would signal a grouping bug).
    /// </summary>
    [SkippableFact]
    public void T2_ZeroCountCategoryOmitted()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var resp = BuyModeHandlers.ListCatalogCategories(null, new JsonObject());

        Assert.True(resp.Ok, $"list-catalog-categories failed: {resp.Error}");

        var payload = resp.Payload as JsonObject
                      ?? JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(resp.Payload))?.AsObject();
        Assert.NotNull(payload);

        var categories = payload["categories"]?.AsArray();
        Assert.NotNull(categories);

        var seenIds = new HashSet<int>();
        foreach (var entry in categories)
        {
            var obj = entry.AsObject();
            var catId = (int)obj["category_id"];
            var count = (int)obj["count"];
            Assert.True(count > 0,
                $"category_id {catId} has count=0 — zero-count categories must be omitted (not returned)");
            Assert.True(seenIds.Add(catId),
                $"category_id {catId} appears more than once in response");
        }
    }
}
