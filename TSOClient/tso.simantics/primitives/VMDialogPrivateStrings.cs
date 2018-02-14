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
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.Primitives
{
    public class VMDialogPrivateStrings : VMPrimitiveHandler
    {
        public static readonly int DIALOG_MAX_WAITTIME = 60 * 30;
        public static Dictionary<VMDialogType, int> TypeToNeighID = new Dictionary<VMDialogType, int>() {
            {VMDialogType.TS1Neighborhood, 4 },
            {VMDialogType.TS1Downtown, 2 },
            {VMDialogType.TS1Vacation, 3 },
            {VMDialogType.TS1StudioTown, 5 },
            {VMDialogType.TS1Magictown, 7 },
        };

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            return ExecuteGeneric(context, args, context.ScopeResource.Get<STR>(301));
        }

        public static VMPrimitiveExitCode ExecuteGeneric(VMStackFrame context, VMPrimitiveOperand args, STR table)
        {
            var operand = (VMDialogOperand)args;
            var curDialog = (VMDialogResult)context.Thread.BlockingState;
            if (curDialog == null)
            {
                //in ts1, it's possible for a lot of blocking dialogs to come in one frame. due to the way our engine works,
                //we cannot pause the rest of the tick as soon as we hit a blocking dialog, and we cannot show more than one blocking dialog.
                //so additional blocking dialogs must wait.
                if (context.VM.TS1 && context.VM.GlobalBlockingDialog != null) return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                VMDialogHandler.ShowDialog(context, operand, table);

                if ((operand.Flags & VMDialogFlags.Continue) == 0)
                {
                    context.Thread.BlockingState = new VMDialogResult
                    {
                        Type = operand.Type,
                        HasDisplayed = true
                    };
                    if (context.VM.TS1)
                    {
                        context.VM.GlobalBlockingDialog = context.Caller;
                        context.VM.LastSpeedMultiplier = context.VM.SpeedMultiplier;
                        context.VM.SpeedMultiplier = 0;
                    }
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                }
                else return VMPrimitiveExitCode.GOTO_TRUE;
            }
            else
            {
                if (curDialog.Responded || curDialog.WaitTime > DIALOG_MAX_WAITTIME)
                {
                    context.Thread.BlockingState = null;
                    context.VM.SignalDialog(null);
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
                            if ((curDialog.ResponseText ?? "") != "")
                            {
                                if (curDialog.ResponseText.Length > 32) curDialog.ResponseText = curDialog.ResponseText.Substring(0, 32);
                                context.StackObject.Name = curDialog.ResponseText;
                            }
                                
                            return VMPrimitiveExitCode.GOTO_TRUE;
                        case VMDialogType.FSOChars:
                            context.StackObject.MyList.Clear();
                            var charR = (curDialog.ResponseText ?? "");
                            if (charR != "") {
                                context.StackObject.MyList.Clear();
                                foreach (var c in charR)
                                    context.StackObject.MyList.AddLast((short)c);
                            }
                            return VMPrimitiveExitCode.GOTO_TRUE;
                        case VMDialogType.NumericEntry: //also downtown
                        case VMDialogType.TS1Vacation:
                        case VMDialogType.TS1Neighborhood:
                        case VMDialogType.TS1StudioTown:
                        case VMDialogType.TS1Magictown:
                            int number;
                            if (!int.TryParse(curDialog.ResponseText, out number)) return VMPrimitiveExitCode.GOTO_FALSE;

                            var tempNumber = ((operand.Flags & VMDialogFlags.UseTemp1) > 0) ? 1 : 0;

                            if ((operand.Flags & VMDialogFlags.UseTempXL) > 0) context.Thread.TempXL[tempNumber] = number;
                            else context.Thread.TempRegisters[tempNumber] = (short)number;
                            return VMPrimitiveExitCode.GOTO_TRUE;
                        case VMDialogType.FSOColor:
                            int number2;
                            if (curDialog.ResponseCode == 1) return VMPrimitiveExitCode.GOTO_FALSE;
                            if (!int.TryParse(curDialog.ResponseText, out number2)) return VMPrimitiveExitCode.GOTO_FALSE;
                            context.Thread.TempRegisters[0] = (byte)(number2 >> 16);
                            context.Thread.TempRegisters[1] = (byte)(number2 >> 8);
                            context.Thread.TempRegisters[2] = (byte)(number2);
                            return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                }
                else
                {
                    if (!curDialog.HasDisplayed)
                    {
                        VMDialogHandler.ShowDialog(context, operand, table);
                        curDialog.HasDisplayed = true;
                    }
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                }
            }
        }
    }

    public class VMDialogOperand : VMPrimitiveOperand
    {
        //engage and block sim, automatic icon, local reference 0, string not debug

        public byte CancelStringID { get; set; } //button 3. renaming used for genie, as an example.
        public byte IconNameStringID { get; set; }
        public byte MessageStringID { get; set; }
        public byte YesStringID { get; set; } //button 1
        public byte NoStringID { get; set; } //button 2
        public VMDialogType Type { get; set; }
        public byte TitleStringID { get; set; }
        public VMDialogFlags Flags; 

        public bool Continue
        {
            get
            {
                return (Flags & VMDialogFlags.Continue) == VMDialogFlags.Continue;
            }
            set
            {
                if (value) Flags |= VMDialogFlags.Continue;
                else Flags &= ~VMDialogFlags.Continue;
            }
        }
        public bool UseTempXL
        {
            get
            {
                return (Flags & VMDialogFlags.UseTempXL) == VMDialogFlags.UseTempXL;
            }
            set
            {
                if (value) Flags |= VMDialogFlags.UseTempXL;
                else Flags &= ~VMDialogFlags.UseTempXL;
            }
        }

        public bool UseTemp1
        {
            get
            {
                return (Flags & VMDialogFlags.UseTemp1) == VMDialogFlags.UseTemp1;
            }
            set
            {
                if (value) Flags |= VMDialogFlags.UseTemp1;
                else Flags &= ~VMDialogFlags.UseTemp1;
            }
        }

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

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(CancelStringID);
                io.Write(IconNameStringID);
                io.Write(MessageStringID);
                io.Write(YesStringID);
                io.Write(NoStringID);
                io.Write((byte)Type);
                io.Write(TitleStringID);
                io.Write((byte)Flags);
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
        UserBitmap = 8,

        TS1Downtown = 5, //house number in temp0
        TS1Clothes = 6,
        TS1Vacation = 7,
        TS1Neighborhood = 8,
        TS1PetChoice = 9,
        TS1PhoneBook = 10,
        TS1StudioTown = 11,
        TS1Spellbook = 12,
        TS1Magictown = 13,
        TS1TransformMe = 14,
        TS1Cookbook = 15,
        
        FSOColor = 128,
        FSOChars = 129
    }

    public class VMDialogResult : VMAsyncState
    {
        public int Timeout = 30 * 60;
        public byte ResponseCode; //0,1,2 = yes/ok,no,cancel.
        public string ResponseText = "";

        //local variables
        public bool HasDisplayed; //re-display dialog if we restore into it being up, in case UI missed it

        public VMDialogType Type; //used for input sanitization

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Timeout);
            writer.Write(ResponseCode);
            writer.Write((ResponseText == null)?"":ResponseText);
            writer.Write((byte)Type);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Timeout = reader.ReadInt32();
            ResponseCode = reader.ReadByte();
            ResponseText = reader.ReadString();
            Type = (VMDialogType)reader.ReadByte();
        }
    }
}
