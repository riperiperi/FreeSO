using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSO.Simantics.net.model
{
    public class VMNetTickList : VMSerializable
    {
        public List<VMNetTick> Ticks;

        #region VMSerializable Members

        public void SerializeInto(BinaryWriter writer)
        {
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
