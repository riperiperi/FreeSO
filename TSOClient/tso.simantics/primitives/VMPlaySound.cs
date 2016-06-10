/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            if (fwav != null)
            {
                var thread = HITVM.Get().PlaySoundEvent(fwav.Name);
                if (thread != null)
                {
                    var owner = (operand.StackObjAsSource)?context.StackObject:context.Caller;
                    if (owner == null) return VMPrimitiveExitCode.GOTO_TRUE;
                    if (!thread.AlreadyOwns(owner.ObjectID)) thread.AddOwner(owner.ObjectID);

                    if (owner is VMAvatar && thread is HITThread) ((VMAvatar)owner).SubmitHITVars((HITThread)thread);

                    var entry = new VMSoundEntry()
                    {
                        Sound = thread,
                        Pan = !operand.NoPan,
                        Zoom = !operand.NoZoom,
                        Loop = operand.Loop,
                        Name = fwav.Name
                    };
                    owner.SoundThreads.Add(entry);
                    if (owner.Thread != null) owner.TickSounds();
                }
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMPlaySoundOperand : VMPrimitiveOperand {

        public ushort EventID { get; set; }
        public ushort Pad;
        public byte Flags { get; set; }
        public byte Volume { get; set; }

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

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(EventID);
                io.Write(Pad);
                io.Write(Flags);
                io.Write(Volume);
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