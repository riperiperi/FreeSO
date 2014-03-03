using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;

namespace TSO.Simantics.primitives
{

    /// <summary>
    /// There isnt actually a private call handler, This is part of the 
    /// </summary>
    public class VMSubRoutineOperand : VMPrimitiveOperand
    {
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
