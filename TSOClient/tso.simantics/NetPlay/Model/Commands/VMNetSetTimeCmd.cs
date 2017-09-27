using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSetTimeCmd : VMNetCommandBodyAbstract
    {
        public int Hours;
        public int Minutes;
        public int Seconds;

        public override bool Execute(VM vm)
        {
            var clock = vm.Context.Clock;
            clock.Hours = Hours;
            clock.Minutes = Minutes;
            clock.MinuteFractions = (Seconds * clock.TicksPerMinute) / 60;
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Hours = reader.ReadInt32();
            Minutes = reader.ReadInt32();
            Seconds = reader.ReadInt32();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Hours);
            writer.Write(Minutes);
            writer.Write(Seconds);
        }
    }
}
