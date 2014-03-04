using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;
using Microsoft.Xna.Framework;

namespace TSO.Simantics.primitives
{
    public class VMSnap : VMPrimitiveHandler // i don't know how routing works!! OH SNAP!!
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            return VMPrimitiveExitCode.GOTO_TRUE; //todo: actually implement once we have the full SLOTS location finder in place.
            //I'm letting this exist and not throw an exception for now so that I can test interactions on the toilet object and
            //its chain to the sinks.
        }
    }

    public class VMSnapOperand : VMPrimitiveOperand
    {
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {

            }
        }
        #endregion
    }
}
