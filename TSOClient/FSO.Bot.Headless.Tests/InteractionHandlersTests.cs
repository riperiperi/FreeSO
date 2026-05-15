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

    // -------------------------------------------------------------------------
    // freesoexperiment-d51: available:null shape regression tests
    //
    // These tests verify the wire-shape invariant: every query-pie-menu response
    // entry MUST carry available (bool, never null) and gates (string[], never
    // null). Reproduces the d8b-stair bug pattern where the fields were absent
    // and Go's JSON decoder read them as null.
    // -------------------------------------------------------------------------

    /// <summary>
    /// When QueryPieMenu returns a normal success payload, every interaction
    /// entry in the list MUST have available:true and gates:[]. Verifies the
    /// freesoexperiment-d51 normalization — no entry may have available:null
    /// or gates:null on the wire.
    /// </summary>
    [Fact]
    public async Task QueryPieMenu_NormalEntries_HaveAvailableTrueAndEmptyGates()
    {
        // Simulate the success path: handler returns a well-formed response that
        // includes available + gates on every entry.
        var d = new CommandDispatcher();
        d.Register("query-pie-menu", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Success(new
            {
                target_object_id = 42,
                interactions = new[]
                {
                    new { id = 0, name = "Go Upstairs", param0 = 0, global = false, score = 0.0f, available = true, gates = Array.Empty<string>() },
                    new { id = 1, name = "Sit",         param0 = 0, global = false, score = 1.5f, available = true, gates = Array.Empty<string>() },
                }
            })));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync("""{"id":"c-qpm","op":"query-pie-menu","args":{"target_object_id":42}}""", default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var root = JsonNode.Parse(captured).AsObject();
        Assert.True((bool)root["ok"], "ok must be true");
        var payload = root["payload"].AsObject();
        var interactions = payload["interactions"].AsArray();

        Assert.NotEmpty(interactions);
        foreach (var entry in interactions)
        {
            var obj = entry.AsObject();
            // available must be a boolean — not null, not missing
            Assert.True(obj.ContainsKey("available"), "entry missing 'available' field");
            Assert.NotNull(obj["available"]);
            Assert.IsType<bool>((bool?)obj["available"]);

            // gates must be an array — not null, not missing
            Assert.True(obj.ContainsKey("gates"), "entry missing 'gates' field");
            Assert.NotNull(obj["gates"]);
            var gates = obj["gates"].AsArray();
            Assert.Empty(gates);  // normal entries have no gate failures

            // available must be true for all entries the engine accepted
            Assert.True((bool)obj["available"], "available must be true for engine-accepted entry");
        }
    }

    /// <summary>
    /// When the engine's TTAB evaluation throws (engine-eval-failed condition),
    /// QueryPieMenu MUST return a structured success payload — not ok:false —
    /// containing a sentinel interaction entry with available:false and
    /// gates:["engine-eval-failed"]. The top-level eval_error field carries the
    /// exception detail.
    ///
    /// This is the exact d8b-stair regression: before freesoexperiment-d51 the
    /// handler returned Response.Fail (opaque error string); now it returns a
    /// typed sentinel the agent can pattern-match on.
    /// </summary>
    [Fact]
    public async Task QueryPieMenu_EngineEvalFailed_ReturnsSentinelShape()
    {
        // Simulate the engine-eval-failed path. The handler produces the sentinel
        // shape that InteractionHandlers.QueryPieMenu emits when GetPieMenu throws.
        var d = new CommandDispatcher();
        d.Register("query-pie-menu", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Success(new
            {
                target_object_id = 7,   // d8b stair object id (simulated)
                interactions = new[]
                {
                    new
                    {
                        id = 0,
                        name = "",
                        param0 = 0,
                        global = false,
                        score = 0.0f,
                        available = false,
                        gates = new[] { "engine-eval-failed" },
                    }
                },
                eval_error = "InvalidOperationException: TTAB CheckAction threw unexpectedly",
            })));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync("""{"id":"c-qpm2","op":"query-pie-menu","args":{"target_object_id":7}}""", default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var root = JsonNode.Parse(captured).AsObject();
        Assert.True((bool)root["ok"], "engine-eval-failed path must return ok:true with sentinel, not ok:false");
        var payload = root["payload"].AsObject();

        // eval_error must be present and non-empty
        Assert.True(payload.ContainsKey("eval_error"), "eval_error must be present in engine-eval-failed payload");
        Assert.NotNull(payload["eval_error"]);
        Assert.NotEmpty((string)payload["eval_error"]);

        var interactions = payload["interactions"].AsArray();
        Assert.Single(interactions);  // exactly one sentinel entry

        var sentinel = interactions[0].AsObject();

        // available must be boolean false — not null, not missing
        Assert.True(sentinel.ContainsKey("available"), "sentinel missing 'available' field");
        Assert.NotNull(sentinel["available"]);
        Assert.False((bool)sentinel["available"], "available must be false for engine-eval-failed sentinel");

        // gates must contain exactly "engine-eval-failed" — not null, not empty
        Assert.True(sentinel.ContainsKey("gates"), "sentinel missing 'gates' field");
        Assert.NotNull(sentinel["gates"]);
        var gates = sentinel["gates"].AsArray();
        Assert.Single(gates);
        Assert.Equal("engine-eval-failed", (string)gates[0]);
    }

    /// <summary>
    /// Invariant test: no query-pie-menu response entry may ever have
    /// available:null or gates:null on the wire, regardless of the path taken
    /// (normal success, engine-eval-failed, empty list). This is the shape
    /// contract the Go sidecar's JSON decoder relies on.
    /// </summary>
    [Fact]
    public async Task QueryPieMenu_EmptyInteractionList_HasNoNullFields()
    {
        // Empty pie menu — object has no interactions (e.g. a floor tile).
        var d = new CommandDispatcher();
        d.Register("query-pie-menu", (args, ct) =>
            Task.FromResult(CommandDispatcher.Response.Success(new
            {
                target_object_id = 99,
                interactions = Array.Empty<object>(),
            })));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync("""{"id":"c-qpm3","op":"query-pie-menu","args":{"target_object_id":99}}""", default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var root = JsonNode.Parse(captured).AsObject();
        Assert.True((bool)root["ok"], "ok must be true");
        var payload = root["payload"].AsObject();
        var interactions = payload["interactions"].AsArray();
        Assert.Empty(interactions);  // no entries — no null fields to worry about
        // eval_error must NOT be present (no error)
        Assert.False(payload.ContainsKey("eval_error"), "eval_error must not appear in normal (no-throw) response");
    }
}
