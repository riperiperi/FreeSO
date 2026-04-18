/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Files.Formats.tsodata;
using FSO.Server.Clients;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Electron.Packets;

namespace FSO.Bot.Headless;

/// <summary>
/// Mail verb family (freesoexperiment-bd2). Ops shipped:
/// <list type="bullet">
///   <item><b>poll-inbox</b> — MailRequest{Type=POLL_INBOX, TimestampID=since_ticks} on the city
///   Aries socket. Awaits MailResponse{Type=POLL_RESPONSE, Messages[]} and returns the messages
///   as JSON. Correlated via a pending TCS keyed by POLL_RESPONSE type; we only permit one in-
///   flight poll at a time because the server has no request correlator on this PDU (it just
///   fires a response back using the session reference).</item>
///   <item><b>send-mail</b> — MailRequest{Type=SEND, Item=MessageItem{TargetID, Subject, Body,
///   Type=0, Subtype=0}}. Server BB-sanitises + caps body at 1500 chars, INSERTs fso_inbox row,
///   pushes NEW_MAIL to target if online. Server replies SEND_SUCCESS / SEND_FAILED /
///   SEND_IGNORING_YOU / SEND_IGNORING_THEM with the echoed MessageItem (with ID now populated).
///   We correlate on "next SEND_* response" (same single-in-flight rationale) and return the
///   echoed message_id / time so the agent can reference it for delete-mail without polling.</item>
///   <item><b>delete-mail</b> — MailRequest{Type=DELETE, TimestampID=mail_id}. Fire-and-forget:
///   the server's MailHandler.DELETE path has no response PDU (the original client blindly
///   deletes locally after sending DELETE). We return ok=true immediately; wire-level effect is
///   verified out-of-band via DB readback or a follow-up poll-inbox.</item>
/// </list>
///
/// <para>
/// <b>read-mail DROPPED (not stubbed).</b> The task brief names "read-mail" as an op that
/// "marks as read on server", but MailRequestType has only POLL_INBOX | SEND | DELETE. The
/// server's MailHandler never mutates fso_inbox.read_state — the column exists and is stored,
/// but is only written on INSERT (always 0). The original TSO client tracks read-state
/// client-side only. Per item constraint "If catalog has no wire PDU for an op, DROP it —
/// do NOT stub.", no convention JSON, no handler.
/// </para>
///
/// <para>
/// <b>Wire path.</b> Mail rides the <b>city</b> Aries socket, not the lot socket. MailRequest /
/// MailResponse are Electron packets (FSO.Server.Protocol.Electron.Packets). The server's
/// MailHandler (FSO.Server/Servers/City/Handlers/MailHandler.cs) is a sibling of
/// MessagingHandler (which handles IM), not the same class — the task-brief hint that
/// "MessagingHandler handles both InstantMessage AND MailRequest" is outdated for this fork.
/// Inspection of the fork: MessagingHandler.cs:21 has `Handle(IVoltronSession, InstantMessage)`
/// only; MailHandler.cs:31 has `Handle(IVoltronSession, MailRequest)`. Two distinct classes.
/// </para>
///
/// <para>
/// <b>Correlation strategy.</b> The MailResponse PDU carries no client-set correlator, only a
/// Type enum. With concurrent polls or sends, multiple responses of the same Type would race.
/// We keep it simple: a single pending TCS per response-type (POLL_RESPONSE, any SEND_*) — a
/// second concurrent call of the same kind fails fast with "another poll-inbox in flight"
/// rather than silently stealing the first caller's response. DELETE has no response so it
/// doesn't need correlation at all. For the agent use case (serial verb invocations) this is
/// a fine degree of concurrency; busier setups would need a real correlator (server-side PDU
/// change — out of scope for this item).
/// </para>
///
/// <para>
/// <b>Thread safety.</b> Correlation slots are <see cref="TaskCompletionSource{T}"/> stored in
/// a single-entry model (one int for the poll-in-flight sentinel via Interlocked, and likewise
/// for send). Inbound MailResponse routing happens on the Aries IO thread via the
/// MailResponseSubscriber; completions hop the runtime thread pool for the awaiter.
/// </para>
/// </summary>
public static class MailHandlers
{
    // Single-in-flight sentinels. 0 = idle, 1 = a call is in flight.
    private static int _pollInFlight;
    private static int _sendInFlight;

