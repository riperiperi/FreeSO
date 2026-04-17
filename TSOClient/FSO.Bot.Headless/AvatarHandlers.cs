/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.Common.DataService;
using FSO.Common.Serialization;
using FSO.Common.Serialization.Primitives;
using FSO.Server.Clients;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Concrete avatar-family command handlers (freesoexperiment-b88). Two ops:
///
/// <list type="bullet">
///   <item><b>change-outfit</b> — change the current avatar's outfit binding. Wire PDU is
///   <see cref="VMNetSetOutfitCmd"/> — the same command fired by the stock client's Dresser /
///   Rack / Trunk EODs. The EOD gate is purely client UX: the server-side
///   <c>VMNetCommandBodyAbstract.Verify</c> returns <c>true</c> unconditionally, and the
///   command simply writes the new outfit id into the VMAvatar's DefaultSuits / DynamicSuits
///   / Decoration slot. We expose the primitive directly so an agent can change clothes
///   without routing through an object interaction.</item>
///
///   <item><b>change-description</b> — set the avatar's public bio string. Syncs via the
///   city-server DataService, not a VMNet command. We construct a <see cref="cTSOTopicUpdateMessage"/>
///   with DotPath [AvatarProviderId, AvatarId, FieldId(Avatar_Description)] and
///   <see cref="DataServiceWrapperPDU"/> body, then write it on the city Aries socket.
///   <c>ServerAvatarProvider.DemandMutation</c> gates on <c>AvatarPermissions.WRITE</c>
///   (own avatar) and rejects strings > 500 chars;
///   <c>ServerAvatarProvider.PersistMutation</c> UPDATEs <c>fso_avatars.description</c>
///   synchronously inside the mutation callback, so the DB row is the ground-truth check.</item>
/// </list>
///
/// <b>Thread safety</b>: change-outfit queues a VMNet command via
/// <c>vmHost.Driver.SendCommand</c> which already holds its own lock
/// (freesoexperiment-a85 covers the outbound queue). change-description writes on the city
/// <c>AriesClient</c>, which delegates to the underlying Mina/Aries session's own write
/// synchronisation. Neither handler touches VM-thread-owned state without a helper.
///
/// <b>DataService bootstrap</b>: a single process-wide <see cref="DataService"/> instance is
/// initialised on first description write. The ctor walks <c>Content.DataDefinition</c> to
/// populate StructField maps (cheap, ~ms). We do not register providers — we only need
/// <c>GetFieldByName</c> + <c>SerializeUpdate(value, params uint[] dotPath)</c>.
/// </summary>
public static class AvatarHandlers
{
    // Singleton DataService used solely for field-id lookup and cTSOTopicUpdateMessage
    // assembly. Constructed lazily on first description write.
    private static DataService _dataService;
    private static readonly object _dataServiceLock = new();

    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost, AriesClient cityClient)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));
        if (cityClient == null) throw new ArgumentNullException(nameof(cityClient));

        dispatcher.Register("change-outfit",
            (args, ct) => Task.FromResult(ChangeOutfit(vmHost, args)));
        dispatcher.Register("change-description",
            (args, ct) => Task.FromResult(ChangeDescription(vmHost, cityClient, args)));
    }

    // ---- change-outfit ----

    /// <summary>
    /// change-outfit — swap the caller's outfit in the named scope. Args:
    ///   outfit_id (required, uint64; 0 is rejected as invalid).
    ///   scope     (optional, string; default "dynamic-daywear").
    ///             Accepted: dynamic-daywear, dynamic-sleepwear, dynamic-swimwear,
    ///             dynamic-costume, default-daywear, default-sleepwear, default-swimwear,
    ///             decoration-head, decoration-back, decoration-shoes, decoration-tail.
    ///
    /// Wire effect: <see cref="VMNetSetOutfitCmd"/>. Server stamps <c>ActorUID</c> with the
    /// caller's persist id in <c>VMServerDriver</c>; we set <c>cmd.UID</c> (the target avatar)
    /// to the caller's persist id as well — the stock EOD plugins pattern.
    /// Verification: query-self exposes the outfit fields under <c>outfits</c> so a subsequent
    /// query will return the new id.
    /// </summary>
    internal static CommandDispatcher.Response ChangeOutfit(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        // outfit_id is a 64-bit integer on the wire. JsonNode round-trips as long; cast to ulong.
        var outfitIdArg = (long?)args["outfit_id"];
        if (!outfitIdArg.HasValue)
            return CommandDispatcher.Response.Fail("change-outfit requires outfit_id (ulong)");
        if (outfitIdArg.Value == 0)
            return CommandDispatcher.Response.Fail("change-outfit: outfit_id must be non-zero");
        ulong outfitId = unchecked((ulong)outfitIdArg.Value);

        var scopeStr = ((string)args["scope"]) ?? "dynamic-daywear";
        if (!TryParseScope(scopeStr, out var scope))
            return CommandDispatcher.Response.Fail(
                $"change-outfit: unknown scope '{scopeStr}'. Accepted: dynamic-daywear, " +
                "dynamic-sleepwear, dynamic-swimwear, dynamic-costume, default-daywear, " +
                "default-sleepwear, default-swimwear, decoration-head, decoration-back, " +
                "decoration-shoes, decoration-tail");

        var cmd = new VMNetSetOutfitCmd
        {
            UID = caller.PersistID,
            Scope = scope,
            Outfit = outfitId,
        };
        vmHost.Driver.SendCommand(cmd);

        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            scope = scopeStr,
            outfit_id = (long)outfitId,
        });
    }

    private static bool TryParseScope(string s, out VMPersonSuits scope)
    {
        switch ((s ?? "").ToLowerInvariant())
        {
            case "dynamic-daywear":   scope = VMPersonSuits.DynamicDaywear;   return true;
            case "dynamic-sleepwear": scope = VMPersonSuits.DynamicSleepwear; return true;
            case "dynamic-swimwear":  scope = VMPersonSuits.DynamicSwimwear;  return true;
            case "dynamic-costume":   scope = VMPersonSuits.DynamicCostume;   return true;
            case "default-daywear":   scope = VMPersonSuits.DefaultDaywear;   return true;
            case "default-sleepwear": scope = VMPersonSuits.DefaultSleepwear; return true;
            case "default-swimwear":  scope = VMPersonSuits.DefaultSwimwear;  return true;
            case "decoration-head":   scope = VMPersonSuits.DecorationHead;   return true;
            case "decoration-back":   scope = VMPersonSuits.DecorationBack;   return true;
            case "decoration-shoes":  scope = VMPersonSuits.DecorationShoes;  return true;
            case "decoration-tail":   scope = VMPersonSuits.DecorationTail;   return true;
            default: scope = default; return false;
        }
    }

    // ---- change-description ----

    /// <summary>
    /// change-description — set the caller's public description / bio string. Args:
    ///   text (required, string). Empty string is rejected (keeps behaviour deterministic —
    ///   stock client refuses to save empty); > 500 chars rejected to avoid the server-side
    ///   <c>throw new Exception("Description too long!")</c> in
    ///   <c>ServerAvatarProvider.DemandMutation</c>.
    ///
    /// Wire effect: <see cref="DataServiceWrapperPDU"/> with a <see cref="cTSOTopicUpdateMessage"/>
    /// body. DotPath = [AvatarProviderId, AvatarId, FieldId(Avatar_Description)]. Writes on the
    /// <b>city</b> Aries socket (descriptions are city-server state, not lot VM state).
    /// <c>ServerAvatarProvider.PersistMutation</c> UPDATEs <c>fso_avatars.description</c>
    /// synchronously; the DB row is the ground-truth check.
    /// </summary>
    internal static CommandDispatcher.Response ChangeDescription(
        HeadlessVMHost vmHost, AriesClient cityClient, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        var text = (string)args["text"];
        if (text == null || text.Length == 0)
            return CommandDispatcher.Response.Fail("change-description requires non-empty text");
        if (text.Length > 500)
            return CommandDispatcher.Response.Fail(
                $"change-description: text length {text.Length} exceeds 500-char server cap");

        if (!cityClient.IsConnected)
            return CommandDispatcher.Response.Fail("change-description: city session not connected");

        // Bootstrap DataService if not yet initialised. This is cheap (map-build over the
        // loaded DataDefinition) and idempotent under the lock.
        DataService ds;
        try
        {
            ds = GetOrInitDataService();
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail(
                $"change-description: DataService init failed: {ex.GetType().Name}: {ex.Message}");
        }

        // Resolve the Avatar_Description field descriptor (carries parent struct id + field id).
        var field = ds.GetFieldByName(typeof(FSO.Common.DataService.Model.Avatar), "Avatar_Description");
        if (field == null)
            return CommandDispatcher.Response.Fail(
                "change-description: Avatar_Description field not found in DataDefinition");

        // DotPath: [ProviderID, AvatarId, FieldId]. ProviderID is the Avatar struct's parent id.
        uint providerId = field.ParentID;
        uint avatarId = vmHost.MyAvatarPersistId;
        uint fieldId = field.ID;

        cTSOTopicUpdateMessage update;
        try
        {
            update = ds.SerializeUpdate((object)text, providerId, avatarId, fieldId);
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail(
                $"change-description: serialize failed: {ex.GetType().Name}: {ex.Message}");
        }
        if (update == null)
            return CommandDispatcher.Response.Fail("change-description: serialize returned null");

        var pdu = new DataServiceWrapperPDU
        {
            SendingAvatarID = avatarId,
            RequestTypeID = 0,
            Body = update,
        };

        try
        {
            cityClient.Write(pdu);
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail(
                $"change-description: city write failed: {ex.GetType().Name}: {ex.Message}");
        }

        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            avatar_id = (long)avatarId,
            length = text.Length,
            text = text,
        });
    }

    private static DataService GetOrInitDataService()
    {
        var ds = _dataService;
        if (ds != null) return ds;
        lock (_dataServiceLock)
        {
            if (_dataService != null) return _dataService;
            var content = FSO.Content.Content.Get()
                ?? throw new InvalidOperationException(
                    "FSO.Content.Content not initialised; change-description requires Content.Init (SERVER mode)");
            _dataService = new DataService(new ModelSerializer(), content);
            return _dataService;
        }
    }
}
