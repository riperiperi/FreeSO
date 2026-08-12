using System.IO;
using System.Net.WebSockets;

namespace FSO.BrowserAries;

public enum JoinStage
{
    Idle,
    CityConnecting,
    CityHandshake,
    CitySessionSent,
    CityHostOnline,
    CityClientOnline,
    AvatarSelect,
    FindLot,
    LotConnecting,
    LotSession,
    LotHostOnline,
    LotJoined,
    Failed,
}

/// <summary>
/// WASM-safe Archive join demo: city WS through FindLot, then lot WS through empty VM tick.
/// Speaks Aries bytes only — no Mina / FSO.Server.Protocol dependency.
/// </summary>
public sealed class ArchiveJoinDemo : IAsyncDisposable
{
    readonly string _cityWs;
    readonly string _lotWs;
    readonly uint _avatarId;
    readonly uint _lotId;

    public JoinStage Stage { get; private set; } = JoinStage.Idle;
    public string Status { get; private set; } = "idle";
    public string? ServerName { get; private set; }
    public string? LotAddress { get; private set; }
    public string? LastError { get; private set; }

    public event Action? Changed;

    public ArchiveJoinDemo(string gatewayHttpOrWsBase, uint avatarId = 1, uint lotId = 1)
    {
        var baseUri = gatewayHttpOrWsBase.TrimEnd('/');
        if (baseUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            baseUri = "ws://" + baseUri["http://".Length..];
        else if (baseUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            baseUri = "wss://" + baseUri["https://".Length..];
        else if (!baseUri.StartsWith("ws", StringComparison.OrdinalIgnoreCase))
            baseUri = "ws://" + baseUri;

        _cityWs = baseUri + "/city";
        _lotWs = baseUri + "/lot";
        _avatarId = avatarId;
        _lotId = lotId;
    }

    void Set(JoinStage stage, string status)
    {
        Stage = stage;
        Status = status;
        Changed?.Invoke();
    }

    static async Task<byte[]> ReceiveMessageAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[16384];
        using var ms = new MemoryStream();
        while (true)
        {
            var result = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
                return Array.Empty<byte>();
            ms.Write(buffer, 0, result.Count);
            if (result.EndOfMessage) break;
        }
        return ms.ToArray();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Set(JoinStage.CityConnecting, "city connecting…");
            using var city = new ClientWebSocket();
            await city.ConnectAsync(new Uri(_cityWs), cancellationToken).ConfigureAwait(false);

            var framer = new AriesFramer();
            string? ticket = null;
            string? lotUser = null;
            string? lotAddress = null;
            var sentClientOnline = false;
            var sentAvatar = false;
            var sentFindLot = false;
            var sawHandshake = false;
            var gotFindLot = false;

            while (city.State == WebSocketState.Open && !gotFindLot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var msg = await ReceiveMessageAsync(city, cancellationToken).ConfigureAwait(false);
                if (msg.Length == 0) break;

                foreach (var frame in framer.Push(msg))
                {
                    if (frame.Type == AriesCodec.RequestClientSessionArchive && !sawHandshake)
                    {
                        sawHandshake = true;
                        ServerName = AriesDecode.TryDecodeArchiveHandshakeName(frame.Payload);
                        Set(JoinStage.CityHandshake, "handshake: " + (ServerName ?? "?"));
                        var sess = AriesCodec.EncodeArchiveSessionResponse("BrowserDemo", "browser-demo-unencrypted");
                        await city.SendAsync(AriesCodec.Frame(AriesCodec.RequestClientSessionResponse, sess),
                            WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                        Set(JoinStage.CitySessionSent, "type 21 sent");
                    }
                    else if (frame.Type == AriesCodec.Voltron
                             && AriesDecode.TryVoltronSubtype(frame.Payload, out var sub))
                    {
                        if (sub == 0x001e && !sentClientOnline)
                        {
                            sentClientOnline = true;
                            Set(JoinStage.CityHostOnline, "city HostOnline");
                            await city.SendAsync(AriesCodec.EncodeClientOnlineBurst(),
                                WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                        }
                        else if (sub == 0x0035 && !sentAvatar)
                        {
                            sentAvatar = true;
                            Set(JoinStage.CityClientOnline, "ignore ack");
                            await city.SendAsync(AriesCodec.EncodeAvatarSelectRequest(_avatarId),
                                WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                            Set(JoinStage.AvatarSelect, "avatar select sent");
                        }
                    }
                    else if (frame.Type == AriesCodec.Electron
                             && AriesDecode.TryVoltronSubtype(frame.Payload, out var esub))
                    {
                        if (esub == 31 && !sentFindLot)
                        {
                            sentFindLot = true;
                            await city.SendAsync(AriesCodec.EncodeFindLotRequest(_lotId),
                                WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                        }
                        else if (esub == 6
                                 && AriesDecode.TryDecodeFindLotResponse(frame.Payload,
                                     out var status, out _, out ticket, out lotAddress, out lotUser)
                                 && status == 0)
                        {
                            LotAddress = lotAddress;
                            Set(JoinStage.FindLot, "FindLot → " + lotAddress);
                            gotFindLot = true;
                            break;
                        }
                    }
                }
            }

            if (ticket == null || lotUser == null)
            {
                Set(JoinStage.Failed, "FindLot did not return a ticket");
                return;
            }

            await JoinLotAsync(ticket, lotUser, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Set(JoinStage.Failed, "failed: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    async Task JoinLotAsync(string ticket, string user, CancellationToken cancellationToken)
    {
        Set(JoinStage.LotConnecting, "lot connecting…");
        using var lot = new ClientWebSocket();
        await lot.ConnectAsync(new Uri(_lotWs), cancellationToken).ConfigureAwait(false);

        var framer = new AriesFramer();
        var sawSession = false;
        var sentOnline = false;

        while (lot.State == WebSocketState.Open)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var msg = await ReceiveMessageAsync(lot, cancellationToken).ConfigureAwait(false);
            if (msg.Length == 0) break;

            foreach (var frame in framer.Push(msg))
            {
                if (frame.Type == AriesCodec.RequestClientSession && !sawSession)
                {
                    sawSession = true;
                    var sess = AriesCodec.EncodeLotSessionResponse(user, ticket);
                    await lot.SendAsync(AriesCodec.Frame(AriesCodec.RequestClientSessionResponse, sess),
                        WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                    Set(JoinStage.LotSession, "lot type 21 sent");
                }
                else if (frame.Type == AriesCodec.Voltron
                         && AriesDecode.TryVoltronSubtype(frame.Payload, out var sub)
                         && sub == 0x001e && !sentOnline)
                {
                    sentOnline = true;
                    Set(JoinStage.LotHostOnline, "lot HostOnline");
                    await lot.SendAsync(AriesCodec.EncodeLotClientOnline(),
                        WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                }
                else if (frame.Type == AriesCodec.Electron
                         && AriesDecode.TryVoltronSubtype(frame.Payload, out var esub)
                         && esub == 7)
                {
                    Set(JoinStage.LotJoined, "lot joined (empty tick)");
                    return;
                }
            }
        }

        if (Stage != JoinStage.LotJoined)
            Set(JoinStage.Failed, "lot closed before tick");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
