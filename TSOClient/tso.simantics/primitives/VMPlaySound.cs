using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.utils;
using TSO.Files.formats.iff.chunks;
using TSO.Simantics.engine;
using TSO.HIT;

namespace TSO.Simantics.primitives
{
    public class VMPlaySound : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMPlaySoundOperand>();
            FWAV fwav = context.CodeOwner.Get<FWAV>(operand.EventID);
            if (fwav == null) fwav = context.VM.Context.Globals.Resource.Get<FWAV>(operand.EventID);

            if (fwav != null) HITVM.Get().PlaySoundEvent(fwav.Name); //todo, recieve and manage thread so we can terminate it on Stop All.

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMPlaySoundOperand : VMPrimitiveOperand {

        public ushort EventID;
        public ushort Pad;
        public byte Flags;
        public byte Volume;

        #region VMPrimitiveOperand Members

        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                EventID = io.ReadUInt16();
                Pad = io.ReadUInt16();
                Flags = io.ReadByte();
                Volume = io.ReadByte();
            }
        }

        #endregion
    }
}