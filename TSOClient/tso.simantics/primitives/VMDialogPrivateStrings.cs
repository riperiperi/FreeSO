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
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.NetPlay.Model;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMDialogPrivateStrings : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            return ExecuteGeneric(context, args, context.ScopeResource.Get<STR>(301));
        }

        public static VMPrimitiveExitCode ExecuteGeneric(VMStackFrame context, VMPrimitiveOperand args, STR table)
        {
            var operand = (VMDialogOperand)args;
            var curDialog = context.Thread.BlockingDialog;
            if (context.Thread.BlockingDialog == null)
            {
                VMDialogHandler.ShowDialog(context, operand, table);

                if ((operand.Flags & VMDialogFlags.Continue) == 0)
                {
                    context.Thread.BlockingDialog = new VMDialogResult
                    {
                        Type = operand.Type
                    };
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                }
                else return VMPrimitiveExitCode.GOTO_TRUE;
            }
            else
            {
                if (curDialog.Responded)
                {
                    context.Thread.BlockingDialog = null;
                    switch (curDialog.Type)
                    {
                        default:
                        case VMDialogType.Message:
                            return VMPrimitiveExitCode.GOTO_TRUE;
                        case VMDialogType.YesNo:
                            return (curDialog.ResponseCode == 0) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                        case VMDialogType.YesNoCancel:
                            if (curDialog.ResponseCode > 1)
                            {
                                context.Thread.TempRegisters[((operand.Flags & VMDialogFlags.UseTemp1) > 0) ? 1 : 0] = (short)((curDialog.ResponseCode - 1) % 2);
                                return VMPrimitiveExitCode.GOTO_FALSE;
                            }
                            else return VMPrimitiveExitCode.GOTO_TRUE;
                        case VMDialogType.TextEntry:
                            //todo: filter profanity, limit name length
                            //also verify behaviour.
                            ((VMAvatar)context.StackObject).Name = curDialog.ResponseText;
                            return VMPrimitiveExitCode.GOTO_TRUE;
                        case VMDialogType.NumericEntry:
                            int number;
                            if (!int.TryParse(curDialog.ResponseText, out number)) return VMPrimitiveExitCode.GOTO_FALSE;

                            var tempNumber = ((operand.Flags & VMDialogFlags.UseTemp1) > 0) ? 1 : 0;

                            if ((operand.Flags & VMDialogFlags.UseTempXL) > 0) context.Thread.TempXL[tempNumber] = number;
                            else context.Thread.TempRegisters[tempNumber] = (short)number;
                            return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                }
                else return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }
        }
    }

    public class VMDialogOperand : VMPrimitiveOperand
    {
        //engage and block sim, automatic icon, local reference 0, string not debug

        public byte CancelStringID; //button 3. renaming used for genie, as an example.
        public byte IconNameStringID;
        public byte MessageStringID;
        public byte YesStringID; //button 1
        public byte NoStringID; //button 2
        public VMDialogType Type;
        public byte TitleStringID;
        public VMDialogFlags Flags; 

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                CancelStringID = io.ReadByte();
                IconNameStringID = io.ReadByte();
                MessageStringID = io.ReadByte();
                YesStringID = io.ReadByte();
                NoStringID = io.ReadByte();
                Type = (VMDialogType)io.ReadByte();
                TitleStringID = io.ReadByte();
                Flags = (VMDialogFlags)io.ReadByte();
            }
        }
        #endregion
    }

    [Flags]
    public enum VMDialogFlags
    {
        Continue = 1,

        Unknown1 = 2, // icon type? 3 bit int
        Unknown2 = 4, //
        Unknown3 = 8, // always 0 in tso.

        // Icon type:
        // 0 = auto,
        // 1 = none,
        // 2 = neighbour,
        // 3 = indexed,
        // 4 = named

        UseTempXL = 16,
        UseTemp1 = 32,
        FilterProfanity = 64,
        NewEngageContinue = 128
    }

    public enum VMDialogType : byte
    {
        Message = 0,
        YesNo = 1,
        YesNoCancel = 2,
        TextEntry = 3,
        Sims1Tutorial = 4,
        NumericEntry = 5,
        ImageMapped = 6, //truncated in edith..
        Custom = 7,
        UserBitmap = 8
    }

    public class VMDialogResult : VMSerializable
    {
        public int Timeout = 30 * 60;
        public bool Responded;
        public byte ResponseCode; //0,1,2 = yes/ok,no,cancel.
        public string ResponseText;

        public VMDialogType Type; //used for input sanitization

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Timeout);
            writer.Write(Responded);
            writer.Write(ResponseCode);
            writer.Write((ResponseText == null)?"":ResponseText);
            writer.Write((byte)Type);
        }

        public void Deserialize(BinaryReader reader)
        {
            Timeout = reader.ReadInt32();
            Responded = reader.ReadBoolean();
            ResponseCode = reader.ReadByte();
            ResponseText = reader.ReadString();
            Type = (VMDialogType)reader.ReadByte();
        }
    }
}
