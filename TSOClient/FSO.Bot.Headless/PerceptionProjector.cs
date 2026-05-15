using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.LotView.Model;

namespace FSO.Bot.Headless;

/// <summary>
/// Projects the local FSO VM state for one target avatar into the perception schema defined in
/// <c>docs/design/embodiment-bridge.md §5</c>. One call produces one "perception" tick object.
///
/// Source data is read directly from live VMAvatar / VMEntity objects after the client VM driver
/// has been ticked (see <see cref="HeadlessVMHost"/>). No server round-trip, no stubs.
///
/// Owned state:
///   - Motive rolling window (per motive, last 60s of samples) → delta_per_minute + trend.
///   - recent_events[] rolling buffer (cap 10) — fed by dialog fires and interaction queue transitions.
///   - last-tick queue snapshot for interaction_start / interaction_end synthesis.
///
/// Animation decoder + interaction-hints are resolved lazily from bundled Content files:
///   - Content/animation_translation.json (list of {pattern, phrase})
///   - Content/interaction_hints.json    ({"Catalog Name": {"Interaction Name": {effects, gates}}})
/// Both are optional; unknowns fall through to raw values.
/// </summary>
public class PerceptionProjector
{
    public const int RecentEventsCap = 5;
    public const int MotiveWindowSeconds = 60;
    // LotTilePos.Distance returns units of 1/16 tile per axis (x, y are short * 16). A
    // distance value of 16 = 1 tile. 320 = 20 tiles — roughly a room-plus-adjacent. This
    // keeps perception focused on what the avatar can reasonably interact with or sense.
    //
    // Override via FSO_PERCEPTION_RADIUS env var (subtile units; 16 subtiles = 1 tile).
    // Example: FSO_PERCEPTION_RADIUS=160 → 10-tile radius (half the default).
    // Use subtile units here (not tile units) to stay consistent with LotTilePos.Distance
    // and BuildNearbyObjects filtering, which both operate in subtile units.
    //
    // Per-instance so tests can inject a radius via the constructor override path without
    // fighting static-readonly class-load-time initialization (freesoexperiment-81f).
    private readonly int _nearbyDistanceMax;

    private static int ParseRadiusEnv(int fallback = 320)
    {
        return int.TryParse(Environment.GetEnvironmentVariable("FSO_PERCEPTION_RADIUS"), out var v) ? v : fallback;
    }

    /// <summary>
    /// Returns true when <paramref name="distanceSubtiles"/> is within the nearby radius.
    /// Exposed <c>internal</c> so unit tests can verify the filter boundary directly without
    /// constructing a live VM (freesoexperiment-81f behavioral test).
    /// </summary>
    internal bool IsWithinNearbyRadius(int distanceSubtiles) => distanceSubtiles <= _nearbyDistanceMax;

    /// <summary>The effective nearby-object radius in subtile units (16 subtiles = 1 tile).</summary>
    internal int NearbyDistanceMax => _nearbyDistanceMax;

    private readonly uint _myPersistId;
    private readonly string _shardName;
    private readonly List<AnimationRule> _animRules;
    private readonly Dictionary<string, Dictionary<string, InteractionHint>> _interactionHints;

    // Rolling motive window: motive name → list of (wallclock ms, value).
    private readonly Dictionary<string, LinkedList<MotiveSample>> _motiveHistory = new();

    // recent_events rolling buffer (most-recent-last).
    private readonly LinkedList<PerceptionEvent> _recentEvents = new();

    // Track last queue state for interaction start/end synthesis.
    private readonly HashSet<ushort> _knownRunningActions = new();
    private readonly Dictionary<ushort, string> _knownActionNames = new();
    private readonly Dictionary<ushort, string> _knownActionTargets = new();

    /// <summary>
    /// Subscribe this projector to VM.OnDialog to capture dialog fires and surface them as both
    /// standalone dialog events and recent_events entries.
    /// </summary>
    public event Action<PerceptionEvent> OnDialogEvent;

