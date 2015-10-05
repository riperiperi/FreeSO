/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GonzoNet;
using FSO.SimAntics.NetPlay.Model;
using GonzoNet.Encryption;
using ProtocolAbstractionLibraryD;
using System.Timers;
using System.Diagnostics;

namespace FSO.SimAntics.NetPlay.Drivers
{
    public delegate void OnStateChangeDelegate(int state, float progress);

    public class VMClientDriver : VMNetDriver
    {
        private Queue<VMNetTick> TickBuffer;
        private Queue<VMNetCommandBodyAbstract> Commands;
        private uint TickID = 0;
        private const int TICKS_PER_PACKET = 2;
        private bool ExecutedAnything;

        public event OnStateChangeDelegate OnStateChange;

        private NetworkClient Client;

        public VMClientDriver(string hostName, int port, OnStateChangeDelegate callback)
        {
            Commands = new Queue<VMNetCommandBodyAbstract>();
            Client = new NetworkClient(hostName, port, EncryptionMode.NoEncryption, true);

            Client.OnConnected += Client_OnConnected;
            Client.OnDisconnect += Client_OnDisconnect;
            OnStateChange += callback;
            Client.Connect(null);

            TickBuffer = new Queue<VMNetTick>();
        }

        private void Client_OnDisconnect()
        {
            if (OnStateChange != null) OnStateChange(4, 0f);
        }

        private void Client_OnConnected(LoginArgsContainer LoginArgs)
        {
            if (OnStateChange != null) OnStateChange(1, 0f);
        }

        public override void SendCommand(VMNetCommandBodyAbstract cmd)
        {
            Commands.Enqueue(cmd);
        }

        private void SendToServer(VMNetCommandBodyAbstract cmd)
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
                stream.WriteInt32(data.Length + (int)PacketHeaders.UNENCRYPTED);
                stream.WriteBytes(data);
                Client.Send(stream.ToArray());
            }
        }

        public override bool Tick(VM vm)
        {
            HandleNet();
            if (Client.Connected)
            {
                while (Commands.Count > 0)
                {
                    SendToServer(Commands.Dequeue());
                }
            }

            lock (TickBuffer)
            {
                var timer = new Stopwatch();
                timer.Start();
                while (TickBuffer.Count > TICKS_PER_PACKET * 2)
                {
                    ExecutedAnything = true;
                    var tick = TickBuffer.Dequeue();
                    InternalTick(vm, tick);
                    if (timer.ElapsedMilliseconds > 25)
                    {
                        timer.Stop();
                        if (!vm.Ready) OnStateChange(2, tick.TickID / (float)(tick.TickID + TickBuffer.Count));
                        return false;
                    }
                }

                if (TickBuffer.Count > 0)
                {
                    ExecutedAnything = true;
                    var tick = TickBuffer.Dequeue();
                    InternalTick(vm, tick);
                }
            }
            if (!vm.Ready)
            {
                if (ExecutedAnything)
                {
                    OnStateChange(3, 0f);
                    return true;
                }
                else return false;
            }
            return true;
            
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
                try {
                    using (var reader = new BinaryReader(packet))
                    {
                        tick.Deserialize(reader);
                    }
                } catch (Exception)
                {
                    client.Disconnect();
                    return;
                }
                for (int i = 0; i < tick.Ticks.Count; i++)
                {
                    TickBuffer.Enqueue(tick.Ticks[i]);
                }
            }
        }

        public override void CloseNet()
        {
            Client.Disconnect();
        }
    }
}
