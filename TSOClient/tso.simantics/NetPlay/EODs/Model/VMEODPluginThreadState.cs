using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.NetPlay.EODs.Model
{
    public class VMEODPluginThreadState : VMAsyncState
    {
        public short AvatarID;
        public short ObjectID;
        public bool Joinable;
        public bool Ended;
        public List<VMEODEvent> Events = new List<VMEODEvent>();

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(AvatarID);
            writer.Write(ObjectID);
            writer.Write(Joinable);
            writer.Write(Ended);
            writer.Write((byte)Events.Count);
            foreach (var evt in Events) evt.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            AvatarID = reader.ReadInt16();
            ObjectID = reader.ReadInt16();
            Joinable = reader.ReadBoolean();
            Ended = reader.ReadBoolean();
            Events = new List<VMEODEvent>();
            var totalEvt = reader.ReadByte();
            for (int i = 0; i < totalEvt; i++)
            {
                var evt = new VMEODEvent();
                evt.Deserialize(reader);
                Events.Add(evt);
            }
        }
    }
}