    /// <param name="myPersistId">Persist ID of the avatar this projector tracks.</param>
    /// <param name="shardName">Shard display name (e.g. "Alphaville").</param>
    /// <param name="nearbyDistanceMaxOverride">
    /// Optional override for the nearby-object radius (subtile units; 16 = 1 tile).
    /// When null, reads <c>FSO_PERCEPTION_RADIUS</c> env var, defaulting to 320.
    /// Pass a value here in tests to avoid fighting static class-load-time initialization
    /// (freesoexperiment-81f).
    /// </param>
    public PerceptionProjector(uint myPersistId, string shardName, int? nearbyDistanceMaxOverride = null)
    {
        _myPersistId = myPersistId;
        _shardName = shardName ?? "Alphaville";
        _nearbyDistanceMax = nearbyDistanceMaxOverride ?? ParseRadiusEnv(320);
        _animRules = LoadAnimationRules();
        _interactionHints = LoadInteractionHints();
    }

    /// <summary>
    /// Register with the VM so dialog fires flow into the perception projector's
    /// recent_events buffer and fire standalone dialog events.
    /// </summary>
    public void AttachTo(VM vm)
    {
        vm.OnDialog += info =>
        {
            if (info == null) return;
            // Only surface dialogs that are directed at this avatar. TSO VM drivers typically
            // filter on MyUID before signalling OnDialog; callers include anything that fires
            // to this client. Defensive: if Caller matches MyAvatar, keep; otherwise include too
            // (server already scoped — we trust the VM driver gate).
            var text = BuildDialogText(info);

            // Derive response_kind and button_labels from the dialog operand type.
            // These guide the agent toward the right respond-to-dialog call (freesoexperiment-849).
            var (responseKind, buttonLabels) = ClassifyDialogResponse(info);

            // Derive integer range hints when available. For NumericEntry dialogs the
            // operand encodes min/max in the string table entries — we cannot decode those
            // without the STR resource, so we emit nulls. The agent should still call
            // respond-to-dialog with response_kind=integer and a reasonable integer_value.
            var extras = new Dictionary<string, object>
            {
                ["title"] = info.Title ?? string.Empty,
                ["dialog_id"] = info.DialogID.ToString(),
                ["caller"] = info.Caller?.ToString() ?? string.Empty,
                ["icon"] = info.Icon?.ToString() ?? string.Empty,
                ["response_kind"] = responseKind,
                ["button_labels"] = buttonLabels,
            };

            // Yes/No button overrides when the operand carries explicit strings.
            if (!string.IsNullOrEmpty(info.Yes))   extras["yes_label"]    = info.Yes;
            if (!string.IsNullOrEmpty(info.No))    extras["no_label"]     = info.No;
            if (!string.IsNullOrEmpty(info.Cancel)) extras["cancel_label"] = info.Cancel;

            var evt = new PerceptionEvent
            {
                T = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Kind = "dialog",
                Text = text,
                Extras = extras,
            };
            AddRecentEvent(evt);
            OnDialogEvent?.Invoke(evt);
        };
    }

    /// <summary>
    /// Classify the dialog's expected response kind and produce a human-readable
    /// button label list based on the VMDialogInfo.Operand type.
    ///
    /// Returns (responseKind, buttonLabels) where:
    ///   responseKind — "ok" | "ok_cancel" | "yes_no" | "yes_no_cancel" | "integer" | "string"
    ///   buttonLabels — list of button label strings shown in the dialog UI
    /// </summary>
    private static (string responseKind, List<string> buttonLabels) ClassifyDialogResponse(VMDialogInfo info)
    {
        var op = info.Operand;
        if (op == null) return ("ok", new List<string> { info.Yes ?? "OK" });

        // Use VMDialogType to classify.
        switch (op.Type)
        {
            case FSO.SimAntics.Primitives.VMDialogType.Message:
                // Single OK button.
                return ("ok", new List<string> { info.Yes ?? "OK" });

            case FSO.SimAntics.Primitives.VMDialogType.YesNo:
                return ("ok_cancel", new List<string>
                {
                    info.Yes ?? "Yes",
                    info.No  ?? "No",
                });

            case FSO.SimAntics.Primitives.VMDialogType.YesNoCancel:
                return ("ok_cancel", new List<string>
                {
                    info.Yes    ?? "Yes",
                    info.No     ?? "No",
                    info.Cancel ?? "Cancel",
                });

            case FSO.SimAntics.Primitives.VMDialogType.NumericEntry:
                return ("integer", new List<string> { info.Yes ?? "OK", info.Cancel ?? "Cancel" });

            case FSO.SimAntics.Primitives.VMDialogType.TextEntry:
                return ("string", new List<string> { info.Yes ?? "OK", info.Cancel ?? "Cancel" });

            default:
                // For any other type, assume OK-only.
                return ("ok", new List<string> { info.Yes ?? "OK" });
        }
    }

