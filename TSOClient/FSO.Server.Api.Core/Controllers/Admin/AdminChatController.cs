using FSO.Server.Common;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin/chat")]
    [ApiController]
    public class AdminChatController : ControllerBase
    {
        // GET admin/chat/ws — upgrade to WebSocket; streams lot chat events as JSON lines.
        // Requires moderator JWT in Authorization header or 'fso' cookie.
        [HttpGet("ws")]
        public async Task Stream()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var api = Api.INSTANCE;
            try
            {
                api.DemandModerator(Request);
            }
            catch
            {
                HttpContext.Response.StatusCode = 401;
                return;
            }

            var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var ct = HttpContext.RequestAborted;

            // Per-client bounded queue + semaphore for async drain.
            var queue = new ConcurrentQueue<string>();
            var signal = new SemaphoreSlim(0, int.MaxValue);

            Action<string> onMessage = json =>
            {
                queue.Enqueue(json);
                try { signal.Release(); } catch { }
            };

            using (api.ChatBroadcast.Subscribe(onMessage))
            {
                while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await signal.WaitAsync(ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    while (queue.TryDequeue(out var msg))
                    {
                        if (ws.State != WebSocketState.Open) break;
                        var bytes = Encoding.UTF8.GetBytes(msg + "\n");
                        try
                        {
                            await ws.SendAsync(
                                new ArraySegment<byte>(bytes),
                                WebSocketMessageType.Text,
                                endOfMessage: true,
                                cancellationToken: ct);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }

            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", CancellationToken.None);
                }
                catch { }
            }
        }
    }
}