/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Common.Domain.Realestate;
using FSO.Server.Clients;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;

namespace FSO.Bot.Headless;

/// <summary>
/// Bot-side handler for the bot-cmd IPC envelope (freesoexperiment-0de).
///
/// <para>
/// Wire shapes (sidecar→bot stdin):
/// <code>{"kind":"bot-cmd","cmd":"probe-lot","correlation_id":"&lt;uuid&gt;","args":{"lot_location":12345}}</code>
/// <code>{"kind":"bot-cmd","cmd":"bot-exit-request","correlation_id":"&lt;uuid&gt;","args":{}}</code>
/// <code>{"kind":"bot-cmd","cmd":"probe-road","correlation_id":"&lt;uuid&gt;","args":{"x":249,"y":348}}</code>
/// <code>{"kind":"bot-cmd","cmd":"purchase-lot","correlation_id":"&lt;uuid&gt;","args":{"location_x":249,"location_y":348,"name":"marlo's place","start_fresh":false}}</code>
/// </para>
///
/// <para>
/// Wire shapes (bot→sidecar stdout):
/// <code>{"kind":"bot-cmd-reply","correlation_id":"&lt;uuid&gt;","ok":true,"data":{"status":"FOUND","lot_id":2}}</code>
/// <code>{"kind":"bot-cmd-reply","correlation_id":"&lt;uuid&gt;","ok":false,"error":"..."}</code>
/// </para>
///
/// <para>
/// <b>Detection:</b> <see cref="CommandDispatcher.HandleLineAsync"/> reads the <c>"kind"</c>
/// field before dispatching. Lines with <c>kind=="bot-cmd"</c> are forwarded here; lines
/// without a <c>"kind"</c> field (or with any other value) go through the normal
/// <c>op</c>-dispatch path.
/// </para>
///
/// <para>
/// <b>probe-lot</b> issues <see cref="FindLotRequest"/> on the city Aries socket and awaits
/// the <see cref="FindLotResponse"/> using a one-shot TCS keyed on the correlation_id.
/// The <see cref="FindLotResponseStatus"/> is surfaced verbatim so the sidecar can route
/// FOUND vs NOT_OPEN (CLOSED) vs any other status without C# interpretation.
/// </para>
///
/// <para>
/// <b>bot-exit-request</b> sends the reply first (so the sidecar can fulfill its
/// convention.Response before the bot exits), then triggers cooperative shutdown
/// via <see cref="Program.ShutdownToken"/> cancellation. The bot exits 0; the
/// sidecar supervisor loop will see the clean exit and relaunch at the next-lot
/// location (or the home lot if next-lot is absent).
/// </para>
///
/// <para>
/// <b>Correlation model:</b> probe-lot uses a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// keyed on correlation_id so multiple concurrent probes (different lot locations) are
/// safe. Bot-exit-request is single-use; the response is emitted directly.
/// </para>
/// </summary>
public static class BotCmdHandler
{
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<FindLotResponse>> _pendingProbes = new();
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<PurchaseLotResponse>> _pendingPurchases = new();
    private static readonly object _subscriberOnce = new();
    private static bool _subscriberAdded;

    /// <summary>
    /// Register this handler's city-socket subscriber. Called once at startup, idempotent.
    /// </summary>
    public static void RegisterSubscriber(AriesClient cityAries)
    {
        if (cityAries == null) throw new ArgumentNullException(nameof(cityAries));
        lock (_subscriberOnce)
        {
            if (_subscriberAdded) return;
            cityAries.AddSubscriber(new ProbeLotSubscriber());
            _subscriberAdded = true;
        }
    }

