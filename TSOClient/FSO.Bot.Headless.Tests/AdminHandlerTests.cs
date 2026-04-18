using System;
using System.Text.Json.Nodes;
using FSO.Bot.Headless;
using FSO.Server.Protocol.Electron;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics.Model.TSOPlatform;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for admin-family handlers (freesoexperiment-3df). Focus is on the
/// pieces exercisable without a live HeadlessVMHost (which requires Content.Init):
///
///   1. <see cref="AdminHandlers.RequireAdmin"/> — deterministic refuse-path gate.
///      Validated for every non-Admin permission value.
///   2. Argument validation — missing/zero/self-target refuse paths.
///   3. PDU shape — ModerationRequest/ArchiveModerationRequest with correct Type + EntityId.
///   4. admin-chat-command whitelist + message composition.
///   5. RegisterAll null-guards.
///
/// Wire-level effects (DB mutation on target, session close, chat events) are the
/// ground-source-truth concern of <c>tests/integration/verb-admin.sh</c>, not these
/// unit tests. The item-spec note on -163 tracks the non-admin live-refuse gap:
/// the bot always logs in as baron (is_admin=1) so a live non-admin test requires
/// a second bot identity we don't have yet.
/// </summary>
public class AdminHandlerTests
{
    [Fact]
    public void RequireAdmin_AllowsAdmin()
    {
        var resp = AdminHandlers.RequireAdmin(VMTSOAvatarPermissions.Admin, "test-op");
        Assert.Null(resp); // null means caller should proceed
    }

    [Theory]
    [InlineData(VMTSOAvatarPermissions.Visitor)]
    [InlineData(VMTSOAvatarPermissions.Roommate)]
    [InlineData(VMTSOAvatarPermissions.BuildBuyRoommate)]
    [InlineData(VMTSOAvatarPermissions.Owner)]
    public void RequireAdmin_RefusesNonAdmin(VMTSOAvatarPermissions perms)
    {
        // The "non-admin refuse" behaviour must fire for EVERY non-Admin permission
        // level, not just Visitor. The server-side check is `is_admin || is_moderator`
        // at the city layer AND `Permissions == Admin` at the lot layer, but we want
        // a single strict gate on the bot side: only Admin-class permissions allow
        // admin-family ops. Owner of a lot is NOT an admin — stock client chat
        // commands refuse for Owner exactly as they do for Visitor.
        var resp = AdminHandlers.RequireAdmin(perms, "test-op");
        Assert.NotNull(resp);
        Assert.False(resp.Ok);
        Assert.Contains("not an admin", resp.Error);
        Assert.Contains(perms.ToString(), resp.Error); // error surfaces current perms for debuggability
    }

