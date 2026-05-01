using System;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Server.Clients;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Bot.Headless;

/// <summary>
/// Navigation verb family (freesoexperiment-a61). Ops shipped in this wave:
///
/// <list type="bullet">
///   <item><c>go-home</c> — already-home path only. Checks the avatar's LotLocation against
///   the current lot's TSOState.LotID. Returns <c>{ok:true, already_home:true}</c> when they
///   match. Cross-lot transitions require tearing down the lot Aries socket and rebuilding
///   the VM — a deep rework of <see cref="HeadlessLotConnection"/> (marked in its own doc as
///   "no reconnect logic (OQ-11 punted to a follow-up item)"). When the bot is NOT on its
///   home lot, this handler returns <c>{ok:true, already_home:false, deferred:true,
///   deferred_reason:"..."}</c> with the home location exposed so an agent can reason about
///   it. Cross-lot transition tracked as freesoexperiment-ca0 follow-up.</item>
///
///   <item><c>visit-lot</c> — same deferral. The wire PDU sequence (FindLotRequest → new
///   lot-Aries connection → JoinLotWithTransitionRequest) cannot run without discarding the
///   existing VMClientDriver + VM + perception projector and rebuilding them for the new
///   lot. That state surgery is out of scope for this session. Handler returns
///   <c>{ok:false, deferred:true, error:"..."}</c> with an honest marker so agents can
///   detect and fall back.</item>
///
///   <item><c>find-avatar</c> — full wire-effect implementation. Sends <c>FindAvatarRequest
///   {AvatarId}</c> on the city Aries socket, awaits <c>FindAvatarResponse{Status, LotId}</c>
///   correlated on the (AvatarId, recent-window) pair, returns <c>{status, lot_location,
///   on_lot}</c>. Note: the server-side <c>LotId</c> field in <c>FindAvatarResponse</c> is
///   actually the lot <b>location code</b> (populated from <c>AvatarClaim.location</c>),
///   not a DB id — same aliasing trap OQ-3 called out for <c>FindLotRequest</c>.</item>
/// </list>
///
/// <b>find-lot dropped.</b> docs/design/verb-catalog.md explicitly states:
/// "<c>find-lot</c> ... In the verb surface this is subsumed by <c>visit-lot</c> and by
/// data-service queries; do not declare as a separate convention op — mentioned for
/// completeness." Item constraint "If d87-0's catalog determines an op in this family has
/// no wire path, DROP the op" applies. No convention JSON, no handler.
///
/// <b>Thread safety.</b> <c>go-home</c> reads <c>VM.TSOState.LotID</c>, routed through
/// <c>HeadlessVMHost.RunUnderTickLock</c>. <c>find-avatar</c> touches only the city
/// AriesClient and a correlation map; the AriesClient's own write path is thread-safe
/// (used by multiple subscribers already). The correlation map is a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>; multiple concurrent find-avatar calls
/// with different target_avatar_ids interleave safely.
/// </summary>
public static class NavigationHandlers
{
    /// <summary>
    /// Pending find-avatar call registry. The server doesn't echo a per-request correlator;
    /// it only sets AvatarId on the response. We use AvatarId as the key — if multiple
    /// concurrent find-avatar calls target the same AvatarId, they all get the same
    /// response (that's fine, they want the same data). First-completer-wins, duplicate
    /// deliveries drop.
    /// </summary>
    private static readonly ConcurrentDictionary<uint, TaskCompletionSource<FindAvatarResponse>> _pendingFinds
        = new();

    /// <summary>
    /// Fallback home lot location captured at session start (from <c>avatar.LotLocation</c>).
    /// Used only when <c>FSO_HOME_LOT_LOCATION</c> env var is 0 or absent. Static because
    /// there is exactly one bot per process.
    /// </summary>
    private static uint _fallbackHomeLotLocation;

