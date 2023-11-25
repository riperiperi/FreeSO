using System.Collections.Generic;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model
{
    public class VMNetTickList : VMSerializable
    {
        public bool ImmediateMode = false;
        public List<VMNetTick> Ticks;

        #region VMSerializable Members

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ImmediateMode);
            if (Ticks == null) writer.Write(0);
            else
            {
                writer.Write(Ticks.Count);
                for (int i=0; i<Ticks.Count; i++)
                {
                    Ticks[i].SerializeInto(writer);
                }
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            ImmediateMode = reader.ReadBoolean();
            Ticks = new List<VMNetTick>();
            int length = reader.ReadInt32();
            for (int i=0; i<length; i++)
            {
                var cmds = new VMNetTick();
                cmds.Deserialize(reader);
                Ticks.Add(cmds);
            }
        }

        #endregion
    }
}
