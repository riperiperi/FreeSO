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
using FSO.Common.DataService.Framework;
using FSO.Common.Serialization.Primitives;
using FSO.Files.Formats.tsodata;
using FSO.Server.Clients;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Voltron.Packets;

namespace FSO.Bot.Headless;

/// <summary>
/// City-family command handlers (freesoexperiment-ded). Five ops, all on the
/// <b>city</b> Aries socket:
///
/// <list type="bullet">
///   <item><b>view-bulletin</b> — <see cref="BulletinRequest"/> with Type=GET_MESSAGES.
///   Correlator: the FIRST inbound <see cref="BulletinResponse"/> with Type=MESSAGES
///   (server streams one per request; we complete the pending TCS on it). Surfaces the
///   full bulletin list as a JSON array.</item>
///
///   <item><b>post-bulletin</b> — <see cref="BulletinRequest"/> with Type=POST_MESSAGE
///   (skipping the client's client-side CAN_POST_MESSAGE handshake — server-side
///   POST_MESSAGE runs the same validation checks anyway; see BulletinHandler.cs:148).
///   Correlator: next inbound <see cref="BulletinResponse"/>; resolves on SEND_SUCCESS
///   (with message id / BulletinItem returned) or any SEND_FAIL_* / FAIL_* code.</item>
///
///   <item><b>vote</b> — <see cref="NhoodRequest"/> with Type=VOTE, TargetAvatar,
///   TargetNHood. Correlator: next inbound <see cref="NhoodResponse"/>. On workshop's
///   current DB state (no active election_cycle in fso_neighborhoods), the server
///   replies Code=ELECTION_OVER — that is our deterministic refuse path for the
///   integration test.</item>
///
///   <item><b>nominate</b> — <see cref="NhoodRequest"/> with Type=NOMINATE, analogous to
///   vote. Also refuses ELECTION_OVER on workshop.</item>
///
///   <item><b>view-neighborhood</b> — <see cref="DataServiceWrapperPDU"/> carrying a
///   <see cref="cTSONetMessageStandard"/> GET request on MaskedStruct.ServerNeighborhood
///   (4236962517). The server replies with a stream of <see cref="DataServiceWrapperPDU"/>
///   packets, each carrying one <see cref="cTSOTopicUpdateMessage"/> delta filling a
///   Neighborhood field; packets correlate back via SendingAvatarID. We accumulate
///   dotpath-keyed values for a bounded window, then return the observed fields.</item>
/// </list>
///
/// <para>
/// <b>Correlation strategy</b>. BulletinResponse and NhoodResponse packets do NOT carry a
/// client-chosen correlator field (unlike InstantMessage's AckID or FindAvatarRequest's
/// AvatarId). The real FSO client uses a <c>GenericActionRegulator</c> state machine that
/// assumes one outstanding request at a time and matches responses to the most recent
/// <c>CurrentRequest</c>. We replicate that: a per-op FIFO of pending TCS, enqueued on
/// send, dequeued on any inbound response. This is correct because the bot only ever has
/// one outstanding bulletin or nhood request in flight (ops are serialised through the
/// IPC dispatcher). DataServiceWrapperPDU has real correlation via SendingAvatarID.
/// </para>
///
/// <para>
/// <b>Thread safety</b>. All city-socket subscriber callbacks fire on Mina's IO thread.
/// TCS completion is RunContinuationsAsynchronously, so awaiters resume on the thread
/// pool. No VM state is touched — city-family reads/writes are entirely city-side.
/// </para>
///
/// <para>
/// <b>No owner gating here</b>. The bulletin / nhood gates are residency-based
/// (must be a roommate of a lot in the target nhood, must not have moved in 7 days, etc.)
/// — those are server-side checks we cannot replicate deterministically without the full
/// fso_avatars / fso_roommates view. Admins (moderation_level >= 1) bypass the move
/// check. We do the basic sanity checks (empty strings, self-nomination, etc.) but
/// ELECTION_OVER-class refusals come from the server, not us.
/// </para>
/// </summary>
public static class CityHandlers
{
    // Default nhood for the workshop shard. Baron's home lot (lot_id=2) is in
    // neighborhood_id=1 per fso_lots. Agents may override via the "neighborhood_id" arg.
    private const uint DefaultNhoodId = 1;

    public static void RegisterAll(CommandDispatcher dispatcher, AriesClient cityAries, uint myAvatarId)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (cityAries == null) throw new ArgumentNullException(nameof(cityAries));

        // Subscribe once for inbound response packets. Idempotent in practice — duplicate
        // subscriptions are harmless since our handler gates on having pending TCS.
        cityAries.AddSubscriber(new CityResponseSubscriber());

