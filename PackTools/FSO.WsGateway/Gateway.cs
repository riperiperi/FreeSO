using System.Net.Sockets;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace FSO.WsGateway
{
    /// <summary>
    /// WebSocket-to-TCP byte gateway. A browser cannot open a raw TCP socket, but the
    /// Aries protocol is just a length-prefixed byte stream, so piping bytes between a
    /// binary WebSocket and the existing city/lot ports needs no FreeSO changes at all.
    /// Routes are fixed at startup (e.g. "/city" -> 127.0.0.1:33101) — this is a bridge
    /// to known game ports, not an open proxy.
    /// </summary>
    public class Gateway
    {
        private readonly Dictionary<string, (string Host, int Port)> Routes;
        private WebApplication App;

        public Gateway(Dictionary<string, (string Host, int Port)> routes)
        {
            Routes = routes;
        }

        /// <summary>Listen address after start, e.g. http://127.0.0.1:8087.</summary>
        public string Address { get; private set; }

        public async Task Start(string listenUrl)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls(listenUrl);
            App = builder.Build();

            App.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

            // The demo/browser client is served from the same origin as the WS routes,
            // so the page can just open a relative ws:// URL.
            App.UseDefaultFiles();
            App.UseStaticFiles();

            App.Run(async context =>
            {
                var path = context.Request.Path.Value?.TrimEnd('/') ?? "";
                if (!Routes.TryGetValue(path, out var target))
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("websocket upgrade required");
                    return;
                }

                using var ws = await context.WebSockets.AcceptWebSocketAsync();
                await Bridge(ws, target.Host, target.Port, context.RequestAborted);
            });

            await App.StartAsync();
            Address = App.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>().Addresses.First();
        }

        public Task Stop() => App?.StopAsync() ?? Task.CompletedTask;

        private static async Task Bridge(WebSocket ws, string host, int port, CancellationToken abort)
        {
            using var tcp = new TcpClient();
            try
            {
                await tcp.ConnectAsync(host, port, abort);
            }
            catch (Exception e)
            {
                await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable,
                    Truncate($"tcp connect failed: {e.Message}"), CancellationToken.None);
                return;
            }
            tcp.NoDelay = true;
            var stream = tcp.GetStream();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(abort);
            var wsToTcp = PumpWsToTcp(ws, stream, cts.Token);
            var tcpToWs = PumpTcpToWs(stream, ws, cts.Token);

            await Task.WhenAny(wsToTcp, tcpToWs);
            cts.Cancel();
            try { await Task.WhenAll(wsToTcp, tcpToWs); } catch { /* one side already gone */ }

            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "tcp closed", CancellationToken.None);
                }
                catch { }
            }
        }

        private static async Task PumpWsToTcp(WebSocket ws, NetworkStream stream, CancellationToken ct)
        {
            var buffer = new byte[16384];
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close) return;
                // Message boundaries are irrelevant: Aries reassembles from a byte
                // stream (CustomCumulativeProtocolDecoder), so raw bytes pass through.
                if (result.Count > 0) await stream.WriteAsync(buffer.AsMemory(0, result.Count), ct);
            }
        }

        private static async Task PumpTcpToWs(NetworkStream stream, WebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[16384];
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, ct);
                if (read == 0) return; // remote closed
                await ws.SendAsync(buffer.AsMemory(0, read), WebSocketMessageType.Binary, true, ct);
            }
        }

        // WebSocket close descriptions are capped at 123 UTF-8 bytes.
        private static string Truncate(string s) => s.Length <= 120 ? s : s.Substring(0, 120);
    }
}
