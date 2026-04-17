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
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Serialization.Primitives;
using FSO.Files.Formats.tsodata;
using FSO.Server.Clients;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Bot.Headless;

/// <summary>
/// Property-family command handlers (freesoexperiment-9c5). Verbs that mutate lot ownership /
/// roommate set / visitor admission. Unlike the lot-VM verbs (speak, walk-to, interact-with),
/// these emit packets on the <b>city</b> Aries socket — roommate management lives in the city
/// server (<c>ChangeRoommateHandler</c>) and admit-mode lives on the city DataService's
/// <c>ServerLotProvider</c>, not inside the running lot VM.
///
/// <para>
/// <b>Ops implemented:</b>
/// <list type="bullet">
///   <item><b>add-roommate</b> — <c>ChangeRoommateRequest{Type=INVITE,AvatarId,LotLocation}</c>.
///   Creates a pending invite; target must <c>accept-roommate</c> to materialise.</item>
///   <item><b>evict-roommate</b> — <c>ChangeRoommateRequest{Type=KICK,AvatarId,LotLocation}</c>.
///   Server-side <c>TryKick</c> removes the target's <c>fso_roommates</c> row and evicts them
///   from the lot if on-line.</item>
///   <item><b>lock-lot</b> — DataService sync of <c>Lot.Lot_LotAdmitInfo.LotAdmitInfo_AdmitMode</c>
///   to <c>3</c> (ban-all). Non-roommates are rejected at the city allocation handler.</item>
///   <item><b>unlock-lot</b> — DataService sync of <c>AdmitMode</c> to <c>0</c> (public, default).</item>
/// </list>
/// </para>
///
/// <para>
/// <b>pay-bills DROPPED (not stubbed)</b>. Per <c>docs/design/verb-catalog.md:127</c>: "Bills are
/// automatic on quarter-day; no player action PDU exists." The UI's <c>BillsButton</c> is even
/// hardcoded <c>Disabled = true</c> (<c>UIHouseMode.cs:76</c>). Dropping per item-spec rule:
/// "If catalog has no wire PDU for an op, DROP it with a note. Don't stub."
/// </para>
///
/// <para>
/// <b>Owner gating enforced here</b>, not just server-side. Per wave-2b veracity lesson on
/// admin-gate deterministic refuse paths (SocialHandlers.Speak rejects leading '/' before the
/// PDU goes out): every op checks <c>vm.TSOState.OwnerID == vmHost.MyAvatarPersistId</c> and
/// returns <c>ok=false, error="caller is not lot owner"</c> if not. The server will also refuse
/// (<c>YOU_ARE_NOT_OWNER</c>, <c>DemandAvatar(Lot_LeaderID)</c>) but we want a deterministic
/// fast-fail the agent can observe without waiting for a server response.
/// </para>
///
/// <para>
/// <b>Thread safety</b>: every owner check reads <c>vm.TSOState</c> fields that the tick thread
/// mutates, so ownership probes route through <see cref="HeadlessVMHost.RunUnderTickLock{T}"/>.
/// Packet writes on the city AriesClient are serialised by Mina internally; no extra locking.
/// </para>
/// </summary>
public static class PropertyHandlers
{
    /// <summary>
    /// Parallel to <see cref="SocialHandlers.RegisterAll"/>. Takes the city socket (for
    /// ChangeRoommateRequest + DataServiceWrapperPDU) and the vmHost (for owner checks +
    /// lot-location + DataService serialization context).
    /// </summary>
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost, AriesClient cityAries, uint lotLocationPacked)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));
        if (cityAries == null) throw new ArgumentNullException(nameof(cityAries));

        var ctx = new PropertyContext(vmHost, cityAries, lotLocationPacked);

        dispatcher.Register("add-roommate",   (args, ct) => Task.FromResult(AddRoommate(ctx, args)));
        dispatcher.Register("evict-roommate", (args, ct) => Task.FromResult(EvictRoommate(ctx, args)));
        dispatcher.Register("lock-lot",       (args, ct) => Task.FromResult(LockLot(ctx, args)));
        dispatcher.Register("unlock-lot",     (args, ct) => Task.FromResult(UnlockLot(ctx, args)));
    }

    internal class PropertyContext
    {
        public HeadlessVMHost VmHost { get; }
        public AriesClient City { get; }
        public uint LotLocationPacked { get; }
        private uint _messageId = 1;
        private readonly object _midLock = new();

        public PropertyContext(HeadlessVMHost vmHost, AriesClient city, uint lotLocationPacked)
        {
            VmHost = vmHost;
            City = city;
            LotLocationPacked = lotLocationPacked;
        }

        public uint NextMessageId()
        {
            lock (_midLock) return _messageId++;
        }
    }

    /// <summary>
    /// Returns (true, ownerId) if caller is the current lot owner. Runs under the tick lock so
    /// TSOState reads don't race with the tick thread's mutation.
    /// </summary>
    private static (bool isOwner, uint ownerId) CheckOwner(HeadlessVMHost vmHost)
    {
        return vmHost.RunUnderTickLock(() =>
        {
            var vm = vmHost.VM;
            if (vm == null) return (false, 0u);
            var lotState = vm.TSOState as VMTSOLotState;
            if (lotState == null) return (false, 0u);
            var ownerId = lotState.OwnerID;
            return (ownerId == vmHost.MyAvatarPersistId && ownerId != 0, ownerId);
        });
    }

    // ---- add-roommate ----

    internal static CommandDispatcher.Response AddRoommate(PropertyContext ctx, JsonObject args)
    {
        var (isOwner, ownerId) = CheckOwner(ctx.VmHost);
        if (!isOwner)
        {
            return CommandDispatcher.Response.Fail($"caller is not lot owner (owner_id={ownerId}, me={ctx.VmHost.MyAvatarPersistId})");
        }

        var targetPid = (long?)args["target_persist_id"] ?? 0L;
        if (targetPid <= 0)
        {
            return CommandDispatcher.Response.Fail("add-roommate requires target_persist_id (> 0)");
        }
        if ((uint)targetPid == ctx.VmHost.MyAvatarPersistId)
        {
            return CommandDispatcher.Response.Fail("add-roommate: cannot invite yourself");
        }

        var req = new ChangeRoommateRequest
        {
            Type = ChangeRoommateType.INVITE,
            AvatarId = (uint)targetPid,
            LotLocation = ctx.LotLocationPacked,
        };
        ctx.City.Write(req);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "add-roommate",
            target_persist_id = targetPid,
            lot_location = ctx.LotLocationPacked,
        });
    }

    // ---- evict-roommate ----

    internal static CommandDispatcher.Response EvictRoommate(PropertyContext ctx, JsonObject args)
    {
        var (isOwner, ownerId) = CheckOwner(ctx.VmHost);
        if (!isOwner)
        {
            return CommandDispatcher.Response.Fail($"caller is not lot owner (owner_id={ownerId}, me={ctx.VmHost.MyAvatarPersistId})");
        }

        var targetPid = (long?)args["target_persist_id"] ?? 0L;
        if (targetPid <= 0)
        {
            return CommandDispatcher.Response.Fail("evict-roommate requires target_persist_id (> 0)");
        }
        if ((uint)targetPid == ctx.VmHost.MyAvatarPersistId)
        {
            return CommandDispatcher.Response.Fail("evict-roommate: cannot evict the owner (yourself)");
        }

        // Sanity check: target should be a current roommate in the VM's view. If they aren't,
        // the server will return YOU_ARE_NOT_ROOMMATE; warn early but still send the PDU (the
        // VM view may lag the city DB by a tick).
        bool targetIsRoommate = ctx.VmHost.RunUnderTickLock(() =>
        {
            var lotState = ctx.VmHost.VM?.TSOState as VMTSOLotState;
            return lotState != null && lotState.Roommates.Contains((uint)targetPid);
        });

        var req = new ChangeRoommateRequest
        {
            Type = ChangeRoommateType.KICK,
            AvatarId = (uint)targetPid,
            LotLocation = ctx.LotLocationPacked,
        };
        ctx.City.Write(req);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "evict-roommate",
            target_persist_id = targetPid,
            target_was_roommate_in_vm = targetIsRoommate,
            lot_location = ctx.LotLocationPacked,
        });
    }

    // ---- lock-lot / unlock-lot ----

    internal static CommandDispatcher.Response LockLot(PropertyContext ctx, JsonObject args)
    {
        return SetAdmitMode(ctx, 3, "lock-lot");
    }

    internal static CommandDispatcher.Response UnlockLot(PropertyContext ctx, JsonObject args)
    {
        return SetAdmitMode(ctx, 0, "unlock-lot");
    }

    /// <summary>
    /// Emit a DataServiceWrapperPDU on the city socket setting
    /// <c>Lot.Lot_LotAdmitInfo.LotAdmitInfo_AdmitMode</c> on the current lot. Requires owner.
    /// </summary>
    private static CommandDispatcher.Response SetAdmitMode(PropertyContext ctx, byte mode, string verb)
    {
        var (isOwner, ownerId) = CheckOwner(ctx.VmHost);
        if (!isOwner)
        {
            return CommandDispatcher.Response.Fail($"caller is not lot owner (owner_id={ownerId}, me={ctx.VmHost.MyAvatarPersistId})");
        }

        // Compose the DotPath for Lot[id].Lot_LotAdmitInfo.LotAdmitInfo_AdmitMode.
        // Path format used by the server's DataServiceWrapperHandler is
        // [structType, key, field_id_on_Lot, field_id_on_LotAdmitInfo]. Struct IDs and field IDs
        // come from the parsed tsodata definition, not from any compile-time enum. We look them
        // up once lazily at call time via FSO.Content.Content.Get().DataDefinition.
        uint[] dotPath;
        try
        {
            dotPath = ComposeLotAdmitModeDotPath(ctx.LotLocationPacked);
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"{verb}: failed to compose dotpath: {ex.GetType().Name}: {ex.Message}");
        }

        var update = new cTSOTopicUpdateMessage
        {
            DotPath = dotPath,
            Value = mode,
        };

        var pdu = new DataServiceWrapperPDU
        {
            RequestTypeID = 0,
            SendingAvatarID = ctx.NextMessageId(),
            Body = update,
        };

        ctx.City.Write(pdu);

        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = verb,
            admit_mode = (int)mode,
            lot_location = ctx.LotLocationPacked,
            dot_path = dotPath.Select(u => (long)u).ToArray(),
        });
    }

    private static uint _cachedLotStructId;
    private static uint _cachedLotLotAdmitInfoFieldId;
    private static uint _cachedLotAdmitInfoAdmitModeFieldId;
    private static bool _cachedDotPathIdsResolved;
    private static readonly object _cacheLock = new();

    /// <summary>
    /// Resolve the DataService dotpath ids once, cache them. The TSO data definition is parsed
    /// during <c>FSO.Content.Content.Init</c> (server mode), which Program.cs runs before any
    /// IPC op can arrive, so the cache is safe to lazily populate on first use.
    /// </summary>
    private static uint[] ComposeLotAdmitModeDotPath(uint lotLocationPacked)
    {
        if (!_cachedDotPathIdsResolved)
        {
            lock (_cacheLock)
            {
                if (!_cachedDotPathIdsResolved)
                {
                    var dd = FSO.Content.Content.Get().DataDefinition;
                    var lotStruct = dd.Structs.FirstOrDefault(s => s.Name == "Lot")
                        ?? throw new InvalidOperationException("TSO DataDefinition missing struct Lot");
                    var lotAdmitInfoStruct = dd.Structs.FirstOrDefault(s => s.Name == "LotAdmitInfo")
                        ?? throw new InvalidOperationException("TSO DataDefinition missing struct LotAdmitInfo");
                    var lotAdmitInfoField = lotStruct.Fields.FirstOrDefault(f => f.Name == "Lot_LotAdmitInfo")
                        ?? throw new InvalidOperationException("TSO DataDefinition: Lot.Lot_LotAdmitInfo not found");
                    var admitModeField = lotAdmitInfoStruct.Fields.FirstOrDefault(f => f.Name == "LotAdmitInfo_AdmitMode")
                        ?? throw new InvalidOperationException("TSO DataDefinition: LotAdmitInfo.LotAdmitInfo_AdmitMode not found");

                    _cachedLotStructId = lotStruct.ID;
                    _cachedLotLotAdmitInfoFieldId = lotAdmitInfoField.ID;
                    _cachedLotAdmitInfoAdmitModeFieldId = admitModeField.ID;
                    _cachedDotPathIdsResolved = true;
                }
            }
        }

        return new uint[]
        {
            _cachedLotStructId,
            lotLocationPacked,
            _cachedLotLotAdmitInfoFieldId,
            _cachedLotAdmitInfoAdmitModeFieldId,
        };
    }
}
