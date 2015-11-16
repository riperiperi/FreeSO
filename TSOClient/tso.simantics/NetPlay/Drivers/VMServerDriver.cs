/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.NetPlay.Model;
using System.IO;
using GonzoNet;
using GonzoNet.Encryption;
using System.Net;
using ProtocolAbstractionLibraryD;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.SimAntics.NetPlay.Drivers
{
    public class VMServerDriver : VMNetDriver
    {
        private List<VMNetCommand> QueuedCmds;

        private const int TICKS_PER_PACKET = 2;
        private List<VMNetTick> TickBuffer;

        private Dictionary<NetworkClient, uint> UIDs;

        private Listener listener;
        private HashSet<NetworkClient> ClientsToDC;
        private HashSet<NetworkClient> ClientsToSync;

        private uint TickID = 0;

        public VMServerDriver(int port)
        {
            listener = new Listener(EncryptionMode.NoEncryption);
            listener.Initialize(new IPEndPoint(IPAddress.Any, port));
            listener.OnConnected += SendLotState;
            listener.OnDisconnected += LotDC;

            ClientsToDC = new HashSet<NetworkClient>();
            ClientsToSync = new HashSet<NetworkClient>();
            QueuedCmds = new List<VMNetCommand>();
            TickBuffer = new List<VMNetTick>();
            UIDs = new Dictionary<NetworkClient, uint>();
        }

        private void LotDC(NetworkClient Client)
        {
            lock (UIDs) {
                if (UIDs.ContainsKey(Client)) {
                    uint UID = UIDs[Client];
                    UIDs.Remove(Client);

                    SendCommand(new VMNetSimLeaveCmd
                    {
                        SimID = UID
                    });
                }
            }
        }

        private void SendLotState(NetworkClient client)
        {
            lock(ClientsToSync)
            {
                ClientsToSync.Add(client);
            }
        }

        private void SendState(VM vm)
        {
            if (ClientsToSync.Count == 0) return;
            var state = vm.Save();
            var cmd = new VMNetCommand(new VMStateSyncCmd { State = state });

            //currently just hack this on the tick system. will change when we switch to not gonzonet
            var ticks = new VMNetTickList { Ticks = new List<VMNetTick> {
                new VMNetTick {
                    Commands = new List<VMNetCommand> { cmd },
                    RandomSeed = 0, //will be restored by client from cmd
                    TickID = TickID
                }
            } };

            byte[] data;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    ticks.SerializeInto(writer);
                }

                data = stream.ToArray();
            }

            byte[] packet;

            using (var stream = new PacketStream((byte)PacketType.VM_PACKET, 0))
            {
                stream.WriteHeader();
                stream.WriteInt32(data.Length + (int)PacketHeaders.UNENCRYPTED);
                stream.WriteBytes(data);

                packet = stream.ToArray();
            }
            foreach (var client in ClientsToSync) client.Send(packet);
            ClientsToSync.Clear();
        }

        public override void SendCommand(VMNetCommandBodyAbstract cmd)
        {
            lock (QueuedCmds)
            {
                QueuedCmds.Add(new VMNetCommand(cmd));
            }
        }

        public override bool Tick(VM vm)
        {
            HandleClients();

            lock (QueuedCmds) { 
                var tick = new VMNetTick();
                tick.Commands = new List<VMNetCommand>(QueuedCmds);
                tick.TickID = TickID++;
                tick.RandomSeed = vm.Context.RandomSeed;

                InternalTick(vm, tick);
                QueuedCmds.Clear();

                TickBuffer.Add(tick);
                if (TickBuffer.Count >= TICKS_PER_PACKET)
                {
                    lock (ClientsToSync)
                    {
                        SendTickBuffer();
                        SendState(vm);
                    }
                }
            }

            return true;
        }

        private void SendTickBuffer()
        {
            var ticks = new VMNetTickList { Ticks = TickBuffer };
            byte[] data;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    ticks.SerializeInto(writer);
                }

                data = stream.ToArray();
            }

            using (var stream = new PacketStream((byte)PacketType.VM_PACKET, 0))
            {
                stream.WriteHeader();
                stream.WriteInt32(data.Length + (int)PacketHeaders.UNENCRYPTED);
                stream.WriteBytes(data);
                Broadcast(stream.ToArray(), ClientsToSync);
            }

            TickBuffer.Clear();
        }

        private void Broadcast(byte[] packet, HashSet<NetworkClient> ignore)
        {
            lock (listener.Clients)
            {
                var clients = new List<NetworkClient>(listener.Clients);
                foreach (var client in clients)
                {
                    if (ignore.Contains(client)) continue;
                    client.Send(packet);
                }
            }
        }

        private void HandleClients()
        {
            lock (listener.Clients)
            {
                ClientsToDC.Clear();
                foreach (var client in listener.Clients)
                {
                    var packets = client.GetPackets();
                    while (packets.Count > 0)
                    {
                        OnPacket(client, packets.Dequeue());
                    }
                }
                foreach (var client in ClientsToDC)
                {
                    client.Disconnect();
                }
            }
        }

        public override void OnPacket(NetworkClient client, ProcessedPacket packet)
        {
            var cmd = new VMNetCommand();
            try {
                using (var reader = new BinaryReader(packet)) {
                    cmd.Deserialize(reader);
                }
            } catch (Exception)
            {
                ClientsToDC.Add(client);
                return;
            }

            if (cmd.Type == VMCommandType.SimJoin)
            {
                if (((VMNetSimJoinCmd)cmd.Command).Version != VMNetSimJoinCmd.CurVer)
                {
                    ClientsToDC.Add(client);
                    return;
                }
                lock (UIDs)
                {
                    UIDs.Add(client, ((VMNetSimJoinCmd)cmd.Command).SimID);
                }
            }
            else if (cmd.Type == VMCommandType.RequestResync)
            {
                lock (ClientsToSync)
                {
                    //todo: throttle
                    ClientsToSync.Add(client);
                }
            }

            SendCommand(cmd.Command);
        }

        public override void CloseNet()
        {
            listener.Close();
        }
    }
}
