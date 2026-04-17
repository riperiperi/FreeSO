using System;
using System.Threading;
using System.Threading.Tasks;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;

namespace FSO.Bot.Headless;

/// <summary>
/// Headless host for an FSO local VM. Mirrors what
/// <c>tso.client/UI/Screens/CoreGameScreen.InitializeLot()</c> does, minus world/graphics
/// construction, UI panels, and asset streaming. Uses the same <c>VMClientDriver</c> the real
/// client uses, but with <c>VM.UseWorld=false</c> and a null <c>LotView.World</c> — the pattern
/// established by <c>FSO.Server/Servers/Lot/Domain/LotContainer.cs</c> for server-side VMs.
///
/// Consumes inbound FSOVMTickBroadcast / FSOVMDirectToClient packets by forwarding them to the
/// driver as VMNetMessage (the CoreGameScreenController pattern). The owning task calls
/// <see cref="Tick"/> on a timer so the driver can flush its internal tick queue into the VM.
/// </summary>
public class HeadlessVMHost : IDisposable
{
    public VM VM { get; }
    public VMClientDriver Driver { get; }
    public uint MyAvatarPersistId { get; set; }

    private readonly object _tickLock = new();
    private bool _disposed;

    public HeadlessVMHost(uint myAvatarPersistId)
    {
        MyAvatarPersistId = myAvatarPersistId;

        // Headless — no world, no graphics, no UI. Same flag the server uses.
        VM.UseWorld = false;

        Driver = new VMClientDriver(OnClientDriverStateChange);
        Driver.OnShutdown += OnClientDriverShutdown;
        // The bot is a read-only observer in d87-a. Outbound commands (walk-to, interact-with) are
        // plumbed in follow-up items (d87-d-*). For now we drop any OnClientCommand payload the
        // driver might try to send (e.g. keepalive pings) so it never blocks the tick loop.
        Driver.OnClientCommand += OnDriverClientCommand;

        VM = new VM(new VMContext(null), Driver, new VMNullHeadlineProvider());
        VM.MyUID = MyAvatarPersistId;
        VM.Init();
    }

    /// <summary>Feed an inbound server broadcast tick into the client driver's queue.</summary>
    public void EnqueueBroadcastTick(FSOVMTickBroadcast broadcast)
    {
        if (_disposed) return;
        var type = broadcast.Catchup ? VMNetMessageType.CatchupTick : VMNetMessageType.BroadcastTick;
        Driver.ServerMessage(new VMNetMessage(type, broadcast.Data));
    }

    /// <summary>Feed a direct-to-client packet (e.g. dialog fire) into the driver.</summary>
    public void EnqueueDirect(FSOVMDirectToClient direct)
    {
        if (_disposed) return;
        Driver.ServerMessage(new VMNetMessage(VMNetMessageType.Direct, direct.Data));
    }

    /// <summary>Run one VM tick. Safe to call from a timer; serialized internally.</summary>
    public bool Tick()
    {
        if (_disposed) return false;
        lock (_tickLock)
        {
            try
            {
                return Driver.Tick(VM);
            }
            catch (Exception e)
            {
                Program.Log($"[vm:tick-error] {e.GetType().Name}: {e.Message}");
                return false;
            }
        }
    }

    /// <summary>Snapshot of avatar + motive state for --verify-lot-join.</summary>
    public AvatarSnapshot SnapshotAvatar()
    {
        // Prefer MyAvatar (by persist id). Fall back to any avatar for the pre-StateSync window
        // where entity IDs may not yet correlate to persist ids (shouldn't happen after catchup
        // completes, but defensive).
        VMAvatar target = null;
        if (MyAvatarPersistId != 0)
        {
            target = VM.GetAvatarByPersist(MyAvatarPersistId);
        }
        if (target == null)
        {
            foreach (var e in VM.Entities)
            {
                if (e is VMAvatar av)
                {
                    target = av;
                    break;
                }
            }
        }

        if (target == null) return null;

        var snap = new AvatarSnapshot
        {
            PersistId = target.PersistID,
            ObjectId = target.ObjectID,
            Name = target.Name,
        };

        // Motives. See tso.simantics/Model/VMMotive.cs. UnusedPhysical/UnusedMental/UnusedStress
        // are intentionally omitted — garbage slots per source comments.
        foreach (var m in new[] {
            VMMotive.Mood, VMMotive.Energy, VMMotive.Comfort, VMMotive.Hunger,
            VMMotive.Hygiene, VMMotive.Bladder, VMMotive.Social, VMMotive.Fun,
            VMMotive.Room, VMMotive.SleepState, VMMotive.HappyLife, VMMotive.HappyWeek,
            VMMotive.HappyDay,
        })
        {
            snap.Motives[m.ToString()] = target.GetMotiveData(m);
        }
        return snap;
    }

    public int EntityCount
    {
        get
        {
            lock (_tickLock) return VM.Entities?.Count ?? 0;
        }
    }

    public int AvatarCount
    {
        get
        {
            lock (_tickLock)
            {
                int n = 0;
                if (VM.Entities != null)
                {
                    foreach (var e in VM.Entities) if (e is VMAvatar) n++;
                }
                return n;
            }
        }
    }

    private void OnClientDriverStateChange(int state, float progress)
    {
        // state: 1=connected, 2=catching up, 3=ready (matches VMClientDriver). Progress is 0..1.
        Program.Log($"[vm:state] state={state} progress={progress:F3}");
    }

    private void OnClientDriverShutdown(VMCloseNetReason reason)
    {
        Program.Log($"[vm:shutdown] reason={reason}");
    }

    private void OnDriverClientCommand(byte[] data)
    {
        // Read-only bot — drop any outbound command bytes. A later item (d87-d-*) will wire these
        // into FSOVMCommand PDUs on the lot Aries socket.
        Program.Log($"[vm:cmd-drop] outbound cmd bytes={data?.Length ?? 0} (read-only bot)");
    }

    public void Dispose()
    {
        _disposed = true;
    }

    public class AvatarSnapshot
    {
        public uint PersistId;
        public short ObjectId;
        public string Name;
        public System.Collections.Generic.Dictionary<string, short> Motives = new();
    }
}
