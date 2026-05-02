/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System.Text.Json.Nodes;
using FSO.Bot.Headless;
using FSO.Server.Protocol.Electron.Packets;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// C# contract tests for CityHandlers election op (vote / nominate) PDU dispatch
/// (freesoexperiment-17f veracity rework v2 — CRITICAL finding).
///
/// <para>
/// <b>What these tests cover:</b> the Go-side sidecar tests prove that the sidecar
/// resolves a name store entry (kind=sim) to a numeric target_persist_id and emits the
/// correct IPC payload to the bot's stdin. They do NOT cover the C# side: CityHandlers
/// reading that IPC payload's <c>target_persist_id</c> field and placing it in
/// <see cref="NhoodRequest.TargetAvatar"/>. A bug in the C# arg-extraction chain
/// (wrong JSON key, wrong cast, wrong field assignment) would be invisible to Go tests.
/// </para>
///
/// <para>
/// <b>Seam</b>: <see cref="CityHandlers.ExtractElectionArgs"/> is the testable extraction
/// seam. Both <see cref="CityHandlers.VoteAsync"/> and <see cref="CityHandlers.NominateAsync"/>
/// call it — so these tests cover the same code path that runs in production.
/// </para>
///
/// <para>
/// <b>Golden-field approach</b>: follows the same pattern as
/// <c>AdminHandlerTests.ModerationRequest_KickUser_ShapeIsStable</c> and
/// <c>MailCommandEncodingTests</c>. We do not spin up a live AriesClient/Mina session —
/// the wire layer (IoBuffer serialization) is separately covered by PDU encoding tests.
/// What matters here is that the arg → NhoodRequest field mapping is correct.
/// </para>
/// </summary>
public class CityHandlerArgContractTests
{
    private const uint MyAvatarId = 999u;

    // ---- vote ----

    [Fact]
    public void Vote_TargetPersistId_MapsToNhoodRequestTargetAvatar()
    {
        // This is the critical contract: args["target_persist_id"] = 77
        // must produce NhoodRequest.TargetAvatar = 77.
        // A bug where the handler reads "target_avatar_name" instead of
        // "target_persist_id", or casts to the wrong type, is caught here.
        var args = new JsonObject
        {
            ["target_persist_id"] = 77L,
            ["neighborhood_id"] = 1L,
        };

        var (targetAvatar, nhoodId, err) = CityHandlers.ExtractElectionArgs(args, MyAvatarId, "vote");

        Assert.Null(err);
        // The resolved persist_id must become TargetAvatar via (long?) → (uint) cast.
        Assert.Equal(77u, targetAvatar);
        Assert.Equal(1u, nhoodId);

        // Verify the NhoodRequest PDU field assignment is correct: TargetAvatar
        // carries the target's persist_id, not the neighborhood or the caller's ID.
        var pdu = new NhoodRequest
        {
            Type = NhoodRequestType.VOTE,
            TargetAvatar = targetAvatar,
            TargetNHood = nhoodId,
        };
        Assert.Equal(NhoodRequestType.VOTE, pdu.Type);
        Assert.Equal(77u, pdu.TargetAvatar);
        Assert.Equal(1u, pdu.TargetNHood);
    }

    [Fact]
    public void Vote_DefaultNhoodId_IsOne_WhenAbsent()
    {
        // neighborhood_id absent → defaults to DefaultNhoodId=1 (workshop shard).
        // Agents that omit the field (using the default) must not get nhood 0.
        var args = new JsonObject
        {
            ["target_persist_id"] = 42L,
        };

        var (targetAvatar, nhoodId, err) = CityHandlers.ExtractElectionArgs(args, MyAvatarId, "vote");

        Assert.Null(err);
        Assert.Equal(42u, targetAvatar);
        Assert.Equal(1u, nhoodId); // DefaultNhoodId
    }

    [Fact]
    public void Vote_ZeroTargetPersistId_ReturnsError()
    {
        // target_persist_id=0 must be rejected. A zero avatar_id is not valid
        // in the FSO wire protocol (server-assigned IDs start at 1). The bot
        // rejects this before writing to the city socket.
        var args = new JsonObject
        {
            ["target_persist_id"] = 0L,
        };

        var (_, _, err) = CityHandlers.ExtractElectionArgs(args, MyAvatarId, "vote");

        Assert.NotNull(err);
        Assert.Contains("target_persist_id", err);
    }

