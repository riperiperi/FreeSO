using System;
using System.Collections.Generic;
using FSO.SimAntics.NetPlay.Model;
using System.IO;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.SimAntics.NetPlay.Drivers
{
    public class VMFSORDriver : VMNetDriver
    {
        private BinaryReader Reader;

        public VMFSORDriver(Stream commandStream)
        {
            Reader = new BinaryReader(commandStream);
            var magic = new String(Reader.ReadChars(4));
            if (magic != "FSOr") throw new Exception("Invalid recording file!");
            var version = Reader.ReadInt32();
        }

        public override string GetUserIP(uint uid)
        {
            return "recorded";
        }

        public override void SendCommand(VMNetCommandBodyAbstract cmd)
        {
             //cannot change playback
        }

        public override void SendDirectCommand(uint pid, VMNetCommandBodyAbstract cmd)
        {
            //cannot change playback
        }

        private uint TickID;

        public override bool Tick(VM vm)
        {
            if (vm.SpeedMultiplier <= 0) return true;
            //try to read a new tick from the stream
            bool success = false;
            if (Reader.BaseStream.Position < Reader.BaseStream.Length - 1)
            {
                try
                {
                    var tick = new VMNetTick();
                    tick.Deserialize(Reader);
                    var ind = tick.Commands.FindIndex(x => x.Command is VMStateSyncCmd);
                    if (ind > 0)
                    {
                        var cmd = tick.Commands[ind];
                        tick.Commands.RemoveAt(ind);
                        tick.Commands.Insert(0, cmd);
                    }
                    InternalTick(vm, tick);
                    TickID = tick.TickID;
                    success = true;
                }
                catch
                {
                    //might be incomplete, but that shouldn't crash the program. just assume there are no more commands.
                }
            }
            if (!success) InternalTick(vm, new VMNetTick() { Commands = new List<VMNetCommand>(), TickID = ++TickID, RandomSeed = vm.Context.RandomSeed });
            return true;
        }
    }
}
