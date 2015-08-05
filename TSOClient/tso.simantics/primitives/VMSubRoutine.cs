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
        #endregion
    }
}
