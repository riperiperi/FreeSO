using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Bot.Headless;

/// <summary>
/// Queries verb family (freesoexperiment-e9f). Pull-on-demand local-VM introspection.
/// Every op here is fully synchronous — no outbound PDU, no server round-trip. The perception
/// emitter already pushes most of this at <c>FSO_PERCEPTION_HZ</c>; queries exist so an agent
/// can get a fresh snapshot mid-turn without waiting for the next perception tick.
///
/// <para>
/// <b>Thread safety:</b> every handler that touches VM-thread-owned state
/// (Entities, avatar fields, Thread.Queue, TSOState) routes through
/// <see cref="HeadlessVMHost.RunUnderTickLock{T}"/> — the tick thread mutates those structures
/// without external locking, so concurrent reads from the dispatcher race without the helper.
/// </para>
///
/// <para>
/// <b>Inventory deferred (freesoexperiment-1a9):</b> the <c>query-inventory</c> handler returns
/// an empty item list with <c>deferred=true</c> and a human-readable note. Inventory data lives
/// in <c>DataServiceWrapperPDU</c> pushes that <c>FSO.Server.Clients</c> does not currently
/// surface in-process. When -1a9 wires the feed in, this handler becomes the read point;
/// shape stays the same so agents don't break.
/// </para>
/// </summary>
public static class QueryHandlers
{
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost, string shardName)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));
        shardName ??= "Alphaville";

        dispatcher.Register("query-self",          (args, ct) => Task.FromResult(QuerySelf(vmHost, shardName)));
        dispatcher.Register("query-nearby",        (args, ct) => Task.FromResult(QueryNearby(vmHost, args)));
        dispatcher.Register("query-lot",           (args, ct) => Task.FromResult(QueryLot(vmHost, shardName)));
        dispatcher.Register("query-relationships", (args, ct) => Task.FromResult(QueryRelationships(vmHost)));
        dispatcher.Register("query-inventory",     (args, ct) => Task.FromResult(QueryInventory(vmHost)));
    }

    // ---- query-self ----

    /// <summary>
    /// query-self — full snapshot of the avatar's current state (motives, position, animation,
    /// queue, skills, balance). Returns the same data shape a perception tick would on these
    /// sub-fields, so agents can treat the response interchangeably with a tick for "what am I
    /// right now" reasoning. No rolling motive window (delta_per_minute) — those are
    /// perception-only because they require historical samples.
    /// </summary>
    internal static CommandDispatcher.Response QuerySelf(HeadlessVMHost vmHost, string shardName)
    {
        var payload = vmHost.RunUnderTickLock<object>(() =>
        {
            var me = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
            if (me == null) return null;

            var motives = new Dictionary<string, object>();
            foreach (var (motive, key) in EmittedMotives)
            {
                motives[key] = new { value = (int)me.GetMotiveData(motive) };
            }

            var skills = new Dictionary<string, int>();
            foreach (var (var, key) in EmittedSkills)
            {
                skills[key] = me.GetPersonData(var);
            }

            var queue = new List<object>();
            if (me.Thread?.Queue != null)
            {
                for (int i = 0; i < me.Thread.Queue.Count; i++)
                {
                    var q = me.Thread.Queue[i];
                    queue.Add(new
                    {
                        interaction_id = (int)q.UID,
                        name = q.Name ?? string.Empty,
                        target_object_id = (int)(q.Callee?.ObjectID ?? 0),
                        status = i == 0 ? "running" : "queued",
                    });
                }
            }

            long balance = me.TSOState is VMTSOEntityState tso ? (long)tso.Budget.Value : 0L;

            return new
            {
                persist_id = (long)me.PersistID,
                name = me.Name ?? string.Empty,
                shard = shardName,
                position = new
                {
                    x = me.Position.x / 16.0,
                    y = me.Position.y / 16.0,
                    level = (int)me.Position.Level,
                    direction = PerceptionProjector.DirectionToCompass(me.RadianDirection),
                },
                animation_raw = me.CurrentAnimationState?.Anim?.Name ?? string.Empty,
                action_queue = queue,
                motives = motives,
                skills = skills,
                balance = balance,
            };
        });

        if (payload == null) return CommandDispatcher.Response.Fail("no live avatar");
        return CommandDispatcher.Response.Success(payload);
    }

    // ---- query-nearby ----

    /// <summary>
    /// query-nearby — the same nearby_objects + nearby_sims slice perception emits, on-demand.
    /// Optional <c>radius_tiles</c> narrows or widens the default (20 tiles, same as perception).
    /// Radius is converted to 1/16-tile units (engine native) for the distance check.
    /// </summary>
    internal static CommandDispatcher.Response QueryNearby(HeadlessVMHost vmHost, JsonObject args)
    {
        double radiusTiles = 20.0;
        if (args["radius_tiles"] is JsonNode rn)
        {
            try { radiusTiles = rn.GetValue<double>(); }
            catch
            {
                try { radiusTiles = rn.GetValue<long>(); } catch { }
            }
            if (radiusTiles <= 0 || double.IsNaN(radiusTiles) || double.IsInfinity(radiusTiles))
                return CommandDispatcher.Response.Fail($"radius_tiles must be positive, got {radiusTiles}");
        }
        int radiusUnits = (int)Math.Round(radiusTiles * 16.0);

        var payload = vmHost.RunUnderTickLock<object>(() =>
        {
            var me = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
            if (me == null) return null;

            var nearbyObjects = new List<object>();
            var nearbySims = new List<object>();

            foreach (var e in vmHost.VM.Entities)
            {
                if (e == me) continue;
                if (e.Position == LotTilePos.OUT_OF_WORLD) continue;
                int dist = LotTilePos.Distance(me.Position, e.Position);
                if (dist > radiusUnits) continue;

                if (e is VMAvatar a)
                {
                    nearbySims.Add(new
                    {
                        persist_id = (long)a.PersistID,
                        name = a.Name ?? a.ToString(),
                        position = new
                        {
                            x = a.Position.x / 16.0,
                            y = a.Position.y / 16.0,
                            level = (int)a.Position.Level,
                        },
                        distance_tiles = Math.Round(dist / 16.0, 2),
                        current_action = a.Thread?.Queue?.Count > 0 ? (a.Thread.Queue[0].Name ?? string.Empty) : string.Empty,
                    });
                }
                else
                {
                    // Skip multi-tile non-root entries so we don't emit one entry per tile.
                    if (e.MultitileGroup != null && e.MultitileGroup.Objects != null &&
                        e.MultitileGroup.Objects.Count > 1 && e.MultitileGroup.BaseObject != e)
                        continue;

                    nearbyObjects.Add(new
                    {
                        object_id = (int)e.ObjectID,
                        name = e.Name ?? e.ToString(),
                        position = new
                        {
                            x = e.Position.x / 16.0,
                            y = e.Position.y / 16.0,
                            level = (int)e.Position.Level,
                        },
                        distance_tiles = Math.Round(dist / 16.0, 2),
                    });
                }
            }

            return new
            {
                radius_tiles = radiusTiles,
                nearby_objects = nearbyObjects,
                nearby_sims = nearbySims,
            };
        });

        if (payload == null) return CommandDispatcher.Response.Fail("no live avatar");
        return CommandDispatcher.Response.Success(payload);
    }

    // ---- query-lot ----

    /// <summary>
    /// query-lot — metadata about the current lot: id, name, shard, roommate set, build-roommate
    /// set, owner_is_me, owner_id, category, size, neighborhood_id. Pulled from
    /// <c>VM.TSOState</c> (VMTSOLotState). Pure observation — no PDU.
    /// </summary>
    internal static CommandDispatcher.Response QueryLot(HeadlessVMHost vmHost, string shardName)
    {
        var payload = vmHost.RunUnderTickLock<object>(() =>
        {
            var vm = vmHost.VM;
            if (vm == null) return null;
            var lotState = vm.TSOState as VMTSOLotState;
            // Category byte uses TSO's community-category enumeration. Name it for convenience;
            // agents typically reason on the id. Map per PropertyCategoryFlags in FSO.Common.
            // Keep raw byte as primary; add a string hint.
            byte category = lotState?.PropertyCategory ?? 0;
            int sizeRaw = lotState?.Size ?? 0;
            int sizeTiles = sizeRaw & 0xFF;           // packed: size | (floors << 8) | (dir << 16)
            int floors = (sizeRaw >> 8) & 0xFF;

            return new
            {
                lot_id = (long)(lotState?.LotID ?? 0u),
                name = vm.LotName ?? lotState?.Name ?? "unknown",
                shard = shardName,
                // owner_is_me: strictly OwnerID == me (freesoexperiment-9c5). Previously
                // computed as Roommates.Contains(me), which returns true for any roommate.
                // Property-family gates rely on a true owner signal so the handler refuses
                // non-owners deterministically.
                owner_is_me = lotState != null && lotState.OwnerID == vmHost.MyAvatarPersistId,
                owner_id = (long)(lotState?.OwnerID ?? 0u),
                is_roommate = lotState != null && lotState.Roommates.Contains(vmHost.MyAvatarPersistId),
                roommates = lotState?.Roommates?.Select(u => (long)u).ToArray() ?? Array.Empty<long>(),
                build_roommates = lotState?.BuildRoommates?.Select(u => (long)u).ToArray() ?? Array.Empty<long>(),
                category_id = (int)category,
                category = CategoryName(category),
                size_tiles = sizeTiles,
                floors = floors,
                neighborhood_id = (long)(lotState?.NhoodID ?? 0u),
                other_avatars = vm.Entities.OfType<VMAvatar>().Count(a => a.PersistID != vmHost.MyAvatarPersistId),
            };
        });

        if (payload == null) return CommandDispatcher.Response.Fail("no live VM");
        return CommandDispatcher.Response.Success(payload);
    }

    // ---- query-relationships ----

    /// <summary>
    /// query-relationships — dump of this avatar's MeToObject relationship map keyed by
    /// other-sim persist_id. Each entry exposes friendly + daily values (RelVar layout). Agents
    /// who need the full rel-var vector can cross-reference verb-catalog.md row notes.
    /// </summary>
    internal static CommandDispatcher.Response QueryRelationships(HeadlessVMHost vmHost)
    {
        var payload = vmHost.RunUnderTickLock<object>(() =>
        {
            var me = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
            if (me == null) return null;

            var list = new List<object>();
            if (me.MeToObject != null)
            {
                foreach (var kv in me.MeToObject)
                {
                    var other = vmHost.VM.GetObjectById((short)kv.Key) as VMAvatar;
                    if (other == null) continue;
                    int friendly = kv.Value.Count > 0 ? kv.Value[0] : 0;
                    int daily = kv.Value.Count > 1 ? kv.Value[1] : 0;
                    list.Add(new
                    {
                        with_persist_id = (long)other.PersistID,
                        with_name = other.Name ?? string.Empty,
                        friendly = friendly,
                        daily = daily,
                    });
                }
            }

            return new { relationships = list };
        });

        if (payload == null) return CommandDispatcher.Response.Fail("no live avatar");
        return CommandDispatcher.Response.Success(payload);
    }

    // ---- query-inventory ----

    /// <summary>
    /// query-inventory — personal inventory list. Currently returns <c>items=[]</c> with
    /// <c>deferred=true</c> and a human-readable note identifying the blocking item
    /// (freesoexperiment-1a9). DataService wiring is a prerequisite for non-empty output;
    /// until that lands, the bot has no visibility into inventory contents. Shape is stable
    /// so agents can poll this now and light up automatically when -1a9 closes.
    /// </summary>
    internal static CommandDispatcher.Response QueryInventory(HeadlessVMHost vmHost)
    {
        // No VM read required — the DataService inventory feed is unwired. Still check that
        // we have an avatar so the response correlates with session state (and so a future
        // implementation that reads from a VM-local cache has the right guardrail).
        var me = vmHost.RunUnderTickLock(() => vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId));
        if (me == null) return CommandDispatcher.Response.Fail("no live avatar");

        return CommandDispatcher.Response.Success(new
        {
            items = Array.Empty<object>(),
            deferred = true,
            deferred_reason = "DataService inventory feed not wired into FSO.Server.Clients (freesoexperiment-1a9)",
        });
    }

    // ---- shared tables ----

    private static readonly (VMMotive motive, string key)[] EmittedMotives = new[]
    {
        (VMMotive.Hunger,  "hunger"),
        (VMMotive.Comfort, "comfort"),
        (VMMotive.Energy,  "energy"),
        (VMMotive.Hygiene, "hygiene"),
        (VMMotive.Bladder, "bladder"),
        (VMMotive.Social,  "social"),
        (VMMotive.Fun,     "fun"),
        (VMMotive.Mood,    "mood"),
    };

    private static readonly (VMPersonDataVariable var, string key)[] EmittedSkills = new[]
    {
        (VMPersonDataVariable.CookingSkill,    "cooking"),
        (VMPersonDataVariable.CharismaSkill,   "charisma"),
        (VMPersonDataVariable.MechanicalSkill, "mechanical"),
        (VMPersonDataVariable.CreativitySkill, "creativity"),
        (VMPersonDataVariable.BodySkill,       "body"),
        (VMPersonDataVariable.LogicSkill,      "logic"),
    };

    // Category name map — TSO community property categories. Taken from
    // FSO.Common/LotCategory.cs (0=None, 1=Residential, 2=Money, ...). Keep in sync if upstream
    // renames any; name is a convenience only — id is the stable wire field.
    private static string CategoryName(byte c) => c switch
    {
        0 => "none",
        1 => "residential",
        2 => "money",
        3 => "services",
        4 => "welcome",
        5 => "games",
        6 => "entertainment",
        7 => "romance",
        8 => "shopping",
        9 => "skills",
        10 => "community",
        11 => "offbeat",
        _ => $"unknown({c})",
    };
}
