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
/// Env-driven config (see CLAUDE.md §Topology for workshop addresses):
///   FSO_API_URL        default http://workshop:9000/
///   FSO_USER           REQUIRED — no default (freesoexperiment-bbc)
///   FSO_PASS           REQUIRED — no default (freesoexperiment-bbc)
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

    // Shutdown coordination. Every exit path routes through one of two mechanisms:
    //
    //   (a) Cooperative: SIGINT (Console.CancelKeyPress) cancels _shutdownCts, the hold loop
    //       observes cancellation, exits naturally, and step 9 runs TryCleanDisconnect on the
    //       main thread — same code path as normal hold-elapsed exit. This is the preferred
    //       path; it gives Mina's IO thread the same runtime treatment as a clean exit.
    //
    //   (b) Last-resort: ProcessExit (runtime-initiated shutdown — SIGTERM on Linux, etc.) and
    //       UnhandledException fire _disconnectCallback synchronously on the calling thread
    //       with an outer time cap. These paths cannot fall back to cooperative cancellation
    //       because the runtime is already tearing the AppDomain down.
    //
    // The Interlocked guard (_disconnectFired) makes (b) idempotent with step 9's own call so
    // ClientByePDU is sent at most once.
    private static readonly CancellationTokenSource _shutdownCts = new();
    private static Func<Task> _disconnectCallback;
    private static int _disconnectFired; // 0 = not yet, 1 = in progress / done

    internal static CancellationToken ShutdownToken => _shutdownCts.Token;

    private static void EnsureCleanDisconnect(string reason)
    {
        if (Interlocked.Exchange(ref _disconnectFired, 1) != 0) return;
        var cb = Volatile.Read(ref _disconnectCallback);
        if (cb == null) return; // session never reached the point where disconnect is meaningful
        try
        {
            Log($"[shutdown] {reason} — sending ClientByePDU");
            // Bound the outer wait to 8s — inside the runtime's ProcessExit grace window.
            // Internally TryCleanDisconnect has a 10s WaitForDisconnected cap and the Mina write
            // flush via HeadlessLotConnection.Disconnect has its own Thread.Sleep.
            var task = cb();
            task.Wait(TimeSpan.FromSeconds(8));
        }
        catch (Exception ex)
        {
            Log($"[shutdown] disconnect callback threw: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static async Task<int> Main(string[] args)
    {
        // Install shutdown hooks BEFORE any networked work. SIGINT (Ctrl-C / docker stop's SIGTERM
        // equivalent reaching the console), AppDomain.ProcessExit (runtime-initiated shutdown,
        // incl. SIGTERM on Linux .NET), and UnhandledException all route to EnsureCleanDisconnect.
        // Each fires at most once due to the Interlocked guard; if the callback hasn't been wired
        // yet (session never reached lot-command-stream), the call is a no-op.
        Console.CancelKeyPress += (sender, e) =>
        {
            // Cancel the default "kill process now" behavior — we want the hold loop to exit
            // cooperatively and flow through the normal step-9 disconnect path on the main
            // thread. That path is identical to a clean hold-elapsed exit (Mina IO thread still
            // fully healthy), so the server reliably logs SESSION-CLOSED.
            e.Cancel = true;
            if (_shutdownCts.IsCancellationRequested) return;
            Log("[shutdown] SIGINT received — cancelling shutdown CTS; hold loop will exit cooperatively");
            try { _shutdownCts.Cancel(); } catch (Exception ex) { Log($"[shutdown] CTS cancel threw: {ex.Message}"); }
        };
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            EnsureCleanDisconnect("ProcessExit (runtime shutdown)");
        };
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            EnsureCleanDisconnect($"UnhandledException: {ex?.GetType().Name}: {ex?.Message}");
        };

        try
        {
            return await MainBody(args);
        }
        finally
        {
            // Defense-in-depth. If Main is returning normally, step 9's TryCleanDisconnect
            // already fired and set _disconnectFired — this call is a no-op. If an exception
            // bubbled out of MainBody past any inner handling, this flushes the PDU before the
            // CLR starts tearing down the AppDomain (ProcessExit will then also no-op).
            EnsureCleanDisconnect("Main try/finally (normal or exceptional exit)");
        }
    }

    private static async Task<int> MainBody(string[] args)
    {
        bool verifyMode = args.Any(a => a == "--verify-lot-join");
        bool emitPerception = args.Any(a => a == "--emit-perception");

        // Critical: before *any* library code runs, reserve real stdout if we're going to emit
        // NDJSON on it. Upstream FSO libraries make bare Console.WriteLine calls (e.g. the VM
        // net driver logs "Jump to tick..." and "Tick wrong..."); those would pollute the
        // perception stream. After ReserveStdout, those lines land on stderr instead.
        if (emitPerception) PerceptionEmitter.ReserveStdout();

        var apiUrl = EnvOrDefault("FSO_API_URL", "http://workshop:9000/");
        // FSO_USER and FSO_PASS must be set explicitly — no defaults. The previous
        // `"baron"` / `"test1234"` defaults (freesoexperiment-bbc) silently authenticated
        // as the workshop admin when unset; worse, they reinforced a weak credential as
        // the de-facto standard. Fail fast instead: operators and CI set both explicitly.
        var username = Environment.GetEnvironmentVariable("FSO_USER");
        var password = Environment.GetEnvironmentVariable("FSO_PASS");
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Console.Error.WriteLine("ERROR: FSO_USER and FSO_PASS must be set (no defaults — freesoexperiment-bbc)");
            return 11;
        }
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
        Log($"shutdown-hooks installed: CancelKeyPress + ProcessExit + UnhandledException → EnsureCleanDisconnect");

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

        // 5a. Wire the VMClientDriver's outbound command stream to the lot Aries socket. This
        // replaces the read-only drop behaviour from d87-a so outbound VMNet*Cmd PDUs (walk-to,
        // queue-interaction, cancel — freesoexperiment-b9c) reach the server. Must happen before
        // the VM starts ticking.
        MovementHandlers.WireOutboundVMCommands(vmHost, lotConn);

        // 5b. Wire the shutdown hook now that lotConn + cityAries exist. Any failure after this
        // point — normal exit, SIGINT, unhandled exception, runtime shutdown — routes through
        // EnsureCleanDisconnect, which invokes TryCleanDisconnect. The Interlocked guard in
        // EnsureCleanDisconnect makes it idempotent so the happy-path TryCleanDisconnect call
        // at step 9 and any concurrent signal handler do not race.
        Volatile.Write(ref _disconnectCallback, () => TryCleanDisconnect(lotConn, cityAries));

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
            // Same guard flip as step 9 — this is a deliberate clean shutdown, take ownership of
            // the PDU send path so the finally/ProcessExit hooks don't double-fire.
            Interlocked.Exchange(ref _disconnectFired, 1);
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

        // 7c. Start the IPC command dispatcher. Stdin is the inbound channel (NDJSON commands
        // sidecar→bot). When --emit-perception is on, stdout is already reserved for NDJSON and
        // responses flow back through PerceptionEmitter.EmitLine, interleaving cleanly with
        // perception/dialog events. Load-bearing for freesoexperiment-b9c and all follow-on
        // verb-family items (d87-d-*).
        CommandDispatcher dispatcher = null;
        if (emitPerception)
        {
            dispatcher = new CommandDispatcher();
            MovementHandlers.RegisterAll(dispatcher, vmHost);
            QueryHandlers.RegisterAll(dispatcher, vmHost, shardName);
            InteractionHandlers.RegisterAll(dispatcher, vmHost);
            SocialHandlers.RegisterAll(dispatcher, vmHost);
            // IM family (freesoexperiment-7d8) — city-socket InstantMessage PDU. Distinct from
            // speak (which is lot-socket VMNetChatCmd): IM rides cityAries directly, and the
            // server's MessagingHandler either forwards to target or replies FAILURE_ACK.
            IMHandlers.RegisterAll(dispatcher, cityAries, avatar.ID);
            // Avatar family (freesoexperiment-b88): change-outfit (lot socket VMNetSetOutfitCmd)
            // + change-description (city socket DataServiceWrapperPDU).
            AvatarHandlers.RegisterAll(dispatcher, vmHost, cityAries);
            // Navigation family (freesoexperiment-a61). go-home already-home fast path;
            // visit-lot returns deferred marker (cross-lot transition → -ca0). find-avatar is
            // a full FindAvatarRequest wire PDU on the city socket.
            NavigationHandlers.RegisterAll(dispatcher, vmHost, cityAries, targetLotLocation);
            // Wire chat events into the perception projector so speak's server-round-tripped
            // echo surfaces as a recent_events entry (kind=chat). Ground-source truth for the
            // verb-social integration test — a bare VMNetChatCmd ACK does not prove wire effect.
            if (projector != null) SocialHandlers.WireChatPerception(vmHost, projector);
            // Wire inbound IM packets on city socket into recent_events (kind=im). Ground-source
            // truth for verb-im: a self-IM round-trip proves the city-socket path works even in
            // single-session tests (target = own avatar id ⇒ server echoes back).
            if (projector != null) IMHandlers.WireIMPerception(cityListener, projector);
            dispatcher.Start();
            Log($"ipc: command dispatcher started (stdin); ops registered: walk-to, cancel, queue-interaction, query-self, query-nearby, query-lot, query-relationships, query-inventory, interact-with, cancel-interaction, query-pie-menu, speak, be-friendly, tell-joke, flirt, be-mean, give-gift, instant-message, change-outfit, change-description, go-home, visit-lot, find-avatar");
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
            // Cooperative SIGINT path: when _shutdownCts is cancelled (by CancelKeyPress), exit
            // the hold loop immediately and proceed to step 9's clean disconnect.
            if (_shutdownCts.IsCancellationRequested)
            {
                Log($"lot: shutdown requested — exiting hold loop at uptime={(DateTime.UtcNow - sessionStart).TotalSeconds:F1}s");
                break;
            }

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

        // 8b. Stop the IPC dispatcher (cancels the stdin read loop). Idempotent.
        dispatcher?.Stop();

        // 9. Clean disconnect (happy path). Flip the guard *before* the call so any concurrent
        // signal handler (rare but possible — user hits Ctrl-C just as the hold loop ends) sees
        // _disconnectFired=1 and becomes a no-op, and the finally/ProcessExit below are also
        // no-ops. This single call owns the PDU send on the normal exit path.
        Interlocked.Exchange(ref _disconnectFired, 1);
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
            // If we exited because of SIGINT (cooperative), report exit code 130 (128 + SIGINT(2))
            // per bash convention so wrapping scripts can distinguish. Disconnect succeeded on
            // this path — the same clean-disconnect path as a normal hold-elapsed exit.
            return _shutdownCts.IsCancellationRequested ? 130 : 0;
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

    /// <summary>
    /// Observer for inbound <see cref="InstantMessage"/> packets on the city socket. The IM
    /// verb family (freesoexperiment-7d8) subscribes here to push inbound IMs (including the
    /// server's echo of a self-IM) into perception's <c>recent_events</c> stream. Kept as a
    /// public event so additional consumers (e.g. a future IM-history cache) can chain without
    /// touching CityListener internals.
    /// </summary>
    public event Action<AriesClient, InstantMessage> OnInstantMessage;

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
        else if (m is InstantMessage im)
        {
            // IM verb family (freesoexperiment-7d8). Fan out to any registered observer;
            // IMHandlers.WireIMPerception is the canonical consumer that pushes the IM into
            // recent_events for the agent to see.
            try { OnInstantMessage?.Invoke(c, im); }
            catch (Exception ex) { Program.Log($"[city:recv] OnInstantMessage handler threw: {ex.GetType().Name}: {ex.Message}"); }
        }
    }

    public void SessionCreated(AriesClient c) => Program.Log("[city:evt] SessionCreated");
    public void SessionOpened(AriesClient c) => Program.Log("[city:evt] SessionOpened");
    public void SessionClosed(AriesClient c) => Program.Log("[city:evt] SessionClosed");
    public void SessionIdle(AriesClient c) => Program.Log("[city:evt] SessionIdle");
    public void InputClosed(AriesClient c) => Program.Log("[city:evt] InputClosed");
}
