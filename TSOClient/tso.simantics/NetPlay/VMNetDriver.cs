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
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Engine.TSOTransaction;
using System.IO;
using FSO.SimAntics.NetPlay.Drivers;

namespace FSO.SimAntics.NetPlay
{
    public abstract class VMNetDriver
    {
        public IVMTSOGlobalLink GlobalLink;
        public abstract void SendCommand(VMNetCommandBodyAbstract cmd);
        public abstract void SendDirectCommand(uint pid, VMNetCommandBodyAbstract cmd);
        public abstract bool Tick(VM vm);
        public abstract string GetUserIP(uint uid);
        public VMCloseNetReason CloseReason;
        private BinaryWriter RecordStream;
        public VMNetCommand Executing;

        /// <summary>
        /// Indicates a VM inspired total connection shutdown. 
        /// </summary>
        public event VMNetClosedHandler OnShutdown;
        public delegate void VMNetClosedHandler(VMCloseNetReason reason);

        private int DesyncCooldown = 0;
        public uint LastTick = 0;

        protected void InternalTick(VM vm, VMNetTick tick)
        {
            if (!tick.ImmediateMode && (tick.Commands.Count == 0 || !(tick.Commands[0].Command is VMStateSyncCmd)) && vm.Context.RandomSeed != tick.RandomSeed)
            {
                if (DesyncCooldown == 0)
                {
                    System.Console.WriteLine("DESYNC - Requested state from host");
                    vm.SendCommand(new VMRequestResyncCmd());
                    DesyncCooldown = 30 * 30;
                } else
                {
                    System.Console.WriteLine("WARN - DESYNC - Too soon to try again!");
                }
            }

            if (RecordStream != null) RecordTick(tick);

            vm.Context.RandomSeed = tick.RandomSeed;
            bool doTick = !tick.ImmediateMode;
            foreach(var cmd in tick.Commands)
            {
                if (cmd.Command is VMStateSyncCmd && ((VMStateSyncCmd)cmd.Command).Run)
                {
                    if (LastTick + 1 != tick.TickID) System.Console.WriteLine("Jump to tick " + tick.TickID);
                    if (!(this is VMFSORDriver)) doTick = false; //something weird here. this can break loading from saves casually - but must not be active for resyncs.
                    //disable just for fsor playback
                }

                var caller = vm.GetAvatarByPersist(cmd.Command.ActorUID);
                Executing = cmd;
                cmd.Command.Execute(vm, caller);
                Executing = null;
            }
            if (tick.TickID < LastTick) System.Console.WriteLine("Tick wrong! Got " + tick.TickID + ", Missed " + ((int)tick.TickID - (LastTick + 1)));
            else if (doTick && vm.Context.Ready)
            {
                if (tick.TickID > LastTick + 1) System.Console.WriteLine("Tick wrong! Got " + tick.TickID + ", Missed " + ((int)tick.TickID - (LastTick + 1)));
#if VM_DESYNC_DEBUG
                vm.Trace.NewTick(tick.TickID);
#endif
                vm.InternalTick(tick.TickID);
                if (DesyncCooldown > 0) DesyncCooldown--;
            }
            LastTick = tick.TickID;
        }

        //functions for recording command streams. 
        //Should work on client or server, assuming there is a sync point at the start.

        public void Record(Stream output)
        {
            RecordStream = new BinaryWriter(output);
            RecordStream.Write(new char[] { 'F', 'S', 'O', 'r' });
            RecordStream.Write(0); //version, for future extension
        }

        public void RecordTick(VMNetTick tick)
        {
            tick.SerializeInto(RecordStream);
        }

        public void EndRecord()
        {
            RecordStream?.BaseStream?.Close();
        }

        public virtual void Shutdown()
        {
            if (OnShutdown != null) OnShutdown(CloseReason);
        }

        public delegate void VMNetMessageHandler(VMNetMessageType type, byte[] data);
    }

    public enum VMCloseNetReason
    {
        Unspecified = 0,
        LeaveLot = 1,
        ServerShutdown = 2,
    }
}
