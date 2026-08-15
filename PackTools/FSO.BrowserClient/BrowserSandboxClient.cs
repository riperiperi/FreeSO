using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FSO.SimAntics.NetPlay.Model;

namespace FSO_BrowserClient
{
    /// <summary>
    /// FSOSandboxClient over a WebSocket: same surface (OnConnectComplete /
    /// OnMessage / Write / Disconnect) and the same 9-byte-header framing
    /// (int32 type, int32 len, byte VMNetMessageType, payload — LE), carried
    /// through the WsGateway /sandbox route to LotHostLite's TCP port.
    ///
    /// WS message boundaries are ignored: frames are reassembled from a byte
    /// buffer, since the gateway pipes the TCP stream as-is. All callbacks fire
    /// from Pump(), called on the game loop — never from the receive task.
    /// </summary>
    public class BrowserSandboxClient
    {
        public event Action OnConnectComplete;
        public event Action<VMNetMessage> OnMessage;
        public event Action<string> OnError;

        ClientWebSocket ws;
        CancellationTokenSource cts;
        readonly Queue<VMNetMessage> received = new Queue<VMNetMessage>();
        readonly object recvLock = new object();
        bool connectPending;
        string pendingError;
        readonly List<byte> buffer = new List<byte>();

        Task sendChain = Task.CompletedTask;

        public bool IsConnected => ws != null && ws.State == WebSocketState.Open;

        public void Connect(string wsUrl)
        {
            cts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            _ = ConnectAndReceiveAsync(wsUrl);
        }

        async Task ConnectAndReceiveAsync(string wsUrl)
        {
            try
            {
                await ws.ConnectAsync(new Uri(wsUrl), cts.Token).ConfigureAwait(true);
                lock (recvLock) connectPending = true;

                var chunk = new byte[16384];
                while (ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(chunk), cts.Token)
                        .ConfigureAwait(true);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    lock (recvLock)
                    {
                        for (int i = 0; i < result.Count; i++) buffer.Add(chunk[i]);
                        ParseFrames();
                    }
                }
            }
            catch (Exception ex)
            {
                lock (recvLock) pendingError = ex.Message;
            }
        }

        /// <summary>Reassemble sandbox frames from the raw byte stream.</summary>
        void ParseFrames()
        {
            int offset = 0;
            while (buffer.Count - offset >= 8)
            {
                // int32 packet type (unused) + int32 payload length, both LE.
                int len = buffer[offset + 4] | (buffer[offset + 5] << 8)
                    | (buffer[offset + 6] << 16) | (buffer[offset + 7] << 24);
                if (len < 1 || buffer.Count - offset - 8 < len) break;
                var type = (VMNetMessageType)buffer[offset + 8];
                var data = new byte[len - 1];
                buffer.CopyTo(offset + 9, data, 0, len - 1);
                received.Enqueue(new VMNetMessage(type, data));
                offset += 8 + len;
            }
            if (offset > 0) buffer.RemoveRange(0, offset);
        }

        /// <summary>Drain queued events on the game loop.</summary>
        public void Pump()
        {
            bool connect;
            string error;
            var msgs = new List<VMNetMessage>();
            lock (recvLock)
            {
                connect = connectPending; connectPending = false;
                error = pendingError; pendingError = null;
                while (received.Count > 0) msgs.Add(received.Dequeue());
            }
            if (connect) OnConnectComplete?.Invoke();
            foreach (var m in msgs) OnMessage?.Invoke(m);
            if (error != null) OnError?.Invoke(error);
        }

        public void Write(VMNetMessage msg)
        {
            var payload = new byte[9 + msg.Data.Length];
            using (var msWriter = new BinaryWriter(new MemoryStream(payload)))
            {
                msWriter.Write(0);                    // packet type
                msWriter.Write(msg.Data.Length + 1);  // payload length
                msWriter.Write((byte)msg.Type);
                msWriter.Write(msg.Data);
            }
            // Chain sends so frames never interleave mid-write.
            sendChain = sendChain.ContinueWith(async _ =>
            {
                if (ws == null || ws.State != WebSocketState.Open) return;
                try
                {
                    await ws.SendAsync(new ArraySegment<byte>(payload),
                        WebSocketMessageType.Binary, true, cts.Token).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    lock (recvLock) pendingError = ex.Message;
                }
            }, TaskScheduler.Default).Unwrap();
        }

        public void Disconnect()
        {
            try { cts?.Cancel(); } catch { }
            try { ws?.Abort(); } catch { }
        }
    }
}
