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

namespace FSO.SimAntics.Primitives
{
    public class VMOnlineJobsCall : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMOnlineJobsCallOperand)args;

            switch (operand.Call)
            {
                case VMOnlineJobsCallMode.SetControllerID:
                    context.VM.SetGlobalValue(21, (context.StackObject == null) ? (short)0 : context.StackObject.ObjectID);
                    break;
                case VMOnlineJobsCallMode.GetRandomJob:
                    //TODO: for now this disables: nightclub (dj, dancer), cook (not actually a job in tso)
                    context.Thread.TempRegisters[0] = (short)(context.VM.Context.NextRandom(2) + 1);
                    break;
                case VMOnlineJobsCallMode.IsJobAvailable:
                    return (context.Thread.TempRegisters[0] < 3)?VMPrimitiveExitCode.GOTO_TRUE:VMPrimitiveExitCode.GOTO_FALSE; 
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
