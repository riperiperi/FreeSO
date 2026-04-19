using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// go-to: high-level "head somewhere" verb. Resolves a target from any of
/// target_object_id, target_sim_id, object_name (substring on entity name), or
/// location (x/y/level in tile units — we multiply by 16 internally). Picks the
/// closest reachable candidate under the tick lock, then dispatches either
/// walk-to-equivalent (<c>VMNetGotoCmd</c>) or walk-and-do (<c>VMNetInteractionCmd</c>)
/// depending on whether <c>interaction</c> is supplied. Queue semantics honor
/// <see cref="QueueModeHelper"/>.
///
/// The verb exists because, in practice, agents almost always know a target by
/// identity ("the toilet", "the nearest sofa") rather than by coordinates. Having
/// to hand-resolve nearby_objects[] before every walk burns reasoning budget and
/// produced run-4's unit-confusion failure (passing x=42,y=62 as if they were
/// tile coords when the handler expects 1/16-tile units). go-to short-circuits
/// that class of mistake by accepting tile-unit location OR object identity and
/// doing the conversion and matching server-side.
/// </summary>
public static class GoToHandlers
{
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));
        dispatcher.Register("go-to", (args, ct) => Task.FromResult(GoTo(vmHost, args)));
    }

    internal static CommandDispatcher.Response GoTo(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        // Selectors — first non-empty wins.
        var targetObj = (long?)args["target_object_id"];
        var targetSim = (long?)args["target_sim_id"];
        var objName = (string)args["object_name"];
        var locArg = args["location"] as JsonObject;
        var interactionName = (string)args["interaction"];
        var maxDist = (long?)args["max_distance_tiles"] ?? 50;

        string queueMode = QueueModeHelper.ReadQueueMode(args);

        // Resolve the target to one of: (ObjectID, name, distance) for object/sim OR
        // (x, y, level) for location. Location is walk-only; interaction is ignored there.
        (short pickedObjectId, string pickedName, double pickedDistTiles, short tx, short ty, sbyte level, bool isLocation) resolved;
        List<string> candidatesSample = null;
        try
        {
            resolved = vmHost.RunUnderTickLock<(short, string, double, short, short, sbyte, bool)>(() =>
            {
                var vm = vmHost.VM;
                if (vm == null) throw new InvalidOperationException("no VM");
                var me = vm.GetAvatarByPersist(vmHost.MyAvatarPersistId);
                if (me == null) throw new InvalidOperationException("no live avatar");

                if (targetSim.HasValue && targetSim.Value != 0)
                {
                    var other = vm.GetAvatarByPersist((uint)targetSim.Value);
                    if (other == null) throw new InvalidOperationException($"sim {targetSim} not in local VM");
                    return (other.ObjectID, other.Name ?? "?", LotTilePos.Distance(me.Position, other.Position) / 16.0,
                        other.Position.x, other.Position.y, other.Position.Level, false);
                }

                if (targetObj.HasValue && targetObj.Value != 0)
                {
                    var ent = vm.GetObjectById(checked((short)targetObj.Value));
                    if (ent == null) throw new InvalidOperationException($"object {targetObj} not in local VM");
                    return (ent.ObjectID, ent.Name ?? "?", LotTilePos.Distance(me.Position, ent.Position) / 16.0,
                        ent.Position.x, ent.Position.y, ent.Position.Level, false);
                }

                if (locArg != null)
                {
                    var lx = (long?)locArg["x"];
                    var ly = (long?)locArg["y"];
                    var llv = (long?)locArg["level"] ?? 1;
                    if (!lx.HasValue || !ly.HasValue)
                        throw new InvalidOperationException("location requires x and y (in tile units)");
                    // Tile units (agent-facing) → 1/16 subtile units (wire).
                    var sx = checked((short)(lx.Value * 16));
                    var sy = checked((short)(ly.Value * 16));
                    var sl = checked((sbyte)llv);
                    return (0, $"tile({lx},{ly},{llv})", 0, sx, sy, sl, true);
                }

                if (!string.IsNullOrEmpty(objName))
                {
                    var needle = objName.ToLowerInvariant();
                    VMEntity best = null;
                    int bestDist = int.MaxValue;
                    var sampled = new List<(string name, int distUnits)>();
                    foreach (var e in vm.Entities)
                    {
                        if (e is VMAvatar) continue;
                        if (e.Position == LotTilePos.OUT_OF_WORLD) continue;
                        if (e.MultitileGroup?.Objects != null && e.MultitileGroup.Objects.Count > 1 && e.MultitileGroup.BaseObject != e) continue;
                        var n = e.Name ?? "";
                        if (string.IsNullOrEmpty(n)) continue;
                        int d = LotTilePos.Distance(me.Position, e.Position);
                        if (d / 16.0 > maxDist) continue;
                        sampled.Add((n, d));
                        if (!n.ToLowerInvariant().Contains(needle)) continue;
                        if (d < bestDist) { bestDist = d; best = e; }
                    }
                    if (best == null)
                    {
                        // Surface a candidate sample for failure response (20 closest by distance).
                        candidatesSample = sampled
                            .OrderBy(x => x.distUnits)
                            .Take(20)
                            .Select(x => $"{x.name} ({Math.Round(x.distUnits / 16.0, 1)}t)")
                            .ToList();
                        throw new InvalidOperationException($"no object matched object_name='{objName}'");
                    }
                    return (best.ObjectID, best.Name ?? "?", Math.Round(bestDist / 16.0, 2),
                        best.Position.x, best.Position.y, best.Position.Level, false);
                }

                throw new InvalidOperationException("go-to requires one of: target_object_id, target_sim_id, object_name, location");
            });
        }
        catch (InvalidOperationException ex)
        {
            var payload = new Dictionary<string, object> { ["error_detail"] = ex.Message };
            if (candidatesSample != null && candidatesSample.Count > 0)
                payload["nearby_names_sample"] = candidatesSample;
            return CommandDispatcher.Response.Fail(ex.Message + (candidatesSample != null ? $"; nearby: {string.Join(", ", candidatesSample.Take(8))}" : ""));
        }

        // Queue-mode pre-emission (cancels Idle or all, per mode).
        if (!QueueModeHelper.ApplyQueueMode(vmHost, queueMode, out var cancelled, out var qmErr))
            return CommandDispatcher.Response.Fail(qmErr);

        // Dispatch. Location → walk-only (VMNetGotoCmd). Otherwise, if interaction
        // is given, resolve to TTAB id via pie-menu match and send VMNetInteractionCmd.
        // Bare object/sim target with no interaction → walk-only.
        if (resolved.isLocation || string.IsNullOrEmpty(interactionName))
        {
            vmHost.Driver.SendCommand(new VMNetGotoCmd
            {
                Interaction = 4, // Run Here
                Param0 = 0,
                x = resolved.tx,
                y = resolved.ty,
                level = resolved.level,
            });
            return CommandDispatcher.Response.Success(new
            {
                mode = "walk",
                picked_object_id = resolved.isLocation ? 0 : (int)resolved.pickedObjectId,
                picked_name = resolved.pickedName,
                picked_distance_tiles = resolved.pickedDistTiles,
                queue_mode = queueMode,
                cancelled,
            });
        }

        // Interaction resolution — find a pie-menu entry on the target whose name
        // contains the requested interaction (case-insensitive). Prefer exact-token
        // match; fall back to substring.
        byte? pickedInteractionId = null;
        string pickedInteractionName = null;
        List<string> availableInteractions = null;
        var pieResult = vmHost.RunUnderTickLock<(byte? id, string name, List<string> available)>(() =>
        {
            var vm = vmHost.VM;
            var callee = vm.GetObjectById(resolved.pickedObjectId);
            if (callee == null) return (null, null, new List<string>());
            List<VMPieMenuInteraction> pie;
            try { pie = callee.GetPieMenu(vm, caller, includeHidden: false, includeGlobal: true); }
            catch { pie = null; }
            if (pie == null || pie.Count == 0) return (null, null, new List<string>());
            var names = pie.Select(p => p.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList();
            var needle = interactionName.ToLowerInvariant().Trim();
            // Exact token match (case-insensitive).
            foreach (var p in pie)
            {
                if (p.Name != null && p.Name.Equals(interactionName, StringComparison.OrdinalIgnoreCase))
                    return (p.ID, p.Name, names);
            }
            // Substring match.
            foreach (var p in pie)
            {
                if (p.Name != null && p.Name.ToLowerInvariant().Contains(needle))
                    return (p.ID, p.Name, names);
            }
            return (null, null, names);
        });
        pickedInteractionId = pieResult.id;
        pickedInteractionName = pieResult.name;
        availableInteractions = pieResult.available;

        if (!pickedInteractionId.HasValue)
        {
            return CommandDispatcher.Response.Fail(
                $"no pie-menu entry on '{resolved.pickedName}' matching interaction='{interactionName}'; " +
                $"available=[{string.Join(", ", availableInteractions.Take(12))}]");
        }

        vmHost.Driver.SendCommand(new VMNetInteractionCmd
        {
            Interaction = pickedInteractionId.Value,
            CalleeID = resolved.pickedObjectId,
            Param0 = 0,
            Global = false,
            CallerID = caller.ObjectID,
        });
        return CommandDispatcher.Response.Success(new
        {
            mode = "walk-and-do",
            picked_object_id = (int)resolved.pickedObjectId,
            picked_name = resolved.pickedName,
            picked_distance_tiles = resolved.pickedDistTiles,
            picked_interaction_id = (int)pickedInteractionId.Value,
            picked_interaction_name = pickedInteractionName,
            queue_mode = queueMode,
            cancelled,
        });
    }
}