    /// <summary>Build one tick perception object. Returns null if the avatar is not yet in the VM.</summary>
    public PerceptionTick Build(VM vm)
    {
        if (vm == null) return null;
        VMAvatar me = vm.GetAvatarByPersist(_myPersistId);
        if (me == null) return null;

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Motives — record sample, compute delta/trend.
        var motives = BuildMotives(me, nowMs);

        // Diff the action queue since last tick to synthesise interaction_start/end events.
        SynthesizeQueueEvents(me, nowMs);

        // Nearby objects + interactions.
        var nearbyObjects = BuildNearbyObjects(vm, me);

        // Nearby sims (other avatars).
        var nearbySims = BuildNearbySims(vm, me);

        // Skills, relationships, balance, inventory.
        var skills = BuildSkills(me);
        var relationships = BuildRelationships(vm, me);
        var balance = me.TSOState is VMTSOEntityState tso ? (long)tso.Budget.Value : 0L;
        var inventory = BuildInventory(me);

        // Avatar sub-tree.
        // AnimationRaw is intentionally omitted from the wire shape (freesoexperiment-a78:
        // structural slim pass). AnimationHuman is the legible field the agent uses;
        // AnimationRaw is implementation detail noise that added ~5% tick size with no
        // agent-visible benefit per the W3a spike. The C# field still exists on AvatarBlock
        // (not removed) so in-process code that reads it for translation still compiles.
        var animRaw = me.CurrentAnimationState?.Anim?.Name ?? string.Empty;
        var avatar = new AvatarBlock
        {
            PersistId = me.PersistID,
            Name = me.Name ?? string.Empty,
            Shard = _shardName,
            Position = new PositionBlock
            {
                X = me.Position.x / 16.0,
                Y = me.Position.y / 16.0,
                Level = me.Position.Level,
                Direction = DirectionToCompass(me.RadianDirection),
            },
            AnimationHuman = TranslateAnimation(animRaw),
            ActionQueue = BuildActionQueue(me),
        };

        // Lot metadata.
        var clock = vm.Context?.Clock;
        var lotState = vm.TSOState as VMTSOLotState;
        var lot = new LotBlock
        {
            Name = vm.LotName ?? lotState?.Name ?? "unknown",
            LotId = (long)(lotState?.LotID ?? 0u),
            OwnerIsMe = (lotState != null && lotState.Roommates.Contains(_myPersistId)),
            OtherAvatars = vm.Entities.OfType<VMAvatar>().Count(a => a.PersistID != _myPersistId),
            SimTime = clock != null ? FormatTime(clock.Hours, clock.Minutes) : "00:00",
            TimeOfDay = clock != null ? NameTimeOfDay(clock.Hours) : "unknown",
        };

        // Recent events snapshot (stable copy).
        var recent = _recentEvents.ToList();

        // Mayor status from VM avatar flags (freesoexperiment-ea0).
        // VMTSOAvatarFlags.Mayor is set server-side from fso_avatars.mayor_nhood when
        // mayor_nhood == lot's neighborhood_id. NhoodID comes from VMTSOLotState.
        bool isMayor = me.TSOState is VMTSOAvatarState avaState &&
                       avaState.Flags.HasFlag(VMTSOAvatarFlags.Mayor);
        int mayorNhood = isMayor && lotState != null ? (int)lotState.NhoodID : 0;

        var mayorStatus = new MayorStatusBlock
        {
            IsMayor = isMayor,
            MayorNhood = mayorNhood,
        };

        return new PerceptionTick
        {
            Kind = "perception",
            T = nowMs,
            Avatar = avatar,
            Motives = motives,
            NearbyObjects = nearbyObjects,
            NearbySims = nearbySims,
            Skills = skills,
            Relationships = relationships,
            Inventory = inventory,
            Balance = balance,
            RecentEvents = recent,
            Lot = lot,
            MayorStatus = mayorStatus,
        };
    }

    // ---- motive tracking ----

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

