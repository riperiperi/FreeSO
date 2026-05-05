/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Bot.Headless;
using FSO.Files.Formats.tsodata;
using FSO.Server.Clients;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit and lifecycle tests for <see cref="BotCmdHandler"/>.
///
/// <para>
/// These tests cover:
/// <list type="bullet">
///   <item>freesoexperiment-923: probe-bulletin dispatch path (original coverage).</item>
///   <item>freesoexperiment-f70 H1: lot-keyed probe correlation.</item>
///   <item>freesoexperiment-f70 H2: SessionClosed / GetOrAdd race guard.</item>
///   <item>freesoexperiment-f70 H3: per-instance (not static) state — two BotCmdHandler
///     instances must NOT share pending dictionaries or subscriber-registered flags.</item>
/// </list>
/// </para>
///
/// <para>
/// BotCmdHandler is now an instance class (H3). Each test creates its own
/// <c>new BotCmdHandler()</c> instance — no reflection-based reset required.
/// </para>
/// </summary>
public class BotCmdHandlerTests
{
    // ---- probe-bulletin dispatch tests (freesoexperiment-923 coverage, updated for instance API) ----

    /// <summary>
    /// probe-bulletin with a null cityAries must emit ok=false with
    /// "city socket unavailable" — proving the branch dispatches (not "unknown bot-cmd").
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_NullCitySocket_EmitsCitySocketUnavailable()
    {
        var handler = new BotCmdHandler();
        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-pb-1","args":{"neighborhood_id":1}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var handled = await handler.TryHandleAsync(node, cityAries: null, default);

        Assert.True(handled, "TryHandleAsync must return true for probe-bulletin (consumed)");
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "bot-cmd-reply never emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("bot-cmd-reply", (string)reply["kind"]);
        Assert.Equal("c-pb-1",        (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);

        var error = (string)reply["error"];
        Assert.DoesNotContain("unknown bot-cmd", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("city socket unavailable", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verify probe-bulletin is NOT routed to the default "unknown bot-cmd" path.
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_DispatchDoesNotFallThroughToUnknownCmd()
    {
        var handler = new BotCmdHandler();
        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-pb-2","args":{}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        await handler.TryHandleAsync(node, cityAries: null, default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "no reply emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        var error = (string)reply["error"] ?? string.Empty;
        Assert.DoesNotContain("unknown bot-cmd", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// probe-bulletin without args (no neighborhood_id) must default to nhood 1.
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_DefaultNeighborhoodId_NullSocketStillHandled()
    {
        var handler = new BotCmdHandler();
        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-pb-3","args":{}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var handled = await handler.TryHandleAsync(node, cityAries: null, default);

        Assert.True(handled);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)));

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("bot-cmd-reply", (string)reply["kind"]);
        Assert.Equal("c-pb-3",        (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);
        Assert.Contains("unavailable", (string)reply["error"], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Existing commands (probe-road) must not be broken by refactor.
    /// </summary>
    [Fact]
    public async Task ExistingCommands_NotBrokenByRefactor()
    {
        var handler = new BotCmdHandler();
        var line = """{"kind":"bot-cmd","cmd":"probe-road","correlation_id":"c-pr-4","args":{"x":249,"y":348}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var handled = await handler.TryHandleAsync(node, cityAries: null, default);
        Assert.True(handled);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)));

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-pr-4", (string)reply["correlation_id"]);
        var error = (string)reply["error"] ?? string.Empty;
        Assert.DoesNotContain("unknown bot-cmd", error, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Full production path: probe-bulletin (veracity, updated for instance API) ----

    /// <summary>
    /// Full production path: BulletinRequest emitted, ProbeBulletinSubscriber notified,
    /// TCS resolves, JSON reply ok=true with correct shape.
    ///
    /// <para>
    /// H3 verification: each test creates its own <c>new BotCmdHandler()</c> and calls
    /// <c>RegisterSubscriber</c> on a fresh AriesClient stub — no shared static state.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_FullProductionPath_OkTrueWithMessages()
    {
        var handler = new BotCmdHandler();
        var stub = new AriesClient(kernel: null);
        handler.RegisterSubscriber(stub);

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

        var deliveryTask = Task.Run(async () =>
        {
            await Task.Delay(20);
            stub.MessageReceived(session: null, message: response);
        });

        var handled = await handler.TryHandleAsync(node, cityAries: stub, default);
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
    /// Mutation test: wrong response type (not MESSAGES) must emit ok=false "server returned".
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_WrongResponseType_EmitsServerReturnedError()
    {
        var handler = new BotCmdHandler();
        var stub = new AriesClient(kernel: null);
        handler.RegisterSubscriber(stub);

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

        await handler.TryHandleAsync(node, cityAries: stub, default);
        await deliveryTask;

        Assert.True(latch.Wait(TimeSpan.FromSeconds(5)), "bot-cmd-reply never emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-wrong-1", (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);

        var error = (string)reply["error"];
        Assert.Contains("server returned", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FAIL_NOT_MAYOR", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Mutation test: wrong packet type must cause timeout.
    /// </summary>
    [Fact]
    public async Task ProbeBulletin_WrongPacketType_TimesOut()
    {
        var handler = new BotCmdHandler();
        var stub = new AriesClient(kernel: null);
        handler.RegisterSubscriber(stub);

        var line = """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"c-timeout-1","args":{"neighborhood_id":1}}""";
        var node = JsonNode.Parse(line).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _cap = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var deliveryTask = Task.Run(async () =>
        {
            await Task.Delay(20);
            stub.MessageReceived(session: null, message: new FindLotResponse());
        });

        using var shortCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await handler.TryHandleAsync(node, cityAries: stub, shortCts.Token);
        await deliveryTask;

        Assert.True(latch.Wait(TimeSpan.FromSeconds(3)), "bot-cmd-reply never emitted after timeout");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-timeout-1", (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);

        var error = (string)reply["error"];
        var isTimeoutOrCancelled =
            error.Contains("timeout",   StringComparison.OrdinalIgnoreCase) ||
            error.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
        Assert.True(isTimeoutOrCancelled, $"expected timeout/cancelled error but got: {error}");
    }

    // ---- H1: lot-keyed probe correlation (freesoexperiment-f70) ----

    /// <summary>
    /// H1 test: When two concurrent probe-lot requests are in flight for DIFFERENT lots,
    /// a FindLotResponse for lot A must resolve the TCS for lot A and NOT lot B.
    ///
    /// <para>
    /// <b>Mutation verification:</b> if the subscriber uses plain FIFO (no lot-keyed match),
    /// it would complete the first TCS regardless of LotId — meaning lot B's TCS could be
    /// resolved with lot A's response. With the fix, the subscriber matches resp.LotId
    /// against the stored LotCtx and only resolves the matching entry.
    /// </para>
    ///
    /// <para>
    /// Test setup:
    /// <list type="number">
    ///   <item>Register probe-lot TCS for lot 100 (corrId "lot-100").</item>
    ///   <item>Register probe-lot TCS for lot 200 (corrId "lot-200").</item>
    ///   <item>Deliver FindLotResponse with LotId=200 via MessageReceived.</item>
    ///   <item>Assert "lot-200" TCS resolves quickly; "lot-100" TCS does NOT resolve.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Fact]
    public async Task H1_ProbeLot_LotKeyedCorrelation_ResponseForLotBDoesNotResolveLotA()
    {
        var handler = new BotCmdHandler();
        var stub = new AriesClient(kernel: null);
        handler.RegisterSubscriber(stub);

        // Set up capture for two correlation IDs.
        string capturedLot100 = null, capturedLot200 = null;
        var latch100 = new ManualResetEventSlim();
        var latch200 = new ManualResetEventSlim();

        // Capture function: multiplex by correlation_id.
        using var _cap = PerceptionEmitterCapture.Capture(s =>
        {
            var r = JsonNode.Parse(s)?.AsObject();
            if (r == null) return;
            var corrId = (string)r["correlation_id"];
            if (corrId == "lot-100") { capturedLot100 = s; latch100.Set(); }
            else if (corrId == "lot-200") { capturedLot200 = s; latch200.Set(); }
        });

        // Issue probe-lot for location 100.
        var node100 = JsonNode.Parse(
            """{"kind":"bot-cmd","cmd":"probe-lot","correlation_id":"lot-100","args":{"lot_location":100}}""").AsObject();
        // Issue probe-lot for location 200.
        var node200 = JsonNode.Parse(
            """{"kind":"bot-cmd","cmd":"probe-lot","correlation_id":"lot-200","args":{"lot_location":200}}""").AsObject();

        // Both handlers are started concurrently but we control delivery manually.
        // Start both but don't await yet — they will block awaiting TCS resolution.
        using var shortCts100 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        using var shortCts200 = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var task100 = handler.TryHandleAsync(node100, cityAries: stub, shortCts100.Token);
        // Small delay to ensure lot-100 TCS is registered before lot-200.
        await Task.Delay(10);
        var task200 = handler.TryHandleAsync(node200, cityAries: stub, shortCts200.Token);
        await Task.Delay(30); // Let both register their TCSes.

        // Deliver a response for LOT 200 only.
        stub.MessageReceived(session: null, message: new FindLotResponse
        {
            Status = FindLotResponseStatus.FOUND,
            LotId  = 200,
            Address = "lot200-address",
        });

        // lot-200 must resolve; lot-100 must time out (its lot context doesn't match 200).
        Assert.True(latch200.Wait(TimeSpan.FromSeconds(3)),
            "lot-200 TCS must resolve when FindLotResponse(LotId=200) arrives");

        var reply200 = JsonNode.Parse(capturedLot200).AsObject();
        Assert.Equal("lot-200", (string)reply200["correlation_id"]);
        Assert.True((bool)reply200["ok"], $"lot-200 expected ok=true but got: {(string)reply200["error"]}");
        Assert.Equal("FOUND", (string)reply200["data"]["status"]);

        // lot-100 must NOT have resolved (no response for lot 100 was delivered).
        // It will time out via shortCts100.
        await task100;  // wait for the 2s timeout to fire
        Assert.True(latch100.Wait(TimeSpan.FromSeconds(1)),
            "lot-100 TCS must emit a timeout/cancelled reply when no matching response arrives");

        var reply100 = JsonNode.Parse(capturedLot100).AsObject();
        Assert.Equal("lot-100", (string)reply100["correlation_id"]);
        Assert.False((bool)reply100["ok"],
            "lot-100 must NOT resolve with lot-200's response — H1 lot-keyed correlation check");

        await task200;
    }

    /// <summary>
    /// H1 test (probe-lot fallback): When FindLotResponse.LotId doesn't match any
    /// stored lot context (e.g., server echoes LotId=0 for NOT_OPEN), the subscriber
    /// falls back to FIFO and completes the first incomplete TCS. This ensures the
    /// fallback path doesn't deadlock.
    /// </summary>
    [Fact]
    public async Task H1_ProbeLot_FallbackFifo_WhenResponseLotIdIsZero()
    {
        var handler = new BotCmdHandler();
        var stub = new AriesClient(kernel: null);
        handler.RegisterSubscriber(stub);

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _cap = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var node = JsonNode.Parse(
            """{"kind":"bot-cmd","cmd":"probe-lot","correlation_id":"lot-zero","args":{"lot_location":999}}""").AsObject();

        var task = handler.TryHandleAsync(node, cityAries: stub, default);
        await Task.Delay(30);

        // Deliver FindLotResponse with LotId=0 (NOT_OPEN responses may do this).
        stub.MessageReceived(session: null, message: new FindLotResponse
        {
            Status = FindLotResponseStatus.NOT_OPEN,
            LotId  = 0,
        });

        Assert.True(latch.Wait(TimeSpan.FromSeconds(3)),
            "probe-lot with LotId=0 fallback must still complete via FIFO");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("lot-zero", (string)reply["correlation_id"]);
        Assert.True((bool)reply["ok"], $"expected ok=true (NOT_OPEN is a valid status) but got: {(string)reply["error"]}");
        Assert.Equal("NOT_OPEN", (string)reply["data"]["status"]);

        await task;
    }

    // ---- H2: SessionClosed / GetOrAdd race (freesoexperiment-f70) ----

    /// <summary>
    /// H2 test: After SessionClosed fires, a subsequent probe-lot call must NOT create
    /// a new TCS entry in _pendingProbes (which would never be resolved since the session
    /// is dead). Instead it must emit ok=false "session already closed" immediately.
    ///
    /// <para>
    /// <b>Mutation verification:</b> on the unfixed code (static class, no _sessionClosed
    /// guard), TryHandleAsync after SessionClosed would call GetOrAdd on _pendingProbes
    /// and create a new TCS that will hang until timeout. With the fix, it checks
    /// _sessionClosed under lock and returns an error reply immediately.
    /// </para>
    ///
    /// <para>
    /// Test: trigger SessionClosed via MessageReceived, then call TryHandleAsync with a
    /// short timeout; verify the reply is "session already closed" (not a timeout).
    /// </para>
    /// </summary>
    [Fact]
    public async Task H2_SessionClosed_FollowupProbeLotEmitsErrorImmediately_NotTimeout()
    {
        var handler = new BotCmdHandler();
        var stub = new AriesClient(kernel: null);
        handler.RegisterSubscriber(stub);

        // Simulate session close: trigger SessionClosed on all registered subscribers.
        stub.SessionClosed(session: null);

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _cap = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var node = JsonNode.Parse(
            """{"kind":"bot-cmd","cmd":"probe-lot","correlation_id":"post-close","args":{"lot_location":100}}""").AsObject();

        // Use a 3s CTS; with the fix the reply comes in < 100 ms.
        // Without the fix it would hang until this CTS fires → "timeout" error.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var start = DateTime.UtcNow;
        await handler.TryHandleAsync(node, cityAries: stub, cts.Token);
        var elapsed = DateTime.UtcNow - start;

        Assert.True(latch.Wait(TimeSpan.FromSeconds(1)), "reply must be emitted quickly after session close");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("post-close", (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);

        var error = (string)reply["error"];
        // The fix emits "session already closed" immediately — NOT a timeout.
        Assert.Contains("session already closed", error, StringComparison.OrdinalIgnoreCase);
        // Must have replied in well under the 3s CTS window (i.e., not a timeout).
        Assert.True(elapsed < TimeSpan.FromSeconds(2),
            $"reply took {elapsed.TotalMilliseconds:F0}ms — should be near-instant for H2 fix, not a timeout");
    }

    /// <summary>
    /// H2 test: concurrent race — SessionClosed fires while a probe-lot is in-flight.
    /// The TCS must be cancelled (not left pending), and TryHandleAsync must complete
    /// with ok=false (cancelled) rather than hanging.
    ///
    /// <para>
    /// This exercises the race condition directly: TryHandleAsync registers its TCS,
    /// then SessionClosed fires and cancels all pending TCSes. The await should
    /// complete via cancellation rather than timeout.
    /// </para>
    /// </summary>
    [Fact]
    public async Task H2_SessionClosed_CancelsInFlightProbeLot()
    {
        var handler = new BotCmdHandler();
        var stub = new AriesClient(kernel: null);
        handler.RegisterSubscriber(stub);

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _cap = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var node = JsonNode.Parse(
            """{"kind":"bot-cmd","cmd":"probe-lot","correlation_id":"inflight-close","args":{"lot_location":100}}""").AsObject();

        // Start probe-lot, allow time for TCS registration, then fire SessionClosed.
        var probeTask = handler.TryHandleAsync(node, cityAries: stub, default);
        await Task.Delay(30); // Let the TCS be registered.

        // Fire SessionClosed — should cancel the in-flight TCS.
        stub.SessionClosed(session: null);

        // probe-lot must complete (not hang) because its TCS was cancelled.
        // We give it a generous window; the cancellation should fire near-instantly.
        var completed = await Task.WhenAny(probeTask, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(object.ReferenceEquals(completed, probeTask),
            "probe-lot must complete when SessionClosed cancels its TCS (H2)");

        Assert.True(latch.Wait(TimeSpan.FromSeconds(1)), "bot-cmd-reply must be emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("inflight-close", (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);
        // Should be cancelled/timeout (TCS was cancelled by SessionClosed).
        var error = (string)reply["error"];
        Assert.True(
            error.Contains("cancelled", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("timeout",   StringComparison.OrdinalIgnoreCase),
            $"expected cancelled/timeout error but got: {error}");
    }

    // ---- H3: per-session state isolation (freesoexperiment-f70) ----

    /// <summary>
    /// H3 test: Two BotCmdHandler instances must NOT share pending-probe state.
    ///
    /// <para>
    /// <b>Mutation verification:</b> on the old static class, a TCS registered by handler1
    /// would be visible in <c>_pendingProbes</c> to handler2's subscriber, causing cross-
    /// session correlation pollution. With the fix (instance class), each handler has its
    /// own <c>_pendingProbes</c> dictionary.
    /// </para>
    ///
    /// <para>
    /// Test:
    /// <list type="number">
    ///   <item>Create handler1 on stub1; register subscriber. Start probe-lot "h1-probe".</item>
    ///   <item>Create handler2 on stub2; register subscriber.</item>
    ///   <item>Deliver FindLotResponse(LotId=100) via stub2.MessageReceived (handler2's stub).</item>
    ///   <item>Assert handler2's response resolves no TCS (handler2 has no pending probe).</item>
    ///   <item>Assert handler1's TCS is NOT resolved (delivery went to handler2's subscriber).</item>
    /// </list>
    /// </para>
    /// </summary>
    [Fact]
    public async Task H3_TwoInstances_DoNotSharePendingState()
    {
        var handler1 = new BotCmdHandler();
        var stub1 = new AriesClient(kernel: null);
        handler1.RegisterSubscriber(stub1);

        var handler2 = new BotCmdHandler();
        var stub2 = new AriesClient(kernel: null);
        handler2.RegisterSubscriber(stub2);

        string capturedH1 = null;
        var latchH1 = new ManualResetEventSlim();
        // Only capture output while handler1's task is running.
        using var _capH1 = PerceptionEmitterCapture.Capture(s =>
        {
            var r = JsonNode.Parse(s)?.AsObject();
            if ((string)r?["correlation_id"] == "h1-probe")
            {
                capturedH1 = s;
                latchH1.Set();
            }
        });

        var nodeH1 = JsonNode.Parse(
            """{"kind":"bot-cmd","cmd":"probe-lot","correlation_id":"h1-probe","args":{"lot_location":100}}""").AsObject();

        // Start handler1's probe (will block waiting for its TCS).
        using var shortCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task1 = handler1.TryHandleAsync(nodeH1, cityAries: stub1, shortCts.Token);
        await Task.Delay(30); // Let handler1's TCS be registered.

        // Deliver a matching response via stub2 (handler2's city socket, NOT handler1's).
        stub2.MessageReceived(session: null, message: new FindLotResponse
        {
            Status = FindLotResponseStatus.FOUND,
            LotId  = 100,
        });

        // handler1's TCS must NOT have been resolved by stub2's subscriber
        // (they are different instances with separate pending dicts).
        await task1; // wait for handler1's timeout to fire.

        Assert.True(latchH1.Wait(TimeSpan.FromSeconds(1)),
            "handler1's probe must emit a reply (either timeout or resolved)");

        var reply1 = JsonNode.Parse(capturedH1).AsObject();
        Assert.Equal("h1-probe", (string)reply1["correlation_id"]);

        // Must be a timeout/cancelled — NOT ok=true, because handler2's delivery
        // must not have crossed into handler1's pending dict.
        Assert.False((bool)reply1["ok"],
            "handler1 must NOT resolve via handler2's delivery — H3 instance isolation check");
        var error = (string)reply1["error"];
        Assert.True(
            error.Contains("timeout",   StringComparison.OrdinalIgnoreCase) ||
            error.Contains("cancelled", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("session already closed", StringComparison.OrdinalIgnoreCase),
            $"expected timeout/cancelled for handler1 but got: {error}");
    }

    /// <summary>
    /// H3 test: RegisterSubscriber on two instances must NOT share the registration gate.
    ///
    /// <para>
    /// On the old static code, <c>_subscriberAdded</c> was static — calling
    /// <c>RegisterSubscriber</c> on handler1 would set the static flag, causing handler2's
    /// <c>RegisterSubscriber</c> to return without registering any subscriber on stub2.
    /// With the fix, each instance has its own <c>_subscriberRegistered</c> flag.
    /// </para>
    ///
    /// <para>
    /// Mutation-verified: if <c>_subscriberRegistered</c> were static, handler2's
    /// <c>RegisterSubscriber</c> would skip registration (flag already true from handler1's call),
    /// and handler2's probe-bulletin would time out instead of resolving.
    /// </para>
    /// </summary>
    [Fact]
    public async Task H3_RegisterSubscriber_PerInstanceGate_BothCanReceiveMessages()
    {
        // Create and register handler1.
        var handler1 = new BotCmdHandler();
        var stub1 = new AriesClient(kernel: null);
        handler1.RegisterSubscriber(stub1);

        // Create and register handler2 AFTER handler1 — registration must succeed independently.
        var handler2 = new BotCmdHandler();
        var stub2 = new AriesClient(kernel: null);
        handler2.RegisterSubscriber(stub2); // would be skipped if _subscriberRegistered were static

        var response = new BulletinResponse
        {
            Type     = BulletinResponseType.MESSAGES,
            Messages = Array.Empty<BulletinItem>(),
        };

        // Test handler2 independently (its subscriber must have been registered).
        string captured2 = null;
        var latch2 = new ManualResetEventSlim();
        using var _cap2 = PerceptionEmitterCapture.Capture(s =>
        {
            var r = JsonNode.Parse(s)?.AsObject();
            if ((string)r?["correlation_id"] == "h2-bulletin")
            {
                captured2 = s;
                latch2.Set();
            }
        });

        var node2 = JsonNode.Parse(
            """{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"h2-bulletin","args":{"neighborhood_id":1}}""").AsObject();

        var task2 = handler2.TryHandleAsync(node2, cityAries: stub2, default);
        await Task.Delay(30); // Let TCS be registered.

        stub2.MessageReceived(session: null, message: response);

        Assert.True(latch2.Wait(TimeSpan.FromSeconds(3)),
            "handler2 must resolve because its subscriber was registered independently (H3 per-instance gate)");

        var reply2 = JsonNode.Parse(captured2).AsObject();
        Assert.Equal("h2-bulletin", (string)reply2["correlation_id"]);
        Assert.True((bool)reply2["ok"],
            $"handler2 must resolve ok=true — if _subscriberRegistered were static this would timeout: {(string)reply2["error"]}");

        await task2;
    }
}
