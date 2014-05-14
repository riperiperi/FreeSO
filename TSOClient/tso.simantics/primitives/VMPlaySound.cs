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

            if (fwav != null)
            {
                var thread = HITVM.Get().PlaySoundEvent(fwav.Name);
                if (thread != null)
                {
                    var owner = (operand.StackObjAsSource)?context.StackObject:context.Caller;

                    if (!thread.AlreadyOwns(owner.ObjectID)) thread.AddOwner(owner.ObjectID);

                    var entry = new VMSoundEntry()
                    {
                        Thread = thread,
                        Pan = !operand.NoPan,
                        Zoom = !operand.NoZoom,
                        Loop = operand.Loop,
                        Name = fwav.Name
                    };
                    owner.SoundThreads.Add(entry);
                    owner.TickSounds();
                }
            }

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

        public bool NoPan {
            get { return (Flags&8) == 8; }
        }

        public bool NoZoom {
            get { return (Flags&4) == 4; }
        }

        public bool Loop
        {
            get { return (Flags & 1) == 1; }
        }

        public bool StackObjAsSource
        {
            get { return (Flags & 2) == 2; }
        }
    }
}