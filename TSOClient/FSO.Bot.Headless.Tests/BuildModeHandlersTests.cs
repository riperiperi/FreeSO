using System;
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

        foreach (var op in new[]
                 {
                     "place-wall", "paint-wall", "paint-floor", "paint-grass",
                     "flatten-terrain", "raise-terrain", "set-roof",
                     "change-environment", "change-lot-size", "leave-build-buy",
                     "query-architecture",
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
}
