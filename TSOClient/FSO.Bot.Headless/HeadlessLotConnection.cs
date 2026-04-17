using System;
using System.Threading;
using System.Threading.Tasks;
using FSO.Server.Clients;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron.Packets;

namespace FSO.Bot.Headless;

/// <summary>
/// Minimal port of <c>TSOClient/tso.client/Regulators/LotConnectionRegulator.cs</c> with UI,
/// Ninject, and GameThread dependencies stripped. Drives the second Aries socket (lot server,
/// typically workshop:34101 for this experiment) through the lot-join handshake.
///
/// State machine (matches upstream but collapsed to a few signals):
///   Disconnected -> (JoinLot) -> FindLotRequest on city -> FindLotResponse -> OpenSocket
///     -> AriesConnected -> RequestClientSession -> HostOnline -> ClientOnlinePDU
///     -> PartiallyConnected -> first FSOVMTickBroadcast -> LotCommandStream.
///
/// On Disconnect():
///   LotCommandStream -> send ClientByePDU -> Disconnect socket -> Disconnected.
///
/// No reconnect logic (OQ-11 punted to a follow-up item — the item spec marks OQ-11 best-effort).
/// </summary>
public class HeadlessLotConnection : IAriesMessageSubscriber, IAriesEventSubscriber
{
    public enum LotState
    {
        Disconnected,
        AwaitingFindLotResponse,
        OpeningSocket,
        RequestedClientSession,
        HostOnline,
        PartiallyConnected,
        LotCommandStream,
        Disconnecting,
    }

    private readonly AriesClient _city;
    public AriesClient Lot { get; }
    private readonly Action<object> _onLotPacket;

    private uint _targetLotId;
    private FindLotResponse _findLotResponse;
    private LotState _state = LotState.Disconnected;
    private readonly object _stateLock = new();

    // TCS are slots so the retry path can swap in fresh ones without leaking old continuations.
    private TaskCompletionSource<bool> _commandStreamTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource<bool> _disconnectedTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int VMTickBroadcastCount;
    public int VMDirectToClientCount;

    public LotState State
    {
        get { lock (_stateLock) return _state; }
    }

    public HeadlessLotConnection(AriesClient cityClient, AriesClient lotClient, Action<object> onLotPacket)
    {
        _city = cityClient ?? throw new ArgumentNullException(nameof(cityClient));
        Lot = lotClient ?? throw new ArgumentNullException(nameof(lotClient));
        _onLotPacket = onLotPacket ?? throw new ArgumentNullException(nameof(onLotPacket));

        _city.AddSubscriber(this);
        Lot.AddSubscriber(this);
    }

    /// <summary>Wait for the connection to enter LotCommandStream (≥1 VM tick received).</summary>
    public Task<bool> WaitForCommandStream(CancellationToken ct)
    {
        Task<bool> task;
        lock (_stateLock) task = _commandStreamTcs.Task;
        return WithCancellation(task, ct);
    }

    /// <summary>Wait until the lot Aries socket is fully torn down (post-Disconnect()).</summary>
    public Task<bool> WaitForDisconnected(CancellationToken ct)
    {
        Task<bool> task;
        lock (_stateLock) task = _disconnectedTcs.Task;
        return WithCancellation(task, ct);
    }

    /// <summary>
    /// Reset the connection state after a failed join attempt so the same city session can retry.
    /// Does NOT touch the city Aries socket — only the lot-side state machine.
    /// </summary>
    public void ResetForRetry()
    {
        lock (_stateLock)
        {
            // Complete any lingering waiters as failures (they already know we failed).
            _commandStreamTcs.TrySetResult(false);
            _disconnectedTcs.TrySetResult(true);

            _commandStreamTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _disconnectedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _state = LotState.Disconnected;
            _findLotResponse = null;
            // Keep tick/direct counters across retries — they're cumulative debug telemetry.
        }
    }

    public void JoinLot(uint lotId, bool openIfClosed = true)
    {
        SetState(LotState.AwaitingFindLotResponse);
        _targetLotId = lotId;
        Program.Log($"[lot-connect] → FindLotRequest lot_id={lotId} openIfClosed={openIfClosed}");
        _city.Write(new FindLotRequest { LotId = lotId, OpenIfClosed = openIfClosed });
    }

    public void Disconnect()
    {
        var snapshot = State;
        if (snapshot == LotState.Disconnected)
        {
            _disconnectedTcs.TrySetResult(true);
            return;
        }
        SetState(LotState.Disconnecting);
        if (Lot.IsConnected)
        {
            Program.Log("[lot-connect] → ClientByePDU + socket disconnect");
            try
            {
                Lot.Write(new ClientByePDU());
            }
            catch (Exception e)
            {
                Program.Log($"[lot-connect] ClientByePDU write failed: {e.Message}");
            }
            // Give Mina's IO thread time to drain the write queue to the TCP socket before we
            // schedule the close. AriesClient.Disconnect() calls Session.Close(false) which
            // SHOULD flush pending writes, but under tight shutdown pressure (SIGINT →
            // EnsureCleanDisconnect → Environment.Exit) empirical observation shows the server
            // logs SESSION-INTERRUPTED without this: the PDU is written to Mina's queue but
            // process exit tears down before Mina's IO thread moves it to the TCP socket and
            // the FIN arrives at the server ahead of the PDU bytes.
            // 1s is generous; the happy path cost is bounded (this only runs at session end,
            // not per packet).
            Thread.Sleep(1000);
            Lot.Disconnect();
        }
        else
        {
            SetState(LotState.Disconnected);
            _disconnectedTcs.TrySetResult(true);
        }
    }

