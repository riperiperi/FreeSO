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
using FSO.SimAntics.Netplay.Model;

namespace FSO.SimAntics.Netplay
{
    public abstract class VMNetDriver
    {
        public bool ExceptionOnDesync;
        public abstract void SendCommand(VMNetCommandBodyAbstract cmd);
        public abstract bool Tick(VM vm);

        protected void InternalTick(VM vm, VMNetTick tick)
        {
            if (vm.Context.RandomSeed != tick.RandomSeed)
            {
                System.Console.WriteLine("WARN! desync, tick wrong: " + vm.Context.RandomSeed + " != " + tick.RandomSeed);
                //if (ExceptionOnDesync) throw new Exception("desync, tick wrong: " + vm.Context.RandomSeed + " != " + tick.RandomSeed);
                ExceptionOnDesync = true;
            }
            vm.Context.RandomSeed = tick.RandomSeed;
            foreach(var cmd in tick.Commands)
            {
                cmd.Command.Execute(vm);
            }
            vm.InternalTick();
        }

        public abstract void CloseNet();
        public abstract void OnPacket(NetworkClient client, ProcessedPacket packet);
    }
}
