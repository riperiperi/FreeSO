using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FSO.Common.Serialization;
using FSO.Server.Clients;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Authorization;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.SimAntics;
using Ninject;

namespace FSO.Bot.Headless;

/// <summary>
/// Headless FSO bot. Logs into the workshop server, joins a lot on the specified shard,
/// holds a live local VM for the session. Two modes:
///   default — just connect, tick, log, disconnect clean.
///   --verify-lot-join — run the full acceptance sweep for freesoexperiment-8ff; exit 0 if all
///     seven conditions pass, non-zero otherwise.
///
/// Env-driven config (all have defaults — see CLAUDE.md §Topology for workshop addresses):
///   FSO_API_URL        default http://workshop:9000/
///   FSO_USER           default baron
///   FSO_PASS           default test1234
///   FSO_SHARD          default Alphaville
///   FSO_LOT_ID         default 2    (the 'Baron's House' test lot)
///   FSO_VERSION        default Version 1.1097.1.0
///   FSO_HOLD_SECS      default 75   (>= 60 to cover session-uptime condition)
///   FSO_GAME_LOCATION  default /home/baron/projects/freeso-experiment/GameAssets/
///                      (where Content.Init(ContentMode.SERVER) will look for TSO data)
///   FSO_VM_TICK_HZ     default 60   (matches VMServerDriver BASE_TICKS_PER_SECOND)
/// </summary>
public class Program
{
    internal static bool LogToStderr = false;

