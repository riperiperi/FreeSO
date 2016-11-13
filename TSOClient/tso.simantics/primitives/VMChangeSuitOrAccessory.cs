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
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Vitaboy;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMChangeSuitOrAccessory : VMPrimitiveHandler {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMChangeSuitOrAccessoryOperand)args;

            var avatar = (VMAvatar)context.Caller;

            if ((operand.Flags & VMChangeSuitOrAccessoryFlags.Update) == VMChangeSuitOrAccessoryFlags.Update)
            { //update default outfit with outfit in stringset 304 with index in temp 0
                avatar.DefaultSuits.Daywear = Convert.ToUInt64(context.Callee.Object.Resource.Get<STR>(304).GetString((context.Thread.TempRegisters[0])), 16);
                avatar.BodyOutfit = avatar.DefaultSuits.Daywear;
            } 
            else 
            {
                var suit = VMSuitProvider.GetSuit(context, operand.SuitScope, operand.SuitData);
                if (suit == null)
                {
                    return VMPrimitiveExitCode.GOTO_TRUE;
                }

                if (suit is string)
                {
                    var suitFile = (string)suit;
                    var apr = (VM.UseWorld)?FSO.Content.Content.Get().AvatarAppearances.Get(suitFile):null;
                    if ((operand.Flags & VMChangeSuitOrAccessoryFlags.Remove) == VMChangeSuitOrAccessoryFlags.Remove)
                    {
                        avatar.BoundAppearances.Remove(suitFile);
                        if (VM.UseWorld && apr != null) avatar.Avatar.RemoveAccessory(apr); 
                    }
                    else
                    {
                        avatar.BoundAppearances.Add(suitFile);
                        if (VM.UseWorld && apr != null) avatar.Avatar.AddAccessory(apr);
                    }
                } else if (suit is ulong)
                {
                    var oft = (ulong)suit;
                    avatar.SetPersonData(Model.VMPersonDataVariable.CurrentOutfit, operand.SuitData);
                    avatar.BodyOutfit = oft;
                }
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMChangeSuitOrAccessoryOperand : VMPrimitiveOperand {

        public byte SuitData;
        public VMSuitScope SuitScope;
        public VMChangeSuitOrAccessoryFlags Flags;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                SuitData = io.ReadByte();
                SuitScope = (VMSuitScope)io.ReadByte();
                Flags = (VMChangeSuitOrAccessoryFlags)io.ReadUInt16();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(SuitData);
                io.Write((byte)SuitScope);
                io.Write((ushort)Flags);
            }
        }
        #endregion
    }

    [Flags]
    public enum VMChangeSuitOrAccessoryFlags
    {
        Remove = 1,
        Update = 4
    }
}