    /// <summary>
    /// Dispatch a bot-cmd line. Returns false if the line is not a bot-cmd (caller handles
    /// it as a normal IPC op). Returns true when the line was handled (reply already emitted).
    /// </summary>
    public static async Task<bool> TryHandleAsync(
        JsonObject node,
        AriesClient cityAries,
        CancellationToken ct)
    {
        if (node == null) return false;
        var kind = (string)node["kind"];
        if (kind != "bot-cmd") return false;

        var corrId = (string)node["correlation_id"];
        var cmd = (string)node["cmd"];

        if (string.IsNullOrEmpty(corrId) || string.IsNullOrEmpty(cmd))
        {
            Program.Log("[bot-cmd] missing correlation_id or cmd (dropped)");
            return true; // consumed but invalid — don't fall through to normal IPC
        }

        var args = node["args"] as JsonObject ?? new JsonObject();

        switch (cmd)
        {
            case "probe-lot":
                await HandleProbeLotAsync(corrId, cityAries, args, ct);
                return true;

            case "probe-road":
                HandleProbeRoad(corrId, args);
                return true;

            case "purchase-lot":
                await HandlePurchaseLotAsync(corrId, cityAries, args, ct);
                return true;

            case "bot-exit-request":
                HandleBotExitRequest(corrId);
                return true;

            default:
                EmitReply(corrId, ok: false, error: $"unknown bot-cmd: {cmd}");
                return true;
        }
    }

    // ---- probe-lot ----

    private static async Task HandleProbeLotAsync(
        string corrId,
        AriesClient cityAries,
        JsonObject args,
        CancellationToken ct)
    {
        if (cityAries == null)
        {
            EmitReply(corrId, ok: false, error: "probe-lot: city socket unavailable");
            return;
        }

        // Parse lot_location arg (uint32 — a location code, not a DB lot_id; see CLAUDE.md gotcha 11).
        uint lotLocation;
        var locNode = args["lot_location"];
        if (locNode == null)
        {
            EmitReply(corrId, ok: false, error: "probe-lot: lot_location required");
            return;
        }
        try
        {
            // Accept both decimal (int64) and hex-string "0x..." forms.
            if (locNode is JsonValue v && v.TryGetValue<long>(out var lv))
                lotLocation = (uint)lv;
            else
                lotLocation = ParseLocationArg(locNode.ToString());
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"probe-lot: bad lot_location: {ex.Message}");
            return;
        }

        // Register a one-shot TCS keyed on corrId.
        var tcs = _pendingProbes.GetOrAdd(corrId,
            _ => new TaskCompletionSource<FindLotResponse>(TaskCreationOptions.RunContinuationsAsynchronously));

