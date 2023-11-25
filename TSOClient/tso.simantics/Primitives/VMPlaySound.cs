using System.Linq;
using FSO.Files.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;
using FSO.HIT;
using System.IO;
using FSO.SimAntics.Model.Sound;

namespace FSO.SimAntics.Primitives
{
    public class VMPlaySound : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            if (!VM.UseWorld) return VMPrimitiveExitCode.GOTO_TRUE;

            var operand = (VMPlaySoundOperand)args;
            FWAV fwav = context.ScopeResource.Get<FWAV>(operand.EventID);
            if (fwav == null) fwav = context.VM.Context.Globals.Resource.Get<FWAV>(operand.EventID);

            var owner = (operand.StackObjAsSource) ? context.StackObject : context.Caller;
            if (owner == null) return VMPrimitiveExitCode.GOTO_TRUE;
            var lastThread = owner.SoundThreads.FirstOrDefault(x => x?.Name == fwav?.Name);

            if ((lastThread?.Sound as HITThread)?.Interruptable == true) lastThread = null;
            if (fwav != null && lastThread == null)
            {
                var thread = HITVM.Get().PlaySoundEvent(fwav.Name);
                if (thread != null)
                {
                    if (owner == null) return VMPrimitiveExitCode.GOTO_TRUE;
                    if (!thread.AlreadyOwns(owner.ObjectID)) thread.AddOwner(owner.ObjectID);

                    var entry = new VMSoundEntry()
                    {
                        Sound = thread,
                        Pan = !operand.NoPan,
                        Zoom = !operand.NoZoom,
                        Loop = operand.Loop || fwav.Name == "piano_play",
                        Name = fwav.Name
                    };

                    if (thread is HITThread)
                    {
                        if (!((HITThread)thread).LoopDefined || fwav.Name == "piano_play")
                        {
                            ((HITThread)thread).Loop = entry.Loop;
                            ((HITThread)thread).HasSetLoop = fwav.Name == "piano_play";
                        }
                        owner.SubmitHITVars((HITThread)thread);
                    }

                    owner.SoundThreads.Add(entry);
                    context.VM.SoundEntities.Add(owner);
                    if (owner.Thread != null) owner.TickSounds();
                }
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMPlaySoundOperand : VMPrimitiveOperand {

        public ushort EventID { get; set; }
        public ushort SampleRate; // 8.8 fixed point, though i've yet to see something use this
        public byte Flags { get; set; }
        public byte Volume { get; set; }

        #region VMPrimitiveOperand Members

        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                EventID = io.ReadUInt16();
                SampleRate = io.ReadUInt16();
                Flags = io.ReadByte();
                Volume = io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(EventID);
                io.Write(SampleRate);
                io.Write(Flags);
                io.Write(Volume);
            }
        }

        #endregion
        public bool SimSpeedAffects
        {
            get { return (Flags & 32) == 32; }
            set
            {
                if (value) Flags |= 32;
                else Flags &= unchecked((byte)~32);
            }
        }

        public bool AutoVary
        {
            get { return (Flags & 16) == 16; }
            set
            {
                if (value) Flags |= 16;
                else Flags &= unchecked((byte)~16);
            }
        }

        public bool NoPan {
            get { return (Flags&8) == 8; }
            set
            {
                if (value) Flags |= 8;
                else Flags &= unchecked((byte)~8);
            }
        }

        public bool NoZoom {
            get { return (Flags&4) == 4; }
            set
            {
                if (value) Flags |= 4;
                else Flags &= unchecked((byte)~4);
            }
        }

        public bool Loop
        {
            get { return (Flags & 1) == 1; }
            set
            {
                if (value) Flags |= 1;
                else Flags &= unchecked((byte)~1);
            }
        }

        public bool StackObjAsSource
        {
            get { return (Flags & 2) == 2; }
            set
            {
                if (value) Flags |= 2;
                else Flags &= unchecked((byte)~2);
            }
        }
    }
}