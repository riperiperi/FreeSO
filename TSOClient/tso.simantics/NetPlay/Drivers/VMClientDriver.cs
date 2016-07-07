/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.SimAntics.NetPlay.Model;
using System.Diagnostics;

namespace FSO.SimAntics.NetPlay.Drivers
{
    public delegate void OnStateChangeDelegate(int state, float progress);

    public class VMClientDriver : VMNetDriver
    {
        private Queue<VMNetTick> TickBuffer;
        private Queue<VMNetCommand> DirectCommands;

        private Queue<VMNetCommandBodyAbstract> OutgoingCommands;
        private Queue<VMNetMessage> ServerMessages;
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

        /// <summary>
        /// Fired when the VM client wishes to send an event to the server.
        /// </summary>
        public event VMClientCommandHandler OnClientCommand;
        public delegate void VMClientCommandHandler(byte[] data); //type is obviously command.

        public event OnStateChangeDelegate OnStateChange;

        public VMClientDriver(OnStateChangeDelegate callback)
        {
            OutgoingCommands = new Queue<VMNetCommandBodyAbstract>();
            ServerMessages = new Queue<VMNetMessage>();
            OnStateChange += callback;

            GlobalLink = null; //transactions only performed by server. transaction results
            //are passed back to the clients as commands (for the primitive, at least)

            TickBuffer = new Queue<VMNetTick>();
        }

        public void Disconnected()
        {
            /*
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

            VMHook.ReplaceNet(new VMServerDriver(37564, null));
#else
            if (OnStateChange != null) OnStateChange(4, (float)CloseReason);
#endif
*/
        }

        public void Connected()
        {
            if (OnStateChange != null) OnStateChange(1, 0f);
        }

        public override void SendCommand(VMNetCommandBodyAbstract cmd)
        {
            OutgoingCommands.Enqueue(cmd);
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

            if (OnClientCommand != null) OnClientCommand(data);
        }

        public override bool Tick(VM vm)
        {
            VMHook = vm;
            DriverTickPhase++;
            HandleNet(); //handle messages queued by external networking
            if (OnClientCommand != null)
            {
                while (OutgoingCommands.Count > 0)
                {
                    SendToServer(OutgoingCommands.Dequeue());
                }
            }

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

            if (!vm.Ready)
            {
                if (ExecutedAnything && vm.Context.Ready)
                {
                    OnStateChange(3, 0f);
                    return true;
                }
                else return false;
            }
            return true;
            
        }

        public void ServerMessage(VMNetMessage message)
        {
            lock (ServerMessages)
            {
                ServerMessages.Enqueue(message);
            }
        }

        private void HandleNet()
        {
            lock (ServerMessages)
            {
                var packets = ServerMessages;
                while (packets.Count > 0)
                {
                    HandleServerMessage(packets.Dequeue());
                }
            }
        }

        private void HandleServerMessage(VMNetMessage message)
        {
            if (message.Type == VMNetMessageType.Direct)
            {
                var cmd = new VMNetCommand();
                try
                {
                    using (var reader = new BinaryReader(new MemoryStream(message.Data)))
                    {
                        cmd.Deserialize(reader);
                    }
                    cmd.Command.Execute(VMHook);
                }
                catch (Exception e)
                {
                    Shutdown();
                    return;
                }
            }
            else
            {
                var tick = new VMNetTickList();
                try
                {
                    using (var reader = new BinaryReader(new MemoryStream(message.Data)))
                    {
                        tick.Deserialize(reader);
                    }
                }
                catch (Exception e)
                {
                    Shutdown();
                    return;
                }

                for (int i = 0; i < tick.Ticks.Count; i++)
                {
                    tick.Ticks[i].ImmediateMode = tick.ImmediateMode;
                    TickBuffer.Enqueue(tick.Ticks[i]);
                }
            }
        }

        public override string GetUserIP(uint uid)
        {
            return "remote";
        }
    }
}
