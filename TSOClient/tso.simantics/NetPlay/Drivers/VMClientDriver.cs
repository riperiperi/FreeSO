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
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.NetPlay.Drivers
{
    public delegate void OnStateChangeDelegate(int state, float progress);

    public class VMClientDriver : VMNetDriver
    {
        private Queue<VMNetTick> TickBuffer;
        private Queue<VMNetCommandBodyAbstract> Commands;
        private uint TickID = 0;
        private const int TICKS_PER_PACKET = 2;
        private const int BUFFER_STABLE_TICKS = 3 * 30; //if buffer does not drop below 2 large for this number of ticks, tighten buffer size

        private int BufferSize = TICKS_PER_PACKET * 2;
        private int TicksSinceCloseCall = 0;
        private bool ReplenishBuffer = false; // when true, ticks run at half speed until BufferSize.
        private bool ExecutedAnything;
        private int DriverTickPhase = 0;

        private VM VMHook; //should probably always backreference the VM anyways, but just used by disconnect
        //todo: clean up everything in all of these classes.

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

            GlobalLink = null; //transactions only performed by server. transaction results
            //are passed back to the clients as commands (for the primitive, at least)

            TickBuffer = new Queue<VMNetTick>();
        }

        private void Client_OnDisconnect()
        {
#if DEBUG
            //switch to server mode for debug purposes
            VMDialogInfo info = new VMDialogInfo
            {
                Caller = null,
                Icon = null,
                Operand = new VMDialogOperand { },
                Message = "You have disconnected from the server. Simulation is continuing locally for debug purposes.",
                Title = "Disconnected!"
            };
            VMHook.SignalDialog(info);

            VMHook.ReplaceNet(new VMServerDriver(37564));
#else
            if (OnStateChange != null) OnStateChange(4, 0f);
#endif
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
            VMHook = vm;
            DriverTickPhase++;
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

                int tickSpeed;

                // === BUFFER SIZE MANAGEMENT (reduces stutter) ===

                if (TickBuffer.Count > BufferSize * 3)
                    tickSpeed = 2000;
                else if (TickBuffer.Count > BufferSize * 2)
                    tickSpeed = 2;
                else tickSpeed = 1;

                if (TickBuffer.Count == 0)
                {
                    tickSpeed = 0;
                    if (!ReplenishBuffer && ExecutedAnything)
                    {
                        BufferSize++;
                        ReplenishBuffer = true;
                    }
                    
                }
                else if (TickBuffer.Count <= TICKS_PER_PACKET)
                    TicksSinceCloseCall = 0;

                if (ReplenishBuffer)
                {
                    if (TickBuffer.Count >= BufferSize) ReplenishBuffer = false;
                    else if (DriverTickPhase % 2 == 0) tickSpeed = 0; //run at half speed til buffer replenished
                }
                else if (TicksSinceCloseCall++ > BUFFER_STABLE_TICKS)
                {
                    TicksSinceCloseCall = 0;
                    BufferSize--;
                    if (BufferSize < 2) BufferSize = 2;
                }

                // === END BUFFER SIZE MANAGEMENT ===

                for (int i = 0; i < tickSpeed && TickBuffer.Count > 0; i++)
                {
                    ExecutedAnything = true;
                    var tick = TickBuffer.Dequeue();
                    InternalTick(vm, tick);
                    if (timer.ElapsedMilliseconds > 66)
                    {
                        timer.Stop();
                        if (!vm.Ready) OnStateChange(2, tick.TickID / (float)(tick.TickID + TickBuffer.Count));
                        return false;
                    }
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
                    tick.Ticks[i].ImmediateMode = tick.ImmediateMode;
                    TickBuffer.Enqueue(tick.Ticks[i]);
                }
            }
        }

        public override void CloseNet()
        {
            Client.Disconnect();
        }

        public override string GetUserIP(uint uid)
        {
            return "remote";
        }
    }
}
