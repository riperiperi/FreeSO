using FSO.Server.Common;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin")]
    [ApiController]
    public class AdminChatController : ControllerBase
    {
        // GET admin/chat/ws — upgrade to WebSocket; streams lot chat events as JSON lines.
        // Requires moderator JWT in Authorization header or 'fso' cookie.
        [HttpGet("chat/ws")]
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

            // Use a linked token so either side closing tears down the other.
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted))
            {
            var ct = cts.Token;

            // Per-client bounded queue + semaphore for async drain.
            var queue = new ConcurrentQueue<string>();
            var signal = new SemaphoreSlim(0, int.MaxValue);

            Action<string> onMessage = json =>
            {
                queue.Enqueue(json);
                try { signal.Release(); } catch { }
            };

            // Read task — drains incoming frames so ASP.NET Core can respond to
            // WebSocket pings and process close frames. Without this, clients time
            // out after one ping_interval + ping_timeout with "no close frame received".
            var readTask = Task.Run(async () =>
            {
                var buf = new byte[256];
                try
                {
                    while (ws.State == WebSocketState.Open)
                    {
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                        if (result.MessageType == WebSocketMessageType.Close)
                            break;
                    }
                }
                catch { }
                finally { cts.Cancel(); } // client disconnected — stop the write loop
            });

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

            await readTask.ConfigureAwait(false);

            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", CancellationToken.None);
                }
                catch { }
            }
            } // end using cts
        }

        // POST admin/lot/{lot_id}/chat/inject — send a Discord message into in-game lot chat.
        // Requires moderator JWT.
        [HttpPost("lot/{lot_id}/chat/inject")]
        public IActionResult Inject(int lot_id)
        {
            var api = Api.INSTANCE;
            try { api.DemandModerator(Request); }
            catch { return Unauthorized(); }

            string body;
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = reader.ReadToEndAsync().GetAwaiter().GetResult();

            dynamic payload;
            try { payload = JsonConvert.DeserializeObject<dynamic>(body); }
            catch { return BadRequest("Invalid JSON"); }

            string avatarName = (string)payload?.avatar_name ?? "";
            string message    = (string)payload?.message    ?? "";

            if (string.IsNullOrWhiteSpace(avatarName) || string.IsNullOrWhiteSpace(message))
                return BadRequest("avatar_name and message are required");

            if (message.Length > 200)
                message = message.Substring(0, 200);

            var json = JsonConvert.SerializeObject(new
            {
                lot_id,
                avatar_name = avatarName,
                message
            });

            api.InjectBroadcast.Publish(json);
            return Ok();
        }

        // POST admin/city/chat/inject — send a Discord message into city-wide chat for all connected clients.
        // Requires moderator JWT. Body: {"avatar_name":"...", "message":"..."}
        [HttpPost("city/chat/inject")]
        public IActionResult InjectCity()
        {
            var api = Api.INSTANCE;
            try { api.DemandModerator(Request); }
            catch { return Unauthorized(); }

            string body;
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = reader.ReadToEndAsync().GetAwaiter().GetResult();

            dynamic payload;
            try { payload = JsonConvert.DeserializeObject<dynamic>(body); }
            catch { return BadRequest("Invalid JSON"); }

            string avatarName = (string)payload?.avatar_name ?? "";
            string message    = (string)payload?.message    ?? "";

            if (string.IsNullOrWhiteSpace(avatarName) || string.IsNullOrWhiteSpace(message))
                return BadRequest("avatar_name and message are required");

            if (message.Length > 200)
                message = message.Substring(0, 200);

            api.GlobalChatInjectBroadcast.Publish(JsonConvert.SerializeObject(new
            {
                avatar_name = avatarName,
                message
            }));
            return Ok();
        }
    }
}