    // Single pending TCS for the currently-in-flight poll / send. Non-null iff the corresponding
    // sentinel is 1.
    private static TaskCompletionSource<MailResponse> _pendingPoll;
    private static TaskCompletionSource<MailResponse> _pendingSend;

    // Server caps from MailHandler.cs:96-97: body BB-sanitised and truncated at 1500. Subject
    // is varchar(128) in fso_inbox schema. We refuse pre-wire rather than silently truncate so
    // agents see the limit explicitly.
    public const int MaxSubjectChars = 128;
    public const int MaxBodyChars = 1500;

    /// <summary>
    /// Register mail-family ops. <paramref name="cityAries"/> is the live city Aries socket
    /// (<c>Program.cs cityAries</c>); <paramref name="myAvatarId"/> is the bot's persist id used
    /// only for logging / payload echo (the server fills SenderID server-side from the session).
    /// </summary>
    public static void RegisterAll(CommandDispatcher dispatcher, AriesClient cityAries, uint myAvatarId)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (cityAries == null) throw new ArgumentNullException(nameof(cityAries));

        // Wire the city subscriber once per registration. Additive — other registrations
        // (FindAvatarSubscriber, CityListener) stay on the chain.
        cityAries.AddSubscriber(new MailResponseSubscriber());

        dispatcher.Register("poll-inbox", (args, ct) => PollInboxAsync(cityAries, args, ct));
        dispatcher.Register("send-mail",  (args, ct) => SendMailAsync(cityAries, myAvatarId, args, ct));
        dispatcher.Register("delete-mail", (args, ct) => Task.FromResult(DeleteMail(cityAries, args)));
    }

    // ---- poll-inbox ----

    /// <summary>
    /// poll-inbox — MailRequest{Type=POLL_INBOX, TimestampID=since_ticks}. Returns the full
    /// message array on the next MailResponse{Type=POLL_RESPONSE}. Default since=0 returns the
    /// entire inbox.
    /// </summary>
    internal static async Task<CommandDispatcher.Response> PollInboxAsync(AriesClient cityAries, JsonObject args, CancellationToken ct)
    {
        long sinceTicks = 0;
        var sinceNode = args["since_ticks"];
        if (sinceNode != null)
        {
            try { sinceTicks = (long)sinceNode; }
            catch { return CommandDispatcher.Response.Fail("since_ticks must be an integer"); }
            if (sinceTicks < 0) return CommandDispatcher.Response.Fail("since_ticks must be >= 0");
        }

        // Claim the single in-flight slot.
        if (Interlocked.Exchange(ref _pollInFlight, 1) == 1)
        {
            return CommandDispatcher.Response.Fail("another poll-inbox is in flight (single-slot correlator limitation — see MailHandlers doc)");
        }

        var tcs = new TaskCompletionSource<MailResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingPoll = tcs;

        try
        {
            cityAries.Write(new MailRequest
            {
                Type = MailRequestType.POLL_INBOX,
                TimestampID = sinceTicks,
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var respTask = tcs.Task;
            var first = await Task.WhenAny(respTask, Task.Delay(Timeout.Infinite, cts.Token));
            if (first != respTask)
            {
                return CommandDispatcher.Response.Fail("poll-inbox timeout after 10s (no MailResponse POLL_RESPONSE)");
            }

            var resp = await respTask;
            var items = new List<object>(resp.Messages?.Length ?? 0);
            if (resp.Messages != null)
            {
                foreach (var m in resp.Messages)
                {
                    items.Add(MessageItemToJson(m));
                }
            }
            return CommandDispatcher.Response.Success(new
            {
                type = resp.Type.ToString(),
                since_ticks = sinceTicks,
                count = items.Count,
                messages = items,
            });
        }
        finally
        {
            _pendingPoll = null;
            Interlocked.Exchange(ref _pollInFlight, 0);
        }
    }

    // ---- send-mail ----

    /// <summary>
    /// send-mail — MailRequest{Type=SEND, Item=MessageItem}. Correlates the next SEND_*
    /// MailResponse and returns the echoed message (with its assigned db id) to the caller.
    /// </summary>
    internal static async Task<CommandDispatcher.Response> SendMailAsync(AriesClient cityAries, uint myAvatarId, JsonObject args, CancellationToken ct)
    {
        var toRaw = args["target_persist_id"];
        if (toRaw == null) return CommandDispatcher.Response.Fail("send-mail requires target_persist_id");
        uint to;
        try { to = (uint)(long)toRaw; }
        catch { return CommandDispatcher.Response.Fail("target_persist_id must be an unsigned integer"); }
        if (to == 0) return CommandDispatcher.Response.Fail("target_persist_id must be > 0");

        var subject = (string)args["subject"];
        if (string.IsNullOrEmpty(subject)) return CommandDispatcher.Response.Fail("send-mail requires non-empty subject");
        if (subject.Length > MaxSubjectChars)
        {
            return CommandDispatcher.Response.Fail($"subject exceeds {MaxSubjectChars} chars (was {subject.Length})");
        }

        var body = (string)args["body"];
        if (string.IsNullOrEmpty(body)) return CommandDispatcher.Response.Fail("send-mail requires non-empty body");
        if (body.Length > MaxBodyChars)
        {
            return CommandDispatcher.Response.Fail($"body exceeds {MaxBodyChars} chars (was {body.Length})");
        }

        if (Interlocked.Exchange(ref _sendInFlight, 1) == 1)
        {
            return CommandDispatcher.Response.Fail("another send-mail is in flight (single-slot correlator limitation)");
        }

        var tcs = new TaskCompletionSource<MailResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingSend = tcs;

        try
        {
            var item = new MessageItem
            {
                SenderID = myAvatarId,   // Server overwrites from session (see MailHandler.cs:91) — we set for echo-coherence.
                TargetID = to,
                Subject = subject,
                Body = body,
                SenderName = "",         // Server overwrites from DataService (MailHandler.cs:92).
                Type = 0,                // Normal mail (0=message per MessageItem.cs comment).
                Subtype = 0,             // Normal (0=Normal per MessageSpecialType enum).
                Time = 0,                // Server stamps on INSERT.
            };

            cityAries.Write(new MailRequest
            {
                Type = MailRequestType.SEND,
                Item = item,
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var respTask = tcs.Task;
            var first = await Task.WhenAny(respTask, Task.Delay(Timeout.Infinite, cts.Token));
            if (first != respTask)
            {
                return CommandDispatcher.Response.Fail("send-mail timeout after 10s (no SEND_* MailResponse)");
            }

            var resp = await respTask;
            var echoed = (resp.Messages != null && resp.Messages.Length > 0) ? resp.Messages[0] : null;
            return CommandDispatcher.Response.Success(new
            {
                type = resp.Type.ToString(),
                success = resp.Type == MailResponseType.SEND_SUCCESS,
                target_persist_id = (long)to,
                subject,
                body_length = body.Length,
                mail_id = echoed?.ID ?? 0,
                echoed_time_ticks = echoed?.Time ?? 0L,
            });
        }
        finally
        {
            _pendingSend = null;
            Interlocked.Exchange(ref _sendInFlight, 0);
        }
    }

    // ---- delete-mail ----

    /// <summary>
    /// delete-mail — MailRequest{Type=DELETE, TimestampID=mail_id}. Server has no response PDU
    /// on this path; returns queued=true. Wire-level effect verification is DB readback.
    /// </summary>
    internal static CommandDispatcher.Response DeleteMail(AriesClient cityAries, JsonObject args)
    {
        var idRaw = args["mail_id"];
        if (idRaw == null) return CommandDispatcher.Response.Fail("delete-mail requires mail_id");
        long mailId;
        try { mailId = (long)idRaw; }
        catch { return CommandDispatcher.Response.Fail("mail_id must be an integer"); }
        if (mailId <= 0) return CommandDispatcher.Response.Fail("mail_id must be > 0");

        try
        {
            cityAries.Write(new MailRequest
            {
                Type = MailRequestType.DELETE,
                TimestampID = mailId,
            });
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"city socket write failed: {ex.GetType().Name}: {ex.Message}");
        }

        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "delete-mail",
            mail_id = mailId,
            note = "server has no response PDU for DELETE; verify deletion via poll-inbox or DB readback",
        });
    }

    // ---- perception wiring ----

    /// <summary>
    /// Wire inbound MailResponse packets of Type=NEW_MAIL (real-time pushes from senders) into
    /// recent_events as kind=mail direction=in. Keeps poll/send responses off the event stream
    /// (those are correlated request-responses, not asynchronous notifications).
    /// </summary>
    public static void WireMailPerception(AriesClient cityAries, PerceptionProjector projector)
    {
        if (cityAries == null) throw new ArgumentNullException(nameof(cityAries));
        if (projector == null) throw new ArgumentNullException(nameof(projector));
        cityAries.AddSubscriber(new NewMailPerceptionSubscriber(projector));
    }

    // ---- internals ----

    private static object MessageItemToJson(MessageItem m)
    {
        if (m == null) return null;
        return new
        {
            id = m.ID,
            sender_persist_id = (long)m.SenderID,
            target_persist_id = (long)m.TargetID,
            sender_name = m.SenderName ?? string.Empty,
            subject = m.Subject ?? string.Empty,
            body = m.Body ?? string.Empty,
            time_ticks = m.Time,
            type = m.Type,
            subtype = m.Subtype,
            read_state = m.ReadState,
            reply_id = m.ReplyID,
        };
    }

    /// <summary>
    /// City-socket subscriber routing inbound MailResponse packets to pending poll/send TCS.
    /// NEW_MAIL pushes are not correlated — those flow through the perception subscriber.
    /// </summary>
    private sealed class MailResponseSubscriber : IAriesMessageSubscriber, IAriesEventSubscriber
    {
        public void MessageReceived(AriesClient client, object message)
        {
            if (message is not MailResponse resp) return;

            switch (resp.Type)
            {
                case MailResponseType.POLL_RESPONSE:
                    var pollTcs = _pendingPoll;
                    if (pollTcs != null) pollTcs.TrySetResult(resp);
                    else Program.Log($"[mail] stray POLL_RESPONSE (no pending poll-inbox)");
                    break;

                case MailResponseType.SEND_SUCCESS:
                case MailResponseType.SEND_FAILED:
                case MailResponseType.SEND_IGNORING_YOU:
                case MailResponseType.SEND_IGNORING_THEM:
                case MailResponseType.SEND_THROTTLED:
                case MailResponseType.SEND_THROTTLED_DAILY:
                    var sendTcs = _pendingSend;
                    if (sendTcs != null) sendTcs.TrySetResult(resp);
                    else Program.Log($"[mail] stray SEND_* response type={resp.Type} (no pending send-mail)");
                    break;

                case MailResponseType.NEW_MAIL:
                    // Handled by NewMailPerceptionSubscriber. Ignore here.
                    break;
            }
        }

        public void SessionCreated(AriesClient c) { }
        public void SessionOpened(AriesClient c) { }
        public void SessionClosed(AriesClient c)
        {
            // Cancel any in-flight polls/sends — the socket is gone.
            _pendingPoll?.TrySetCanceled();
            _pendingSend?.TrySetCanceled();
        }
        public void SessionIdle(AriesClient c) { }
        public void InputClosed(AriesClient c) { }
    }

    /// <summary>
    /// Subscriber that projects inbound NEW_MAIL responses into perception. Each MessageItem in
    /// the MailResponse becomes a separate recent_events entry of kind=mail, direction=in, so
    /// an agent sees each new letter as a discrete event.
    /// </summary>
    private sealed class NewMailPerceptionSubscriber : IAriesMessageSubscriber, IAriesEventSubscriber
    {
        private readonly PerceptionProjector _projector;
        public NewMailPerceptionSubscriber(PerceptionProjector projector) { _projector = projector; }

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is not MailResponse resp) return;
            if (resp.Type != MailResponseType.NEW_MAIL) return;
            if (resp.Messages == null) return;

            foreach (var m in resp.Messages)
            {
                try
                {
                    var extras = new Dictionary<string, object>
                    {
                        ["direction"] = "in",
                        ["mail_id"] = m.ID,
                        ["sender_persist_id"] = (long)m.SenderID,
                        ["sender_name"] = m.SenderName ?? string.Empty,
                        ["subject"] = m.Subject ?? string.Empty,
                        ["time_ticks"] = m.Time,
                    };
                    var evt = new PerceptionEvent
                    {
                        T = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Kind = "mail",
                        Text = m.Body ?? string.Empty,
                        Extras = extras,
                    };
                    _projector.AddRecentEvent(evt);
                }
                catch (Exception ex)
                {
                    Program.Log($"[perception] mail emit failed: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        public void SessionCreated(AriesClient c) { }
        public void SessionOpened(AriesClient c) { }
        public void SessionClosed(AriesClient c) { }
        public void SessionIdle(AriesClient c) { }
        public void InputClosed(AriesClient c) { }
    }
}