        try
        {
            cityAries.Write(new FindLotRequest
            {
                LotId = lotLocation,    // FSO wire field is called LotId but carries the location code (see OQ-3)
                OpenIfClosed = false,   // probe only — do not side-effect the lot state
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var first = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (first != tcs.Task)
            {
                EmitReply(corrId, ok: false, error: "probe-lot: timeout waiting for FindLotResponse");
                return;
            }

            var resp = await tcs.Task;
            EmitReply(corrId, ok: true, data: new
            {
                status = resp.Status.ToString(),
                lot_id = (long)resp.LotId,
                address = resp.Address ?? string.Empty,
            });
        }
        catch (OperationCanceledException)
        {
            EmitReply(corrId, ok: false, error: "probe-lot: cancelled");
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"probe-lot: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _pendingProbes.TryRemove(new System.Collections.Generic.KeyValuePair<string, TaskCompletionSource<FindLotResponse>>(corrId, tcs));
        }
    }

    // ---- probe-road ----

    /// <summary>
    /// probe-road: synchronous road-bits check. Reads the city map's road bitmap
    /// at the given (x, y) tile and returns {has_road, road_bits, x, y}.
    ///
    /// CLAUDE.md gotcha 12: lot placement requires road bits on the lot tile
    /// itself. IsOpenable(x,y) checks (GetRoad(x,y) &amp; 0xF) != 0. A tile adjacent
    /// to a road does NOT qualify unless its own road-bitmask is non-zero.
    ///
    /// This is synchronous — no city socket round-trip, just a Content.CityMaps
    /// lookup which is already in-memory after game init.
    /// </summary>
    private static void HandleProbeRoad(string corrId, JsonObject args)
    {
        ushort x, y;
        try
        {
            x = ParseUShort(args, "x");
            y = ParseUShort(args, "y");
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"probe-road: bad args: {ex.Message}");
            return;
        }

        try
        {
            // FSO shard always uses city_0001 on workshop (CLAUDE.md note 10).
            // CityMapsProvider.Get("1") → CityMap for map id 1.
            var cityMaps = FSO.Content.Content.Get().CityMaps;
            var cityMap = cityMaps.Get("1");
            if (cityMap == null)
            {
                EmitReply(corrId, ok: false, error: "probe-road: city map 0001 not loaded");
                return;
            }

            byte roadBits = cityMap.GetRoad(x, y);
            bool hasRoad = (roadBits & 0x0F) != 0;

            EmitReply(corrId, ok: true, data: new
            {
                has_road  = hasRoad,
                road_bits = (int)roadBits,
                x         = (int)x,
                y         = (int)y,
            });
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"probe-road: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ---- purchase-lot ----

    /// <summary>
    /// purchase-lot: sends <see cref="PurchaseLotRequest"/> on the city Aries socket
    /// and awaits <see cref="PurchaseLotResponse"/>.
    ///
    /// Args (validated by the sidecar before this bot-cmd is dispatched):
    ///   location_x  (ushort) — high 16 bits of the packed lot location
    ///   location_y  (ushort) — low 16 bits of the packed lot location
    ///   name        (string) — lot name
    ///   start_fresh (bool)   — optional; defaults false
    ///
    /// Reply: {ok, status, reason?, lot_id, new_funds}
    ///   ok=true  → status=="SUCCESS", lot_id = new DB lot_id, new_funds = updated balance
    ///   ok=true, status=="FAILED" → server-side refuse; sidecar interprets reason
    ///   ok=false → local error (timeout, bad args, city socket down)
    /// </summary>
    private static async Task HandlePurchaseLotAsync(
        string corrId,
        AriesClient cityAries,
        JsonObject args,
        CancellationToken ct)
    {
        if (cityAries == null)
        {
            EmitReply(corrId, ok: false, error: "purchase-lot: city socket unavailable");
            return;
        }

        ushort locX, locY;
        string name;
        bool startFresh;
        try
        {
            locX = ParseUShort(args, "location_x");
            locY = ParseUShort(args, "location_y");
            name = (string)args["name"] ?? throw new ArgumentException("name required");
            startFresh = args["start_fresh"] is JsonValue sfv && sfv.TryGetValue<bool>(out var sfb) && sfb;
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"purchase-lot: bad args: {ex.Message}");
            return;
        }

        // Register one-shot TCS keyed on corrId.
        var tcs = _pendingPurchases.GetOrAdd(corrId,
            _ => new TaskCompletionSource<PurchaseLotResponse>(TaskCreationOptions.RunContinuationsAsynchronously));

        try
        {
            cityAries.Write(new PurchaseLotRequest
            {
                LotLocation_X = locX,
                LotLocation_Y = locY,
                Name          = name,
                StartFresh    = startFresh,
                MayorMode     = false,  // agents never purchase in mayor mode (sidecar has no mayor flag)
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var first = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (first != tcs.Task)
            {
                EmitReply(corrId, ok: false, error: "purchase-lot: timeout waiting for PurchaseLotResponse");
                return;
            }

            var resp = await tcs.Task;

            // Surface the response verbatim. ok=true even for server-side failures so the
            // sidecar can inspect the status/reason fields and apply escalation hints
            // (NHOOD_RESERVED etc.) without the Go forwardIPC layer hiding them in ok:false.
            EmitReply(corrId, ok: true, data: new
            {
                status    = resp.Status.ToString(),
                reason    = resp.Status == PurchaseLotStatus.FAILED ? resp.Reason.ToString() : null,
                lot_id    = (long)resp.NewLotId,
                new_funds = (long)resp.NewFunds,
            });
        }
        catch (OperationCanceledException)
        {
            EmitReply(corrId, ok: false, error: "purchase-lot: cancelled");
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"purchase-lot: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _pendingPurchases.TryRemove(
                new System.Collections.Generic.KeyValuePair<string, TaskCompletionSource<PurchaseLotResponse>>(
                    corrId, tcs));
        }
    }

    // ---- bot-exit-request ----

    private static void HandleBotExitRequest(string corrId)
    {
        // Emit the reply BEFORE triggering shutdown. The sidecar awaits this reply
        // to fulfill its convention.Response; if we exit first, the sidecar never
        // sees the reply and the convention call times out (design §Q3 step 7 / B1
        // mitigation: "handler writes fulfills BEFORE issuing bot-exit-request").
        EmitReply(corrId, ok: true, data: new { accepted = true, reason = "graceful cross-lot transition" });

        // Cooperative shutdown: cancel the shared CTS so the hold loop in Program.cs
        // exits cleanly via the SIGINT path, which then calls TryCleanDisconnect for a
        // clean ClientByePDU + city disconnect. The bot exits 0; the sidecar supervisor
        // sees a clean exit and relaunches at next-lot.
        Program.Log("[bot-cmd] bot-exit-request received — initiating cooperative shutdown");
        try
        {
            // Use the same CTS that SIGINT / CancelKeyPress cancels.
            // Field is internal to Program — we call the static helper.
            BotCmdHandler.TriggerShutdown();
        }
        catch (Exception ex)
        {
            Program.Log($"[bot-cmd] bot-exit-request shutdown trigger: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Trigger cooperative shutdown. Called by bot-exit-request handler.
    /// Cancels <see cref="Program.ShutdownToken"/>'s source so the hold loop
    /// exits on the next iteration.
    /// </summary>
    internal static void TriggerShutdown()
    {
        Program.RequestShutdown("[bot-cmd] bot-exit-request");
    }

    // ---- city-socket subscriber ----

    private sealed class ProbeLotSubscriber : IAriesMessageSubscriber, IAriesEventSubscriber
    {
        public void MessageReceived(AriesClient client, object message)
        {
            // FindLotResponse → complete the first pending probe-lot TCS (FIFO).
            if (message is FindLotResponse resp)
            {
                // All TCS in _pendingProbes are from probe-lot calls. We can't
                // distinguish by correlation_id alone here (server doesn't echo
                // correlation_id). In practice at most one probe is pending at a time
                // (serial IPC dispatch). FIFO delivery is correct.
                foreach (var kv in _pendingProbes)
                {
                    if (kv.Value.Task.IsCompleted) continue;
                    kv.Value.TrySetResult(resp);
                    break;
                }
                return;
            }

            // PurchaseLotResponse → complete the first pending purchase-lot TCS.
            // Same serial-dispatch reasoning as probe-lot: at most one purchase in flight.
            if (message is PurchaseLotResponse pr)
            {
                foreach (var kv in _pendingPurchases)
                {
                    if (kv.Value.Task.IsCompleted) continue;
                    kv.Value.TrySetResult(pr);
                    break;
                }
            }
        }

        public void SessionCreated(AriesClient c) { }
        public void SessionOpened(AriesClient c) { }
        public void SessionClosed(AriesClient c)
        {
            foreach (var kv in _pendingProbes)
                kv.Value.TrySetCanceled();
            _pendingProbes.Clear();
            foreach (var kv in _pendingPurchases)
                kv.Value.TrySetCanceled();
            _pendingPurchases.Clear();
        }
        public void SessionIdle(AriesClient c) { }
        public void InputClosed(AriesClient c) { }
    }

    // ---- helpers ----

    /// <summary>
    /// Read a ushort arg from a JsonObject. Accepts int64 JSON numbers and
    /// numeric strings. Throws ArgumentException on missing or bad value.
    /// </summary>
    private static ushort ParseUShort(JsonObject args, string key)
    {
        var node = args[key];
        if (node == null) throw new ArgumentException($"{key} required");
        if (node is JsonValue v && v.TryGetValue<long>(out var lv)) return (ushort)lv;
        if (ushort.TryParse(node.ToString(), out var u)) return u;
        throw new ArgumentException($"{key} must be a valid ushort (got {node})");
    }

    // ---- emit helper ----

    private static void EmitReply(string corrId, bool ok, object data = null, string error = null)
    {
        try
        {
            var obj = new JsonObject
            {
                ["kind"]           = "bot-cmd-reply",
                ["correlation_id"] = corrId,
                ["ok"]             = ok,
            };
            if (ok && data != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                obj["data"] = System.Text.Json.Nodes.JsonNode.Parse(json);
            }
            if (!ok)
            {
                obj["error"] = error ?? "unknown";
            }
            PerceptionEmitter.EmitLine(obj.ToJsonString());
        }
        catch (Exception ex)
        {
            Program.Log($"[bot-cmd] EmitReply {corrId} failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ---- helpers ----

    private static uint ParseLocationArg(string s)
    {
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToUInt32(s, 16);
        }
        return uint.Parse(s);
    }
}
