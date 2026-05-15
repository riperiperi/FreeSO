using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Bot.Headless;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for the interaction-family IPC handlers (freesoexperiment-2a8).
///
/// <para>
/// The wire-PDU encoding is already pinned by <see cref="MovementCommandEncodingTests"/>
/// — <c>VMNetInteractionCmd</c> (14 bytes) for interact-with and
/// <c>VMNetInteractionCancelCmd</c> (7 bytes) for cancel-interaction. Those
/// tests guard the PDU shape against accidental reorder/width changes.
/// </para>
///
/// <para>
/// Full handler-through-VM testing requires a live VM (Content.Init +
/// VMContext.InitVMConfig). Per <see cref="PerceptionProjectorTests"/>'s
/// precedent, that kind of setup belongs in the integration test
/// (<c>tests/integration/verb-interaction.sh</c>) which runs against live
/// workshop, not in xUnit. We assert here the things we CAN without a VM:
/// CommandDispatcher argument-guard behaviour and family registration.
/// </para>
/// </summary>
public class InteractionHandlersTests
{
    /// <summary>
    /// RegisterAll rejects null dispatcher and null vmHost — argument-guard
    /// check per the method contract.
    /// </summary>
    [Fact]
    public void RegisterAll_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => InteractionHandlers.RegisterAll(null, null));
    }

    /// <summary>
    /// Dispatching an interaction-family op that has NOT been registered
    /// produces a structured "unknown op" response. This is the shape contract
    /// the sidecar's convention handlers rely on.
    /// </summary>
    [Fact]
    public async Task Dispatcher_UnregisteredInteractWith_ReturnsUnknownOp()
    {
        var d = new CommandDispatcher();
        // Deliberately do NOT call InteractionHandlers.RegisterAll — we want
        // the unknown-op path.
        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync("""{"id":"c-iw","op":"interact-with","args":{}}""", default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var node = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-iw", (string)node["cmd_id"]);
        Assert.False((bool)node["ok"]);
        Assert.Contains("unknown op", (string)node["error"]);
    }

    /// <summary>
    /// The dispatcher happily serialises a handler's structured
    /// <c>CommandDispatcher.Response.Fail</c> result as ok=false. Proves the
    /// error-path shape the InteractionHandlers use (fail("no live avatar"),
    /// fail("cancel-interaction requires action_uid"), etc.) reaches the wire
    /// as the agent expects.
    /// </summary>
    [Fact]
    public async Task Dispatcher_HandlerReturnsFail_SerializesAsOkFalse()
    {
        var d = new CommandDispatcher();
        d.Register("interact-with", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("no live avatar")));
        d.Register("cancel-interaction", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("cancel-interaction requires action_uid")));
        d.Register("query-pie-menu", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Fail("query-pie-menu requires target_object_id or target_sim_id")));

        Assert.True(d.Has("interact-with"));
        Assert.True(d.Has("cancel-interaction"));
        Assert.True(d.Has("query-pie-menu"));

        foreach (var (op, expectedFragment) in new[]
        {
            ("interact-with", "avatar"),
            ("cancel-interaction", "action_uid"),
            ("query-pie-menu", "target"),
        })
        {
            string captured = null;
            var latch = new ManualResetEventSlim();
            using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
            var line = "{\"id\":\"c-" + op + "\",\"op\":\"" + op + "\",\"args\":{}}";
            await d.HandleLineAsync(line, default);
            Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), $"response never emitted for {op}");

            var node = JsonNode.Parse(captured).AsObject();
            Assert.Equal($"c-{op}", (string)node["cmd_id"]);
            Assert.False((bool)node["ok"]);
            Assert.Contains(expectedFragment, (string)node["error"], StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Golden-byte wire shape sanity: the interaction family uses
    /// <c>VMNetInteractionCmd</c> (14 bytes with InteractionType=1) and
    /// <c>VMNetInteractionCancelCmd</c> (7 bytes with InteractionCancel=7).
    /// Those PDUs are exhaustively tested in <see cref="MovementCommandEncodingTests"/>;
    /// this test simply asserts the class identity so a future refactor
    /// renaming the PDU would break here immediately.
    /// </summary>
    [Fact]
    public void WirePdus_ClassIdentity()
    {
        Assert.NotNull(typeof(FSO.SimAntics.NetPlay.Model.Commands.VMNetInteractionCmd));
        Assert.NotNull(typeof(FSO.SimAntics.NetPlay.Model.Commands.VMNetInteractionCancelCmd));
    }

    // ---- queue-interactions / query-action-queue (freesoexperiment-36a / -dbe) ----
    //
    // Live-VM behaviour is covered by the integration smoke on workshop (the
    // body-cf round-trip set), which proved: queue-interactions queues N
    // entries in one cf call, query-action-queue returns the resulting UIDs,
    // include_idle=false filters autopilot. Here we lock in the null-VM
    // refuse path — both handlers must surface "no live avatar" rather than
    // NRE'ing, since the bot occasionally runs with vmHost=null during
    // disconnect-reconnect windows.

    [Fact]
    public void QueueInteractions_NullVMHost_ReturnsNoLiveAvatar()
    {
        var resp = InteractionHandlers.QueueInteractions(
            null, new JsonObject { ["interactions"] = new JsonArray() });
        Assert.False(resp.Ok);
        Assert.Contains("avatar", resp.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QueryActionQueue_NullVMHost_ReturnsNoLiveAvatar()
    {
        var resp = InteractionHandlers.QueryActionQueue(null, new JsonObject());
        Assert.False(resp.Ok);
        Assert.Contains("avatar", resp.Error, StringComparison.OrdinalIgnoreCase);
    }
}