    public static async Task<int> Main(string[] args)
    {
        bool verifyMode = args.Any(a => a == "--verify-lot-join");
        bool emitPerception = args.Any(a => a == "--emit-perception");

        // Critical: before *any* library code runs, reserve real stdout if we're going to emit
        // NDJSON on it. Upstream FSO libraries make bare Console.WriteLine calls (e.g. the VM
        // net driver logs "Jump to tick..." and "Tick wrong..."); those would pollute the
        // perception stream. After ReserveStdout, those lines land on stderr instead.
        if (emitPerception) PerceptionEmitter.ReserveStdout();

        var apiUrl = EnvOrDefault("FSO_API_URL", "http://workshop:9000/");
        var username = EnvOrDefault("FSO_USER", "baron");
        var password = EnvOrDefault("FSO_PASS", "test1234");
        var shardName = EnvOrDefault("FSO_SHARD", "Alphaville");
        // FSO_LOT_LOCATION accepts hex (0x...) or decimal. Default 0 means "use avatar's current
        // LotLocation". The FindLotRequest packet takes a *location code*, not a DB lot_id —
        // see FSO.Server/Servers/City/Domain/LotAllocations.cs which calls GetByLocation on it.
        var targetLotLocationStr = EnvOrDefault("FSO_LOT_LOCATION", "0");
        uint targetLotLocation = ParseLocation(targetLotLocationStr);
        var version = EnvOrDefault("FSO_VERSION", "Version 1.1097.1.0");
        var holdSecs = int.Parse(EnvOrDefault("FSO_HOLD_SECS", "75"));
        var gameLocation = EnvOrDefault("FSO_GAME_LOCATION", "/home/baron/projects/freeso-experiment/GameAssets/");
        var tickHz = int.Parse(EnvOrDefault("FSO_VM_TICK_HZ", "60"));
        var perceptionHz = double.Parse(EnvOrDefault("FSO_PERCEPTION_HZ", "1"), System.Globalization.CultureInfo.InvariantCulture);

        // In --emit-perception mode, stdout is reserved for NDJSON. Force all log writes to stderr.
        LogToStderr = emitPerception;

        Log($"config api={apiUrl} user={username} shard={shardName} lot_location={(targetLotLocation == 0 ? "auto" : "0x" + targetLotLocation.ToString("X"))} hold={holdSecs}s verify={verifyMode} emit-perception={emitPerception} perception_hz={perceptionHz}");
        Log($"config gameLocation={gameLocation} tickHz={tickHz}");

        // 1. Content init (headless/server mode — same path as FSO.Server).
        try
        {
            FSO.SimAntics.VMContext.InitVMConfig(false);
            FSO.Content.Content.Init(gameLocation, FSO.Content.ContentMode.SERVER);
            Log($"content-init: ok ({gameLocation})");
        }
        catch (Exception e)
        {
            Log($"content-init: FAILED — {e.GetType().Name}: {e.Message}");
            if (verifyMode) PrintVerifyReport(new VerifyResult { ContentInit = false });
            return 10;
        }

        // 2. Auth + city-select (same as 5f0).
        var auth = new AuthClient(apiUrl);
        var authResult = auth.Authenticate(new AuthRequest
        {
            Username = username,
            Password = password,
            ServiceID = "2",
            Version = version,
            ClientID = "freeso-bot"
        });
        if (authResult == null || !authResult.Valid)
        {
            Log($"auth FAILED valid={authResult?.Valid} reasonText={authResult?.ReasonText}");
            return 1;
        }
        Log($"auth OK");

        var city = new CityClient(apiUrl);
        var ic = city.InitialConnectServlet(new InitialConnectServletRequest { Ticket = authResult.Ticket, Version = version });
        if (ic.Status != InitialConnectServletResultType.Authorized)
        {
            Log($"initial-connect NOT authorized: code={ic.Error?.Code}");
            return 2;
        }

        var avatars = city.AvatarDataServlet();
        if (avatars.Count == 0) { Log("no avatars"); return 3; }
        var avatar = avatars.FirstOrDefault(a => a.ShardName == shardName) ?? avatars[0];
        Log($"selected avatar id={avatar.ID} name={avatar.Name} lot_id={avatar.LotId} lot_location=0x{avatar.LotLocation:X} lot_name={avatar.LotName}");

        if (targetLotLocation == 0)
        {
            if (!avatar.LotLocation.HasValue || avatar.LotLocation.Value == 0)
            {
                Log("avatar has no home lot and FSO_LOT_LOCATION unset — cannot continue");
                return 3;
            }
            targetLotLocation = avatar.LotLocation.Value;
            Log($"using avatar's home lot location 0x{targetLotLocation:X}");
        }

        var shardResp = city.ShardSelectorServlet(new ShardSelectorServletRequest
        {
            ShardName = shardName,
            AvatarID = avatar.ID.ToString()
        });
        Log($"shard-selector addr={shardResp.Address} playerId={shardResp.PlayerID} avatarId={shardResp.AvatarID}");

        // 3. Build the two Aries sessions (city + lot). Both need the same Ninject wiring for the
        // protocol codec. Upstream keeps the two as distinct singletons; we do the same but inline.
        var kernel = new StandardKernel();
        kernel.Bind<IModelSerializer>().ToConstant(new ModelSerializer());
        kernel.Bind<ISerializationContext>().To<SerializationContext>().InSingletonScope();
        kernel.Bind<AriesProtocolDecoder>().ToSelf().InSingletonScope();
        kernel.Bind<AriesProtocolEncoder>().ToSelf().InSingletonScope();

        var cityAries = new AriesClient(kernel);
        var lotAries = new AriesClient(kernel);

        var cityListener = new CityListener
        {
            ShardResp = shardResp,
            SendClientOnline = EnvOrDefault("FSO_SEND_CLIENT_ONLINE", "1") == "1"
        };
        cityAries.AddSubscriber(cityListener);

        var verify = new VerifyResult { ContentInit = true };

        // 4. VM host. Created here (not lazily) so catchup ticks have somewhere to land as soon
        // as the lot socket flips to LotCommandStream.
        var vmHost = new HeadlessVMHost(avatar.ID);
        verify.VmInitOk = true;

        // 5. Lot connection. Every inbound lot packet is fed through its MessageReceived; the
        // onLotPacket callback also forwards VM data packets into the VM host queue.
        var lotConn = new HeadlessLotConnection(cityAries, lotAries, msg =>
        {
            switch (msg)
            {
                case FSOVMTickBroadcast tick:
                    vmHost.EnqueueBroadcastTick(tick);
                    break;
                case FSOVMDirectToClient direct:
                    vmHost.EnqueueDirect(direct);
                    break;
            }
        });

        // 6. Connect to city.
        Log($"aries(city) → {shardResp.Address}");
        cityAries.Connect(shardResp.Address);

        // Wait for city HostOnline + ClientOnlinePDU — signalled when CityListener has sent
        // ClientOnlinePDU. Reuses same state signal 5f0 established.
        if (!await cityListener.WaitForClientOnlineAck(TimeSpan.FromSeconds(20)))
        {
            Log("city connect: timed out waiting for HostOnlinePDU");
            return 4;
        }
        verify.CityConnected = true;
        Log("city connect: ok");

        // 7. Kick off the lot-join. The regulator drives everything from here.
        // FSO servers sometimes hold a stale avatar/lot claim for a few seconds after a prior
        // session ends (observed: after ClientByePDU, the next join can be rejected with
        // "could not find session for userID" + ServerByePDU). Retry once with a back-off rather
        // than propagate that racy failure up to the test harness.
        int maxJoinAttempts = int.Parse(EnvOrDefault("FSO_LOT_JOIN_ATTEMPTS", "3"));
        bool joinedStream = false;
        for (int attempt = 1; attempt <= maxJoinAttempts; attempt++)
        {
            Log($"lot join attempt {attempt}/{maxJoinAttempts}");
            lotConn.JoinLot(targetLotLocation);

            var joinTimeout = TimeSpan.FromSeconds(45);
            using var joinCts = new CancellationTokenSource(joinTimeout);
            try
            {
                joinedStream = await lotConn.WaitForCommandStream(joinCts.Token);
            }
            catch (OperationCanceledException)
            {
                Log($"lot join attempt {attempt}: TIMED OUT after {joinTimeout.TotalSeconds:F0}s in state {lotConn.State}");
                joinedStream = false;
            }

            if (joinedStream) break;

            if (attempt < maxJoinAttempts)
            {
                Log($"lot join attempt {attempt} failed; resetting for retry");
                lotConn.ResetForRetry();
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        if (!joinedStream)
        {
            Log($"lot join: failed after {maxJoinAttempts} attempts; final state {lotConn.State}");
            await TryCleanDisconnect(lotConn, cityAries);
            verify.LotShard = shardName;
            verify.LotLocation = targetLotLocation;
            verify.LotDbId = avatar.LotId ?? 0;
            if (verifyMode) PrintVerifyReport(verify);
            return 6;
        }

        verify.LotJoined = true;
        verify.LotShard = shardName;
        verify.LotLocation = targetLotLocation;
        verify.LotDbId = avatar.LotId ?? 0;
        Log("lot join: ok (command stream live)");

        // 7b. If we're emitting perception, build the projector + wire it to VM.OnDialog now.
        PerceptionProjector projector = null;
        if (emitPerception)
        {
            projector = new PerceptionProjector(avatar.ID, shardName);
            projector.AttachTo(vmHost.VM);
            projector.OnDialogEvent += evt =>
            {
                try
                {
                    PerceptionEmitter.EmitLine(PerceptionEmitter.SerializeEvent(evt, "dialog"));
                }
                catch (Exception ex)
                {
                    Log($"[perception] dialog emit failed: {ex.GetType().Name}: {ex.Message}");
                }
            };
            Log($"perception projector attached (hz={perceptionHz})");
        }

        // 8. Tick loop. The real client ticks at FSOEnvironment.RefreshRate (60Hz). The driver
        // will throttle itself based on buffer size; we just need to call Tick frequently enough
        // that the buffered server ticks get drained before the session-uptime window expires.
        var sessionStart = DateTime.UtcNow;
        var deadline = sessionStart.AddSeconds(holdSecs);
        var tickInterval = TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, tickHz));
        var perceptionInterval = TimeSpan.FromMilliseconds(1000.0 / Math.Max(0.1, perceptionHz));
        DateTime nextPerceptionTick = DateTime.UtcNow + perceptionInterval;
        int perceptionTickCount = 0;

        int reportBroadcastThreshold = 10;
        bool reportedTickMilestone = false;

        while (DateTime.UtcNow < deadline)
        {
            var loopStart = DateTime.UtcNow;
            vmHost.Tick();

            if (!reportedTickMilestone &&
                lotConn.VMTickBroadcastCount >= reportBroadcastThreshold)
            {
                Log($"vm: reached {lotConn.VMTickBroadcastCount} tick broadcasts; entities={vmHost.EntityCount} avatars={vmHost.AvatarCount}");
                reportedTickMilestone = true;
            }

            if (!lotAries.IsConnected && lotConn.State != HeadlessLotConnection.LotState.LotCommandStream)
            {
                Log($"lot: socket went away in state {lotConn.State} — bailing out of hold loop");
                break;
            }

            // Perception tick. Gated to not starve the VM tick; runs at FSO_PERCEPTION_HZ.
            if (projector != null && DateTime.UtcNow >= nextPerceptionTick)
            {
                try
                {
                    var tick = projector.Build(vmHost.VM);
                    if (tick != null)
                    {
                        PerceptionEmitter.EmitLine(PerceptionEmitter.Serialize(tick));
                        perceptionTickCount++;
                    }
                }
                catch (Exception ex)
                {
                    Log($"[perception] tick build failed: {ex.GetType().Name}: {ex.Message}");
                }
                nextPerceptionTick = DateTime.UtcNow + perceptionInterval;
            }

            var elapsed = DateTime.UtcNow - loopStart;
            var sleep = tickInterval - elapsed;
            if (sleep > TimeSpan.Zero) await Task.Delay(sleep);
        }

        var uptime = DateTime.UtcNow - sessionStart;
        verify.SessionUptimeSecs = uptime.TotalSeconds;
        verify.VmTickBroadcastCount = lotConn.VMTickBroadcastCount;
        verify.VmDirectCount = lotConn.VMDirectToClientCount;
        verify.VmEntityCount = vmHost.EntityCount;
        verify.AvatarCount = vmHost.AvatarCount;

        var snap = vmHost.SnapshotAvatar();
        if (snap != null)
        {
            verify.AvatarMotives = snap.Motives;
            verify.AvatarPersistId = snap.PersistId;
            verify.AvatarName = snap.Name;
            Log($"avatar snapshot persist={snap.PersistId} name={snap.Name} motives={JsonSerializer.Serialize(snap.Motives)}");
        }
        else
        {
            Log("avatar snapshot: no avatar found in local VM");
        }

        // 9. Clean disconnect.
        verify.CleanDisconnect = await TryCleanDisconnect(lotConn, cityAries);

        // 10. Report.
        if (verifyMode)
        {
            PrintVerifyReport(verify);
            return verify.AllPassed ? 0 : 20;
        }
        else
        {
            Log($"hold elapsed; tickBroadcasts={lotConn.VMTickBroadcastCount} entities={vmHost.EntityCount} uptime={uptime.TotalSeconds:F1}s perceptionTicks={perceptionTickCount}");
            return 0;
        }
    }