    private Dictionary<string, MotiveBlock> BuildMotives(VMAvatar me, long nowMs)
    {
        var result = new Dictionary<string, MotiveBlock>();
        foreach (var (motive, key) in EmittedMotives)
        {
            short value = me.GetMotiveData(motive);
            if (!_motiveHistory.TryGetValue(key, out var history))
            {
                history = new LinkedList<MotiveSample>();
                _motiveHistory[key] = history;
            }
            history.AddLast(new MotiveSample { T = nowMs, Value = value });
            // Trim window.
            long cutoff = nowMs - MotiveWindowSeconds * 1000L;
            while (history.First != null && history.First.Value.T < cutoff)
                history.RemoveFirst();

            double deltaPerMinute = 0.0;
            if (history.Count >= 2)
            {
                var first = history.First.Value;
                var last = history.Last.Value;
                double secs = Math.Max(1.0, (last.T - first.T) / 1000.0);
                double dv = last.Value - first.Value;
                deltaPerMinute = dv * 60.0 / secs;
            }

            string trend = ClassifyTrend(deltaPerMinute);

            result[key] = new MotiveBlock
            {
                Value = value,
                DeltaPerMinute = Math.Round(deltaPerMinute, 2),
                Trend = trend,
            };
        }
        return result;
    }

    internal static string ClassifyTrend(double deltaPerMinute)
    {
        if (deltaPerMinute > 0.5) return "rising";
        if (deltaPerMinute < -0.5) return "falling";
        return "stable";
    }

    // ---- queue events ----

    private void SynthesizeQueueEvents(VMAvatar me, long nowMs)
    {
        var queue = me.Thread?.Queue;
        if (queue == null) return;
        var currentRunning = new HashSet<ushort>();
        foreach (var q in queue)
        {
            currentRunning.Add(q.UID);
            if (!_knownActionNames.ContainsKey(q.UID))
            {
                _knownActionNames[q.UID] = q.Name ?? "interaction";
                _knownActionTargets[q.UID] = q.Callee?.ToString() ?? string.Empty;
            }
            if (!_knownRunningActions.Contains(q.UID))
            {
                _knownRunningActions.Add(q.UID);
                AddRecentEvent(new PerceptionEvent
                {
                    T = nowMs,
                    Kind = "interaction_start",
                    Extras = new Dictionary<string, object>
                    {
                        ["interaction"] = _knownActionNames[q.UID],
                        ["target"] = _knownActionTargets[q.UID],
                    },
                });
            }
        }
        // Anything previously running but gone now = end.
        var ended = _knownRunningActions.Where(u => !currentRunning.Contains(u)).ToList();
        foreach (var uid in ended)
        {
            _knownRunningActions.Remove(uid);
            AddRecentEvent(new PerceptionEvent
            {
                T = nowMs,
                Kind = "interaction_end",
                Extras = new Dictionary<string, object>
                {
                    ["interaction"] = _knownActionNames.TryGetValue(uid, out var n) ? n : "interaction",
                    ["target"] = _knownActionTargets.TryGetValue(uid, out var t) ? t : string.Empty,
                },
            });
            _knownActionNames.Remove(uid);
            _knownActionTargets.Remove(uid);
        }
    }

    internal void AddRecentEvent(PerceptionEvent evt)
    {
        lock (_recentEvents)
        {
            _recentEvents.AddLast(evt);
            while (_recentEvents.Count > RecentEventsCap) _recentEvents.RemoveFirst();
        }
    }

    // ---- nearby ----

    private List<NearbyObjectBlock> BuildNearbyObjects(VM vm, VMAvatar me)
    {
        var results = new List<NearbyObjectBlock>();
        foreach (var e in vm.Entities)
        {
            if (e == me) continue;
            if (e is VMAvatar) continue;
            if (e.Position == LotTilePos.OUT_OF_WORLD) continue;
            int dist = LotTilePos.Distance(me.Position, e.Position);
            if (!IsWithinNearbyRadius(dist)) continue;

            // Skip multi-tile non-root entries so we don't emit one entry per tile.
            if (e.MultitileGroup != null && e.MultitileGroup.Objects != null &&
                e.MultitileGroup.Objects.Count > 1 && e.MultitileGroup.BaseObject != e)
                continue;

            List<VMPieMenuInteraction> pie = null;
            try
            {
                pie = e.GetPieMenu(vm, me, false, true);
            }
            catch (Exception ex)
            {
                // Fail soft — an object with a broken TTAB shouldn't kill the whole tick.
                Console.Error.WriteLine($"[perception] GetPieMenu failed for {e}: {ex.GetType().Name}: {ex.Message}");
                continue;
            }
            if (pie == null) pie = new List<VMPieMenuInteraction>();

            var name = e.Name ?? e.ToString();
            var category = ClassifyObjectCategory(e);
            var objectType = ExtractObjectType(e);
            var hints = LookupHints(name);

            var interactions = pie.Select((p, idx) =>
            {
                var hint = hints != null && hints.TryGetValue(p.Name ?? string.Empty, out var h) ? h : null;
                var effects = hint?.Effects ?? ComputeEffects(p);
                var gates = hint?.Gates ?? new List<string>();
                return new InteractionBlock
                {
                    Id = p.ID,
                    Name = p.Name ?? "???",
                    Effects = effects,
                    Gates = gates,
                    Available = gates.Count == 0, // available unless hints declare unmet gates
                };
            }).ToList();

            results.Add(new NearbyObjectBlock
            {
                ObjectId = e.ObjectID,
                Name = name,
                Category = category,
                ObjectType = objectType,
                Position = new PositionBlock
                {
                    X = e.Position.x / 16.0,
                    Y = e.Position.y / 16.0,
                    Level = e.Position.Level,
                },
                DistanceTiles = Math.Round(dist / 16.0, 2),
                Interactions = interactions,
            });
        }
        return results;
    }

