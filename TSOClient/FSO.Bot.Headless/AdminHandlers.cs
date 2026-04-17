/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.Server.Clients;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Admin-family command handlers (freesoexperiment-3df). Verbs gated on the caller's session
/// carrying the admin bit (<c>fso_users.is_admin=1</c>, or <c>is_moderator=1</c> for the
/// <c>ModerationRequest</c> path — the server's <see cref="FSO.Server.Servers.City.Handlers.ModerationHandler"/>
/// accepts either). The bot enforces a <b>deterministic refuse path</b> by inspecting its own
/// in-VM <see cref="VMTSOAvatarPermissions"/> before sending any PDU — matching the pattern
/// used by <see cref="PropertyHandlers"/>' owner gate and <see cref="SocialHandlers"/>' leading-'/'
/// refuse.
///
/// <para>
/// <b>Catalog rows shipped (6):</b>
/// <list type="bullet">
///   <item><b>kick-user</b> — <c>ModerationRequest{Type=KICK_USER, EntityId=avatar_id}</c> on the
///   city socket. Server closes the target's session.</item>
///   <item><b>ban-user</b> — <c>ModerationRequest{Type=BAN_USER, EntityId=avatar_id}</c>. Sets
///   <c>fso_users.is_banned=1</c> and closes the session.</item>
///   <item><b>ipban-user</b> — <c>ModerationRequest{Type=IPBAN_USER, EntityId=avatar_id}</c>.
///   Same as ban-user plus an <c>fso_bans</c> row for the target's last IP (unless loopback).</item>
///   <item><b>archive-approve-user</b> — <c>ArchiveModerationRequest{Type=APPROVE_USER, EntityId=user_id}</c>.
///   Sets <c>fso_users.is_verified=1</c>. Note: EntityId is a <i>user_id</i>, not an avatar_id.</item>
///   <item><b>archive-reject-user</b> — <c>ArchiveModerationRequest{Type=REJECT_USER, EntityId=user_id}</c>.
///   Closes the target's session with a <c>VerificationNotification{IsVerified=false}</c>.</item>
///   <item><b>admin-chat-command</b> — <c>VMNetChatCmd{Message="/cmd args"}</c> on the lot socket.
///   Dispatched by <c>VMNetChatCmd.Execute</c>; server checks
///   <c>avatar.AvatarState.Permissions &gt;= VMTSOAvatarPermissions.Admin</c>. Supports the full
///   stock command set (ban, banip, unban, banlist, builder, admin, roomie, visitor, close,
///   qtrday, setjob, trace, reload, time, tickrate, speed, tuning, fixall, testcollision). The
///   handler constructs the slash-prefixed message; command validity itself is the server's job.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Catalog row dropped:</b>
/// <list type="bullet">
///   <item><b>cheat-command</b> (catalog row 110) — the <c>!</c>-prefixed commands are all
///   <c>UICheatHandler</c> client-side (diagnostic prints) or forward to verbs already in other
///   families (e.g. <c>!del</c> → <c>VMNetDeleteObjectCmd</c>, owned by build-buy-catalog's
///   <c>delete-object</c>). There is no server-side <c>!</c>-prefix dispatch in
///   <c>VMNetChatCmd.Execute</c>; a <c>!del 123</c> string would be surfaced as a chat message,
///   not a deletion. Dropping per the item-spec rule "if catalog lists ops that don't have a
///   clean wire path, drop them with a note — do NOT stub." Delete semantics will arrive via
///   build-buy-catalog's <c>delete-object</c> op.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Admin gate enforcement.</b> Every op's handler calls <see cref="CheckAdmin"/>, which reads
/// <c>vmHost.VM.GetAvatarByPersist(myPid).AvatarState.Permissions</c> under the tick lock. The
/// server assigns <c>VMTSOAvatarPermissions.Admin</c> to avatars whose backing
/// <c>fso_avatars.moderation_level &gt; 0</c> (see <c>LotContainer.GetAvatarPermissions:2036</c>).
/// That's the same bit the city's <see cref="FSO.Server.Servers.City.Handlers.ModerationHandler"/>
/// reads. Checking it locally gives a deterministic refuse-path without waiting for the server
/// (which itself silently drops non-admin requests — no <c>ModerationResponse</c> exists).
/// </para>
///
/// <para>
/// <b>Non-admin refuse-path test gap.</b> Baron's session is always admin, so this bot cannot
/// exercise the refuse path against a live server. Unit-tested by constructing a stub avatar
/// state with <c>Permissions=Visitor</c> and asserting the handler returns <c>ok=false</c>. Same
/// gap as <see cref="PropertyHandlers"/> owner gate (freesoexperiment-163 tracks a future
/// secondary-bot harness).
/// </para>
///
/// <para>
/// <b>Fire-and-forget wire shape.</b> Neither <c>ModerationRequest</c> nor <c>ArchiveModerationRequest</c>
/// has a response packet — the server just mutates DB + closes sessions. Handlers return
/// <c>ok=true</c> once the PDU is written; ground-source verification is via DB readback in the
/// integration test (fso_users.is_banned, fso_users.is_verified, fso_bans row presence).
/// </para>
///
/// <para>
/// <b>Thread safety.</b> <see cref="CheckAdmin"/> runs under <see cref="HeadlessVMHost.RunUnderTickLock{T}"/>
/// — Permissions lives on <c>VMTSOAvatarState</c> which the tick thread mutates. City-socket
/// writes and lot driver SendCommand are internally synchronized.
/// </para>
/// </summary>
public static class AdminHandlers
{
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost, AriesClient cityAries)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));
        if (cityAries == null) throw new ArgumentNullException(nameof(cityAries));

        dispatcher.Register("kick-user",            (args, ct) => Task.FromResult(KickUser(vmHost, cityAries, args)));
        dispatcher.Register("ban-user",             (args, ct) => Task.FromResult(BanUser(vmHost, cityAries, args)));
        dispatcher.Register("ipban-user",           (args, ct) => Task.FromResult(IpbanUser(vmHost, cityAries, args)));
        dispatcher.Register("archive-approve-user", (args, ct) => Task.FromResult(ArchiveApproveUser(vmHost, cityAries, args)));
        dispatcher.Register("archive-reject-user",  (args, ct) => Task.FromResult(ArchiveRejectUser(vmHost, cityAries, args)));
        dispatcher.Register("admin-chat-command",   (args, ct) => Task.FromResult(AdminChatCommand(vmHost, args)));
    }

    /// <summary>
    /// Returns (isAdmin, currentPermissions). <c>isAdmin</c> is true iff the caller's in-VM
    /// <see cref="VMTSOAvatarState.Permissions"/> is <see cref="VMTSOAvatarPermissions.Admin"/>.
    /// </summary>
    internal static (bool isAdmin, VMTSOAvatarPermissions perms) CheckAdmin(HeadlessVMHost vmHost)
    {
        return vmHost.RunUnderTickLock(() =>
        {
            var vm = vmHost.VM;
            if (vm == null) return (false, VMTSOAvatarPermissions.Visitor);
            var caller = vm.GetAvatarByPersist(vmHost.MyAvatarPersistId);
            if (caller == null) return (false, VMTSOAvatarPermissions.Visitor);
            var state = caller.TSOState as VMTSOAvatarState;
            if (state == null) return (false, VMTSOAvatarPermissions.Visitor);
            return (state.Permissions == VMTSOAvatarPermissions.Admin, state.Permissions);
        });
    }

    /// <summary>
    /// Unit-test seam: <see cref="CheckAdmin"/> with an externally-supplied permissions value
    /// instead of a VM lookup. Tests can exercise the refuse path against a stub avatar state
    /// without standing up a HeadlessVMHost (which requires FSO.Content.Init).
    /// </summary>
    internal static CommandDispatcher.Response RequireAdmin(VMTSOAvatarPermissions perms, string verb)
    {
        if (perms != VMTSOAvatarPermissions.Admin)
        {
            return CommandDispatcher.Response.Fail($"{verb}: caller is not an admin (current_permissions={perms})");
        }
        return null; // caller should continue
    }

    // ---- ModerationRequest ops (city socket; EntityId = avatar_id) ----

    internal static CommandDispatcher.Response KickUser(HeadlessVMHost vmHost, AriesClient city, JsonObject args)
        => ModerationOp(vmHost, city, args, ModerationRequestType.KICK_USER, "kick-user");

    internal static CommandDispatcher.Response BanUser(HeadlessVMHost vmHost, AriesClient city, JsonObject args)
        => ModerationOp(vmHost, city, args, ModerationRequestType.BAN_USER, "ban-user");

    internal static CommandDispatcher.Response IpbanUser(HeadlessVMHost vmHost, AriesClient city, JsonObject args)
        => ModerationOp(vmHost, city, args, ModerationRequestType.IPBAN_USER, "ipban-user");

    private static CommandDispatcher.Response ModerationOp(
        HeadlessVMHost vmHost, AriesClient city, JsonObject args,
        ModerationRequestType type, string verb)
    {
        var (isAdmin, perms) = CheckAdmin(vmHost);
        if (!isAdmin)
        {
            return CommandDispatcher.Response.Fail($"{verb}: caller is not an admin (current_permissions={perms})");
        }

        var targetPid = (long?)args["target_persist_id"] ?? 0L;
        if (targetPid <= 0)
        {
            return CommandDispatcher.Response.Fail($"{verb} requires target_persist_id (> 0)");
        }
        if ((uint)targetPid == vmHost.MyAvatarPersistId)
        {
            return CommandDispatcher.Response.Fail($"{verb}: refusing to target self");
        }

        var req = new ModerationRequest
        {
            Type = type,
            EntityId = (uint)targetPid,
        };
        city.Write(req);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = verb,
            type = type.ToString(),
            target_persist_id = targetPid,
        });
    }

    // ---- ArchiveModerationRequest ops (city socket; EntityId = user_id, NOT avatar_id) ----

    internal static CommandDispatcher.Response ArchiveApproveUser(HeadlessVMHost vmHost, AriesClient city, JsonObject args)
        => ArchiveModerationOp(vmHost, city, args, ArchiveModerationRequestType.APPROVE_USER, "archive-approve-user");

    internal static CommandDispatcher.Response ArchiveRejectUser(HeadlessVMHost vmHost, AriesClient city, JsonObject args)
        => ArchiveModerationOp(vmHost, city, args, ArchiveModerationRequestType.REJECT_USER, "archive-reject-user");

    private static CommandDispatcher.Response ArchiveModerationOp(
        HeadlessVMHost vmHost, AriesClient city, JsonObject args,
        ArchiveModerationRequestType type, string verb)
    {
        var (isAdmin, perms) = CheckAdmin(vmHost);
        if (!isAdmin)
        {
            return CommandDispatcher.Response.Fail($"{verb}: caller is not an admin (current_permissions={perms})");
        }

        // Archive moderation uses user_id, not avatar_id (ArchiveModerationHandler:40 — targets
        // are users, not Sims). The agent must know the distinction; we surface it explicitly
        // in the arg name.
        var targetUserId = (long?)args["target_user_id"] ?? 0L;
        if (targetUserId <= 0)
        {
            return CommandDispatcher.Response.Fail($"{verb} requires target_user_id (> 0; note: this is a user_id, not an avatar_id)");
        }

        var req = new ArchiveModerationRequest
        {
            Type = type,
            EntityId = (uint)targetUserId,
            Value = 0,
        };
        city.Write(req);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = verb,
            type = type.ToString(),
            target_user_id = targetUserId,
        });
    }

    // ---- admin-chat-command (lot socket; VMNetChatCmd with /cmd args) ----

    /// <summary>
    /// Whitelist of accepted /cmd tokens. Matches <c>VMNetChatCmd.Execute</c> switch cases. Any
    /// other token yields a clean refuse so agents don't lob arbitrary strings at the server.
    /// </summary>
    internal static readonly System.Collections.Generic.HashSet<string> AllowedAdminCommands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ban", "banip", "unban", "banlist",
            "builder", "admin", "roomie", "visitor",
            "close", "qtrday", "setjob",
            "trace", "reload",
            "time", "tickrate", "speed",
            "tuning", "fixall", "testcollision",
        };

    internal static CommandDispatcher.Response AdminChatCommand(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isAdmin, perms) = CheckAdmin(vmHost);
        if (!isAdmin)
        {
            return CommandDispatcher.Response.Fail($"admin-chat-command: caller is not an admin (current_permissions={perms})");
        }

        var command = (string)args["command"];
        if (string.IsNullOrWhiteSpace(command))
        {
            return CommandDispatcher.Response.Fail("admin-chat-command requires command (e.g. \"banlist\", \"qtrday\", \"speed\")");
        }
        command = command.Trim();
        if (command.StartsWith("/")) command = command.Substring(1); // tolerate leading slash
        if (!AllowedAdminCommands.Contains(command))
        {
            return CommandDispatcher.Response.Fail(
                $"admin-chat-command: unrecognised command '{command}'. Allowed: {string.Join(", ", AllowedAdminCommands)}");
        }

        var cmdArgs = (string)args["args"] ?? string.Empty;
        // VMNetChatCmd.Execute truncates Message at 200 chars. Build "/cmd args" and refuse if
        // the slash-form would overflow — better a clean fail than a silent truncation that
        // lops off the last arg.
        var message = cmdArgs.Length > 0 ? $"/{command} {cmdArgs}" : $"/{command}";
        if (message.Length > 200)
        {
            return CommandDispatcher.Response.Fail($"admin-chat-command: message too long ({message.Length} > 200 chars)");
        }

        var cmd = new VMNetChatCmd
        {
            Message = message,
            ChannelID = 0,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "admin-chat-command",
            command = command,
            args = cmdArgs,
            message_length = message.Length,
        });
    }
}
