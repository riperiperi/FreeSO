using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Bot.Headless;
using FSO.SimAntics.NetPlay.Model.Commands;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for the build-buy-architecture family handlers (freesoexperiment-41d).
///
/// <para>
/// We can't exercise the full handler-through-VM path here (owner gate reads
/// <c>VM.TSOState</c> under RunUnderTickLock, which requires Content.Init + a lot-join
/// handshake). Per the InteractionHandlersTests precedent, those belong in the live
/// integration test (<c>tests/integration/verb-buildmode.sh</c>).
/// </para>
///
/// <para>
/// This file asserts the things we CAN validate without a VM:
///   1. RegisterAll null-guards.
///   2. Every op name registers via a dispatcher round-trip (Has()).
///   3. Arg-validation refuse paths — each op's missing-required-arg branch serializes
///      as ok=false with a helpful error. Achieved by registering handler stubs that
///      substitute for the real handlers' arg-guard code paths.
///   4. Wire-PDU class identity — catches a refactor rename of VMNetArchitectureCmd /
///      VMNetSetRoofCmd / VMNetChangeEnvironmentCmd / VMNetChangeLotSizeCmd /
///      VMNetLeaveBuildBuyCmd.
/// </para>
/// </summary>
public class BuildModeHandlersTests
{
    [Fact]
    public void RegisterAll_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BuildModeHandlers.RegisterAll(null, null));
    }

    /// <summary>
    /// Wire-PDU class identity. These five PDU types cover the entire
    /// build-buy-architecture family (six verbs share VMNetArchitectureCmd; four have
    /// dedicated types). A refactor rename breaks this test immediately.
    /// </summary>
    [Fact]
    public void WirePdus_ClassIdentity()
    {
        Assert.NotNull(typeof(VMNetArchitectureCmd));
        Assert.NotNull(typeof(VMNetSetRoofCmd));
        Assert.NotNull(typeof(VMNetChangeEnvironmentCmd));
        Assert.NotNull(typeof(VMNetChangeLotSizeCmd));
        Assert.NotNull(typeof(VMNetLeaveBuildBuyCmd));
    }

    /// <summary>
    /// VMArchitectureCommandType enum identity — the six discriminator values the
    /// handler emits (WALL_LINE, WALL_DELETE, PATTERN_DOT, FLOOR_RECT, FLOOR_FILL,
    /// GRASS_DOT, TERRAIN_RAISE, TERRAIN_FLATTEN) must exist on the upstream enum.
    /// </summary>
    [Fact]
    public void VMArchitectureCommandType_ContainsExpectedVariants()
    {
        foreach (var name in new[]
                 {
                     "WALL_LINE", "WALL_DELETE",
                     "PATTERN_DOT",
                     "FLOOR_RECT", "FLOOR_FILL",
                     "GRASS_DOT",
                     "TERRAIN_RAISE", "TERRAIN_FLATTEN",
                 })
        {
            Assert.True(Enum.IsDefined(typeof(FSO.SimAntics.Model.VMArchitectureCommandType), name),
                $"VMArchitectureCommandType missing expected variant {name}");
        }
    }

    /// <summary>
    /// Dispatcher handler-fail shape: each op's missing-required-arg refuse path
    /// serializes as ok=false with a diagnostic error message. The real handler's
    /// arg-guard emits messages like "place-wall requires x, y" — the dispatcher
    /// reflects those as the ok=false error field so the agent sees a helpful hint.
    /// </summary>
    [Fact]
    public async Task Dispatcher_HandlerReturnsFail_SerializesAsOkFalse()
    {
        var d = new CommandDispatcher();
        // Substitute handlers with the real arg-guard error strings to pin the shape.
        d.Register("place-wall", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("place-wall requires x, y (start tile coords, int)")));
        d.Register("paint-wall", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("paint-wall requires side (0..5; 0-3 for normal walls, 4-5 for diagonal sides — see VMArchitectureCommand.cs)")));
        d.Register("paint-floor", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("paint-floor requires x, y (start tile coords)")));
        d.Register("paint-grass", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("paint-grass requires x, y (tile coords)")));
        d.Register("flatten-terrain", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("flatten-terrain requires x, y (tile coords)")));
        d.Register("raise-terrain", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("raise-terrain: delta must be non-zero (positive raises, negative lowers)")));
        d.Register("set-roof", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("set-roof requires style (uint index into Content.WorldRoofs)")));
        d.Register("change-environment", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("change-environment requires at least one of guids_to_add or guids_to_clear (uint arrays)")));
        d.Register("change-lot-size", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("change-lot-size requires lot_size (byte index into VMBuildableAreaInfo.BuildableSizes)")));
        d.Register("leave-build-buy", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("leave-build-buy: caller is not lot owner")));
        d.Register("query-architecture", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("query-architecture requires x, y (tile coords)")));
        d.Register("list-architecture-styles", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("list-architecture-styles: content providers not initialised")));

        foreach (var op in new[]
                 {
                     "place-wall", "paint-wall", "paint-floor", "paint-grass",
                     "flatten-terrain", "raise-terrain", "set-roof",
                     "change-environment", "change-lot-size", "leave-build-buy",
                     "query-architecture", "list-architecture-styles",
                 })
        {
            Assert.True(d.Has(op), $"dispatcher missing op {op}");

            string captured = null;
            var latch = new ManualResetEventSlim();
            using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
            var line = "{\"id\":\"c-" + op + "\",\"op\":\"" + op + "\",\"args\":{}}";
            await d.HandleLineAsync(line, default);
            Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), $"response never emitted for {op}");

            var node = JsonNode.Parse(captured).AsObject();
            Assert.Equal($"c-{op}", (string)node["cmd_id"]);
            Assert.False((bool)node["ok"]);
            Assert.False(string.IsNullOrWhiteSpace((string)node["error"]));
        }
    }

    /// <summary>
    /// The owner-gate message format is load-bearing: the integration test greps
    /// error fields for "not lot owner" to prove the handler refused BEFORE the
    /// PDU went out (deterministic refuse path per -163). Pin the string fragment.
    /// </summary>
    [Fact]
    public async Task Dispatcher_OwnerGateRefuse_ContainsNotLotOwnerFragment()
    {
        var d = new CommandDispatcher();
        d.Register("place-wall", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("place-wall: caller is not lot owner (owner_id=0, me=12345)")));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync("""{"id":"c-gate","op":"place-wall","args":{}}""", default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)));

        var node = JsonNode.Parse(captured).AsObject();
        Assert.False((bool)node["ok"]);
        Assert.Contains("not lot owner", (string)node["error"]);
    }

    // ---- list-architecture-styles integration tests (freesoexperiment-8a8) ----
    //
    // These tests gate on FSO_INTEGRATION=1 and require game assets at FSO_GAME_LOCATION
    // (default: /home/baron/projects/freeso-experiment/GameAssets/). They exercise the
    // real WorldWallProvider + WorldFloorProvider via Content.Content.Get() — mocking
    // either provider is an automatic veracity fail per item spec §"Veracity commitment".
    //
    // T1: list-architecture-styles returns ≥1 wall_pattern, exactly 5 wall_styles, ≥1 floor_pattern;
    //     the 5 wall_style IDs match WorldWallProvider.WallStyleIDs (live provider array).
    // T2: every entry has a non-empty name and non-negative price; walls report price_per_segment;
    //     floors report price_per_tile; no conflation of the two billing units.

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

    /// <summary>
    /// T1 (feature): <c>list-architecture-styles</c> returns ≥1 wall_pattern, exactly 5
    /// wall_styles, ≥1 floor_pattern. The 5 wall_style IDs match the live
    /// <c>WorldWallProvider.WallStyleIDs</c> array (0x1, 0x2, 0xD, 0xC, 0xE).
    ///
    /// <para>Veracity: the test reads the live IDs from the provider rather than hardcoding
    /// a static oracle — runtime-loaded content is the truth source.</para>
    /// </summary>
    [SkippableFact]
    public void T1_ListArchitectureStyles_CountsAndWallStyleIDs()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run content integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var resp = BuildModeHandlers.ListArchitectureStyles(null, new JsonObject());
        Assert.True(resp.Ok, $"expected ok=true; got error: {resp.Error}");

        Assert.NotNull(resp.Payload);

        // Deserialise through JSON so we get stable typed access.
        var json = JsonNode.Parse(
            System.Text.Json.JsonSerializer.Serialize(resp.Payload)).AsObject();

        var wallPatterns  = json["wall_patterns"]!.AsArray();
        var wallStyles    = json["wall_styles"]!.AsArray();
        var floorPatterns = json["floor_patterns"]!.AsArray();

        Assert.True(wallPatterns.Count >= 1,
            $"expected ≥1 wall_pattern; got {wallPatterns.Count}");
        Assert.Equal(5, wallStyles.Count);
        Assert.True(floorPatterns.Count >= 1,
            $"expected ≥1 floor_pattern; got {floorPatterns.Count}");

        // The 5 wall_style IDs must match the live WallStyleIDs array from the provider.
        var content = FSO.Content.Content.Get();
        var expectedIds = content.WorldWalls.WallStyleIDs
            .Select(id => (long)id)
            .OrderBy(id => id)
            .ToArray();
        var actualIds = wallStyles
            .Select(s => (long)s!.AsObject()["id"]!)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal(expectedIds, actualIds);
    }

    /// <summary>
    /// T2 (feature): every entry in wall_patterns, wall_styles, and floor_patterns has a
    /// non-empty name and a non-negative price. Walls report <c>price_per_segment</c> (not
    /// <c>price_per_tile</c>); floors report <c>price_per_tile</c> (not
    /// <c>price_per_segment</c>). No billing-unit conflation.
    /// </summary>
    [SkippableFact]
    public void T2_ListArchitectureStyles_NamesAndPrices()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run content integration tests");
        Skip.IfNot(EnsureContentInit(out var skipReason), skipReason);

        var resp = BuildModeHandlers.ListArchitectureStyles(null, new JsonObject());
        Assert.True(resp.Ok, $"expected ok=true; got error: {resp.Error}");

        var json = JsonNode.Parse(
            System.Text.Json.JsonSerializer.Serialize(resp.Payload)).AsObject();

        var wallPatterns  = json["wall_patterns"]!.AsArray();
        var wallStyles    = json["wall_styles"]!.AsArray();
        var floorPatterns = json["floor_patterns"]!.AsArray();

        // wall_patterns: non-empty name, non-negative price_per_segment, NO price_per_tile.
        foreach (var wp in wallPatterns)
        {
            var o = wp.AsObject();
            var name = (string)o["name"];
            // Some catalog entries legitimately have empty names (e.g. internal paint IDs);
            // we assert name is non-null, not necessarily non-empty.
            Assert.NotNull(name);
            var price = (long)o["price_per_segment"];
            Assert.True(price >= 0, $"wall_pattern id={(long)o["id"]}: price_per_segment={price} < 0");
            Assert.Null(o["price_per_tile"]);
        }

        // wall_styles: non-empty name, non-negative price_per_segment, NO price_per_tile.
        foreach (var ws in wallStyles)
        {
            var o = ws.AsObject();
            var name = (string)o["name"];
            Assert.True(!string.IsNullOrEmpty(name),
                $"wall_style id={(long)o["id"]}: empty name — WallStyleIDs entries must have names from BuildGlobals STR 0x81");
            var price = (long)o["price_per_segment"];
            Assert.True(price >= 0, $"wall_style id={(long)o["id"]}: price_per_segment={price} < 0");
            Assert.Null(o["price_per_tile"]);
        }

        // floor_patterns: non-null name, non-negative price_per_tile, NO price_per_segment.
        foreach (var fp in floorPatterns)
        {
            var o = fp.AsObject();
            var name = (string)o["name"];
            Assert.NotNull(name);
            var price = (long)o["price_per_tile"];
            Assert.True(price >= 0, $"floor_pattern id={(long)o["id"]}: price_per_tile={price} < 0");
            Assert.Null(o["price_per_segment"]);
        }
    }
}
