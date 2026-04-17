using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Concrete movement-family command handlers (freesoexperiment-b9c). Translate JSON args into
/// outbound <c>VMNet*Cmd</c> PDUs via <c>vmHost.Driver.SendCommand</c>. The driver's
/// <c>OnClientCommand</c> byte-array is wrapped in <c>FSOVMCommand</c> and written to the lot
/// Aries socket by <see cref="WireOutboundVMCommands"/>.
///
/// Sync semantics: these VM commands are fire-and-forget on the wire (see verb-catalog.md
/// §Response model). The handler returns <c>{"ok":true}</c> ACK once the command is queued
/// into the driver; the caller observes the wire-level effect via the perception stream.
/// </summary>
public static class MovementHandlers
{
    /// <summary>
    /// Wire the VMClientDriver's outbound-command stream to the lot Aries socket, replacing the
    /// read-only drop behaviour established in d87-a. Must be called exactly once per bot
    /// session, after <see cref="HeadlessLotConnection"/> and <see cref="HeadlessVMHost"/> are
    /// both constructed but before the VM starts ticking.
    /// </summary>
    public static void WireOutboundVMCommands(HeadlessVMHost vmHost, HeadlessLotConnection lotConn)
    {
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));
        if (lotConn == null) throw new ArgumentNullException(nameof(lotConn));
        vmHost.Driver.OnClientCommand += data =>
        {
            try
            {
                if (!lotConn.Lot.IsConnected)
                {
                    Program.Log($"[vm:cmd-drop] lot socket not connected; bytes={data?.Length ?? 0}");
                    return;
                }
                lotConn.Lot.Write(new FSOVMCommand { Data = data });
            }
            catch (Exception ex)
            {
                Program.Log($"[vm:cmd-send] {ex.GetType().Name}: {ex.Message}");
            }
        };
    }

    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));

        dispatcher.Register("walk-to", (args, ct) => Task.FromResult(WalkTo(vmHost, args)));
        dispatcher.Register("cancel", (args, ct) => Task.FromResult(Cancel(vmHost, args)));
        dispatcher.Register("queue-interaction",
            (args, ct) => Task.FromResult(QueueInteraction(vmHost, args)));
    }

    /// <summary>
    /// walk-to — travel to a tile in the current lot. Maps to <c>VMNetGotoCmd</c>. Target is
    /// <c>{x,y,level}</c> in 1/16-tile units (catalog row). <c>target_object_id</c> and
    /// <c>target_sim_id</c> resolve to the target's current position via the local VM.
    /// </summary>
    internal static CommandDispatcher.Response WalkTo(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        short tx, ty;
        sbyte level = 1;

        // Resolution order: target_sim_id → target_object_id → (x,y,level).
        var targetSim = (long?)args["target_sim_id"];
        if (targetSim.HasValue && targetSim.Value != 0)
        {
            var other = vmHost.VM.GetAvatarByPersist((uint)targetSim.Value);
            if (other == null) return CommandDispatcher.Response.Fail($"sim {targetSim} not in local VM");
            tx = other.Position.x;
            ty = other.Position.y;
            level = other.Position.Level;
        }
        else
        {
            var targetObj = (long?)args["target_object_id"];
            if (targetObj.HasValue && targetObj.Value != 0)
            {
                var ent = vmHost.VM.GetObjectById(checked((short)targetObj.Value));
                if (ent == null) return CommandDispatcher.Response.Fail($"object {targetObj} not in local VM");
                tx = ent.Position.x;
                ty = ent.Position.y;
                level = ent.Position.Level;
            }
            else
            {
                var xArg = (long?)args["x"];
                var yArg = (long?)args["y"];
                if (!xArg.HasValue || !yArg.HasValue)
                    return CommandDispatcher.Response.Fail("walk-to requires x,y OR target_object_id OR target_sim_id");
                tx = checked((short)xArg.Value);
                ty = checked((short)yArg.Value);
                var lArg = (long?)args["level"];
                if (lArg.HasValue) level = checked((sbyte)lArg.Value);
            }
        }

        // Interaction=4 "Run Here" matches the pie-menu path the UI uses (verb-catalog.md row).
        var interaction = (ushort)((long?)args["interaction"] ?? 4);
        var param0 = (short)((long?)args["param0"] ?? 0);

        var cmd = new VMNetGotoCmd
        {
            Interaction = interaction,
            Param0 = param0,
            x = tx,
            y = ty,
            level = level,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new { queued = true, x = (int)tx, y = (int)ty, level = (int)level });
    }

    /// <summary>
    /// cancel — remove a queued/running interaction from the avatar's action queue. Maps to
    /// <c>VMNetInteractionCancelCmd</c> with a specific <c>action_uid</c>, OR iterates the
    /// locally-observed queue when omitted (cancels all).
    ///
    /// The wire cannot distinguish "cancel-current" from "clear-queue" with a single PDU; the
    /// stock client fires one cancel per queue entry from UIInteractionQueue. Our default
    /// (no <c>action_uid</c>) mirrors that behaviour — one cancel per visible queued action.
    /// </summary>
    internal static CommandDispatcher.Response Cancel(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        var actionUidArg = (long?)args["action_uid"];
        int cancelled = 0;

        if (actionUidArg.HasValue)
        {
            vmHost.Driver.SendCommand(new VMNetInteractionCancelCmd
            {
                ActionUID = checked((ushort)actionUidArg.Value),
            });
            cancelled = 1;
        }
        else
        {
            // Cancel every queued action we can see locally. Engine ignores cancels for unknown
            // UIDs, so this is safe. We snapshot the queue first to avoid iterating a mutating
            // collection (same thread in practice, but defensive).
            var queueSnapshot = caller.Thread?.Queue?.ToArray();
            if (queueSnapshot != null)
            {
                foreach (var action in queueSnapshot)
                {
                    vmHost.Driver.SendCommand(new VMNetInteractionCancelCmd { ActionUID = action.UID });
                    cancelled++;
                }
            }
            if (cancelled == 0)
            {
                // Queue empty locally but caller explicitly asked for a cancel. Send a zero UID
                // so the server sees the intent (no-op if queue is empty on its side too).
                vmHost.Driver.SendCommand(new VMNetInteractionCancelCmd { ActionUID = 0 });
            }
        }

        return CommandDispatcher.Response.Success(new { cancelled });
    }

    /// <summary>
    /// queue-interaction — push an interaction onto the avatar's action queue. Maps to
    /// <c>VMNetInteractionCmd</c>. Same PDU as <c>interact-with</c> (catalog row); the
    /// distinction is semantic only — the server always queues behind the currently-running
    /// action when the queue is non-empty. Callers wanting a distinct "run now" primitive must
    /// cancel the current action first (see <see cref="Cancel"/>).
    /// </summary>
    internal static CommandDispatcher.Response QueueInteraction(HeadlessVMHost vmHost, JsonObject args)
    {
        var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
        if (caller == null) return CommandDispatcher.Response.Fail("no live avatar");

        var interactionArg = (long?)args["interaction"];
        var calleeArg = (long?)args["callee_id"];
        if (!interactionArg.HasValue) return CommandDispatcher.Response.Fail("queue-interaction requires interaction (TTAB index)");
        if (!calleeArg.HasValue || calleeArg.Value == 0) return CommandDispatcher.Response.Fail("queue-interaction requires callee_id");

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
        });
    }
}