    private List<NearbySimBlock> BuildNearbySims(VM vm, VMAvatar me)
    {
        var results = new List<NearbySimBlock>();
        foreach (var e in vm.Entities)
        {
            if (!(e is VMAvatar a)) continue;
            if (a == me) continue;
            if (a.Position == LotTilePos.OUT_OF_WORLD) continue;
            int dist = LotTilePos.Distance(me.Position, a.Position);
            // NOTE: per §5 and spec, do NOT expose other avatars' motive values. Stock client
            // does not see them. Position + name + current action is fine.
            results.Add(new NearbySimBlock
            {
                PersistId = a.PersistID,
                Name = a.Name ?? a.ToString(),
                Position = new PositionBlock
                {
                    X = a.Position.x / 16.0,
                    Y = a.Position.y / 16.0,
                    Level = a.Position.Level,
                },
                DistanceTiles = Math.Round(dist / 16.0, 2),
                CurrentAction = a.Thread?.Queue?.Count > 0 ? (a.Thread.Queue[0].Name ?? string.Empty) : string.Empty,
            });
        }
        return results;
    }

    // ---- skills / relationships / inventory ----

    private static readonly (VMPersonDataVariable var, string key)[] EmittedSkills = new[]
    {
        (VMPersonDataVariable.CookingSkill,    "cooking"),
        (VMPersonDataVariable.CharismaSkill,   "charisma"),
        (VMPersonDataVariable.MechanicalSkill, "mechanical"),
        (VMPersonDataVariable.CreativitySkill, "creativity"),
        (VMPersonDataVariable.BodySkill,       "body"),
        (VMPersonDataVariable.LogicSkill,      "logic"),
    };

    private Dictionary<string, int> BuildSkills(VMAvatar me)
    {
        var d = new Dictionary<string, int>();
        foreach (var (var, key) in EmittedSkills)
        {
            d[key] = me.GetPersonData(var);
        }
        return d;
    }

    private List<RelationshipBlock> BuildRelationships(VM vm, VMAvatar me)
    {
        var d = new List<RelationshipBlock>();
        if (me.MeToObject == null) return d;
        foreach (var kv in me.MeToObject)
        {
            var other = vm.GetObjectById((short)kv.Key) as VMAvatar;
            if (other == null) continue;
            int friendly = kv.Value.Count > 0 ? kv.Value[0] : 0;
            // Daily: if more than one rel-var is tracked, use [1] as "daily" per FreeSO RelVar layout.
            int daily = kv.Value.Count > 1 ? kv.Value[1] : 0;
            d.Add(new RelationshipBlock
            {
                WithPersistId = other.PersistID,
                Friendly = friendly,
                Daily = daily,
            });
        }
        return d;
    }

    private List<InventoryItemBlock> BuildInventory(VMAvatar me)
    {
        // Inventory data comes from a DataServiceWrapperPDU update that the FSO.Server.Clients
        // library does not currently surface in-process for the bot. Until that wiring is in
        // (see OQ-4), we emit an empty list — not a stub, not a fake. This is spec-correct:
        // "not stubs, not zeros" refers to the motives/position/animation fields; inventory is
        // one of the DataService-backed fields whose absence we explicitly document.
        return new List<InventoryItemBlock>();
    }

