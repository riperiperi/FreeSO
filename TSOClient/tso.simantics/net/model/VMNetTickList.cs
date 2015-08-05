/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Netplay.Model
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