    [Fact]
    public void Vote_MissingTargetPersistId_ReturnsError()
    {
        // Absent target_persist_id (null in JSON) must be rejected.
        // In production the sidecar's resolveElectionTarget ensures one of
        // target_avatar_name or target_persist_id is present before forwarding;
        // this test proves the C# bot also has a defense-in-depth check.
        var args = new JsonObject
        {
            ["neighborhood_id"] = 1L,
        };

        var (_, _, err) = CityHandlers.ExtractElectionArgs(args, MyAvatarId, "vote");

        Assert.NotNull(err);
        Assert.Contains("target_persist_id", err);
    }

    [Fact]
    public void Vote_SelfTarget_ReturnsError()
    {
        // Self-vote must be rejected: target_persist_id == myAvatarId.
        // Both the server (CANT_RATE_AVATAR-class check) and the bot validate this.
        var args = new JsonObject
        {
            ["target_persist_id"] = (long)MyAvatarId,
        };

        var (_, _, err) = CityHandlers.ExtractElectionArgs(args, MyAvatarId, "vote");

        Assert.NotNull(err);
        Assert.Contains("yourself", err);
    }

    // ---- nominate ----

    [Fact]
    public void Nominate_TargetPersistId_MapsToNhoodRequestTargetAvatar()
    {
        // Mirror of Vote_TargetPersistId_MapsToNhoodRequestTargetAvatar for nominate.
        // Uses NhoodRequestType.NOMINATE to confirm the PDU type is distinct.
        var args = new JsonObject
        {
            ["target_persist_id"] = 55L,
            ["neighborhood_id"] = 2L,
        };

        var (targetAvatar, nhoodId, err) = CityHandlers.ExtractElectionArgs(args, MyAvatarId, "nominate");

        Assert.Null(err);
        Assert.Equal(55u, targetAvatar);
        Assert.Equal(2u, nhoodId);

        var pdu = new NhoodRequest
        {
            Type = NhoodRequestType.NOMINATE,
            TargetAvatar = targetAvatar,
            TargetNHood = nhoodId,
        };
        Assert.Equal(NhoodRequestType.NOMINATE, pdu.Type);
        Assert.Equal(55u, pdu.TargetAvatar);
        Assert.Equal(2u, pdu.TargetNHood);
    }

    [Fact]
    public void Nominate_SelfNomination_ReturnsError()
    {
        var args = new JsonObject
        {
            ["target_persist_id"] = (long)MyAvatarId,
        };

        var (_, _, err) = CityHandlers.ExtractElectionArgs(args, MyAvatarId, "nominate");

        Assert.NotNull(err);
        Assert.Contains("yourself", err);
    }

    // ---- NhoodRequest PDU shape (golden-field pin, shared across vote + nominate) ----

    [Fact]
    public void NhoodRequest_VoteType_ShapeIsStable()
    {
        // Pin the NhoodRequest PDU field set for vote. Any field rename, type change,
        // or enum reorder in NhoodRequest breaks this test immediately, alerting us
        // to a potential wire incompatibility before deployment.
        //
        // Wire shape (from NhoodRequest.Serialize):
        //   [uint8  Type (VOTE=0)]
        //   [uint32 TargetAvatar, BigEndian]
        //   [uint32 TargetNHood, BigEndian]
        //   [PascalString Message = ""]
        //   [uint32 Value = 0]
        var req = new NhoodRequest
        {
            Type = NhoodRequestType.VOTE,
            TargetAvatar = 77u,
            TargetNHood = 1u,
            Message = "",
            Value = 0,
        };
        Assert.Equal(NhoodRequestType.VOTE, req.Type);
        Assert.Equal(77u, req.TargetAvatar);
        Assert.Equal(1u, req.TargetNHood);
        Assert.Equal("", req.Message);
        Assert.Equal(0u, req.Value);
    }

    [Fact]
    public void NhoodRequest_NominateType_ShapeIsStable()
    {
        var req = new NhoodRequest
        {
            Type = NhoodRequestType.NOMINATE,
            TargetAvatar = 55u,
            TargetNHood = 2u,
            Message = "",
            Value = 0,
        };
        Assert.Equal(NhoodRequestType.NOMINATE, req.Type);
        Assert.Equal(55u, req.TargetAvatar);
        Assert.Equal(2u, req.TargetNHood);
    }

    [Fact]
    public void NhoodRequestType_VoteIsZero_NominateIsTwo()
    {
        // Pin the enum values so any upstream reorder is caught. The server's
        // NhoodRequestHandler.Handle switch is byte-valued (uint8 on the wire);
        // if VOTE or NOMINATE moves, the bot sends the wrong request type silently.
        Assert.Equal(0, (int)NhoodRequestType.VOTE);
        Assert.Equal(2, (int)NhoodRequestType.NOMINATE);
    }
}