    /// <summary>
    /// Reads the effective home lot location for this invocation.
    /// Priority:
    ///   1. <c>FSO_HOME_LOT_LOCATION</c> environment variable (set by the sidecar on each
    ///      launch from <c>owned-lots.json</c> — the post-purchase home).
    ///   2. <c>_fallbackHomeLotLocation</c> captured at registration from
    ///      <c>avatar.LotLocation</c> (pre-purchase default).
    ///
    /// Re-reads the env on every call so a future in-process env update (if ever needed)
    /// is picked up without restarting. Currently the only writer is the sidecar supervisor
    /// which sets it before launching this process, so in practice the value is stable
    /// across the bot's lifetime.
    /// </summary>
    internal static uint ReadHomeLotLocation()
    {
        var envVal = Environment.GetEnvironmentVariable("FSO_HOME_LOT_LOCATION");
        if (!string.IsNullOrWhiteSpace(envVal))
        {
            // Accept both decimal and "0x..." hex forms.
            if (envVal.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (uint.TryParse(envVal.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out uint parsed) && parsed != 0)
                    return parsed;
            }
            else
            {
                if (uint.TryParse(envVal, out uint parsed) && parsed != 0)
                    return parsed;
            }
        }
        return _fallbackHomeLotLocation;
    }

    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost, AriesClient cityClient, uint homeLotLocation)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));
        if (cityClient == null) throw new ArgumentNullException(nameof(cityClient));

        _fallbackHomeLotLocation = homeLotLocation;

        // Wire a city subscriber for FindAvatarResponse. Done once at registration; the
        // city socket is long-lived for the life of the bot process.
        cityClient.AddSubscriber(new FindAvatarSubscriber());

        dispatcher.Register("go-home", (args, ct) => Task.FromResult(GoHome(vmHost)));
        dispatcher.Register("visit-lot", (args, ct) => Task.FromResult(VisitLot(args)));
        dispatcher.Register("find-avatar", (args, ct) => FindAvatarAsync(cityClient, args, ct));
    }

    // ---- go-home ----

    /// <summary>
    /// go-home (already-home path only). Returns:
    /// <code>
    ///   {ok:true, already_home:true,  current_lot_location:"0xF8F0DC", home_lot_location:"0xF8F0DC"}
    ///   {ok:true, already_home:false, deferred:true, deferred_reason:"...",
    ///    current_lot_location:"0x...", home_lot_location:"0x..."}
    ///   {ok:false, error:"no owned lot — persona has not purchased a lot yet"}
    /// </code>
    /// Agents MUST check <c>already_home</c> before assuming success. When
    /// <c>already_home=false</c>, the bot has not moved; <c>deferred=true</c> signals the
    /// cross-lot transition is unimplemented (tracked in rd follow-up).
    /// When <c>ok=false</c> with no <c>home_lot_location</c>, the persona owns no lot.
    /// </summary>
    internal static CommandDispatcher.Response GoHome(HeadlessVMHost vmHost)
    {
        // Re-read home lot location on every invocation: the sidecar sets
        // FSO_HOME_LOT_LOCATION before launching this process from owned-lots.json.
        // If the env var is 0 (absent or unset), no owned lot is known — return
        // ok:false so the agent can branch correctly.
        uint homeLotLocation = ReadHomeLotLocation();
        if (homeLotLocation == 0)
        {
            return CommandDispatcher.Response.Fail("no owned lot — persona has not purchased a lot yet (FSO_HOME_LOT_LOCATION not set)");
        }

        uint currentLotLocation = vmHost.RunUnderTickLock<uint>(() =>
        {
            var lotState = vmHost.VM?.TSOState as VMTSOLotState;
            return lotState?.LotID ?? 0u;
        });

        if (currentLotLocation == 0)
        {
            return CommandDispatcher.Response.Fail("no live lot (TSOState.LotID == 0)");
        }

        bool alreadyHome = currentLotLocation == homeLotLocation;
        string homeHex = "0x" + homeLotLocation.ToString("X");
        string curHex = "0x" + currentLotLocation.ToString("X");

        if (alreadyHome)
        {
            return CommandDispatcher.Response.Success(new
            {
                already_home = true,
                current_lot_location = curHex,
                home_lot_location = homeHex,
            });
        }

        // Deferred — cross-lot transition not implemented. Return ok=true with a
        // deferred marker so agents can branch on it; the bot's state is unchanged.
        return CommandDispatcher.Response.Success(new
        {
            already_home = false,
            deferred = true,
            deferred_reason = "cross-lot transition unimplemented in HeadlessLotConnection; tracked as freesoexperiment-ca0 follow-up",
            current_lot_location = curHex,
            home_lot_location = homeHex,
        });
    }

    // ---- visit-lot ----

    /// <summary>
    /// visit-lot — deferred. A real visit-lot tears down the Lot Aries socket, rebuilds the
    /// VMClientDriver + VM + perception projector for the new lot, and issues the
    /// <see cref="JoinLotWithTransitionRequest"/> handshake. <see cref="HeadlessLotConnection"/>
    /// has no reconnect primitive today (OQ-11 deferred). Shipping a stub that fakes success
    /// would mis-train agents. Returns honest deferral.
    /// </summary>
    internal static CommandDispatcher.Response VisitLot(JsonObject args)
    {
        // Still require the arg so the convention contract is stable — when transition
        // support lands, the same clients keep working.
        if (args["target_lot_location"] == null)
        {
            return CommandDispatcher.Response.Fail("target_lot_location required (hex 0x... or decimal; it is a location code, not a DB id — per OQ-3)");
        }
        // Return Ok=true with deferred=true in the payload. Rationale: forwardIPC on the Go
        // side surfaces only `error` when Ok=false; Ok=true+deferred=true preserves the
        // structured deferral marker so agents can branch on it programmatically (per
        // wave-2b "deferral honesty" standards — deferred:true marker must be machine-
        // detectable in the payload, not buried in a prose error string).
        return CommandDispatcher.Response.Success(new
        {
            deferred = true,
            deferred_reason = "visit-lot requires teardown+rebuild of VMClientDriver + VM + perception projector and the JoinLotWithTransitionRequest handshake. HeadlessLotConnection has no reconnect primitive today. Tracked as freesoexperiment-ca0.",
            target_lot_location = args["target_lot_location"].ToString(),
            bot_state_unchanged = true,
        });
    }

    // ---- find-avatar ----

    /// <summary>
    /// find-avatar — <see cref="FindAvatarRequest"/> on the city socket, correlated response
    /// via a pending-TCS map keyed by AvatarId. 10s timeout (matches sidecar-side default).
    /// </summary>
    internal static async Task<CommandDispatcher.Response> FindAvatarAsync(AriesClient cityClient, JsonObject args, CancellationToken ct)
    {
        if (args["target_avatar_id"] == null)
        {
            return CommandDispatcher.Response.Fail("target_avatar_id required");
        }
        uint targetAvatarId;
        try
        {
            // Accept both number and numeric-string forms.
            targetAvatarId = args["target_avatar_id"] is JsonValue v && v.TryGetValue<long>(out var lv)
                ? (uint)lv
                : uint.Parse(args["target_avatar_id"].ToString());
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"target_avatar_id not a valid uint: {ex.Message}");
        }
        if (targetAvatarId == 0)
        {
            return CommandDispatcher.Response.Fail("target_avatar_id must be > 0");
        }

        // Register pending slot. If a slot already exists for this avatar_id (concurrent
        // caller), reuse it — both callers await the same response.
        var tcs = _pendingFinds.GetOrAdd(targetAvatarId,
            _ => new TaskCompletionSource<FindAvatarResponse>(TaskCreationOptions.RunContinuationsAsynchronously));

        try
        {
            // Write outbound. AriesClient.Write is thread-safe; no extra lock needed.
            cityClient.Write(new FindAvatarRequest { AvatarId = targetAvatarId });

            // Wait for response or timeout.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var responseTask = tcs.Task;
            var first = await Task.WhenAny(responseTask, Task.Delay(Timeout.Infinite, cts.Token));
            if (first != responseTask)
            {
                return CommandDispatcher.Response.Fail($"find-avatar timeout after 10s for avatar_id={targetAvatarId}");
            }

            var resp = await responseTask;
            return CommandDispatcher.Response.Success(new
            {
                target_avatar_id = (long)resp.AvatarId,
                status = resp.Status.ToString(),
                on_lot = resp.Status == FindAvatarResponseStatus.FOUND,
                lot_location = resp.LotId == 0 ? null : "0x" + resp.LotId.ToString("X"),
                lot_location_raw = (long)resp.LotId,
            });
        }
        finally
        {
            // If we're the last caller to exit, drop the slot. Use remove-if-match.
            _pendingFinds.TryRemove(new System.Collections.Generic.KeyValuePair<uint, TaskCompletionSource<FindAvatarResponse>>(targetAvatarId, tcs));
        }
    }

    /// <summary>
    /// City-socket subscriber that routes inbound <see cref="FindAvatarResponse"/> packets
    /// to the pending-TCS registry. All other packets pass through untouched — this is
    /// additive on top of any existing city subscribers.
    /// </summary>
    private sealed class FindAvatarSubscriber : IAriesMessageSubscriber, IAriesEventSubscriber
    {
        public void MessageReceived(AriesClient client, object message)
        {
            if (message is FindAvatarResponse resp)
            {
                if (_pendingFinds.TryGetValue(resp.AvatarId, out var tcs))
                {
                    tcs.TrySetResult(resp);
                }
                else
                {
                    Program.Log($"[nav] stray FindAvatarResponse avatar_id={resp.AvatarId} status={resp.Status} (no pending)");
                }
            }
        }

        public void SessionCreated(AriesClient c) { }
        public void SessionOpened(AriesClient c) { }
        public void SessionClosed(AriesClient c)
        {
            // Drop any pending — the city socket went away.
            foreach (var kv in _pendingFinds)
            {
                kv.Value.TrySetCanceled();
            }
            _pendingFinds.Clear();
        }
        public void SessionIdle(AriesClient c) { }
        public void InputClosed(AriesClient c) { }
    }
}
