using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GonzoNet;
using TSO.Simantics.net.model;

namespace TSO.Simantics.net
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
