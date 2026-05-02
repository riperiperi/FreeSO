/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Bot.Headless;
using FSO.Files.Formats.tsodata;
using FSO.Server.Clients;
using FSO.Server.Protocol.Electron.Packets;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for <see cref="BotCmdHandler"/> — specifically the
/// <c>probe-bulletin</c> case added in freesoexperiment-923.
///
/// <para>
/// These tests exercise the dispatch path through
/// <see cref="BotCmdHandler.TryHandleAsync"/>: the switch branch must
/// exist and route to the handler, which must emit a <c>bot-cmd-reply</c>
/// on the stdout sink. The city Aries socket is not live in unit tests —
/// we pass <c>null</c> for the city socket and verify the "city socket
/// unavailable" refuse path, which proves the branch dispatches correctly
/// without requiring a live server fixture.
/// </para>
///
/// <para>
/// A second test verifies the unknown-cmd fallback no longer fires for
/// <c>probe-bulletin</c>, anchoring the regression the item was filed to fix:
/// live <c>interact-with bulletin_board</c> returning "unknown bot-cmd" because
/// the switch was missing the case (freesoexperiment-923 root cause).
/// </para>
/// </summary>
public class BotCmdHandlerTests
{
    /// <summary>
    /// probe-bulletin with a null cityAries must emit ok=false with
    /// "city socket unavailable" — proving the branch dispatches (not
    /// "unknown bot-cmd") and the handler's null-guard fires correctly.
    ///
    /// <para>
    /// This is the regression test for freesoexperiment-923: before the fix,
    /// TryHandleAsync would emit <c>ok=false, error="unknown bot-cmd: probe-bulletin"</c>.
    /// After the fix, it emits <c>ok=false, error="probe-bulletin: city socket unavailable"</c>.
    /// The exact error string is the observable difference that proves the branch exists.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_NullCitySocket_EmitsCitySocketUnavailable()
    {
        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-pb-1","args":{"neighborhood_id":1}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var handled = await BotCmdHandler.TryHandleAsync(node, cityAries: null, default);

        Assert.True(handled, "TryHandleAsync must return true for probe-bulletin (consumed)");
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "bot-cmd-reply never emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("bot-cmd-reply", (string)reply["kind"]);
        Assert.Equal("c-pb-1",        (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);

        var error = (string)reply["error"];
        // Must NOT be the old "unknown bot-cmd" error — that's the regression this test guards.
        Assert.DoesNotContain("unknown bot-cmd", error, StringComparison.OrdinalIgnoreCase);
        // Must be the null-guard path, proving the branch was entered.
        Assert.Contains("city socket unavailable", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verify probe-bulletin is NOT routed to the default "unknown bot-cmd" path.
    /// A string-contains check on the error verifies the dispatch reaches the
    /// probe-bulletin handler, not the default case.
    ///
    /// This is the minimal signal that the switch branch is present and wired:
    /// "city socket unavailable" can only appear if the probe-bulletin case
    /// was matched. "unknown bot-cmd: probe-bulletin" means the case is absent.
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_DispatchDoesNotFallThroughToUnknownCmd()
    {
        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-pb-2","args":{}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        await BotCmdHandler.TryHandleAsync(node, cityAries: null, default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "no reply emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        var error = (string)reply["error"] ?? string.Empty;

        // The regression: before the fix, error would be "unknown bot-cmd: probe-bulletin".
        Assert.DoesNotContain("unknown bot-cmd", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// probe-bulletin without args (no neighborhood_id) must default to nhood 1
    /// and still enter the handler — not crash or return unknown-cmd.
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_DefaultNeighborhoodId_NullSocketStillHandled()
    {
        // No args at all — neighborhood_id should default to 1.
        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-pb-3","args":{}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var handled = await BotCmdHandler.TryHandleAsync(node, cityAries: null, default);

        Assert.True(handled);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)));

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("bot-cmd-reply", (string)reply["kind"]);
        Assert.Equal("c-pb-3",        (string)reply["correlation_id"]);
        // null cityAries → ok=false, but handler was entered (not unknown-cmd).
        Assert.False((bool)reply["ok"]);
        Assert.Contains("unavailable", (string)reply["error"], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Existing commands (probe-lot, probe-road, purchase-lot, bot-exit-request)
    /// must not be broken by the new case insertion.
    /// bot-exit-request is simplest to test: it produces ok=true with accepted=true
    /// immediately, then triggers shutdown.
    /// </summary>
    [Fact]
    public async Task ExistingCommands_NotBrokenByNewCase()
    {
        // probe-road: no city socket needed; it reads Content.CityMaps.
        // Content is not initialised in unit tests → exception → ok=false with GetType().Name.
        // What we care about: it's NOT "unknown bot-cmd" (meaning the switch reached probe-road).
        var line = """{"kind":"bot-cmd","cmd":"probe-road","correlation_id":"c-pr-4","args":{"x":249,"y":348}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var handled = await BotCmdHandler.TryHandleAsync(node, cityAries: null, default);
        Assert.True(handled);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)));

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-pr-4", (string)reply["correlation_id"]);
        // Must not be "unknown bot-cmd: probe-road".
        var error = (string)reply["error"] ?? string.Empty;
        Assert.DoesNotContain("unknown bot-cmd", error, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Full production path tests (veracity rework freesoexperiment-923) ----
    //
    // These tests exercise the COMPLETE BulletinRequest → ProbeBulletinSubscriber → TCS → JSON
    // path, not just the null-guard. The seam is AriesClient itself:
    //
    //   • AriesClient is not sealed.
    //   • AriesClient.Write guards on Session!=null — when Session is null (no Connect() called),
    //     Write is a silent no-op.  No exception, no crash.  This makes AriesClient usable as a
    //     headless stub without subclassing.
    //   • AriesClient.MessageReceived(IoSession, object) is a PUBLIC method (IoHandler interface)
    //     that fans out to all registered IAriesMessageSubscriber instances.  Calling it directly
    //     simulates an inbound packet from the city socket.
    //
    // The production path under test:
    //   TryHandleAsync
    //     → checks cityAries != null        (NOT the null-guard path)
    //     → cityAries.Write(BulletinRequest) (silent no-op — no Session)
    //     → registers TCS in _pendingBulletins
    //     → awaits TCS
    //   [concurrent task]
    //     → stub.MessageReceived(null, BulletinResponse)
    //     → ProbeBulletinSubscriber.MessageReceived
    //     → TCS.TrySetResult
    //   TryHandleAsync
    //     → TCS resolves
    //     → JSON projection → EmitReply (ok=true, neighborhood_id, count, messages[])
    //
    // To isolate tests, we reset BotCmdHandler._subscriberAdded via reflection before each
    // full-path test so RegisterSubscriber always registers a fresh ProbeBulletinSubscriber on
    // our stub instance.

    /// <summary>
    /// Resets <see cref="BotCmdHandler._subscriberAdded"/> to false via reflection so that
    /// <see cref="BotCmdHandler.RegisterSubscriber"/> will register a fresh subscriber on the
    /// given <paramref name="client"/> instance even if it was already called before.
    /// </summary>
    private static void ResetSubscriberAdded()
    {
        var f = typeof(BotCmdHandler).GetField(
            "_subscriberAdded",
            BindingFlags.NonPublic | BindingFlags.Static);
        f?.SetValue(null, false);
    }

    /// <summary>
    /// Full production path: BulletinRequest emitted, ProbeBulletinSubscriber notified via
    /// stub.MessageReceived, TCS resolves, JSON reply ok=true with correct shape.
    ///
    /// <para>
    /// This test FAILS if any of the following is broken:
    /// <list type="bullet">
    ///   <item>The BulletinRequest is sent (Write called with wrong type → subscriber never
    ///     matches → TCS times out → ok=false).</item>
    ///   <item>ProbeBulletinSubscriber is not registered (subscriber not called → TCS
    ///     times out → ok=false).</item>
    ///   <item>The TCS correlator is broken (TCS never resolved → timeout → ok=false).</item>
    ///   <item>The JSON projection is wrong (assertion on data fields fails).</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Seam documentation: <c>AriesClient.Write</c> is non-virtual but safe with a null
    /// <c>Session</c> (silent no-op at line 173: <c>if (this.Session != null &amp;&amp;
    /// this.Session.Connected)</c>).  <c>AriesClient.MessageReceived</c> is a public
    /// <c>IoHandler</c> interface method that fan-outs to all registered
    /// <c>IAriesMessageSubscriber</c> instances — including <c>ProbeBulletinSubscriber</c>
    /// registered via <c>BotCmdHandler.RegisterSubscriber</c>.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_FullProductionPath_OkTrueWithMessages()
    {
        // Reset subscriber gate so our stub gets a fresh ProbeBulletinSubscriber.
        ResetSubscriberAdded();
        var stub = new AriesClient(kernel: null);
        BotCmdHandler.RegisterSubscriber(stub);

        // Build a fixture BulletinResponse with one message.
        var fixtureBulletin = new BulletinItem
        {
            ID        = 42,
            NhoodID   = 1,
            SenderID  = 2,
            SenderName = "baron",
            Subject   = "hello city",
            Body      = "first bulletin post",
            Time      = 1714950000L,
            Type      = BulletinType.Community,
            Flags     = 0,
            LotID     = 2,
        };
        var response = new BulletinResponse
        {
            Type     = BulletinResponseType.MESSAGES,
            Messages = new[] { fixtureBulletin },
        };

        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-full-1","args":{"neighborhood_id":1}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _cap = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        // Concurrently deliver the response after a short yield, simulating the city socket reply.
        // The delay must be > 0 so TryHandleAsync has time to register the TCS before
        // MessageReceived fires.  Task.Yield() alone can race; 20 ms is safe for unit tests.
        var deliveryTask = Task.Run(async () =>
        {
            await Task.Delay(20);
            stub.MessageReceived(session: null, message: response);
        });

        var handled = await BotCmdHandler.TryHandleAsync(node, cityAries: stub, default);
        await deliveryTask;

        Assert.True(handled, "TryHandleAsync must return true (consumed)");
        Assert.True(latch.Wait(TimeSpan.FromSeconds(5)), "bot-cmd-reply never emitted — TCS was not resolved");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("bot-cmd-reply",   (string)reply["kind"]);
        Assert.Equal("c-full-1",        (string)reply["correlation_id"]);
        Assert.True((bool)reply["ok"],  $"expected ok=true but got error: {(string)reply["error"]}");

        var data = reply["data"].AsObject();
        Assert.Equal(1L, (long)data["neighborhood_id"]);
        Assert.Equal(1L, (long)data["count"]);

        var messages = data["messages"].AsArray();
        Assert.Single(messages);
        var msg = messages[0].AsObject();
        Assert.Equal(42L,              (long)msg["bulletin_id"]);
        Assert.Equal("baron",          (string)msg["sender_name"]);
        Assert.Equal("hello city",     (string)msg["subject"]);
        Assert.Equal("first bulletin post", (string)msg["body"]);
        Assert.Equal(1L,               (long)msg["nhood_id"]);
        Assert.Equal(2L,               (long)msg["lot_id"]);
        Assert.Equal("Community",      (string)msg["type"]);
    }

    /// <summary>
    /// Mutation test: sending a BulletinResponse with the wrong type (not MESSAGES) must
    /// NOT resolve the happy path.  This verifies the packet-type check in
    /// <see cref="HandleProbeBulletinAsync"/> (the <c>resp.Type != BulletinResponseType.MESSAGES</c>
    /// guard). The TCS is resolved with the wrong-type response, so ok=false with the
    /// "server returned" error — not a timeout, not a null-guard path.
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_WrongResponseType_EmitsServerReturnedError()
    {
        ResetSubscriberAdded();
        var stub = new AriesClient(kernel: null);
        BotCmdHandler.RegisterSubscriber(stub);

        // Respond with FAIL_NOT_MAYOR instead of MESSAGES.
        var wrongResponse = new BulletinResponse
        {
            Type     = BulletinResponseType.FAIL_NOT_MAYOR,
            Messages = Array.Empty<BulletinItem>(),
        };

        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-wrong-1","args":{"neighborhood_id":1}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _cap = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var deliveryTask = Task.Run(async () =>
        {
            await Task.Delay(20);
            stub.MessageReceived(session: null, message: wrongResponse);
        });

        await BotCmdHandler.TryHandleAsync(node, cityAries: stub, default);
        await deliveryTask;

        Assert.True(latch.Wait(TimeSpan.FromSeconds(5)), "bot-cmd-reply never emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-wrong-1", (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"], "expected ok=false for non-MESSAGES response type");

        var error = (string)reply["error"];
        // Must contain "server returned" from the packet-type guard, not "city socket unavailable"
        // (null-guard path) and not "timeout".
        Assert.Contains("server returned", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FAIL_NOT_MAYOR", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Mutation test: if a non-BulletinResponse packet arrives, the subscriber ignores it,
    /// the TCS is never resolved, and the handler times out with ok=false "timeout".
    /// This verifies the subscriber's packet-type filter is correctly wired.
    /// Uses a short timeout (1s) to keep the test fast.
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_WrongPacketType_TimesOut()
    {
        ResetSubscriberAdded();
        var stub = new AriesClient(kernel: null);
        BotCmdHandler.RegisterSubscriber(stub);

        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-timeout-1","args":{"neighborhood_id":1}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _cap = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        // Deliver a FindLotResponse (wrong type) — subscriber should ignore it.
        var deliveryTask = Task.Run(async () =>
        {
            await Task.Delay(20);
            stub.MessageReceived(session: null, message: new FindLotResponse());
        });

        // Use a short cancellation token to avoid the full 10-second production timeout.
        using var shortCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await BotCmdHandler.TryHandleAsync(node, cityAries: stub, shortCts.Token);
        await deliveryTask;

        Assert.True(latch.Wait(TimeSpan.FromSeconds(3)), "bot-cmd-reply never emitted after timeout");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-timeout-1", (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);

        var error = (string)reply["error"];
        // Must be "timeout" or "cancelled" — not "city socket unavailable" (null-guard path).
        var isTimeoutOrCancelled =
            error.Contains("timeout",   StringComparison.OrdinalIgnoreCase) ||
            error.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
        Assert.True(isTimeoutOrCancelled,
            $"expected timeout/cancelled error but got: {error}");
    }
}
