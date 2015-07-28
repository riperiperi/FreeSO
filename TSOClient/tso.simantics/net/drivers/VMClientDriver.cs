using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GonzoNet;
using TSO.Simantics.net.model;
using GonzoNet.Encryption;
using ProtocolAbstractionLibraryD;

namespace TSO.Simantics.net.drivers
{
    public class VMClientDriver : VMNetDriver
    {
        private Queue<VMNetTick> TickBuffer;
        private uint TickID = 0;
        private const int TICKS_PER_PACKET = 2;

        private NetworkClient Client;

        public VMClientDriver(string hostName, int port)
        {
            Client = new NetworkClient(hostName, port, EncryptionMode.NoEncryption, true);
            //client.OnNetworkError += new NetworkErrorDelegate(m_LoginClient_OnNetworkError);
            //client.OnConnected += new OnConnectedDelegate(m_LoginClient_OnConnected);
            Client.Connect(null);

            TickBuffer = new Queue<VMNetTick>();
        }

        public override void SendCommand(VMNetCommandBodyAbstract cmd)
        {
            byte[] data;
            using (var stream = new MemoryStream())
            {
                var cmd2 = new VMNetCommand(cmd);
                using (var writer = new BinaryWriter(stream))
                {
                    cmd2.SerializeInto(writer);
                }
                data = stream.ToArray();
            }
            using (var stream = new PacketStream((byte)PacketType.VM_PACKET, 0))
            {
                stream.WriteHeader();
                stream.WriteInt32(data.Length+(int)PacketHeaders.UNENCRYPTED);
                stream.WriteBytes(data);
                Client.Send(stream.ToArray());
            }
        }

        public override void Tick(VM vm)
        {
            HandleNet();

            lock (TickBuffer)
            {
                while (TickBuffer.Count > TICKS_PER_PACKET * 2)
                {
                    var tick = TickBuffer.Dequeue();
                    InternalTick(vm, tick);
                }

                if (TickBuffer.Count > 0)
                {
                    var tick = TickBuffer.Dequeue();
                    InternalTick(vm, tick);
                }
            }
        }

        private void HandleNet()
        {
            var packets = Client.GetPackets();
            while (packets.Count > 0)
            {
                OnPacket(Client, packets.Dequeue());
            }
        }

        public override void OnPacket(NetworkClient client, ProcessedPacket packet)
        {
            lock (TickBuffer)
            {
                var tick = new VMNetTickList();
                using (var reader = new BinaryReader(packet))
                {
                    tick.Deserialize(reader);
                }
                for (int i = 0; i < tick.Ticks.Count; i++)
                {
                    TickBuffer.Enqueue(tick.Ticks[i]);
                }
            }
        }
    }
}