    private static async Task<bool> TryCleanDisconnect(HeadlessLotConnection lotConn, AriesClient cityAries)
    {
        bool ok = false;
        try
        {
            lotConn.Disconnect();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            ok = await lotConn.WaitForDisconnected(cts.Token);
        }
        catch (Exception e)
        {
            Log($"clean-disconnect: {e.GetType().Name}: {e.Message}");
        }
        try { cityAries.Disconnect(); } catch { }
        await Task.Delay(300);
        return ok;
    }

    private static uint ParseLocation(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToUInt32(s.Substring(2), 16);
        return uint.Parse(s);
    }

    static string EnvOrDefault(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { } v && v.Length > 0 ? v : fallback;

    internal static void Log(string s)
    {
        var line = $"[{DateTime.UtcNow:HH:mm:ss.fff}] {s}";
        if (LogToStderr) Console.Error.WriteLine(line);
        else Console.WriteLine(line);
    }

    private static void PrintVerifyReport(VerifyResult v)
    {
        // Machine-scannable, one "<key>: <value>" per line. The integration test greps for these.
        Console.WriteLine("=== verify-lot-join ===");
        Console.WriteLine($"connected-to-city: {(v.CityConnected ? "ok" : "fail")}");
        Console.WriteLine($"lot-join: {(v.LotJoined ? $"ok (lot_id={v.LotDbId}, shard={v.LotShard})" : $"fail (lot_id={v.LotDbId}, shard={v.LotShard})")}");
        Console.WriteLine($"lot-location: 0x{v.LotLocation:X}");
        Console.WriteLine($"vm-tick-count: {v.VmTickBroadcastCount}");
        Console.WriteLine($"vm-direct-count: {v.VmDirectCount}");
        Console.WriteLine($"vm-entity-count: {v.VmEntityCount}");
        Console.WriteLine($"avatar-motives-present: {((v.AvatarMotives != null && v.AvatarMotives.Count > 0) ? "true" : "false")}");
        Console.WriteLine($"session-uptime: {v.SessionUptimeSecs:F1}s");
        Console.WriteLine($"clean-disconnect: {(v.CleanDisconnect ? "ok" : "fail")}");
        Console.WriteLine($"all-passed: {(v.AllPassed ? "true" : "false")}");
        if (v.AvatarMotives != null && v.AvatarMotives.Count > 0)
        {
            Console.WriteLine($"avatar-persist-id: {v.AvatarPersistId}");
            Console.WriteLine($"avatar-name: {v.AvatarName}");
            Console.WriteLine($"motives-json: {JsonSerializer.Serialize(v.AvatarMotives)}");
        }
        Console.WriteLine("=== end verify-lot-join ===");
    }

    internal class VerifyResult
    {
        public bool ContentInit;
        public bool VmInitOk;
        public bool CityConnected;
        public bool LotJoined;
        public string LotShard;
        public uint LotDbId;
        public uint LotLocation;
        public int VmTickBroadcastCount;
        public int VmDirectCount;
        public int VmEntityCount;
        public int AvatarCount;
        public double SessionUptimeSecs;
        public bool CleanDisconnect;
        public Dictionary<string, short> AvatarMotives;
        public uint AvatarPersistId;
        public string AvatarName;

        public bool AllPassed =>
            ContentInit && VmInitOk && CityConnected && LotJoined &&
            VmTickBroadcastCount >= 10 &&
            VmEntityCount > 0 &&
            (AvatarMotives != null && AvatarMotives.Count > 0) &&
            SessionUptimeSecs >= 60.0 &&
            CleanDisconnect;
    }
}

/// <summary>
/// City-server Aries listener. Same contract as 5f0's Listener class — responds to
/// RequestClientSession + HostOnlinePDU, then exposes a signal when the city side is fully
/// logged in. Lot-server packet handling is done by <see cref="HeadlessLotConnection"/>.
/// </summary>
public class CityListener : IAriesMessageSubscriber, IAriesEventSubscriber
{
    public ShardSelectorServletResponse ShardResp;
    public bool SendClientOnline = true;
    public int PacketCount;