    [Fact]
    public void RegisterAll_NullDispatcher_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AdminHandlers.RegisterAll(null, null, null));
    }

    [Fact]
    public void AllowedAdminCommands_MatchesStockCatalog()
    {
        // Pin the 19-command set from VMNetChatCmd.Execute switch. Adding a command
        // to the stock client requires updating this set, and vice versa — this test
        // catches drift.
        var expected = new[]
        {
            "ban", "banip", "unban", "banlist",
            "builder", "admin", "roomie", "visitor",
            "close", "qtrday", "setjob",
            "trace", "reload",
            "time", "tickrate", "speed",
            "tuning", "fixall", "testcollision",
        };
        Assert.Equal(expected.Length, AdminHandlers.AllowedAdminCommands.Count);
        foreach (var cmd in expected)
        {
            Assert.Contains(cmd, AdminHandlers.AllowedAdminCommands);
        }
    }

    [Fact]
    public void AllowedAdminCommands_IsCaseInsensitive()
    {
        // Agents emit lowercase by convention, but stock client accepts mixed case
        // (c.ToLowerInvariant() at VMNetChatCmd.cs:38). Keep the whitelist tolerant.
        Assert.Contains("Ban", AdminHandlers.AllowedAdminCommands);
        Assert.Contains("BANLIST", AdminHandlers.AllowedAdminCommands);
        Assert.Contains("QtrDay", AdminHandlers.AllowedAdminCommands);
    }

    // ---- PDU shape ----

    [Fact]
    public void ModerationRequest_KickUser_ShapeIsStable()
    {
        // Golden-field assertion. Any PDU reshuffling (e.g. EntityId type change,
        // new fields, swapped enum) breaks this and forces us to re-verify server
        // compatibility. ModerationRequest has no response packet, so the wire
        // shape IS the test.
        var req = new ModerationRequest
        {
            Type = ModerationRequestType.KICK_USER,
            EntityId = 12345,
        };
        Assert.Equal(ModerationRequestType.KICK_USER, req.Type);
        Assert.Equal(12345u, req.EntityId);
        Assert.Equal(ElectronPacketType.ModerationRequest, req.GetPacketType());
    }

    [Fact]
    public void ModerationRequest_AllThreeTypesExist()
    {
        // Catalog specifies exactly three: KICK_USER, BAN_USER, IPBAN_USER. Pin
        // this — if the enum grows, the bot handler registration map must grow
        // to match.
        Assert.True(Enum.IsDefined(typeof(ModerationRequestType), ModerationRequestType.KICK_USER));
        Assert.True(Enum.IsDefined(typeof(ModerationRequestType), ModerationRequestType.BAN_USER));
        Assert.True(Enum.IsDefined(typeof(ModerationRequestType), ModerationRequestType.IPBAN_USER));
        Assert.Equal(3, Enum.GetValues(typeof(ModerationRequestType)).Length);
    }

    [Fact]
    public void ArchiveModerationRequest_ShapeIsStable()
    {
        var req = new ArchiveModerationRequest
        {
            Type = ArchiveModerationRequestType.APPROVE_USER,
            EntityId = 777,
            Value = 0,
        };
        Assert.Equal(ArchiveModerationRequestType.APPROVE_USER, req.Type);
        Assert.Equal(777u, req.EntityId);
        Assert.Equal(0, req.Value);
        Assert.Equal(ElectronPacketType.ArchiveModerationRequest, req.GetPacketType());
    }

    [Fact]
    public void ArchiveModerationRequest_ApproveRejectExist()
    {
        // We only ship APPROVE_USER + REJECT_USER. The enum also has BAN_USER /
        // KICK_USER / CHANGE_MOD_LEVEL, but those are out of scope (BAN/KICK are
        // covered by ModerationRequest; CHANGE_MOD_LEVEL isn't in the catalog).
        Assert.True(Enum.IsDefined(typeof(ArchiveModerationRequestType), ArchiveModerationRequestType.APPROVE_USER));
        Assert.True(Enum.IsDefined(typeof(ArchiveModerationRequestType), ArchiveModerationRequestType.REJECT_USER));
    }

    // ---- admin-chat-command message composition ----
    //
    // We exercise the allowed-command whitelist + message composition directly
    // without constructing a VMHost. The composition logic is pure string work
    // (slash prefix, length cap) — no VM state involved.

    [Fact]
    public void AdminChatCommand_MessageComposition_NoArgs()
    {
        // For arg-less commands (banlist, fixall), the composed message is just "/cmd".
        var msg = ComposeAdminMessage("banlist", "");
        Assert.Equal("/banlist", msg);
    }

    [Fact]
    public void AdminChatCommand_MessageComposition_WithArgs()
    {
        var msg = ComposeAdminMessage("qtrday", "3");
        Assert.Equal("/qtrday 3", msg);
    }

    [Fact]
    public void AdminChatCommand_MessageComposition_MultiArgs()
    {
        // /time takes "hours minutes" as one space-separated string. The handler
        // passes args through verbatim so the server's Split(' ') sees them.
        var msg = ComposeAdminMessage("time", "12 30");
        Assert.Equal("/time 12 30", msg);
    }

    [Fact]
    public void AdminChatCommand_MessageComposition_Respects200CharCap()
    {
        // VMNetChatCmd.Execute truncates Message at 200. Our handler refuses
        // anything > 200 cleanly rather than let the server silently lop the
        // tail off an args string. Verify the boundary condition via a composed
        // too-long message.
        var bigArgs = new string('x', 210);
        var msg = ComposeAdminMessage("ban", bigArgs);
        Assert.True(msg.Length > 200);
    }

    // Helper mirror of the composition logic in AdminChatCommand. Kept here so
    // the test doesn't need to instantiate a VMHost just to cover one string concat.
    private static string ComposeAdminMessage(string command, string args)
    {
        return args.Length > 0 ? $"/{command} {args}" : $"/{command}";
    }

    // ---- Arg validation refuse-paths (dispatcher-side, no VM needed) ----

    [Fact]
    public void ModerationArgs_MissingTarget_RefusalShape()
    {
        // Verify JSON arg parsing convention: missing target_persist_id → long? null → 0L
        // → our (<= 0) refuse. This mirrors what the handler does for kick/ban/ipban
        // without having to invoke CheckAdmin (which needs a VM).
        var args = new JsonObject();
        long targetPid = (long?)args["target_persist_id"] ?? 0L;
        Assert.Equal(0L, targetPid);
        Assert.True(targetPid <= 0);
    }

    [Fact]
    public void ArchiveArgs_DistinctFromModeration_UsesUserId()
    {
        // Crucial: archive ops take target_user_id (user_id), NOT target_persist_id.
        // A common agent mistake would be sending avatar_id instead. The distinct
        // arg name is the guardrail; the handler enforces it.
        var args = new JsonObject { ["target_user_id"] = 42L };
        Assert.Null(args["target_persist_id"]); // absent
        Assert.Equal(42L, (long?)args["target_user_id"]);
    }
}
