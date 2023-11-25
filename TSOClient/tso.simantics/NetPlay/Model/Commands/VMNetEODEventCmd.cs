using FSO.SimAntics.NetPlay.EODs.Model;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetEODEventCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public VMEODEvent Event;

        public override bool AcceptFromClient { get { return false; } }

        public override bool Execute(VM vm)
        {
            //notify thread of an EOD event.
            var obj = vm.GetObjectById(ObjectID);
            if (obj == null || obj.Thread == null || obj.Thread.EODConnection == null)
                return false; //rats.

            var state = obj.Thread.EODConnection;
            state.Events.Add(Event);

            if (Event.Code == -1) state.Ended = true;

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
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
