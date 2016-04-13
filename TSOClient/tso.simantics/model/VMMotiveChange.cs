/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.SimAntics.Model
{
    public class VMMotiveChange : VMSerializable
    {
        public short PerHourChange;
        public short MaxValue;
        public VMMotive Motive;
        private double fractional;
        public bool Ticked;

        public void Clear()
        {
            PerHourChange = 0;
            MaxValue = short.MaxValue;
        }

        public void Tick(VMAvatar avatar)
        {
            Ticked = true;
            if (PerHourChange != 0)
            {
                double rate = (PerHourChange/60.0)/(30.0*5.0); //timed for 5 second minutes
                fractional += rate;
                if (Math.Abs(fractional) >= 1)
                {
                    var motive = avatar.GetMotiveData(Motive);
                    motive += (short)(fractional);
                    fractional %= 1.0;

                    if (((rate > 0) && (motive > MaxValue)) || ((rate < 0) && (motive < MaxValue))) { motive = MaxValue; Clear(); }
                    avatar.SetMotiveData(Motive, motive);
                }
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(PerHourChange);
            writer.Write(MaxValue);
            writer.Write((byte)Motive);
            writer.Write(fractional);
        }

        public void Deserialize(BinaryReader reader)
        {
            PerHourChange = reader.ReadInt16();
            MaxValue = reader.ReadInt16();
            Motive = (VMMotive)reader.ReadByte();
            fractional = reader.ReadDouble();
        }
    }
}