        dispatcher.Register("view-bulletin",     (args, ct) => ViewBulletinAsync(cityAries, args, ct));
        dispatcher.Register("post-bulletin",     (args, ct) => PostBulletinAsync(cityAries, myAvatarId, args, ct));
        dispatcher.Register("vote",              (args, ct) => VoteAsync(cityAries, myAvatarId, args, ct));
        dispatcher.Register("nominate",          (args, ct) => NominateAsync(cityAries, myAvatarId, args, ct));
        dispatcher.Register("view-neighborhood", (args, ct) => ViewNeighborhoodAsync(cityAries, args, ct));
    }

    // ---- pending-response queues ----
    //
    // Bulletin and Nhood request/response pairs have no wire correlator. We serialise
    // outstanding requests per op and take the next inbound response to resolve the next
    // pending. This mirrors how the stock client's GenericActionRegulator works.

    private static readonly object _pendingBulletinLock = new();
    private static readonly Queue<TaskCompletionSource<BulletinResponse>> _pendingBulletin = new();

    private static readonly object _pendingNhoodLock = new();
    private static readonly Queue<TaskCompletionSource<NhoodResponse>> _pendingNhood = new();

    // DataService responses DO correlate via SendingAvatarID. Keyed on message id.
    private static readonly ConcurrentDictionary<uint, DataServicePending> _pendingDataService = new();
    private static uint _dataServiceMessageId = 1;
    private static readonly object _dsMidLock = new();

    private sealed class DataServicePending
    {
        public List<cTSOTopicUpdateMessage> Updates = new();
        public DateTimeOffset FirstSeen;
    }

    // ---- view-bulletin ----

    internal static async Task<CommandDispatcher.Response> ViewBulletinAsync(AriesClient cityAries, JsonObject args, CancellationToken ct)
    {
        uint nhoodId = ReadNhoodId(args);

        var tcs = new TaskCompletionSource<BulletinResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_pendingBulletinLock) { _pendingBulletin.Enqueue(tcs); }

        try
        {
            cityAries.Write(new BulletinRequest
            {
                Type = BulletinRequestType.GET_MESSAGES,
                TargetNHood = nhoodId,
            });

            var resp = await AwaitWithTimeout(tcs.Task, TimeSpan.FromSeconds(10), ct);
            if (resp == null)
            {
                // Drain our slot on timeout so a stale reply doesn't leak into a later call.
                DrainMatching(_pendingBulletin, _pendingBulletinLock, tcs);
                return CommandDispatcher.Response.Fail("view-bulletin: timeout waiting for BulletinResponse");
            }

            if (resp.Type == BulletinResponseType.MESSAGES)
            {
                var items = (resp.Messages ?? Array.Empty<BulletinItem>()).Select(b => new
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

                return CommandDispatcher.Response.Success(new
                {
                    neighborhood_id = (long)nhoodId,
                    count = items.Length,
                    bulletins = items,
                });
            }

            return CommandDispatcher.Response.Fail($"view-bulletin: server returned {resp.Type}");
        }
        finally
        {
            DrainMatching(_pendingBulletin, _pendingBulletinLock, tcs);
        }
    }

    // ---- post-bulletin ----

    internal static async Task<CommandDispatcher.Response> PostBulletinAsync(AriesClient cityAries, uint myAvatarId, JsonObject args, CancellationToken ct)
    {
        uint nhoodId = ReadNhoodId(args);

        var subject = (string)args["subject"];
        var body = (string)args["body"];
        if (string.IsNullOrEmpty(subject)) return CommandDispatcher.Response.Fail("post-bulletin requires non-empty subject");
        if (string.IsNullOrEmpty(body)) return CommandDispatcher.Response.Fail("post-bulletin requires non-empty body");
        if (subject.Length > 64) return CommandDispatcher.Response.Fail("post-bulletin: subject > 64 chars (server caps at 64)");
        if (body.Length > 1000) return CommandDispatcher.Response.Fail("post-bulletin: body > 1000 chars (server caps at 1000)");

        uint lotId = 0;
        var lotNode = args["lot_id"];
        if (lotNode != null)
        {
            try { lotId = (uint)(long)lotNode; }
            catch { return CommandDispatcher.Response.Fail("lot_id must be a uint (fso_lots.lot_id, or 0 to omit)"); }
        }

        var tcs = new TaskCompletionSource<BulletinResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_pendingBulletinLock) { _pendingBulletin.Enqueue(tcs); }

        try
        {
            cityAries.Write(new BulletinRequest
            {
                Type = BulletinRequestType.POST_MESSAGE,
                TargetNHood = nhoodId,
                Title = subject,
                Message = body,
                LotID = lotId,
            });

            var resp = await AwaitWithTimeout(tcs.Task, TimeSpan.FromSeconds(10), ct);
            if (resp == null)
            {
                DrainMatching(_pendingBulletin, _pendingBulletinLock, tcs);
                return CommandDispatcher.Response.Fail("post-bulletin: timeout waiting for BulletinResponse");
            }

            if (resp.Type == BulletinResponseType.SEND_SUCCESS)
            {
                var created = (resp.Messages != null && resp.Messages.Length > 0) ? resp.Messages[0] : null;
                return CommandDispatcher.Response.Success(new
                {
                    neighborhood_id = (long)nhoodId,
                    response_type = resp.Type.ToString(),
                    bulletin_id = created != null ? (long)created.ID : 0L,
                    subject = created?.Subject ?? subject,
                    body = created?.Body ?? body,
                    sender_id = created != null ? (long)created.SenderID : (long)myAvatarId,
                    type = created?.Type.ToString() ?? string.Empty,
                });
            }

            // Any SEND_FAIL_* / FAIL_* code. Surface as ok=false with the enum name so the
            // integration test can assert specific refuse reasons (e.g. SEND_FAIL_JUST_MOVED
            // for non-admin callers within 7 days of move, SEND_FAIL_TOO_FREQUENT for
            // repeat posts within Bulletin_Post_Frequency=3 days).
            return CommandDispatcher.Response.Fail($"post-bulletin: server returned {resp.Type} ({(resp.BanEndDate != 0 ? "ban_end=" + resp.BanEndDate : "")}{resp.Message ?? ""})");
        }
        finally
        {
            DrainMatching(_pendingBulletin, _pendingBulletinLock, tcs);
        }
    }

    // ---- vote ----

    internal static async Task<CommandDispatcher.Response> VoteAsync(AriesClient cityAries, uint myAvatarId, JsonObject args, CancellationToken ct)
    {
        uint nhoodId = ReadNhoodId(args);

        var targetPid = (long?)args["target_persist_id"] ?? 0L;
        if (targetPid <= 0) return CommandDispatcher.Response.Fail("vote requires target_persist_id (> 0)");
        if ((uint)targetPid == myAvatarId) return CommandDispatcher.Response.Fail("vote: cannot vote for yourself (server also refuses CANT_RATE_AVATAR-class checks)");

        return await SendNhoodRequestAsync(cityAries, NhoodRequestType.VOTE, (uint)targetPid, nhoodId, "", 0, "vote", ct);
    }

    // ---- nominate ----

    internal static async Task<CommandDispatcher.Response> NominateAsync(AriesClient cityAries, uint myAvatarId, JsonObject args, CancellationToken ct)
    {
        uint nhoodId = ReadNhoodId(args);

        var targetPid = (long?)args["target_persist_id"] ?? 0L;
        if (targetPid <= 0) return CommandDispatcher.Response.Fail("nominate requires target_persist_id (> 0)");
        if ((uint)targetPid == myAvatarId) return CommandDispatcher.Response.Fail("nominate: cannot nominate yourself");

        return await SendNhoodRequestAsync(cityAries, NhoodRequestType.NOMINATE, (uint)targetPid, nhoodId, "", 0, "nominate", ct);
    }

    private static async Task<CommandDispatcher.Response> SendNhoodRequestAsync(
        AriesClient cityAries, NhoodRequestType type, uint targetAvatar, uint nhoodId,
        string message, uint value, string verb, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<NhoodResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_pendingNhoodLock) { _pendingNhood.Enqueue(tcs); }

        try
        {
            cityAries.Write(new NhoodRequest
            {
                Type = type,
                TargetAvatar = targetAvatar,
                TargetNHood = nhoodId,
                Message = message ?? "",
                Value = value,
            });

            var resp = await AwaitWithTimeout(tcs.Task, TimeSpan.FromSeconds(10), ct);
            if (resp == null)
            {
                DrainMatching(_pendingNhood, _pendingNhoodLock, tcs);
                return CommandDispatcher.Response.Fail($"{verb}: timeout waiting for NhoodResponse");
            }

            if (resp.Code == NhoodResponseCode.SUCCESS)
            {
                return CommandDispatcher.Response.Success(new
                {
                    verb = verb,
                    neighborhood_id = (long)nhoodId,
                    target_persist_id = (long)targetAvatar,
                    response_code = resp.Code.ToString(),
                });
            }

            // Surface server's refuse code so the agent / test can branch.
            // NHOOD_NO_ELECTION / ELECTION_OVER / BAD_STATE are the common ones when no
            // cycle is active. NOT_IN_NHOOD if caller has no roommate row.
            // YOU_MOVED_RECENTLY if caller move_date < 30 days ago (non-admin).
            return CommandDispatcher.Response.Fail($"{verb}: server returned {resp.Code}{(string.IsNullOrEmpty(resp.Message) ? "" : ": " + resp.Message)}");
        }
        finally
        {
            DrainMatching(_pendingNhood, _pendingNhoodLock, tcs);
        }
    }

    // ---- view-neighborhood ----

    internal static async Task<CommandDispatcher.Response> ViewNeighborhoodAsync(AriesClient cityAries, JsonObject args, CancellationToken ct)
    {
        uint nhoodId = ReadNhoodId(args);

        uint messageId;
        lock (_dsMidLock) { messageId = _dataServiceMessageId++; }

        var pending = new DataServicePending { FirstSeen = DateTimeOffset.UtcNow };
        _pendingDataService[messageId] = pending;

        try
        {
            // MaskedStruct.ServerNeighborhood = 4236962517. Fetch the full struct by id.
            const uint ServerNeighborhoodMaskId = 4236962517;

            var pdu = new DataServiceWrapperPDU
            {
                RequestTypeID = ServerNeighborhoodMaskId,
                SendingAvatarID = messageId,
                Body = new cTSONetMessageStandard
                {
                    DataServiceType = ServerNeighborhoodMaskId,
                    Parameter = nhoodId,
                },
            };

            cityAries.Write(pdu);

            // Server streams multiple cTSOTopicUpdateMessage responses, each with our
            // SendingAvatarID (the client's ClientDataService.MessageReceived confirms:
            // "Reusing this field for easier callbacks rather than scoping them"). There's
            // no explicit "done" marker, so we collect for a bounded window and return.
            // 2s is enough for the workshop server's local-DB resolution.
            var collectWindow = TimeSpan.FromSeconds(2);
            var deadline = DateTimeOffset.UtcNow + collectWindow;

            try
            {
                while (DateTimeOffset.UtcNow < deadline)
                {
                    await Task.Delay(200, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Outer IPC timeout (or explicit cancel) interrupts the collection window;
                // that's fine — return whatever deltas arrived so far rather than fail the
                // call. The caller's IPC-side timeout is longer than our 2s window.
            }

            // Extract fields from the accumulated updates. Each update's DotPath is
            // [struct_id, key, field_id, ...] or sometimes [provider_id, key, field_id, ...]
            // — we surface raw dotpaths + values so the caller can reconstruct, and also
            // project well-known Neighborhood fields by id.
            var updates = pending.Updates;
            var rawFields = updates.Select(u => new
            {
                dot_path = u.DotPath?.Select(v => (long)v).ToArray() ?? Array.Empty<long>(),
                value = SerializePrimValue(u.Value),
            }).ToArray();

            var projected = ProjectNhoodFields(updates);

            return CommandDispatcher.Response.Success(new
            {
                neighborhood_id = (long)nhoodId,
                message_id = (long)messageId,
                update_count = updates.Count,
                fields = projected,
                raw_dotpath_updates = rawFields,
            });
        }
        finally
        {
            _pendingDataService.TryRemove(messageId, out _);
        }
    }

    // Project known Neighborhood struct fields by name. We do not lazy-resolve field IDs
    // via DataDefinition here (like PropertyHandlers does for Lot_LotAdmitInfo) because
    // the projection is best-effort — the raw_dotpath_updates array always carries the
    // canonical data, and the test cross-checks against fso_neighborhoods directly.
    private static Dictionary<string, object> ProjectNhoodFields(List<cTSOTopicUpdateMessage> updates)
    {
        // Resolve well-known field IDs once via DataDefinition.
        var fields = new Dictionary<string, object>();
        try
        {
            var dd = FSO.Content.Content.Get().DataDefinition;
            var nhoodStruct = dd.Structs.FirstOrDefault(s => s.Name == "Neighborhood");
            if (nhoodStruct == null) return fields;

            var nameById = new Dictionary<uint, string>();
            foreach (var f in nhoodStruct.Fields)
            {
                nameById[f.ID] = f.Name;
            }

            foreach (var u in updates)
            {
                if (u.DotPath == null || u.DotPath.Length < 3) continue;
                // DotPath shape for ServerNeighborhood responses: [provider/struct_id, key, field_id, ...]
                // The field_id is at index 2 (the 3rd element). Nested paths (e.g.
                // Neighborhood_ElectionCycle.CycleState) have additional ids we drop.
                var fieldId = u.DotPath[2];
                if (nameById.TryGetValue(fieldId, out var fieldName) && u.DotPath.Length == 3)
                {
                    // Only surface flat fields; nested ones go in raw_dotpath_updates.
                    fields[fieldName] = SerializePrimValue(u.Value);
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[city] ProjectNhoodFields failed: {ex.GetType().Name}: {ex.Message}");
        }
        return fields;
    }

    private static object SerializePrimValue(object v)
    {
        if (v == null) return null;
        switch (v)
        {
            case string s: return s;
            case uint u: return (long)u;
            case int i: return (long)i;
            case long l: return l;
            case ulong ul: return (long)ul;
            case byte b: return (long)b;
            case short s2: return (long)s2;
            case ushort us: return (long)us;
            case bool b2: return b2;
            case float f: return f;
            case double d: return d;
            default: return v.ToString();
        }
    }

    // ---- inbound subscriber ----

    private sealed class CityResponseSubscriber : IAriesMessageSubscriber, IAriesEventSubscriber
    {
        public void MessageReceived(AriesClient client, object message)
        {
            switch (message)
            {
                case BulletinResponse br:
                {
                    TaskCompletionSource<BulletinResponse> tcs = null;
                    lock (_pendingBulletinLock)
                    {
                        if (_pendingBulletin.Count > 0) tcs = _pendingBulletin.Dequeue();
                    }
                    if (tcs != null) tcs.TrySetResult(br);
                    else Program.Log($"[city] stray BulletinResponse type={br.Type} (no pending)");
                    break;
                }
                case NhoodResponse nr:
                {
                    TaskCompletionSource<NhoodResponse> tcs = null;
                    lock (_pendingNhoodLock)
                    {
                        if (_pendingNhood.Count > 0) tcs = _pendingNhood.Dequeue();
                    }
                    if (tcs != null) tcs.TrySetResult(nr);
                    else Program.Log($"[city] stray NhoodResponse code={nr.Code} (no pending)");
                    break;
                }
                case NhoodCandidateList cl:
                {
                    // Supplemental packet that precedes the NhoodResponse for CAN_VOTE / CAN_NOMINATE.
                    // We don't issue those ops, but log if we ever see one.
                    Program.Log($"[city] NhoodCandidateList nominationMode={cl.NominationMode} candidates={cl.Candidates?.Count ?? 0}");
                    break;
                }
                case DataServiceWrapperPDU dsw:
                {
                    if (dsw.Body is cTSOTopicUpdateMessage tu)
                    {
                        if (_pendingDataService.TryGetValue(dsw.SendingAvatarID, out var pending))
                        {
                            lock (pending) { pending.Updates.Add(tu); }
                        }
                    }
                    break;
                }
            }
        }

        public void SessionCreated(AriesClient c) { }
        public void SessionOpened(AriesClient c) { }
        public void SessionClosed(AriesClient c)
        {
            // Drop pending on disconnect.
            lock (_pendingBulletinLock)
            {
                while (_pendingBulletin.Count > 0) _pendingBulletin.Dequeue().TrySetCanceled();
            }
            lock (_pendingNhoodLock)
            {
                while (_pendingNhood.Count > 0) _pendingNhood.Dequeue().TrySetCanceled();
            }
            _pendingDataService.Clear();
        }
        public void SessionIdle(AriesClient c) { }
        public void InputClosed(AriesClient c) { }
    }

    // ---- helpers ----

    private static uint ReadNhoodId(JsonObject args)
    {
        var node = args["neighborhood_id"];
        if (node == null) return DefaultNhoodId;
        try { return (uint)(long)node; }
        catch { return DefaultNhoodId; }
    }

    private static async Task<T> AwaitWithTimeout<T>(Task<T> task, TimeSpan timeout, CancellationToken ct) where T : class
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        var first = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
        if (first != task) return null;
        return await task;
    }

    private static void DrainMatching<T>(Queue<TaskCompletionSource<T>> q, object l, TaskCompletionSource<T> match)
    {
        // If our slot is still at the head (we timed out), pop it so a future stray
        // response doesn't complete a cancelled TCS. If a stray is dequeued, it is
        // simply discarded. We rebuild the queue without the cancelled entry.
        lock (l)
        {
            if (q.Count == 0) return;
            var rebuild = new Queue<TaskCompletionSource<T>>(q.Count);
            while (q.Count > 0)
            {
                var item = q.Dequeue();
                if (!ReferenceEquals(item, match)) rebuild.Enqueue(item);
            }
            while (rebuild.Count > 0) q.Enqueue(rebuild.Dequeue());
        }
    }
}
