using FSO.SimAntics.NetPlay.EODs.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    class VMNetEODEventCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public VMEODEvent Event;

        public override bool Execute(VM vm)
        {
            //notify thread of an EOD event.
            var obj = vm.GetObjectById(ObjectID);
            if (obj == null || obj.Thread == null || obj.Thread.BlockingState == null || !(obj.Thread.BlockingState is VMEODPluginThreadState))
                return false; //rats.

            var state = obj.Thread.BlockingState as VMEODPluginThreadState;
            state.Events.Add(Event);

            if (Event.Code == -3) state.Ended = true;

            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectID);
            Event.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectID = reader.ReadInt16();
            Event = new VMEODEvent();
            Event.Deserialize(reader);
        }
        #endregion
    }
}
