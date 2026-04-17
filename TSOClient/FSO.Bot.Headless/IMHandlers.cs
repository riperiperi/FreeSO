/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.Server.Clients;
using FSO.Server.Protocol.Electron.Packets;

namespace FSO.Bot.Headless;

/// <summary>
/// Instant-messaging verb family (freesoexperiment-7d8). Single op:
/// <c>instant-message</c> — send a typed cross-lot IM to another avatar by persist_id.
///
/// <para>
/// <b>Wire path (distinct from speak!):</b> IM rides the <b>CITY</b> Aries socket as
/// <c>InstantMessage</c> (Electron packet), NOT the lot socket and NOT a VMNetCmd. The server's
/// <c>FSO.Server.Servers.City.Handlers.MessagingHandler</c> receives the packet, looks up the
/// target avatar's live city session via <c>Sessions.GetByAvatarId</c>, and either
/// <list type="bullet">
///   <item>forwards the <c>InstantMessage</c> unchanged to the target session (success path — no
///   SUCCESS_ACK is sent to the sender; the only wire-level confirmation is that no FAILURE_ACK
///   came back), OR</item>
///   <item>returns a <c>FAILURE_ACK</c> variant to the sender with a reason
///   (<c>THEY_ARE_OFFLINE</c>, <c>THEY_ARE_IGNORING_YOU</c>, <c>YOU_ARE_IGNORING_THEM</c>,
///   <c>MESSAGE_QUEUE_FULL</c>).</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Self-IM is a legitimate path</b> on this server: if the target persist id equals our own
/// avatar id, <c>Sessions.GetByAvatarId</c> returns our own session back, and the server's
/// <c>targetSession.Write(message)</c> echoes the IM to us. That's the wire-level-effect
/// verification lever the integration test pulls: a round-tripped IM proves the city-socket
/// plumbing works end-to-end without requiring a second live bot session (which is -365 / -abf
/// scope).
/// </para>
///
/// <para>
/// <b>AckID</b> — the IM protocol carries a GUID string correlator. We mint a fresh one per
/// outbound IM and stash it so the inbound echo (or any FAILURE_ACK) correlates back to the
/// originating call. Correlated responses land in <c>recent_events</c> as kind=<c>im</c>.
/// </para>
///
/// <para>
/// <b>Thread safety:</b> IM send is straight <c>cityAries.Write(packet)</c>; AriesClient's
/// serializer is already thread-safe (Mina IoFilter chain). The pending-AckID table is guarded
/// by a plain lock. No VM state touched — no tick-lock needed.
/// </para>
/// </summary>
public static class IMHandlers
{
    // Outbound AckID → metadata. Populated on send, drained on receive (echo or fail), so the
    // perception event can carry "outgoing" vs "incoming" context for the agent. We deliberately
    // do NOT block the sender on an ACK — MessagingHandler never sends a SUCCESS_ACK, so waiting
    // would deadlock. Send is fire-and-forget; the round-trip is observed out-of-band via
    // perception recent_events (kind=im).
    private static readonly Dictionary<string, PendingIM> _pending = new();
    private static readonly object _pendingLock = new();
    private const int PendingCap = 64;

    private sealed class PendingIM
    {
        public uint To;
        public string Text;
        public long SentAtMs;
    }

    /// <summary>
    /// Register the <c>instant-message</c> op. <paramref name="cityAries"/> is the live city
    /// Aries connection (Program.cs's <c>cityAries</c>); <paramref name="myAvatarId"/> is the
    /// bot's own avatar persist_id for self-targeting validation.
    /// </summary>
    public static void RegisterAll(CommandDispatcher dispatcher, AriesClient cityAries, uint myAvatarId)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (cityAries == null) throw new ArgumentNullException(nameof(cityAries));

