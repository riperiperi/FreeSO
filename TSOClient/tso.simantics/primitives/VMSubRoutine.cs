/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using System.IO;

namespace FSO.SimAntics.Primitives
{

    /// <summary>
    /// There isnt actually a private call handler, This is part of the 
    /// </summary>
    public class VMSubRoutineOperand : VMPrimitiveOperand
    {
        public VMSubRoutineOperand()
        {
            Arguments = new short[4];
        }

        public VMSubRoutineOperand(short[] Args)
        {
            Arguments = Args;
        }

        public short[] Arguments;

        public short Arg0 { get { return Arguments[0]; } set { Arguments[0] = value; } }
        public short Arg1 { get { return Arguments[1]; } set { Arguments[1] = value; } }
        public short Arg2 { get { return Arguments[2]; } set { Arguments[2] = value; } }
        public short Arg3 { get { return Arguments[3]; } set { Arguments[3] = value; } }


        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                Arguments = new short[4];
                Arguments[0] = io.ReadInt16();
                Arguments[1] = io.ReadInt16();
                Arguments[2] = io.ReadInt16();
                Arguments[3] = io.ReadInt16();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Arg0);
                io.Write(Arg1);
                io.Write(Arg2);
                io.Write(Arg3);
            }
        }
        #endregion
    }
}
