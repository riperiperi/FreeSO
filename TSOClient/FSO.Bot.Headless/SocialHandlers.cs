/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Concrete social-family command handlers (freesoexperiment-9ae). Translates JSON args into
/// outbound <c>VMNetChatCmd</c> (public speech) and <c>VMNetInteractionCmd</c> (directed social
/// interactions on another Sim resolved by pie-menu name) PDUs via
/// <c>vmHost.Driver.SendCommand</c>.
///
/// <para>
/// The six verbs fall into two shapes:
/// <list type="bullet">
///   <item><b>speak</b> — channel chat via <c>VMNetChatCmd</c>. Fire-and-forget on the wire; the
///   lot VM echoes it as a <c>VMChatEvent</c> routed through <c>VM.OnChatEvent</c>, surfaced here
///   as a perception <c>recent_events</c> entry of kind <c>chat</c>.</item>
///   <item><b>be-friendly, tell-joke, flirt, be-mean, give-gift</b> — directed socials. Each is a
///   pre-parameterized specialisation of <c>interact-with</c>: the target's pie menu is queried
///   on the local VM, the first entry whose name matches the verb's alias set is selected, and
///   <c>VMNetInteractionCmd</c> is emitted with that TTAB index. If no matching entry exists on
///   the target (wrong sim class, missing social unlock, hidden by gates), the handler returns
///   <c>ok=false</c> with a descriptive error — never a silent no-op.</item>
/// </list>
/// </para>
///
/// <para>
/// Thread safety: reading <c>VMAvatar</c>.<c>GetPieMenu</c> touches TTAB evaluation that runs
/// SimAntics primitives — VM-tick-thread-owned state. We route those reads through
/// <see cref="HeadlessVMHost.RunUnderTickLock{T}(Func{T})"/> to serialise against the tick thread
/// (enforced by freesoexperiment-a85). Command emission via the driver is already serialised by
/// the CommandDispatcher's serial ReadLoop plus VMClientDriver's outgoing lock.
/// </para>
/// </summary>
public static class SocialHandlers
{
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));

        dispatcher.Register("speak", (args, ct) => Task.FromResult(Speak(vmHost, args)));
        dispatcher.Register("be-friendly", (args, ct) => Task.FromResult(DirectedSocial(vmHost, args, SocialVerb.BeFriendly)));
        dispatcher.Register("tell-joke", (args, ct) => Task.FromResult(DirectedSocial(vmHost, args, SocialVerb.TellJoke)));
        dispatcher.Register("flirt", (args, ct) => Task.FromResult(DirectedSocial(vmHost, args, SocialVerb.Flirt)));
        dispatcher.Register("be-mean", (args, ct) => Task.FromResult(DirectedSocial(vmHost, args, SocialVerb.BeMean)));
        dispatcher.Register("give-gift", (args, ct) => Task.FromResult(DirectedSocial(vmHost, args, SocialVerb.GiveGift)));
    }

    /// <summary>
    /// Wire <see cref="VM.OnChatEvent"/> to the perception projector so outbound <c>speak</c>
    /// commands (and any chat observed on the lot) become <c>recent_events</c> entries of kind
    /// <c>chat</c>. Load-bearing for the integration test's ground-source-truth check: a bare
    /// <c>VMNetChatCmd</c> ACK does not prove the server round-tripped — the echoed chat event
    /// does.
    /// </summary>
    public static void WireChatPerception(HeadlessVMHost vmHost, PerceptionProjector projector)
    {
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));
        if (projector == null) throw new ArgumentNullException(nameof(projector));
        if (vmHost.VM == null) return; // VM not yet live; caller is expected to invoke post-lot-join.

        vmHost.VM.OnChatEvent += evt =>
        {
            try
            {
                if (evt == null) return;
                // Types: 0=Message, 1=MessageMe. Join/Leave/Generic/Debug are not agent-facing
                // speech; surface only actual lines. Text is a string[] (sender, body) — join.
                if (evt.Type != VMChatEventType.Message && evt.Type != VMChatEventType.MessageMe) return;
                var body = evt.Text != null && evt.Text.Length >= 2 ? evt.Text[1] : (evt.Text != null ? string.Join(" ", evt.Text) : string.Empty);
                var sender = evt.Text != null && evt.Text.Length >= 1 ? evt.Text[0] : string.Empty;
                var pevt = new PerceptionEvent
                {
                    T = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Kind = "chat",
                    Text = body ?? string.Empty,
                    Extras = new Dictionary<string, object>
                    {
                        ["sender"] = sender ?? string.Empty,
                        ["sender_persist_id"] = evt.SenderUID,
                        ["channel_id"] = (int)evt.ChannelID,
                        ["type"] = evt.Type.ToString(),
                    },
                };
                projector.AddRecentEvent(pevt);
            }
            catch (Exception ex)
            {
                Program.Log($"[perception] chat emit failed: {ex.GetType().Name}: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// speak — <c>VMNetChatCmd</c>. Required arg: <c>text</c>. Optional: <c>channel_id</c>
    /// (default 0 = lot default). Leading '/' is reserved for admin commands server-side and is
    /// rejected here so a non-admin agent cannot accidentally invoke one.
    /// </summary>
    internal static CommandDispatcher.Response Speak(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        var text = (string)args["text"];
        if (string.IsNullOrEmpty(text)) return CommandDispatcher.Response.Fail("speak requires text");
        // 200-char server-side truncation; mirror it so payload echo is accurate.
        if (text.Length > 200) text = text.Substring(0, 200);
        // Admin-command gate: only Admin perms can fire / commands (VMNetChatCmd.Execute
        // enforces this, but reject at the bot so the agent sees a clean failure).
        if (text.Length > 0 && text[0] == '/' )
        {
            return CommandDispatcher.Response.Fail("speak: leading '/' is reserved for admin chat commands — use an explicit admin op");
        }

        byte channelId = (byte)((long?)args["channel_id"] ?? 0L);

        var cmd = new VMNetChatCmd
        {
            Message = text,
            ChannelID = channelId,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            text = text,
            channel_id = (int)channelId,
            length = text.Length,
        });
    }

    // ---- directed social ----

    public enum SocialVerb
    {
        BeFriendly,
        TellJoke,
        Flirt,
        BeMean,
        GiveGift,
    }

    /// <summary>
    /// Alias table: each verb maps to an ordered list of substring matchers tried (case-insensitive)
    /// against the target's pie-menu interaction names. The first match wins. Order is
    /// specificity-first so "Tell Joke" is preferred over a bare "Joke" subcategory when both exist.
    /// <para>
    /// Strings are deliberately permissive because TTO's social pie menu is asset-driven and has
    /// variants (e.g. "Be Friendly — Hug", "Tell Joke — Knock Knock"). The catch-all substring
    /// match gives the agent one stable verb per intent even if the asset catalogue carries many
    /// flavours. If a verb has zero matches on the live pie menu we fail explicitly.
    /// </para>
    /// </summary>
    private static readonly Dictionary<SocialVerb, string[]> VerbAliases = new()
    {
        [SocialVerb.BeFriendly] = new[] { "be friendly", "friendly", "hug", "compliment", "greet" },
        [SocialVerb.TellJoke]   = new[] { "tell joke", "joke" },
        [SocialVerb.Flirt]      = new[] { "flirt", "kiss", "cuddle", "romantic" },
        [SocialVerb.BeMean]     = new[] { "be mean", "mean", "insult", "tease", "attack" },
        [SocialVerb.GiveGift]   = new[] { "give gift", "gift" },
    };

    internal static CommandDispatcher.Response DirectedSocial(HeadlessVMHost vmHost, JsonObject args, SocialVerb verb)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        // Target resolution: target_sim_id (preferred) or target_object_id (for VMAvatar objects).
        var targetSim = (long?)args["target_sim_id"];
        var targetObj = (long?)args["target_object_id"];
        VMAvatar target = null;
        if (targetSim.HasValue && targetSim.Value != 0)
        {
            target = vmHost.VM.GetAvatarByPersist((uint)targetSim.Value);
            if (target == null) return CommandDispatcher.Response.Fail($"sim {targetSim} not in local VM");
        }
        else if (targetObj.HasValue && targetObj.Value != 0)
        {
            var ent = vmHost.VM.GetObjectById(checked((short)targetObj.Value));
            if (ent is VMAvatar av) target = av;
            if (target == null) return CommandDispatcher.Response.Fail($"object {targetObj} is not a Sim");
        }
        else
        {
            return CommandDispatcher.Response.Fail($"{VerbName(verb)} requires target_sim_id or target_object_id");
        }

        // Resolve pie-menu entry by alias. The pie-menu build evaluates SimAntics "check" trees on
        // the target — VM-tick-owned state — so route through the tick lock.
        var aliases = VerbAliases[verb];
        var (interactionId, matchedName, availableNames) = vmHost.RunUnderTickLock(() =>
        {
            List<VMPieMenuInteraction> pie = null;
            try { pie = target.GetPieMenu(vmHost.VM, caller, includeHidden: false, includeGlobal: true); }
            catch { pie = null; }
            if (pie == null || pie.Count == 0) return ((byte?)null, (string)null, (List<string>)new List<string>());

            var names = pie.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList();
            foreach (var alias in aliases)
            {
                foreach (var p in pie)
                {
                    if (p.Name == null) continue;
                    if (p.Name.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return ((byte?)p.ID, p.Name, names);
                    }
                }
            }
            return ((byte?)null, (string)null, names);
        });

        if (!interactionId.HasValue)
        {
            // Surface the available names so the agent (and the test) can see why the match failed.
            return CommandDispatcher.Response.Fail(
                $"{VerbName(verb)}: no pie-menu entry on target matching any of [{string.Join(", ", aliases)}]; " +
                $"available=[{string.Join(", ", availableNames.Take(32))}]");
        }

        var queueMode = QueueModeHelper.ReadQueueMode(args);
        if (!QueueModeHelper.ApplyQueueMode(vmHost, queueMode, out var cancelled, out var qmErr))
            return CommandDispatcher.Response.Fail(qmErr);

        var cmd = new VMNetInteractionCmd
        {
            Interaction = interactionId.Value,
            CalleeID = target.ObjectID,
            Param0 = 0,
            Global = false,
            CallerID = caller.ObjectID,
        };
        vmHost.Driver.SendCommand(cmd);

        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = VerbName(verb),
            interaction = (int)interactionId.Value,
            matched_name = matchedName,
            callee_id = (int)target.ObjectID,
            target_sim_id = (long)target.PersistID,
            queue_mode = queueMode,
            cancelled,
        });
    }

    private static string VerbName(SocialVerb v) => v switch
    {
        SocialVerb.BeFriendly => "be-friendly",
        SocialVerb.TellJoke => "tell-joke",
        SocialVerb.Flirt => "flirt",
        SocialVerb.BeMean => "be-mean",
        SocialVerb.GiveGift => "give-gift",
        _ => v.ToString(),
    };
}
