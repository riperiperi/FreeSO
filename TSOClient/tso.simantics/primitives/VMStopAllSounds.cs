using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;

namespace TSO.Simantics.primitives
{
    public class VMStopAllSounds : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMStopAllSoundsOperand>();

            var owner = (operand.Flags == 1)?context.StackObject:context.Caller;
            var threads = owner.SoundThreads;

            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].Thread.RemoveOwner(owner.ObjectID);
            }
            threads.Clear();

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMStopAllSoundsOperand : VMPrimitiveOperand
    {
        public byte Flags;
        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Flags = io.ReadByte();
            }
        }
        #endregion
    }
}
