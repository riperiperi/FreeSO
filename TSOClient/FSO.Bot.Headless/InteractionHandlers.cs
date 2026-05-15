using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
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
        dispatcher.Register("queue-interactions",
            (args, ct) => Task.FromResult(QueueInteractions(vmHost, args)));
        dispatcher.Register("cancel-interaction",
            (args, ct) => Task.FromResult(CancelInteraction(vmHost, args)));
        dispatcher.Register("query-pie-menu",
            (args, ct) => Task.FromResult(QueryPieMenu(vmHost, args)));
        dispatcher.Register("query-action-queue",
            (args, ct) => Task.FromResult(QueryActionQueue(vmHost, args)));
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
    ///       {
    ///         "id": 3, "name": "Sit", "param0": 0, "global": false, "score": 0.0,
    ///         "available": true, "gates": []
    ///       }
    ///     ]
    ///   }
    /// }
    /// </code>
    ///
    /// Wire-shape invariants (freesoexperiment-d51):
    /// <list type="bullet">
    ///   <item><c>available</c> is ALWAYS a boolean — never null. <c>true</c> for every entry
    ///   that the engine's TTAB gate evaluation accepted. Interactions that failed the gate check
    ///   are not included in the list (the engine drops them silently). There is no
    ///   <c>available: null</c> shape.</item>
    ///   <item><c>gates</c> is ALWAYS a string array — never null. Empty for normally-accepted
    ///   interactions. Contains <c>"engine-eval-failed"</c> when an exception occurred during
    ///   <see cref="VMEntity.GetPieMenu"/> — in that case the full list is replaced by a single
    ///   sentinel entry whose <c>available</c> is <c>false</c>, and the top-level
    ///   <c>eval_error</c> field carries the exception message for diagnostics.</item>
    /// </list>
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

            // freesoexperiment-d51: engine-eval-failed normalization.
            // When GetPieMenu throws, return a structured sentinel instead of Response.Fail so
            // the caller receives a well-typed payload they can act on (available: false,
            // gates: ["engine-eval-failed"]) rather than an opaque error string. The full
            // exception message is preserved in eval_error for diagnostics.
            List<VMPieMenuInteraction> pie;
            string evalError = null;
            try
            {
                pie = callee.GetPieMenu(vm, caller, includeHidden, includeGlobal);
            }
            catch (Exception ex)
            {
                evalError = $"{ex.GetType().Name}: {ex.Message}";
                pie = null;
            }

            if (evalError != null)
            {
                // Engine threw during TTAB evaluation — return the sentinel shape so the agent
                // sees a structured available:false + gates:["engine-eval-failed"] entry.
                var sentinel = new List<object>(1)
                {
                    new
                    {
                        id = 0,
                        name = "",
                        param0 = 0,
                        global = false,
                        score = 0.0f,
                        available = false,
                        gates = new[] { "engine-eval-failed" },
                    }
                };
                return (CommandDispatcher.Response.Success(new
                {
                    target_object_id = (int)targetObjectId,
                    interactions = sentinel,
                    eval_error = evalError,
                }), targetObjectId);
            }

            // Project to POCOs while still under the lock — VMPieMenuInteraction references
            // VMEntity/TTABInteraction objects that mutate under the tick thread. We copy
            // the scalars and release.
            // freesoexperiment-d51: available is always true for returned entries (the engine
            // drops interactions that fail the TTAB gate check — they never appear here).
            // gates is always an empty array for normal entries. Neither field is ever null.
            var emptyGates = Array.Empty<string>();
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
                        available = true,
                        gates = emptyGates,
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

    /// <summary>
    /// queue-interactions — push N interactions onto the caller's action queue in one
    /// call (freesoexperiment-36a). Replaces N sequential <c>interact-with</c> calls
    /// at the cf round-trip floor with a single round-trip; engine same-tick atomicity
    /// (VMNetDriver.InternalTick) guarantees the N PDUs land sequentially before any
    /// next-tick BHAV pushes.
    ///
    /// <para>
    /// Args: <c>interactions</c> (array, required, ≥1) of
    /// <c>{interaction:int, callee_id:int, param0?:int, global?:bool}</c>, and one
    /// <c>queue_mode</c> string ("queue" default cancels engine-Mode-Idle entries
    /// first; "preempt" cancels the whole queue). queue_mode is applied ONCE before
    /// the batch — applying it between entries would cancel earlier batch members
    /// under the Idle-mode logic.
    /// </para>
    ///
    /// <para>
    /// Response: <c>{queued, cancelled, queue_mode}</c>. Does NOT include action_uids
    /// — those are assigned by the engine in EnqueueAction (VMThread.cs:770) when the
    /// PDU runs on the server tick, after the response is sent. Agents that need UIDs
    /// (for selective cancellation) should call <c>query-action-queue</c> after the
    /// batch lands.
    /// </para>
    ///
    /// <para>
    /// Validation is strict: any malformed entry rejects the entire batch BEFORE
    /// queue_mode is applied, so a bad batch can't leave the caller with a wiped
    /// queue and no replacement actions.
    /// </para>
    /// </summary>
    internal static CommandDispatcher.Response QueueInteractions(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost?.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        var arr = args["interactions"] as JsonArray;
        if (arr == null)
            return CommandDispatcher.Response.Fail("queue-interactions requires 'interactions' array");
        if (arr.Count == 0)
            return CommandDispatcher.Response.Fail("queue-interactions requires non-empty 'interactions' array");

        var entries = new List<VMNetInteractionCmd>(arr.Count);
        for (int i = 0; i < arr.Count; i++)
        {
            var e = arr[i] as JsonObject;
            if (e == null)
                return CommandDispatcher.Response.Fail($"queue-interactions[{i}]: entry is not an object");
            var interaction = (long?)e["interaction"];
            var calleeId = (long?)e["callee_id"];
            if (!interaction.HasValue)
                return CommandDispatcher.Response.Fail($"queue-interactions[{i}]: missing 'interaction' (TTAB index)");
            if (!calleeId.HasValue || calleeId.Value == 0)
                return CommandDispatcher.Response.Fail($"queue-interactions[{i}]: missing 'callee_id'");

            entries.Add(new VMNetInteractionCmd
            {
                Interaction = checked((ushort)interaction.Value),
                CalleeID = checked((short)calleeId.Value),
                Param0 = (short)((long?)e["param0"] ?? 0),
                Global = (bool?)e["global"] ?? false,
                CallerID = caller.ObjectID,
            });
        }

        var queueMode = QueueModeHelper.ReadQueueMode(args);
        if (!QueueModeHelper.ApplyQueueMode(vmHost, queueMode, out var cancelled, out var qmErr))
            return CommandDispatcher.Response.Fail(qmErr);

        foreach (var cmd in entries)
            vmHost.Driver.SendCommand(cmd);

        return CommandDispatcher.Response.Success(new
        {
            queued = entries.Count,
            cancelled,
            queue_mode = queueMode,
        });
    }

    /// <summary>
    /// query-action-queue — return the caller's <c>Thread.Queue</c> contents on demand
    /// (freesoexperiment-dbe). Perception already broadcasts the queue every tick via
    /// <see cref="PerceptionProjector"/>'s ActionQueue block, but this verb gives the
    /// agent a precision read right after a mutation (queue-interactions, cancel,
    /// build batch) before the next perception fires.
    ///
    /// <para>
    /// Args: <c>include_idle</c> (bool, default true) — when false, filters out
    /// <see cref="VMQueueMode.Idle"/> engine-autopilot entries so the agent sees only
    /// deliberate actions.
    /// </para>
    ///
    /// <para>
    /// Response: <c>{count, queue:[{action_uid, name, interaction_id, callee_id,
    /// callee_kind, callee_guid_hex, mode, priority, param0, status}]}</c>.
    /// <c>action_uid</c> is the cancellation handle (<c>q.UID</c>);
    /// <c>interaction_id</c> is the TTAB index (<c>q.InteractionNumber</c>) — they are
    /// distinct, do not confuse them. <c>status</c> is "running" for entry 0 (currently
    /// executing) and "queued" for the rest.
    /// </para>
    /// </summary>
    internal static CommandDispatcher.Response QueryActionQueue(HeadlessVMHost vmHost, JsonObject args)
    {
        if (vmHost == null) return CommandDispatcher.Response.Fail("no live avatar");
        bool includeIdle = (bool?)args["include_idle"] ?? true;

        var payload = vmHost.RunUnderTickLock<object>(() =>
        {
            var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
            if (caller == null) return null;

            var queue = caller.Thread?.Queue;
            var items = new List<object>();
            if (queue != null)
            {
                for (int i = 0; i < queue.Count; i++)
                {
                    var q = queue[i];
                    if (!includeIdle && q.Mode == VMQueueMode.Idle) continue;

                    string mode = q.Mode switch
                    {
                        VMQueueMode.Normal => "normal",
                        VMQueueMode.Idle => "idle",
                        VMQueueMode.ParentIdle => "parent-idle",
                        VMQueueMode.ParentExit => "parent-exit",
                        _ => q.Mode.ToString().ToLowerInvariant(),
                    };

                    string priority = Enum.IsDefined(typeof(VMQueuePriority), q.Priority)
                        ? ((VMQueuePriority)q.Priority).ToString().ToLowerInvariant()
                        : $"raw_{q.Priority}";

                    short param0 = (q.Args != null && q.Args.Length > 0) ? q.Args[0] : (short)0;

                    string calleeKind = q.Callee is VMAvatar ? "avatar"
                        : q.Callee != null ? "object" : "";
                    string calleeGuid = q.Callee?.Object?.OBJ != null
                        ? "0x" + q.Callee.Object.OBJ.GUID.ToString("X8")
                        : "";

                    items.Add(new
                    {
                        action_uid      = (int)q.UID,
                        name            = q.Name ?? "",
                        interaction_id  = q.InteractionNumber,
                        callee_id       = (int)(q.Callee?.ObjectID ?? 0),
                        callee_kind     = calleeKind,
                        callee_guid_hex = calleeGuid,
                        mode,
                        priority,
                        param0          = (int)param0,
                        status          = i == 0 ? "running" : "queued",
                    });
                }
            }
            return new { count = items.Count, queue = items };
        });

        if (payload == null) return CommandDispatcher.Response.Fail("no live avatar");
        return CommandDispatcher.Response.Success(payload);
    }
}