        dispatcher.Register("instant-message", (args, ct) => Task.FromResult(InstantMessage(cityAries, myAvatarId, args)));
    }

    /// <summary>
    /// Subscribe to inbound <c>InstantMessage</c> packets on the city socket and project them
    /// into perception as <c>recent_events</c> entries of kind <c>im</c>. Handles three inbound
    /// shapes:
    /// <list type="bullet">
    ///   <item><c>Type=MESSAGE</c> — a real IM delivered to us (either from another avatar, or
    ///   the server's echo of a self-IM we just sent). Surfaced as kind=<c>im</c> with
    ///   direction=<c>in</c>.</item>
    ///   <item><c>Type=FAILURE_ACK</c> — our outbound IM couldn't be delivered; server tells us
    ///   why via <c>Reason</c>. Surfaced as kind=<c>im</c> with direction=<c>fail</c> and the
    ///   reason enum name.</item>
    ///   <item><c>Type=SUCCESS_ACK</c> — the server never sends this on the canonical code path
    ///   (MessagingHandler uses fire-and-forget forwarding), but if a future server revision
    ///   starts emitting them we still surface it so the agent sees the confirmation.</item>
    /// </list>
    /// Load-bearing for the integration test: a bare IPC ACK does not prove the PDU reached the
    /// server's MessagingHandler. A self-IM that round-trips back as a perception recent_events
    /// entry does.
    /// </summary>
    public static void WireIMPerception(CityListener cityListener, PerceptionProjector projector)
    {
        if (cityListener == null) throw new ArgumentNullException(nameof(cityListener));
        if (projector == null) throw new ArgumentNullException(nameof(projector));
        cityListener.OnInstantMessage += (AriesClient _, InstantMessage im) =>
        {
            try
            {
                if (im == null) return;
                var direction = im.Type == InstantMessageType.MESSAGE ? "in"
                              : im.Type == InstantMessageType.FAILURE_ACK ? "fail"
                              : im.Type == InstantMessageType.SUCCESS_ACK ? "ack"
                              : "unknown";

                // Pop pending metadata if the AckID matches a send we made; helps the agent
                // correlate an incoming echo with its originating call.
                PendingIM pending = null;
                if (!string.IsNullOrEmpty(im.AckID))
                {
                    lock (_pendingLock)
                    {
                        if (_pending.TryGetValue(im.AckID, out pending))
                        {
                            _pending.Remove(im.AckID);
                        }
                    }
                }

                var extras = new Dictionary<string, object>
                {
                    ["direction"] = direction,
                    ["from_persist_id"] = im.From,
                    ["to_persist_id"] = im.To,
                    ["ack_id"] = im.AckID ?? string.Empty,
                    ["im_type"] = im.Type.ToString(),
                    ["reason"] = im.Reason.ToString(),
                };
                if (pending != null)
                {
                    extras["correlated_send"] = true;
                    extras["outbound_text"] = pending.Text;
                }

                var evt = new PerceptionEvent
                {
                    T = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Kind = "im",
                    Text = im.Message ?? string.Empty,
                    Extras = extras,
                };
                projector.AddRecentEvent(evt);
            }
            catch (Exception ex)
            {
                Program.Log($"[perception] im emit failed: {ex.GetType().Name}: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// instant-message — send an <see cref="InstantMessage"/> PDU to another avatar by
    /// persist_id. Required args: <c>target_persist_id</c> (uint), <c>text</c> (string).
    /// Optional: <c>color</c> (uint, default 0xFF000000 — fully opaque default; server ORs in
    /// <c>0xFF000000</c> anyway). The handler does NOT wait for a reply — MessagingHandler
    /// never sends SUCCESS_ACK, so the only observable wire effect is an inbound MESSAGE (echo
    /// in the self-IM case) or a FAILURE_ACK, both surfaced as perception <c>recent_events</c>.
    /// </summary>
    internal static CommandDispatcher.Response InstantMessage(AriesClient cityAries, uint myAvatarId, JsonObject args)
    {
        var toRaw = args["target_persist_id"];
        if (toRaw == null) return CommandDispatcher.Response.Fail("instant-message requires target_persist_id");
        uint to;
        try { to = (uint)(long)toRaw; }
        catch { return CommandDispatcher.Response.Fail("target_persist_id must be an unsigned integer"); }
        if (to == 0) return CommandDispatcher.Response.Fail("target_persist_id must be non-zero");

        var text = (string)args["text"];
        if (string.IsNullOrEmpty(text)) return CommandDispatcher.Response.Fail("instant-message requires non-empty text");
        // Server-side MessageQueue caps total traffic by count not length, but truncate long
        // payloads defensively so we never blow the Pascal VLC string (< 2GB but practically
        // server UI caps render long lines unusably). Match the speak cap for consistency.
        const int MaxTextChars = 1000;
        if (text.Length > MaxTextChars) text = text.Substring(0, MaxTextChars);

        uint color = unchecked((uint)0xFF000000);
        var colorNode = args["color"];
        if (colorNode != null)
        {
            try { color = (uint)(long)colorNode; }
            catch { return CommandDispatcher.Response.Fail("color must be an unsigned integer"); }
        }

        var ackId = Guid.NewGuid().ToString("N");

        lock (_pendingLock)
        {
            // Simple FIFO cap — if the agent fires a burst and we never hear back, drop oldest.
            if (_pending.Count >= PendingCap)
            {
                string oldest = null;
                long oldestTs = long.MaxValue;
                foreach (var kv in _pending)
                {
                    if (kv.Value.SentAtMs < oldestTs) { oldestTs = kv.Value.SentAtMs; oldest = kv.Key; }
                }
                if (oldest != null) _pending.Remove(oldest);
            }
            _pending[ackId] = new PendingIM
            {
                To = to,
                Text = text,
                SentAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
        }

        var pkt = new InstantMessage
        {
            // FromType / From are overwritten by the server from the session; we fill them here
            // for completeness and so a self-IM echo carries a coherent From field even on the
            // inbound side.
            FromType = FSO.Common.Enum.UserReferenceType.AVATAR,
            From = myAvatarId,
            To = to,
            Type = InstantMessageType.MESSAGE,
            Message = text,
            AckID = ackId,
            Color = color,
        };

        try
        {
            cityAries.Write(pkt);
        }
        catch (Exception ex)
        {
            // Remove from pending since the wire write blew up.
            lock (_pendingLock) { _pending.Remove(ackId); }
            return CommandDispatcher.Response.Fail($"city socket write failed: {ex.GetType().Name}: {ex.Message}");
        }

        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            target_persist_id = (long)to,
            self_target = to == myAvatarId,
            text = text,
            length = text.Length,
            ack_id = ackId,
        });
    }
}