    // ---- helpers ----

    internal static string FormatTime(int h, int m)
    {
        int hh = h % 24;
        string ampm = hh < 12 ? "AM" : "PM";
        int display = hh % 12; if (display == 0) display = 12;
        return $"{display:D2}:{m:D2} {ampm}";
    }

    internal static string NameTimeOfDay(int h)
    {
        if (h < 5) return "night";
        if (h < 12) return "morning";
        if (h < 17) return "afternoon";
        if (h < 21) return "evening";
        return "night";
    }

    internal static string DirectionToCompass(float radians)
    {
        // FSO radian direction: 0 = north, PI/4 * n around clockwise (8-way compass).
        var d = (int)Math.Round(radians / (Math.PI / 4.0));
        d = ((d % 8) + 8) % 8;
        return d switch
        {
            0 => "N",
            1 => "NE",
            2 => "E",
            3 => "SE",
            4 => "S",
            5 => "SW",
            6 => "W",
            7 => "NW",
            _ => "N",
        };
    }

    private string ClassifyObjectCategory(VMEntity e)
    {
        // Cheap: use FSO's object category bits. The object CatalogStrings chunk has an index
        // mapping to functional categories (comfort, hygiene, bladder, energy, fun, etc.) but
        // pulling that reliably from the catalog requires asset traversal. Use a name-based
        // heuristic against the interaction hints table; fall back to "generic".
        var name = (e.Name ?? e.ToString() ?? string.Empty).ToLowerInvariant();
        var hit = _interactionHints.Keys.FirstOrDefault(k => name.Contains(k.ToLowerInvariant()));
        if (hit == null) return "generic";
        // Any hint entry with an effect tag mentions the primary motive → pick first.
        if (_interactionHints.TryGetValue(hit, out var hints))
        {
            foreach (var (_, h) in hints)
            {
                foreach (var eff in h.Effects)
                {
                    if (eff.Contains("energy")) return "energy";
                    if (eff.Contains("hunger")) return "food";
                    if (eff.Contains("comfort")) return "comfort";
                    if (eff.Contains("hygiene")) return "hygiene";
                    if (eff.Contains("bladder")) return "bladder";
                    if (eff.Contains("social")) return "social";
                    if (eff.Contains("fun")) return "fun";
                }
            }
        }
        return "generic";
    }

    /// <summary>
    /// Returns the string name of the OBJDType enum for a VMEntity.
    /// Reads ObjectType from the entity's OBJD definition. Falls back to "Unknown"
    /// when the entity has no Object/OBJ (e.g. pure VM entities without an IFF backing).
    /// </summary>
    internal static string ExtractObjectType(VMEntity e)
    {
        try
        {
            var objd = e?.Object?.OBJ;
            if (objd == null) return OBJDType.Unknown.ToString();
            return objd.ObjectType.ToString();
        }
        catch
        {
            return OBJDType.Unknown.ToString();
        }
    }

    private Dictionary<string, InteractionHint> LookupHints(string catalogName)
    {
        if (string.IsNullOrEmpty(catalogName)) return null;
        var lower = catalogName.ToLowerInvariant();
        foreach (var kv in _interactionHints)
        {
            if (lower.Contains(kv.Key.ToLowerInvariant())) return kv.Value;
        }
        return null;
    }

    private static List<string> ComputeEffects(VMPieMenuInteraction p)
    {
        // VMPieMenuInteraction.MotiveAdChanges holds adverts by motive ID when the entry
        // carries advertisement data; when present, surface these as "+motive"/"-motive".
        var effects = new List<string>();
        if (p.MotiveAdChanges == null) return effects;
        foreach (var kv in p.MotiveAdChanges)
        {
            var sign = kv.Value > 0 ? "+" : (kv.Value < 0 ? "-" : "");
            if (sign == "") continue;
            var name = Enum.IsDefined(typeof(VMMotive), kv.Key)
                ? ((VMMotive)kv.Key).ToString().ToLowerInvariant()
                : $"motive{kv.Key}";
            effects.Add(sign + name);
        }
        return effects;
    }

