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
                    DesyncCooldown = 30 * 3;
                } else
                {
                    System.Console.WriteLine("WARN - DESYNC - Too soon to try again!");
                }
            }

            vm.Context.RandomSeed = tick.RandomSeed;
            bool doTick = !tick.ImmediateMode;
            foreach(var cmd in tick.Commands)
            {
                if (cmd.Command is VMStateSyncCmd)
                {
                    if (LastTick + 1 != tick.TickID) System.Console.WriteLine("Jump to tick " + tick.TickID);
                    doTick = false;
                }

                var caller = vm.GetAvatarByPersist(cmd.Command.ActorUID);
                cmd.Command.Execute(vm, caller);
            }
            if (tick.TickID < LastTick) System.Console.WriteLine("Tick wrong! Got " + tick.TickID + ", Missed " + ((int)tick.TickID - (LastTick + 1)));
            else if (doTick && vm.Context.Ready)
            {
                if (tick.TickID > LastTick + 1) System.Console.WriteLine("Tick wrong! Got " + tick.TickID + ", Missed " + ((int)tick.TickID - (LastTick + 1)));
                vm.InternalTick();
                if (DesyncCooldown > 0) DesyncCooldown--;
            }
            LastTick = tick.TickID;
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
