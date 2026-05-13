/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Common.Domain.Realestate;
using FSO.Files.Formats.tsodata;
using FSO.Server.Clients;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Server.Protocol.Voltron.Packets;

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
/// <code>{"kind":"bot-cmd","cmd":"probe-bulletin","correlation_id":"&lt;uuid&gt;","args":{"neighborhood_id":1}}</code>
/// <code>{"kind":"bot-cmd","cmd":"create-avatar","correlation_id":"&lt;uuid&gt;","args":{"first_name":"Vivi","last_name":"Romero","gender":"F","skin_tone":"light","head_guid":399,"body_guid":1192}}</code>
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
/// <b>Correlation model (freesoexperiment-f70 H1):</b> pending dictionaries store
/// <c>(lotContext, TCS)</c> tuples so that <c>MessageReceived</c> can match
/// <see cref="FindLotResponse.LotId"/> against the requested <c>lot_location</c>
/// rather than completing the first pending entry regardless of which lot was requested.
/// For bulletin, the neighborhood_id is stored alongside the TCS for diagnostic purposes.
/// </para>
///
/// <para>
/// <b>Lifecycle (freesoexperiment-f70 H2/H3):</b> this is an <b>instance class</b>
/// (not static). One instance is created per bot session in <see cref="Program"/> and
/// bound to its <see cref="AriesClient"/>. Instance state (<c>_pendingProbes</c>,
/// <c>_sessionClosed</c>, etc.) is per-session — parallel bot sessions cannot share
/// state. <c>GetOrAdd</c> is guarded against post-<c>SessionClosed</c> re-creation
/// via a lock+flag pattern: if the session is already closed, <c>GetOrAdd</c> returns
/// <c>null</c> and the caller emits an error immediately.
/// </para>
/// </summary>
public sealed class BotCmdHandler
{
    // ---- H1: lot-keyed pending dictionaries ----
    //
    // Each entry stores (lotContext, tcs) so MessageReceived can match by lot/nhood
    // rather than blindly completing the first incomplete TCS.
    //
    // For probe-lot: lotContext = lotLocation (uint) cast to ulong.
    // For probe-bulletin: lotContext = nhoodId (uint) cast to ulong.
    //   BulletinResponse doesn't echo nhood; we store it for diagnostics and fall
    //   back to FIFO when the response type is ambiguous (same semantics as before
    //   but with explicit lot-keying in the dict value).
    // For purchase-lot: lotContext = packed (locX << 16 | locY).

    private readonly ConcurrentDictionary<string, (ulong LotCtx, TaskCompletionSource<FindLotResponse> Tcs)>
        _pendingProbes = new();
    private readonly ConcurrentDictionary<string, (ulong LotCtx, TaskCompletionSource<PurchaseLotResponse> Tcs)>
        _pendingPurchases = new();
    private readonly ConcurrentDictionary<string, (ulong LotCtx, TaskCompletionSource<BulletinResponse> Tcs)>
        _pendingBulletins = new();

    // create-avatar: FIFO (CreateASimResponse carries no correlator; serial IPC dispatch).
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CreateASimResponse>>
        _pendingCreateSim = new();

    // ---- H2: SessionClosed / GetOrAdd race guard ----
    //
    // Protecting against: SessionClosed fires, sets _sessionClosed = true, clears dicts;
    // concurrently, a handler task reads _sessionClosed == false (before the set),
    // then SessionClosed clears, then GetOrAdd recreates a TCS for a dead session.
    //
    // Fix: use _pendingLock for the check-then-add critical section AND for the
    // check-then-cancel-clear in SessionClosed. GetOrAdd-equivalent is done
    // manually under the lock: if closed, return null (caller emits error).
    private readonly object _pendingLock = new();
    private volatile bool _sessionClosed;

    // ---- subscriber registration (H3: per-instance, not static) ----
    private bool _subscriberRegistered;

