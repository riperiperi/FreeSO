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
using FSO.SimAntics.Engine;
using System.IO;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Primitives
{
    public class VMOnlineJobsCall : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMOnlineJobsCallOperand)args;

            if (operand.Call == VMOnlineJobsCallMode.SetWorkMode || operand.Call == VMOnlineJobsCallMode.RemoveStatusMessage
                || operand.Call == VMOnlineJobsCallMode.AddStatusMessage || operand.Call == VMOnlineJobsCallMode.SetTimeRemaining)
            {
                if (context.VM.TSOState.JobUI == null) context.VM.TSOState.JobUI = new VMTSOJobUI();
            }
            var jobui = context.VM.TSOState.JobUI;
            switch (operand.Call)
            {
                case VMOnlineJobsCallMode.GotoJobLot:
                    context.VM.SignalLotSwitch(0x200);
                    break;
                case VMOnlineJobsCallMode.SetControllerID:
                    context.VM.SetGlobalValue(21, (context.StackObject == null) ? (short)0 : context.StackObject.ObjectID);
                    break;
                case VMOnlineJobsCallMode.GetRandomJob:
                    //TODO: for now this disables: nightclub (dj, dancer), cook (not actually a job in tso)
                    context.Thread.TempRegisters[0] = (short)(context.VM.Context.NextRandom(2) + 1);
                    break;
                case VMOnlineJobsCallMode.IsJobAvailable:
                    return (context.Thread.TempRegisters[0] < 3)?VMPrimitiveExitCode.GOTO_TRUE:VMPrimitiveExitCode.GOTO_FALSE;
                case VMOnlineJobsCallMode.AttemptToValidateWorker:
                    var avatar = ((VMAvatar)context.Caller);
                    if (avatar.GetPersonData(VMPersonDataVariable.OnlineJobStatusFlags) == 0) avatar.SetPersonData(VMPersonDataVariable.OnlineJobStatusFlags, 1);
                    break;
                case VMOnlineJobsCallMode.AddStatusMessage:
                    //from STR# 506, adds to the end of the job status text.
                    //temp 0: is semiglobals (1 for yes)
                    //temp 1: string id to add (1 based, like dialog)
                    var table = (context.Thread.TempRegisters[0] == 0) ?
                        context.ScopeResource.Get<STR>(506) :
                        context.Callee.SemiGlobal.Get<STR>(506);
                    var str = table.GetString(Math.Max(0, context.Thread.TempRegisters[1] - 1));
                    var message = VMDialogHandler.ParseDialogString(context, str, table);
                    jobui.MessageText.Add(message);
                    break;
                case VMOnlineJobsCallMode.RemoveStatusMessage:
                    //remove status message at index temp 0. (that doesnt seem to work so we're just removing the 1st one we can find)
                    var index = context.Thread.TempRegisters[0];
                    jobui.MessageText.Clear();
                    /*
                    if (index > 0 && index < jobui.MessageText.Count)
                    {

                    }*/
                    break;
                case VMOnlineJobsCallMode.SetTimeRemaining:
                    //temp 0: minutes
                    //temp 1: seconds
                    jobui.Minutes = context.Thread.TempRegisters[0];
                    jobui.Seconds = context.Thread.TempRegisters[1];
                    break;
                case VMOnlineJobsCallMode.SetWorkMode:
                    //temp 0: work mode
                    //0: prework, 1:afterwork, 2:intermission, 3:round
                    jobui.Mode = (VMTSOJobMode)context.Thread.TempRegisters[0];
                    break;
                default:
                    break; 
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }

    }

    public class VMOnlineJobsCallOperand : VMPrimitiveOperand
    {
        public VMOnlineJobsCallMode Call;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Call = (VMOnlineJobsCallMode)io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write((byte)Call);
            }
        }
        #endregion
    }

    public enum VMOnlineJobsCallMode : byte
    {
        GotoJobLot = 0,
        SetControllerID = 1,
        AttemptToValidateWorker = 2,
        Unused1 = 3,
        Unused2 = 4,
        AddStatusMessage = 5,
        RemoveStatusMessage = 6,
        SetTimeRemaining = 7,
        GetRandomJob = 8,
        IsJobAvailable = 9,
        SignalJobTypeChange = 10,
        SignalJobGradeChange = 11,
        SetWorkMode = 12
    }
}