    private readonly TaskCompletionSource<bool> _clientOnlineAck =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<bool> WaitForClientOnlineAck(TimeSpan timeout)
    {
        var delay = Task.Delay(timeout);
        var first = await Task.WhenAny(_clientOnlineAck.Task, delay);
        return first == _clientOnlineAck.Task && _clientOnlineAck.Task.Result;
    }

    public void MessageReceived(AriesClient c, object m)
    {
        Interlocked.Increment(ref PacketCount);
        Program.Log($"[city:recv] {m.GetType().Name}");

        if (m is RequestClientSession)
        {
            c.Write(new RequestClientSessionResponse
            {
                Password = ShardResp.Ticket,
                User = ShardResp.AvatarID
            });
        }
        else if (m is HostOnlinePDU host)
        {
            if (SendClientOnline)
            {
                c.Write(
                    new ClientOnlinePDU(),
                    new SetIgnoreListPDU { PlayerIds = new List<uint>() }
                );
            }
            _clientOnlineAck.TrySetResult(true);
        }
        else if (m is ServerByePDU)
        {
            Program.Log("[city:recv] ServerByePDU");
        }
    }

    public void SessionCreated(AriesClient c) => Program.Log("[city:evt] SessionCreated");
    public void SessionOpened(AriesClient c) => Program.Log("[city:evt] SessionOpened");
    public void SessionClosed(AriesClient c) => Program.Log("[city:evt] SessionClosed");
    public void SessionIdle(AriesClient c) => Program.Log("[city:evt] SessionIdle");
    public void InputClosed(AriesClient c) => Program.Log("[city:evt] InputClosed");
}
