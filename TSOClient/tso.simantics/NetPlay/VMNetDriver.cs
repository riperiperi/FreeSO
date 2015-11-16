/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GonzoNet;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.SimAntics.NetPlay
{
    public abstract class VMNetDriver
    {
        public bool ExceptionOnDesync;
        public abstract void SendCommand(VMNetCommandBodyAbstract cmd);
        public abstract bool Tick(VM vm);

        private int DesyncCooldown = 0;

        protected void InternalTick(VM vm, VMNetTick tick)
        {
            if ((tick.Commands.Count == 0 || !(tick.Commands[0].Command is VMStateSyncCmd)) && vm.Context.RandomSeed != tick.RandomSeed)
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
                ExceptionOnDesync = true;
            }
            vm.Context.RandomSeed = tick.RandomSeed;
            bool doTick = true;
            foreach(var cmd in tick.Commands)
            {
                if (cmd.Command is VMStateSyncCmd) doTick = false;
                cmd.Command.Execute(vm);
            }
            if (doTick) vm.InternalTick();
            if (DesyncCooldown > 0) DesyncCooldown--;
        }

        public abstract void CloseNet();
        public abstract void OnPacket(NetworkClient client, ProcessedPacket packet);
    }
}
