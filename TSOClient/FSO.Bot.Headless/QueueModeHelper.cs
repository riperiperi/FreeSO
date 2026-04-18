using System.Linq;
using FSO.SimAntics.Engine;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Shared helper for queue-pushing verbs (walk-to, interact-with, directed socials).
/// visit-lot/go-home/walk-to-lot are scaffold-only stubs today (cross-lot transition
/// tracked as -ca0); when they ship they should adopt this helper too. Implements the
/// lizard-brain / prefrontal model:
///
/// <list type="bullet">
///   <item><c>queue</c> (default) — cancel only Mode.Idle entries (engine autopilot
///   from spawn-objects and similar BHAVs), then enqueue. Deliberate actions outrank
///   autopilot, but other deliberate actions (your own queue, social interrupts from
///   other Sims) are preserved.</item>
///   <item><c>preempt</c> — cancel ALL queue entries regardless of mode, then enqueue.
///   "Drop everything I'm doing, this instead."</item>
/// </list>
///
/// Both modes rely on server-side same-tick atomicity (VMNetDriver.InternalTick at
/// NetPlay/VMNetDriver.cs:48 executes all VMNetCommands in a tick sequentially before
/// running BHAV scripts), so cancels and the subsequent verb's PDU land in the same
/// server tick and the new action is queued ahead of any next-tick BHAV pushes.
/// </summary>
public static class QueueModeHelper
{
    public const string ModeQueue = "queue";
    public const string ModePreempt = "preempt";
    public const string DefaultMode = ModeQueue;

    /// <summary>
    /// Validate <paramref name="mode"/> and emit cancels per the mode's policy. Returns
    /// false (with <paramref name="error"/> set) only for invalid mode strings; an empty
    /// queue or no-op cancel set returns true with <paramref name="cancelled"/>=0.
    /// </summary>
    public static bool ApplyQueueMode(HeadlessVMHost vmHost, string mode, out int cancelled, out string error)
    {
        cancelled = 0;
        error = null;
        if (mode != ModeQueue && mode != ModePreempt)
        {
            error = $"queue_mode must be '{ModeQueue}' or '{ModePreempt}' (got '{mode}')";
            return false;
        }

        var snapshot = vmHost.RunUnderTickLock(() =>
        {
            var caller = vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId);
            return caller?.Thread?.Queue?.ToArray();
        });
        if (snapshot == null) return true;

        foreach (var action in snapshot)
        {
            bool shouldCancel = mode switch
            {
                ModePreempt => true,
                ModeQueue => action.Mode == VMQueueMode.Idle,
                _ => false,
            };
            if (shouldCancel)
            {
                vmHost.Driver.SendCommand(new VMNetInteractionCancelCmd { ActionUID = action.UID });
                cancelled++;
            }
        }
        return true;
    }

    /// <summary>
    /// Read the queue_mode arg from a JSON args object, defaulting to "queue" when absent.
    /// </summary>
    public static string ReadQueueMode(System.Text.Json.Nodes.JsonObject args)
    {
        var v = (string)args["queue_mode"];
        return string.IsNullOrEmpty(v) ? DefaultMode : v;
    }
}
