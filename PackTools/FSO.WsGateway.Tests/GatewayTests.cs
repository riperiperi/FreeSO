using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using Mina.Core.Buffer;
using Xunit;

namespace FSO.WsGateway.Tests
{
    public class GatewayTests
    {
        /// <summary>TCP listener on an ephemeral port that runs a handler per connection.</summary>
        private static (TcpListener Listener, int Port) TcpServer(Func<NetworkStream, Task> handler)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => handler(client.GetStream()));
                }
            });
            return (listener, port);
        }

        private static async Task<(Gateway Gw, ClientWebSocket Ws)> ConnectThroughGateway(int tcpPort)
        {
            var gateway = new Gateway(new Dictionary<string, (string, int)>
            {
                ["/city"] = ("127.0.0.1", tcpPort),
            });
            await gateway.Start("http://127.0.0.1:0");
            var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(gateway.Address.Replace("http", "ws") + "/city"), CancellationToken.None);
            return (gateway, ws);
        }

        private static async Task<byte[]> ReceiveExactly(ClientWebSocket ws, int count)
        {
            var result = new byte[count];
            var got = 0;
            var buffer = new byte[16384];
            while (got < count)
            {
                var r = await ws.ReceiveAsync(buffer, new CancellationTokenSource(10000).Token);
                Assert.Equal(WebSocketMessageType.Binary, r.MessageType);
                Array.Copy(buffer, 0, result, got, Math.Min(r.Count, count - got));
                got += r.Count;
            }
            Assert.Equal(count, got);
            return result;
        }

        [Fact]
        public async Task Echo_RoundTripsBytesBothWays()
        {
            var (listener, port) = TcpServer(async stream =>
            {
                var buffer = new byte[16384];
                int read;
                while ((read = await stream.ReadAsync(buffer)) > 0)
                    await stream.WriteAsync(buffer.AsMemory(0, read));
            });

            var (gateway, ws) = await ConnectThroughGateway(port);
            try
            {
                var payload = new byte[4096];
                new Random(42).NextBytes(payload);
                await ws.SendAsync(payload, WebSocketMessageType.Binary, true, CancellationToken.None);

                var echoed = await ReceiveExactly(ws, payload.Length);
                Assert.Equal(payload, echoed);
            }
            finally
            {
                listener.Stop();
                await gateway.Stop();
            }
        }

        /// <summary>
        /// The proof the spike exists for: a RequestClientSessionArchive packet — the first
        /// thing the Archive city server sends on connect — serialized with FreeSO's real
        /// protocol code, framed exactly like AriesProtocolEncoder frames it, survives the
        /// WS pipe and decodes on the browser side of the bridge.
        /// </summary>
        [Fact]
        public async Task ArchiveHandshake_SurvivesTheBridge()
        {
            var sent = new RequestClientSessionArchive
            {
                Name = "Kat's Archive",
                PlayerCount = 1,
                VersionInfo = "spike-test",
                ServerKey = "serverkey",
                Nonce = "nonce123",
                ArchiveConfig = 0,
                ShardId = 1,
                ShardName = "San Francisco",
                ShardMap = "city_0900",
            };

            var frame = AriesFrame(2000, sent);

            var (listener, port) = TcpServer(async stream =>
            {
                // Server-initiated, exactly like CityServer.ArchiveHandshake.
                await stream.WriteAsync(frame);
            });

            var (gateway, ws) = await ConnectThroughGateway(port);
            try
            {
                var received = await ReceiveExactly(ws, frame.Length);

                // Parse the 12-byte Aries header (little-endian).
                var type = BitConverter.ToUInt32(received, 0);
                var payloadSize = BitConverter.ToUInt32(received, 8);
                Assert.Equal(2000u, type);
                Assert.Equal((uint)(frame.Length - 12), payloadSize);

                // Decode the payload with the real packet class.
                var payload = IoBuffer.Wrap(received, 12, (int)payloadSize);
                payload.Order = ByteOrder.LittleEndian;
                var decoded = new RequestClientSessionArchive();
                decoded.Deserialize(payload, null);

                Assert.Equal(sent.Name, decoded.Name);
                Assert.Equal(sent.PlayerCount, decoded.PlayerCount);
                Assert.Equal(sent.ShardName, decoded.ShardName);
                Assert.Equal(sent.ShardMap, decoded.ShardMap);
                Assert.Equal(sent.Nonce, decoded.Nonce);
            }
            finally
            {
                listener.Stop();
                await gateway.Stop();
            }
        }

        [Fact]
        public async Task UnknownRoute_Is404()
        {
            var gateway = new Gateway(new Dictionary<string, (string, int)>());
            await gateway.Start("http://127.0.0.1:0");
            try
            {
                using var http = new HttpClient();
                var response = await http.GetAsync(gateway.Address + "/nope");
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
            finally
            {
                await gateway.Stop();
            }
        }

        /// <summary>
        /// Documents the wire format the browser must emit for the Archive handshake reply:
        /// RequestClientSessionResponse (Aries type 21) with Unknown=40 so Password is
        /// PascalVLC (base64 RSA ciphertext on a live server). Fixed fields are Mina
        /// PutString(..., ASCII) — NUL-padded to the field width.
        /// </summary>
        [Fact]
        public void SessionResponse_ArchiveMode_WireFormat()
        {
            var sent = new RequestClientSessionResponse
            {
                User = "BrowserDemo",
                AriesVersion = "",
                Email = "",
                Authserv = "",
                Product = 0,
                Unknown = 40,
                ServiceIdent = "",
                Unknown2 = 4,
                Password = "dGVzdA==", // stand-in for base64(RSA_PKCS1(nonce\userId))
            };

            var payload = SerializeAriesPayload(sent);

            // Fixed block before password:
            // 112 User + 80 AriesVersion + 40 Email + 84 Authserv
            // + 2 Product + 1 Unknown + 3 ServiceIdent + 2 Unknown2 = 324
            const int passwordOffset = 324;
            Assert.Equal(passwordOffset + 1 + "dGVzdA==".Length, payload.Length);
            Assert.Equal(40, payload[318]); // Unknown

            // User is ASCII at offset 0, NUL-padded to 112.
            Assert.Equal((byte)'B', payload[0]);
            Assert.Equal(0, payload[11]); // "BrowserDemo".Length == 11

            // Password is PascalVLC immediately after the fixed block.
            Assert.Equal((byte)"dGVzdA==".Length, payload[passwordOffset]);
            Assert.Equal("dGVzdA==", System.Text.Encoding.UTF8.GetString(payload, passwordOffset + 1, 8));

            // Round-trip through the real deserializer.
            var io = IoBuffer.Wrap(payload);
            io.Order = ByteOrder.LittleEndian;
            var decoded = new RequestClientSessionResponse();
            decoded.Deserialize(io, null);
            Assert.Equal(sent.User, decoded.User);
            Assert.Equal(40, decoded.Unknown);
            Assert.Equal(4, decoded.Unknown2);
            Assert.Equal(sent.Password, decoded.Password);
        }

        /// <summary>
        /// Browser → gateway → TCP: a type-21 Archive session response survives the bridge
        /// and deserializes with FreeSO's protocol code — the send half of the handshake test.
        /// </summary>
        [Fact]
        public async Task SessionResponse_SurvivesTheBridge()
        {
            var sent = new RequestClientSessionResponse
            {
                User = "BrowserDemo",
                AriesVersion = "",
                Email = "",
                Authserv = "",
                Unknown = 40,
                ServiceIdent = "",
                Unknown2 = 4,
                Password = "dGVzdA==",
            };
            var frame = AriesFrame(21, sent);
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            var (listener, port) = TcpServer(async stream =>
            {
                var buf = new byte[frame.Length];
                var got = 0;
                while (got < buf.Length)
                {
                    var n = await stream.ReadAsync(buf.AsMemory(got, buf.Length - got));
                    if (n == 0) break;
                    got += n;
                }
                tcs.TrySetResult(buf.AsSpan(0, got).ToArray());
            });

            var (gateway, ws) = await ConnectThroughGateway(port);
            try
            {
                await ws.SendAsync(frame, WebSocketMessageType.Binary, true, CancellationToken.None);
                var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

                Assert.Equal(21u, BitConverter.ToUInt32(received, 0));
                var payloadSize = BitConverter.ToUInt32(received, 8);
                Assert.Equal((uint)(frame.Length - 12), payloadSize);

                var payload = IoBuffer.Wrap(received, 12, (int)payloadSize);
                payload.Order = ByteOrder.LittleEndian;
                var decoded = new RequestClientSessionResponse();
                decoded.Deserialize(payload, null);
                Assert.Equal(sent.User, decoded.User);
                Assert.Equal(40, decoded.Unknown);
                Assert.Equal(sent.Password, decoded.Password);
            }
            finally
            {
                listener.Stop();
                await gateway.Stop();
            }
        }

        /// <summary>
        /// Lot type-21 uses Unknown=39 and a 32-byte ASCII ticket (not PascalVLC).
        /// </summary>
        [Fact]
        public void SessionResponse_LotMode_WireFormat()
        {
            var sent = new RequestClientSessionResponse
            {
                User = "1",
                AriesVersion = "",
                Email = "",
                Authserv = "",
                Unknown = 39,
                ServiceIdent = "",
                Unknown2 = 4,
                Password = "demo-ticket",
            };
            var payload = SerializeAriesPayload(sent);
            Assert.Equal(39, payload[318]);
            Assert.Equal(324 + 32, payload.Length);
            Assert.Equal((byte)'d', payload[324]);
            // NUL-padded after "demo-ticket"
            Assert.Equal(0, payload[324 + "demo-ticket".Length]);

            var io = IoBuffer.Wrap(payload);
            io.Order = ByteOrder.LittleEndian;
            var decoded = new RequestClientSessionResponse();
            decoded.Deserialize(io, null);
            Assert.Equal(39, decoded.Unknown);
            Assert.Equal("demo-ticket", decoded.Password);
            Assert.Equal("1", decoded.User);
        }

        /// <summary>
        /// Electron FindLotResponse FOUND body the fake city / browser decode against.
        /// Aries type 1000, subtype 6, BE body: status u16, lotId u32, 3× PascalVLC.
        /// </summary>
        [Fact]
        public void FindLotResponse_Found_WireFormat()
        {
            var sent = new FSO.Server.Protocol.Electron.Packets.FindLotResponse
            {
                Status = FSO.Server.Protocol.Electron.Model.FindLotResponseStatus.FOUND,
                LotId = 1,
                LotServerTicket = "demo-ticket",
                Address = "127.0.0.1:34101",
                User = "1",
            };
            var payload = IoBuffer.Allocate(64);
            payload.Order = ByteOrder.BigEndian;
            payload.AutoExpand = true;
            sent.Serialize(payload, null);
            payload.Flip();
            var body = new byte[payload.Remaining];
            payload.Get(body, 0, body.Length);

            Assert.Equal(0, body[0]); // status FOUND hi
            Assert.Equal(0, body[1]); // status FOUND lo
            Assert.Equal(0, body[2]); Assert.Equal(0, body[3]); Assert.Equal(0, body[4]); Assert.Equal(1, body[5]); // lotId=1

            // Round-trip
            var io = IoBuffer.Wrap(body);
            io.Order = ByteOrder.BigEndian;
            var decoded = new FSO.Server.Protocol.Electron.Packets.FindLotResponse();
            decoded.Deserialize(io, null);
            Assert.Equal(sent.Status, decoded.Status);
            Assert.Equal(sent.LotId, decoded.LotId);
            Assert.Equal(sent.Address, decoded.Address);
            Assert.Equal(sent.LotServerTicket, decoded.LotServerTicket);
        }

        /// <summary>
        /// Documents the post-HostOnline client burst the browser must emit:
        /// ClientOnlinePDU (0x000a, 22-byte zero body) inside Aries type 0.
        /// </summary>
        [Fact]
        public void ClientOnlinePDU_WireFormat_AllZeroBody()
        {
            var payload = IoBuffer.Allocate(64);
            payload.Order = ByteOrder.BigEndian;
            payload.AutoExpand = true;
            new ClientOnlinePDU().Serialize(payload, null);
            payload.Flip();
            Assert.Equal(22, payload.Remaining);

            // Voltron header + body, then Aries wrap (as browser encodeClientOnlineBurst).
            var body = new byte[22];
            payload.Get(body, 0, 22);
            Assert.All(body, b => Assert.Equal(0, b));

            var voltron = new byte[6 + 22];
            // subtype 0x000a BE, size 28 BE
            voltron[0] = 0x00; voltron[1] = 0x0a;
            voltron[2] = 0x00; voltron[3] = 0x00; voltron[4] = 0x00; voltron[5] = 0x1c;
            body.CopyTo(voltron, 6);

            var frame = new byte[12 + voltron.Length];
            BitConverter.GetBytes(0u).CopyTo(frame, 0); // Aries Voltron
            BitConverter.GetBytes(0u).CopyTo(frame, 4);
            BitConverter.GetBytes((uint)voltron.Length).CopyTo(frame, 8);
            voltron.CopyTo(frame, 12);

            Assert.Equal(40, frame.Length); // 12 + 28
            Assert.Equal(0x0a, frame[13]);
            Assert.Equal(0x1c, frame[17]);
        }

        /// <summary>Aries framing per AriesProtocolEncoder.EncodeAries: 12-byte LE header + payload.</summary>
        private static byte[] AriesFrame(uint packetType, IAriesPacket packet)
        {
            var payloadBytes = SerializeAriesPayload(packet);
            var frame = new byte[12 + payloadBytes.Length];
            BitConverter.GetBytes(packetType).CopyTo(frame, 0);
            BitConverter.GetBytes(0u).CopyTo(frame, 4); // timestamp, unused by the decoder
            BitConverter.GetBytes((uint)payloadBytes.Length).CopyTo(frame, 8);
            payloadBytes.CopyTo(frame, 12);
            return frame;
        }

        private static byte[] SerializeAriesPayload(IAriesPacket packet)
        {
            var payload = IoBuffer.Allocate(128);
            payload.Order = ByteOrder.LittleEndian;
            payload.AutoExpand = true;
            packet.Serialize(payload, null);
            payload.Flip();
            var bytes = new byte[payload.Remaining];
            payload.Get(bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