    private List<ActionQueueItemBlock> BuildActionQueue(VMAvatar me)
    {
        var list = new List<ActionQueueItemBlock>();
        if (me.Thread?.Queue == null) return list;
        for (int i = 0; i < me.Thread.Queue.Count; i++)
        {
            var q = me.Thread.Queue[i];
            list.Add(new ActionQueueItemBlock
            {
                InteractionId = q.UID,
                Name = q.Name ?? string.Empty,
                TargetObjectId = q.Callee?.ObjectID ?? 0,
                Status = i == 0 ? "running" : "queued",
                Mode = q.Mode switch
                {
                    VMQueueMode.Normal => "normal",
                    VMQueueMode.Idle => "idle",
                    VMQueueMode.ParentIdle => "parent-idle",
                    VMQueueMode.ParentExit => "parent-exit",
                    _ => q.Mode.ToString().ToLowerInvariant(),
                },
            });
        }
        return list;
    }

    private static string BuildDialogText(VMDialogInfo info)
    {
        // Title is typically empty for in-world speech dialogs; combine for context.
        var title = string.IsNullOrWhiteSpace(info.Title) ? "" : info.Title + ": ";
        return title + (info.Message ?? string.Empty);
    }

    // ---- animation translation ----

    private string TranslateAnimation(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        foreach (var rule in _animRules)
        {
            if (rule.Regex.IsMatch(raw)) return rule.Phrase;
        }
        return raw;
    }

