using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.utils;
using TSO.Simantics.engine;

namespace TSO.Simantics.primitives
{
    public class VMOnlineJobsCall : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMOnlineJobsCallOperand>();

            switch (operand.Call)
            {
                case VMOnlineJobsCallMode.SetControllerID:
                    context.VM.SetGlobalValue(21, (context.StackObject == null)?(short)0:context.StackObject.ObjectID);
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