    public void MessageReceived(AriesClient client, object message)
    {
        if (client == _city)
        {
            if (message is FindLotResponse response)
            {
                HandleFindLotResponse(response);
            }
            return;
        }

        if (client != Lot) return;

        _onLotPacket(message);

        switch (message)
        {
            case RequestClientSession _:
                HandleRequestClientSession();
                break;
            case HostOnlinePDU host:
                HandleHostOnline(host);
                break;
            case FSOVMTickBroadcast tick:
                Interlocked.Increment(ref VMTickBroadcastCount);
                TransitionToCommandStream();
                break;
            case FSOVMDirectToClient direct:
                Interlocked.Increment(ref VMDirectToClientCount);
                TransitionToCommandStream();
                break;
            case ServerByePDU _:
                Program.Log("[lot-connect] ← ServerByePDU (server wants to close)");
                Disconnect();
                break;
        }
    }

    private void HandleFindLotResponse(FindLotResponse response)
    {
        Program.Log($"[lot-connect] ← FindLotResponse status={response.Status} lot_id={response.LotId} addr={response.Address} user={response.User}");
        if (response.Status != Server.Protocol.Electron.Model.FindLotResponseStatus.FOUND)
        {
            Program.Log($"[lot-connect] FindLot failed — bailing");
            SetState(LotState.Disconnected);
            _commandStreamTcs.TrySetResult(false);
            _disconnectedTcs.TrySetResult(true);
            return;
        }

        _findLotResponse = response;
        var address = TransformLotAddress(response.Address);
        var normalized = PortTransformer.TransformAddress(address);
        Program.Log($"[lot-connect] → connecting lot socket to {normalized}");
        SetState(LotState.OpeningSocket);
        Lot.Connect(normalized);
    }

    private string TransformLotAddress(string address)
    {
        if (address.StartsWith("0.0.0.0:"))
        {
            var cityEp = _city.RemoteEndPoint;
            if (cityEp == null)
            {
                throw new InvalidOperationException("City client has no remote endpoint; cannot rewrite 0.0.0.0 lot address");
            }
            return cityEp.Address.ToString() + address.Substring(7);
        }
        return address;
    }

    private void HandleRequestClientSession()
    {
        Program.Log($"[lot-connect] ← RequestClientSession; → RequestClientSessionResponse user={_findLotResponse.User}");
        SetState(LotState.RequestedClientSession);
        Lot.Write(new RequestClientSessionResponse
        {
            Password = _findLotResponse.LotServerTicket,
            User = _findLotResponse.User,
            // ServiceIdent null — no JLT transition; we're joining directly.
        });
    }

    private void HandleHostOnline(HostOnlinePDU host)
    {
        Program.Log($"[lot-connect] ← HostOnlinePDU hostVersion={host.HostVersion} bufSize={host.ClientBufSize}");
        SetState(LotState.HostOnline);
        Lot.Write(new ClientOnlinePDU());
        SetState(LotState.PartiallyConnected);
        Program.Log("[lot-connect] → ClientOnlinePDU; awaiting tick stream");
    }

    private void TransitionToCommandStream()
    {
        bool shouldSignal;
        lock (_stateLock)
        {
            if (_state == LotState.LotCommandStream) return;
            if (_state != LotState.PartiallyConnected && _state != LotState.HostOnline)
            {
                return; // ignore ticks in wrong state (e.g. already disconnecting)
            }
            _state = LotState.LotCommandStream;
            shouldSignal = true;
        }
        if (shouldSignal)
        {
            Program.Log("[lot-connect] state → LotCommandStream (tick stream live)");
            _commandStreamTcs.TrySetResult(true);
        }
    }

    private void SetState(LotState next)
    {
        LotState prev;
        lock (_stateLock) { prev = _state; _state = next; }
        if (prev != next)
        {
            Program.Log($"[lot-connect] state {prev} → {next}");
        }
    }

    public void SessionCreated(AriesClient client)
    {
        if (client != Lot) return;
        Program.Log("[lot-connect:evt] SessionCreated (lot)");
    }

    public void SessionOpened(AriesClient client) { }

    public void SessionClosed(AriesClient client)
    {
        if (client != Lot) return;
        Program.Log("[lot-connect:evt] SessionClosed (lot)");
        SetState(LotState.Disconnected);
        _commandStreamTcs.TrySetResult(false);
        _disconnectedTcs.TrySetResult(true);
    }

    public void SessionIdle(AriesClient client) { }
    public void InputClosed(AriesClient client)
    {
        if (client == Lot) SessionClosed(client);
    }

    private static async Task<bool> WithCancellation(Task<bool> task, CancellationToken ct)
    {
        if (ct == CancellationToken.None) return await task.ConfigureAwait(false);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (ct.Register(() => tcs.TrySetCanceled(ct)))
        {
            var first = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
            if (first == tcs.Task) throw new OperationCanceledException(ct);
            return await task.ConfigureAwait(false);
        }
    }
}