    /// <summary>
    /// Register this handler's city-socket subscriber on the given <paramref name="cityAries"/>.
    /// Idempotent — safe to call multiple times; only registers once per instance.
    /// </summary>
    public void RegisterSubscriber(AriesClient cityAries)
    {
        if (cityAries == null) throw new ArgumentNullException(nameof(cityAries));
        lock (_pendingLock)
        {
            if (_subscriberRegistered) return;
            cityAries.AddSubscriber(new ProbeLotAndPurchaseSubscriber(this));
            cityAries.AddSubscriber(new ProbeBulletinSubscriber(this));
            cityAries.AddSubscriber(new CreateASimSubscriber(this));
            _subscriberRegistered = true;
        }
    }

    /// <summary>
    /// Dispatch a bot-cmd line. Returns false if the line is not a bot-cmd (caller handles
    /// it as a normal IPC op). Returns true when the line was handled (reply already emitted).
    /// </summary>
    public async Task<bool> TryHandleAsync(
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

            case "probe-bulletin":
                await HandleProbeBulletinAsync(corrId, cityAries, args, ct);
                return true;

            case "create-avatar":
                await HandleCreateAvatarAsync(corrId, cityAries, args, ct);
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

    private async Task HandleProbeLotAsync(
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

        // H2 + H1: under lock, check session not closed, then add lot-keyed TCS.
        TaskCompletionSource<FindLotResponse> tcs;
        lock (_pendingLock)
        {
            if (_sessionClosed)
            {
                EmitReply(corrId, ok: false, error: "probe-lot: session already closed");
                return;
            }
            var entry = (LotCtx: (ulong)lotLocation,
                         Tcs: new TaskCompletionSource<FindLotResponse>(TaskCreationOptions.RunContinuationsAsynchronously));
            _pendingProbes[corrId] = entry;
            tcs = entry.Tcs;
        }

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
            _pendingProbes.TryRemove(corrId, out _);
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
    private async Task HandlePurchaseLotAsync(
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

        // H2 + H1: under lock, check session not closed, then add lot-keyed TCS.
        // lot context = packed location (locX << 16 | locY).
        TaskCompletionSource<PurchaseLotResponse> tcs;
        lock (_pendingLock)
        {
            if (_sessionClosed)
            {
                EmitReply(corrId, ok: false, error: "purchase-lot: session already closed");
                return;
            }
            var lotCtx = (ulong)(((uint)locX << 16) | locY);
            var entry = (LotCtx: lotCtx,
                         Tcs: new TaskCompletionSource<PurchaseLotResponse>(TaskCreationOptions.RunContinuationsAsynchronously));
            _pendingPurchases[corrId] = entry;
            tcs = entry.Tcs;
        }

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
            _pendingPurchases.TryRemove(corrId, out _);
        }
    }

    // ---- probe-bulletin ----

    /// <summary>
    /// probe-bulletin (freesoexperiment-923): issues <see cref="BulletinRequest"/>
    /// with <c>Type=GET_MESSAGES</c> on the city Aries socket and awaits the
    /// inbound <see cref="BulletinResponse"/>.
    ///
    /// <para>
    /// This mirrors the sidecar-facing path in <see cref="CityHandlers.ViewBulletinAsync"/>
    /// but is reached via the bot-cmd IPC channel rather than through the normal IPC
    /// op dispatcher. The sidecar's <c>bulletinBoardHandler</c> (interaction_handlers.go)
    /// sends <c>probe-bulletin</c> when the agent invokes
    /// <c>interact-with object_type=bulletin_board</c>.
    /// </para>
    ///
    /// <para>
    /// <b>Args</b>: <c>neighborhood_id</c> (uint, optional; defaults to 1).
    /// </para>
    ///
    /// <para>
    /// <b>Reply data shape</b> (consumed by the sidecar's bulletinBoardHandler):
    /// <code>{"messages":[{...},...], "count":N}</code>
    /// Each message object matches the shape produced by
    /// <see cref="CityHandlers.ViewBulletinAsync"/>:
    /// <c>bulletin_id, nhood_id, sender_id, sender_name, subject, body, time,
    /// type, flags, lot_id</c>.
    /// </para>
    ///
    /// <para>
    /// <b>Correlation model (H1)</b>: nhoodId is stored alongside the TCS so the
    /// subscriber can log which nhood was requested. <see cref="BulletinResponse"/>
    /// doesn't echo the nhood id; the subscriber uses FIFO completion (first incomplete
    /// TCS), which is safe given serial IPC dispatch.
    /// </para>
    /// </summary>
    private async Task HandleProbeBulletinAsync(
        string corrId,
        AriesClient cityAries,
        JsonObject args,
        CancellationToken ct)
    {
        if (cityAries == null)
        {
            EmitReply(corrId, ok: false, error: "probe-bulletin: city socket unavailable");
            return;
        }

        // Neighborhood id — optional, default 1 (workshop's only nhood).
        uint nhoodId = 1;
        var nhoodNode = args["neighborhood_id"];
        if (nhoodNode != null)
        {
            try
            {
                if (nhoodNode is JsonValue nv && nv.TryGetValue<long>(out var lv))
                    nhoodId = (uint)lv;
                else
                    nhoodId = uint.Parse(nhoodNode.ToString());
            }
            catch (Exception ex)
            {
                EmitReply(corrId, ok: false, error: $"probe-bulletin: bad neighborhood_id: {ex.Message}");
                return;
            }
        }

        // H2 + H1: under lock, check session not closed, then add nhood-keyed TCS.
        TaskCompletionSource<BulletinResponse> tcs;
        lock (_pendingLock)
        {
            if (_sessionClosed)
            {
                EmitReply(corrId, ok: false, error: "probe-bulletin: session already closed");
                return;
            }
            var entry = (LotCtx: (ulong)nhoodId,
                         Tcs: new TaskCompletionSource<BulletinResponse>(TaskCreationOptions.RunContinuationsAsynchronously));
            _pendingBulletins[corrId] = entry;
            tcs = entry.Tcs;
        }

        try
        {
            cityAries.Write(new BulletinRequest
            {
                Type = BulletinRequestType.GET_MESSAGES,
                TargetNHood = nhoodId,
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var first = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (first != tcs.Task)
            {
                EmitReply(corrId, ok: false, error: "probe-bulletin: timeout waiting for BulletinResponse");
                return;
            }

            var resp = await tcs.Task;
            if (resp.Type != BulletinResponseType.MESSAGES)
            {
                EmitReply(corrId, ok: false, error: $"probe-bulletin: server returned {resp.Type}");
                return;
            }

            var messages = (resp.Messages ?? Array.Empty<BulletinItem>()).Select(b => new
            {
                bulletin_id = (long)b.ID,
                nhood_id = (long)b.NhoodID,
                sender_id = (long)b.SenderID,
                sender_name = b.SenderName ?? string.Empty,
                subject = b.Subject ?? string.Empty,
                body = b.Body ?? string.Empty,
                time = b.Time,
                type = b.Type.ToString(),
                flags = (int)b.Flags,
                lot_id = (long)b.LotID,
            }).ToArray();

            EmitReply(corrId, ok: true, data: new
            {
                neighborhood_id = (long)nhoodId,
                count = messages.Length,
                messages,
            });
        }
        catch (OperationCanceledException)
        {
            EmitReply(corrId, ok: false, error: "probe-bulletin: cancelled");
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"probe-bulletin: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _pendingBulletins.TryRemove(corrId, out _);
        }
    }

    // ---- create-avatar (freesoexperiment-92a) ----

    /// <summary>
    /// create-avatar: sends <see cref="RSGZWrapperPDU"/> on the city Aries socket and
    /// awaits the <see cref="CreateASimResponse"/> from the server's RegistrationHandler.
    ///
    /// <para>
    /// The bot must be running in <c>--chargen-mode</c> (city-connected, no lot joined).
    /// In normal (lot-joined) mode this command will still reach the city socket but the
    /// server may reject it because the avatar claim is already in use — the sidecar returns
    /// <c>ok:false reason:session-already-bound</c> in that case.
    /// </para>
    ///
    /// <para>
    /// <b>Args</b>:
    ///   first_name  (string) — non-empty (validated sidecar-side before this is called)
    ///   last_name   (string) — non-empty
    ///   gender      (string) — "M" or "F" (upper-case, already normalised by sidecar)
    ///   skin_tone   (string) — "light", "medium", or "dark" (lower-case, normalised)
    ///   head_guid   (uint)   — high 32 bits of TSO head OutfitID (must be > 0)
    ///   body_guid   (uint)   — high 32 bits of TSO body OutfitID (must be > 0)
    ///   description (string) — avatar bio; may be empty
    /// </para>
    ///
    /// <para>
    /// <b>Reply data shape</b> (ok=true for both SUCCESS and engine-side FAILED, so
    /// the sidecar can inspect status/reason without Go's forwardIPC hiding them):
    /// <code>{"status":"SUCCESS","avatar_id":N}</code>
    /// <code>{"status":"FAILED","reason":"NAME_TAKEN"}</code>
    /// </para>
    /// </summary>
    private async Task HandleCreateAvatarAsync(
        string corrId,
        AriesClient cityAries,
        JsonObject args,
        CancellationToken ct)
    {
        if (cityAries == null)
        {
            EmitReply(corrId, ok: false, error: "create-avatar: city socket unavailable");
            return;
        }

        // Parse args (already validated sidecar-side; parse again defensively).
        string firstName, lastName, genderStr, skinToneStr, description;
        uint headGuid, bodyGuid;
        try
        {
            firstName   = (string)args["first_name"]  ?? throw new ArgumentException("first_name required");
            lastName    = (string)args["last_name"]   ?? throw new ArgumentException("last_name required");
            genderStr   = (string)args["gender"]      ?? throw new ArgumentException("gender required");
            skinToneStr = (string)args["skin_tone"]   ?? "light";
            description = (string)args["description"] ?? "";

            headGuid = ParseUInt32Arg(args, "head_guid");
            bodyGuid = ParseUInt32Arg(args, "body_guid");

            if (headGuid == 0) throw new ArgumentException("head_guid must be > 0");
            if (bodyGuid == 0) throw new ArgumentException("body_guid must be > 0");
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"create-avatar: bad args: {ex.Message}");
            return;
        }

        Gender gender = genderStr.Equals("F", StringComparison.OrdinalIgnoreCase) ? Gender.FEMALE : Gender.MALE;
        SkinTone skinTone = skinToneStr switch
        {
            "medium" => SkinTone.MEDIUM,
            "dark"   => SkinTone.DARK,
            _        => SkinTone.LIGHT,
        };

        // H2: under lock, check session not closed, then register a TCS for the response.
        TaskCompletionSource<CreateASimResponse> tcs;
        lock (_pendingLock)
        {
            if (_sessionClosed)
            {
                EmitReply(corrId, ok: false, error: "create-avatar: session already closed");
                return;
            }
            tcs = new TaskCompletionSource<CreateASimResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingCreateSim[corrId] = tcs;
        }

        // FSO full name wire format: "FirstName LastName" as a single Name field.
        string fullName = $"{firstName} {lastName}";

        try
        {
            cityAries.Write(new RSGZWrapperPDU
            {
                Name         = fullName,
                Description  = description,
                Gender       = gender,
                SkinTone     = skinTone,
                HeadOutfitId = headGuid,
                BodyOutfitId = bodyGuid,
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var first = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (first != tcs.Task)
            {
                EmitReply(corrId, ok: false, error: "create-avatar: timeout waiting for CreateASimResponse");
                return;
            }

            var resp = await tcs.Task;

            if (resp.Status == CreateASimStatus.SUCCESS)
            {
                EmitReply(corrId, ok: true, data: new
                {
                    status    = "SUCCESS",
                    avatar_id = (long)resp.NewAvatarId,
                });
            }
            else
            {
                // Emit ok=true so the sidecar can translate reason into the convention
                // response shape (ok:false, reason:"category:<name>").
                EmitReply(corrId, ok: true, data: new
                {
                    status = "FAILED",
                    reason = resp.Reason.ToString(),
                });
            }
        }
        catch (OperationCanceledException)
        {
            EmitReply(corrId, ok: false, error: "create-avatar: cancelled");
        }
        catch (Exception ex)
        {
            EmitReply(corrId, ok: false, error: $"create-avatar: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _pendingCreateSim.TryRemove(corrId, out _);
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
            TriggerShutdown();
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

    // ---- city-socket subscribers ----

    /// <summary>
    /// Forwards inbound <see cref="FindLotResponse"/> and <see cref="PurchaseLotResponse"/>
    /// packets to the pending probe-lot and purchase-lot TCS entries.
    ///
    /// <para>
    /// <b>H1 (lot-keyed correlation):</b> for <see cref="FindLotResponse"/>, we match
    /// <c>resp.LotId</c> against the <c>LotCtx</c> stored alongside each TCS so that a
    /// response for lot A doesn't resolve a pending probe-lot for lot B. For purchase-lot,
    /// <see cref="PurchaseLotResponse"/> doesn't carry back the location; FIFO is used
    /// (serial IPC dispatch means at most one purchase is in flight at a time).
    /// </para>
    ///
    /// <para>
    /// <b>H2 (SessionClosed safety):</b> <c>SessionClosed</c> sets the handler's
    /// <c>_sessionClosed</c> flag under <c>_pendingLock</c> BEFORE clearing the dicts,
    /// and calls <c>TrySetCanceled</c> on all pending TCSes. The lock prevents new TCSes
    /// from being added after the session is closed.
    /// </para>
    /// </summary>
    private sealed class ProbeLotAndPurchaseSubscriber : IAriesMessageSubscriber, IAriesEventSubscriber
    {
        private readonly BotCmdHandler _handler;
        public ProbeLotAndPurchaseSubscriber(BotCmdHandler handler) => _handler = handler;

        public void MessageReceived(AriesClient client, object message)
        {
            // H1: match FindLotResponse by resp.LotId (lot-keyed correlation).
            if (message is FindLotResponse resp)
            {
                foreach (var kv in _handler._pendingProbes)
                {
                    if (kv.Value.Tcs.Task.IsCompleted) continue;
                    // Match the response's LotId against the stored lot context.
                    if (kv.Value.LotCtx == resp.LotId)
                    {
                        kv.Value.Tcs.TrySetResult(resp);
                        return;
                    }
                }
                // No match by LotId — fall back to FIFO for the first incomplete TCS.
                // This handles the case where the server echoes a different LotId than
                // requested (e.g., NOT_OPEN responses where LotId may be 0).
                foreach (var kv in _handler._pendingProbes)
                {
                    if (kv.Value.Tcs.Task.IsCompleted) continue;
                    kv.Value.Tcs.TrySetResult(resp);
                    return;
                }
                return;
            }

            // PurchaseLotResponse — FIFO (no lot-id echo in the response to match against).
            if (message is PurchaseLotResponse pr)
            {
                foreach (var kv in _handler._pendingPurchases)
                {
                    if (kv.Value.Tcs.Task.IsCompleted) continue;
                    kv.Value.Tcs.TrySetResult(pr);
                    return;
                }
            }
        }

        public void SessionCreated(AriesClient c) { }
        public void SessionOpened(AriesClient c) { }
        public void SessionClosed(AriesClient c)
        {
            // H2: set the closed flag and cancel all pending TCSes under lock.
            // This prevents GetOrAdd-equivalent in handlers from recreating TCSes
            // for a dead session.
            List<TaskCompletionSource<FindLotResponse>>     probes;
            List<TaskCompletionSource<PurchaseLotResponse>> purchases;
            lock (_handler._pendingLock)
            {
                _handler._sessionClosed = true;
                probes    = _handler._pendingProbes.Values.Select(v => v.Tcs).ToList();
                purchases = _handler._pendingPurchases.Values.Select(v => v.Tcs).ToList();
                _handler._pendingProbes.Clear();
                _handler._pendingPurchases.Clear();
            }
            foreach (var tcs in probes)    tcs.TrySetCanceled();
            foreach (var tcs in purchases) tcs.TrySetCanceled();
        }
        public void SessionIdle(AriesClient c) { }
        public void InputClosed(AriesClient c) { }
    }

    // ---- probe-bulletin city-socket subscriber ----

    /// <summary>
    /// Forwards inbound <see cref="BulletinResponse"/> packets to pending
    /// <c>probe-bulletin</c> TCS entries.
    ///
    /// <para>
    /// <b>H1 (lot-keyed correlation):</b> <see cref="BulletinResponse"/> doesn't carry
    /// back the neighborhood id. The subscriber falls back to FIFO delivery of the first
    /// incomplete TCS. The nhood is stored in <c>LotCtx</c> alongside the TCS for
    /// diagnostic purposes.
    /// </para>
    ///
    /// <para>
    /// <b>H2 (SessionClosed safety):</b> mirrors <see cref="ProbeLotAndPurchaseSubscriber"/>.
    /// </para>
    /// </summary>
    private sealed class ProbeBulletinSubscriber : IAriesMessageSubscriber, IAriesEventSubscriber
    {
        private readonly BotCmdHandler _handler;
        public ProbeBulletinSubscriber(BotCmdHandler handler) => _handler = handler;

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is BulletinResponse br)
            {
                // FIFO — BulletinResponse carries no nhood correlator.
                foreach (var kv in _handler._pendingBulletins)
                {
                    if (kv.Value.Tcs.Task.IsCompleted) continue;
                    kv.Value.Tcs.TrySetResult(br);
                    return;
                }
            }
        }

        public void SessionCreated(AriesClient c) { }
        public void SessionOpened(AriesClient c) { }
        public void SessionClosed(AriesClient c)
        {
            // H2: cancel all pending bulletin TCSes under lock.
            List<TaskCompletionSource<BulletinResponse>> bulletins;
            lock (_handler._pendingLock)
            {
                _handler._sessionClosed = true;
                bulletins = _handler._pendingBulletins.Values.Select(v => v.Tcs).ToList();
                _handler._pendingBulletins.Clear();
            }
            foreach (var tcs in bulletins) tcs.TrySetCanceled();
        }
        public void SessionIdle(AriesClient c) { }
        public void InputClosed(AriesClient c) { }
    }

    // ---- create-avatar city-socket subscriber ----

    /// <summary>
    /// Forwards inbound <see cref="CreateASimResponse"/> packets to pending
    /// <c>create-avatar</c> TCS entries.
    ///
    /// <para>
    /// <b>FIFO delivery</b>: <see cref="CreateASimResponse"/> carries no echo of the
    /// correlation_id, so we complete the first incomplete TCS. Serial IPC dispatch means
    /// at most one <c>create-avatar</c> is in flight at a time — safe assumption.
    /// </para>
    ///
    /// <para>
    /// <b>SessionClosed</b>: mirrors <see cref="ProbeBulletinSubscriber"/> — sets the
    /// handler's closed flag under lock and cancels all pending TCSes.
    /// </para>
    /// </summary>
    private sealed class CreateASimSubscriber : IAriesMessageSubscriber, IAriesEventSubscriber
    {
        private readonly BotCmdHandler _handler;
        public CreateASimSubscriber(BotCmdHandler handler) => _handler = handler;

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is CreateASimResponse resp)
            {
                // FIFO — no correlator in CreateASimResponse.
                foreach (var kv in _handler._pendingCreateSim)
                {
                    if (kv.Value.Task.IsCompleted) continue;
                    kv.Value.TrySetResult(resp);
                    return;
                }
                Program.Log("[chargen] CreateASimResponse arrived with no pending TCS (stray packet)");
            }
        }

        public void SessionCreated(AriesClient c) { }
        public void SessionOpened(AriesClient c) { }
        public void SessionClosed(AriesClient c)
        {
            List<TaskCompletionSource<CreateASimResponse>> pending;
            lock (_handler._pendingLock)
            {
                _handler._sessionClosed = true;
                pending = _handler._pendingCreateSim.Values.ToList();
                _handler._pendingCreateSim.Clear();
            }
            foreach (var tcs in pending) tcs.TrySetCanceled();
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

    /// <summary>
    /// Read a uint32 arg from a JsonObject. Accepts int64 JSON numbers and numeric strings.
    /// Throws ArgumentException on missing or bad value. Negative values are treated as 0
    /// (will fail the caller's > 0 guard).
    /// </summary>
    private static uint ParseUInt32Arg(JsonObject args, string key)
    {
        var node = args[key];
        if (node == null) throw new ArgumentException($"{key} required");
        if (node is JsonValue v)
        {
            if (v.TryGetValue<long>(out var lv)) return lv < 0 ? 0u : (uint)lv;
            if (v.TryGetValue<ulong>(out var ulv)) return (uint)Math.Min(ulv, uint.MaxValue);
        }
        if (uint.TryParse(node.ToString(), out var u)) return u;
        throw new ArgumentException($"{key} must be a valid uint32 (got {node})");
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
