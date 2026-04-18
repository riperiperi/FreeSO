using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Concrete interaction-family command handlers (freesoexperiment-2a8). Three ops:
///
/// <list type="bullet">
///   <item><c>interact-with</c> — invoke a named pie-menu interaction on an object or Sim.
///   Wire-level PDU is <see cref="VMNetInteractionCmd"/>. Accepts a <c>queue_mode</c> arg
///   ("queue" default cancels engine-Mode-Idle entries before enqueueing; "preempt" cancels
///   the entire queue first). The previously-separate <c>queue-interaction</c> verb has been
///   removed — its semantics (append behind current action) are now <c>queue_mode: queue</c>.</item>
///
///   <item><c>cancel-interaction</c> — cancel ONE specific queued/running interaction by
///   <c>action_uid</c>. Wire-level PDU is <see cref="VMNetInteractionCancelCmd"/>. Distinct
///   from the movement-family <c>cancel</c> op, which defaults to "cancel everything I see
///   locally" (one PDU per visible queue entry). This handler REQUIRES <c>action_uid</c> —
///   it is the precision primitive; the broad sweep lives under <c>cancel</c>.</item>
///
///   <item><c>query-pie-menu</c> — local VM introspection, no outbound PDU. Computes the
///   caller's pie-menu against a target object or Sim using
///   <see cref="VMEntity.GetPieMenu(VM, VMEntity, bool, bool)"/> — the same method the
///   perception emitter already exercises (2048 pie-menus/64 ticks proven in
///   freesoexperiment-66c). Returns the list of <see cref="VMPieMenuInteraction"/> the UI
///   would show: each entry's <c>id</c> (TTAB index), <c>param0</c>, <c>global</c>,
///   <c>name</c>. The agent uses this to discover valid <c>interaction</c> / <c>param0</c>
///   values for <c>interact-with</c>.</item>
/// </list>
///
/// Thread safety: <c>query-pie-menu</c> reads deep into VM state (callee.TreeTable,
/// caller.Thread.CheckAction, interaction guards) — all of which the tick thread may be
/// mutating concurrently. We route the entire pie-menu computation through
/// <see cref="HeadlessVMHost.RunUnderTickLock{T}"/> (freesoexperiment-a85 contract). The
/// wire PDU handlers (<c>interact-with</c>, <c>cancel-interaction</c>) only read the
/// caller's identity (<c>MyAvatarPersistId</c>) and <c>VM.GetObjectById</c>, then call
/// <c>Driver.SendCommand</c> — the serial-dispatch contract in <see cref="CommandDispatcher"/>
/// plus the lock inside <c>VMClientDriver.SendCommand</c> covers those.
/// </summary>
public static class InteractionHandlers
{
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));

        dispatcher.Register("interact-with",
            (args, ct) => Task.FromResult(InteractWith(vmHost, args)));
        dispatcher.Register("cancel-interaction",
            (args, ct) => Task.FromResult(CancelInteraction(vmHost, args)));
        dispatcher.Register("query-pie-menu",
            (args, ct) => Task.FromResult(QueryPieMenu(vmHost, args)));
    }

    /// <summary>
    /// interact-with — push a named object interaction onto the caller's action queue.
    /// Args: interaction (ushort TTAB index, required), callee_id (short ObjectID, required),
    /// param0 (short, default 0), global (bool, default false), queue_mode (string, default
    /// "queue"). queue_mode controls how the new interaction lands: "queue" cancels engine
    /// autopilot (Mode.Idle) entries first so deliberate action outranks autopilot; "preempt"
    /// cancels the entire queue. See <see cref="QueueModeHelper"/>.
    /// </summary>
    internal static CommandDispatcher.Response InteractWith(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        var interactionArg = (long?)args["interaction"];
        var calleeArg = (long?)args["callee_id"];
        if (!interactionArg.HasValue)
            return CommandDispatcher.Response.Fail("interact-with requires interaction (TTAB index)");
        if (!calleeArg.HasValue || calleeArg.Value == 0)
            return CommandDispatcher.Response.Fail("interact-with requires callee_id");

        var queueMode = QueueModeHelper.ReadQueueMode(args);
        if (!QueueModeHelper.ApplyQueueMode(vmHost, queueMode, out var cancelled, out var qmErr))
            return CommandDispatcher.Response.Fail(qmErr);

        var cmd = new VMNetInteractionCmd
        {
            Interaction = checked((ushort)interactionArg.Value),
            CalleeID = checked((short)calleeArg.Value),
            Param0 = (short)((long?)args["param0"] ?? 0),
            Global = (bool?)args["global"] ?? false,
            CallerID = caller.ObjectID,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            interaction = (int)cmd.Interaction,
            callee_id = (int)cmd.CalleeID,
            param0 = (int)cmd.Param0,
            global = cmd.Global,
            queue_mode = queueMode,
            cancelled,
        });
    }

    /// <summary>
    /// cancel-interaction — abort ONE queued/running interaction by <c>action_uid</c>. Wire
    /// PDU is <see cref="VMNetInteractionCancelCmd"/>. Unlike the broad <c>cancel</c> op
    /// (which iterates the locally-visible queue and fires one cancel per entry), this
    /// handler REQUIRES <c>action_uid</c> and rejects missing/zero values — it is the
    /// precision primitive. Use this when the agent wants to abort a specific long-running
    /// interaction while preserving the rest of the queue.
    /// </summary>
    internal static CommandDispatcher.Response CancelInteraction(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        var actionUidArg = (long?)args["action_uid"];
        if (!actionUidArg.HasValue)
            return CommandDispatcher.Response.Fail("cancel-interaction requires action_uid (use 'cancel' for broad sweep)");
        var actionUid = checked((ushort)actionUidArg.Value);
        if (actionUid == 0)
            return CommandDispatcher.Response.Fail("cancel-interaction requires non-zero action_uid");

        vmHost.Driver.SendCommand(new VMNetInteractionCancelCmd { ActionUID = actionUid });
        return CommandDispatcher.Response.Success(new { cancelled = 1, action_uid = (int)actionUid });
    }

    /// <summary>
    /// query-pie-menu — compute the local pie-menu for a target object or Sim from the
    /// caller's perspective. No outbound PDU; reads the bot's replica VM and returns what the
    /// UI would show. Args: target_object_id (short, required unless target_sim_id set),
    /// target_sim_id (uint persist id, resolves to that avatar's ObjectID),
    /// include_hidden (bool, default false), include_global (bool, default true).
    ///
    /// Response payload shape:
    /// <code>
    /// {
    ///   "ok": true,
    ///   "payload": {
    ///     "target_object_id": 17,
    ///     "interactions": [
    ///       {"id": 3, "name": "Sit", "param0": 0, "global": false, "score": 0.0}
    ///     ]
    ///   }
    /// }
    /// </code>
    /// </summary>
    internal static CommandDispatcher.Response QueryPieMenu(HeadlessVMHost vmHost, JsonObject args)
    {
        // Resolve target: target_sim_id → avatar.ObjectID; else target_object_id.
        short targetObjectId = 0;
        var targetSimArg = (long?)args["target_sim_id"];
        var targetObjArg = (long?)args["target_object_id"];

        bool includeHidden = (bool?)args["include_hidden"] ?? false;
        bool includeGlobal = (bool?)args["include_global"] ?? true;

        // Entire computation under the tick lock: callee.TreeTable + caller.Thread.CheckAction
        // are tick-thread-owned. Keep the action tight; the tick thread blocks while we hold
        // the lock.
        var result = vmHost.RunUnderTickLock<(CommandDispatcher.Response resp, short tid)>(() =>
        {
            var vm = vmHost.VM;
            if (vm == null)
                return (CommandDispatcher.Response.Fail("no VM"), (short)0);

            var caller = vm.GetAvatarByPersist(vmHost.MyAvatarPersistId);
            if (caller == null)
                return (CommandDispatcher.Response.Fail("no live avatar"), (short)0);

            VMEntity callee = null;
            if (targetSimArg.HasValue && targetSimArg.Value != 0)
            {
                var av = vm.GetAvatarByPersist((uint)targetSimArg.Value);
                if (av == null)
                    return (CommandDispatcher.Response.Fail($"sim {targetSimArg} not in local VM"), (short)0);
                callee = av;
                targetObjectId = av.ObjectID;
            }
            else if (targetObjArg.HasValue && targetObjArg.Value != 0)
            {
                targetObjectId = checked((short)targetObjArg.Value);
                callee = vm.GetObjectById(targetObjectId);
                if (callee == null)
                    return (CommandDispatcher.Response.Fail($"object {targetObjArg} not in local VM"), targetObjectId);
            }
            else
            {
                return (CommandDispatcher.Response.Fail("query-pie-menu requires target_object_id or target_sim_id"), (short)0);
            }

            List<VMPieMenuInteraction> pie;
            try
            {
                pie = callee.GetPieMenu(vm, caller, includeHidden, includeGlobal);
            }
            catch (Exception ex)
            {
                return (CommandDispatcher.Response.Fail($"pie-menu compute: {ex.GetType().Name}: {ex.Message}"), targetObjectId);
            }

            // Project to POCOs while still under the lock — VMPieMenuInteraction references
            // VMEntity/TTABInteraction objects that mutate under the tick thread. We copy
            // the scalars and release.
            var items = new List<object>(pie?.Count ?? 0);
            if (pie != null)
            {
                foreach (var e in pie)
                {
                    items.Add(new
                    {
                        id = (int)e.ID,
                        name = e.Name ?? "",
                        param0 = (int)e.Param0,
                        global = e.Global,
                        score = e.Score,
                    });
                }
            }
            return (CommandDispatcher.Response.Success(new
            {
                target_object_id = (int)targetObjectId,
                interactions = items,
            }), targetObjectId);
        });
        return result.resp;
    }
}