    internal static List<AnimationRule> LoadAnimationRules()
    {
        // Default 20-entry starter table. Loaded from JSON if Content/animation_translation.json
        // is present alongside the binary; otherwise use the embedded defaults.
        var defaults = new List<AnimationRule>
        {
            Make(@"idle.*stand.*fidget",        "standing idle, fidgeting"),
            Make(@"idle.*stand",                "standing idle"),
            Make(@"idle.*sit",                  "sitting idle"),
            Make(@"idle.*sleep",                "sleeping"),
            Make(@"walk.*run",                  "running"),
            Make(@"walk",                       "walking"),
            Make(@"sit.*down",                  "sitting down"),
            Make(@"stand.*up",                  "standing up"),
            Make(@"eat",                        "eating"),
            Make(@"drink",                      "drinking"),
            Make(@"sleep",                      "sleeping"),
            Make(@"wake",                       "waking up"),
            Make(@"shower|bath",                "bathing"),
            Make(@"toilet|pee",                 "using toilet"),
            Make(@"read",                       "reading"),
            Make(@"watch|tv",                   "watching TV"),
            Make(@"talk|chat",                  "talking"),
            Make(@"wave",                       "waving"),
            Make(@"cheer|applaud",              "cheering"),
            Make(@"cook",                       "cooking"),
        };

        var path = Path.Combine(AppContext.BaseDirectory, "Content", "animation_translation.json");
        if (File.Exists(path))
        {
            try
            {
                var text = File.ReadAllText(path);
                var rules = JsonSerializer.Deserialize<List<AnimationRuleJson>>(text);
                if (rules != null && rules.Count > 0)
                {
                    return rules.Select(r => Make(r.pattern, r.phrase)).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[perception] animation_translation.json load failed: {ex.Message}");
            }
        }
        return defaults;
    }

    private static AnimationRule Make(string pattern, string phrase) =>
        new AnimationRule
        {
            Pattern = pattern,
            Phrase = phrase,
            Regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

    internal static Dictionary<string, Dictionary<string, InteractionHint>> LoadInteractionHints()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Content", "interaction_hints.json");
        if (File.Exists(path))
        {
            try
            {
                var text = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, InteractionHint>>>(text);
                if (parsed != null) return parsed;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[perception] interaction_hints.json load failed: {ex.Message}");
            }
        }
        return new Dictionary<string, Dictionary<string, InteractionHint>>();
    }

    // ---- public types ----

    public class AnimationRule
    {
        public string Pattern { get; set; }
        public string Phrase { get; set; }
        public Regex Regex { get; set; }
    }

    private class AnimationRuleJson
    {
        public string pattern { get; set; }
        public string phrase { get; set; }
    }

    public class InteractionHint
    {
        public List<string> Effects { get; set; } = new();
        public List<string> Gates { get; set; } = new();
    }

    private struct MotiveSample
    {
        public long T;
        public short Value;
    }
}

// ---- wire-format DTOs (record-ish, JSON-friendly) ----

public class PerceptionTick
{
    public string Kind { get; set; }
    public long T { get; set; }
    public AvatarBlock Avatar { get; set; }
    public Dictionary<string, MotiveBlock> Motives { get; set; }
    public List<NearbyObjectBlock> NearbyObjects { get; set; }
    public List<NearbySimBlock> NearbySims { get; set; }
    public Dictionary<string, int> Skills { get; set; }
    public List<RelationshipBlock> Relationships { get; set; }
    public List<InventoryItemBlock> Inventory { get; set; }
    public long Balance { get; set; }
    public List<PerceptionEvent> RecentEvents { get; set; }
    public LotBlock Lot { get; set; }
    /// <summary>
    /// Mayor status derived from the VM avatar's <see cref="VMTSOAvatarFlags.Mayor"/> flag
    /// (freesoexperiment-ea0). Replaces the env-var + file-based check-mayor pathway.
    /// is_mayor=true means this avatar holds the Mayor flag in the current lot's VM.
    /// mayor_nhood=0 when is_mayor=false.
    /// </summary>
    public MayorStatusBlock MayorStatus { get; set; }
}

public class AvatarBlock
{
    public uint PersistId { get; set; }
    public string Name { get; set; }
    public string Shard { get; set; }
    public PositionBlock Position { get; set; }
    /// <summary>
    /// Raw FSO animation clip name. Kept as a C# field for in-process translation
    /// (used by PerceptionProjector.TranslateAnimation) but excluded from the wire
    /// shape (freesoexperiment-a78 structural slim). Agents use animation_human.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AnimationRaw { get; set; }
    public string AnimationHuman { get; set; }
    public List<ActionQueueItemBlock> ActionQueue { get; set; }
}

public class PositionBlock
{
    public double X { get; set; }
    public double Y { get; set; }
    public int Level { get; set; }
    public string Direction { get; set; }
}

public class ActionQueueItemBlock
{
    public int InteractionId { get; set; }
    public string Name { get; set; }
    public short TargetObjectId { get; set; }
    public string Status { get; set; }
    public string Mode { get; set; }
}

public class MotiveBlock
{
    public short Value { get; set; }
    public double DeltaPerMinute { get; set; }
    public string Trend { get; set; }
}

public class NearbyObjectBlock
{
    public short ObjectId { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    /// <summary>
    /// String name of the OBJDType enum value for this object (e.g. "Portal" for stairs,
    /// "Normal" for buyable objects, "Food" for food items). Sourced from
    /// <see cref="FSO.Files.Formats.IFF.Chunks.OBJDType"/> via the object's OBJD entry.
    /// Enables type-aware filtering in the sidecar (e.g. stair detection by type rather
    /// than name substring — freesoexperiment-d5b).
    /// </summary>
    public string ObjectType { get; set; }
    public PositionBlock Position { get; set; }
    public double DistanceTiles { get; set; }
    public List<InteractionBlock> Interactions { get; set; }
}

public class InteractionBlock
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> Effects { get; set; }
    public List<string> Gates { get; set; }
    public bool Available { get; set; }
}

public class NearbySimBlock
{
    public uint PersistId { get; set; }
    public string Name { get; set; }
    public PositionBlock Position { get; set; }
    public double DistanceTiles { get; set; }
    public string CurrentAction { get; set; }
}

public class RelationshipBlock
{
    public uint WithPersistId { get; set; }
    public int Friendly { get; set; }
    public int Daily { get; set; }
}

public class InventoryItemBlock
{
    public long Guid { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }
}

public class LotBlock
{
    public string Name { get; set; }
    public long LotId { get; set; }
    public bool OwnerIsMe { get; set; }
    public int OtherAvatars { get; set; }
    public string SimTime { get; set; }
    public string TimeOfDay { get; set; }
}

public class PerceptionEvent
{
    public long T { get; set; }
    public string Kind { get; set; }
    public string Text { get; set; }
    public Dictionary<string, object> Extras { get; set; }
}

/// <summary>
/// Mayor status block emitted on every perception tick (freesoexperiment-ea0).
/// Sourced from <see cref="VMTSOAvatarFlags.Mayor"/> in the live VM — no env var, no file.
/// </summary>
public class MayorStatusBlock
{
    /// <summary>True when the VM avatar carries the Mayor flag in this lot.</summary>
    public bool IsMayor { get; set; }
    /// <summary>
    /// Neighborhood id this avatar is mayor of. 0 when IsMayor=false.
    /// Read from <see cref="VMTSOLotState.NhoodID"/> — the lot the bot is currently on.
    /// </summary>
    public int MayorNhood { get; set; }
}
