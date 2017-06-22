using FSO.Files.Utils;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Primitives
{
    public class VMGenericTS1Call : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGenericTS1CallOperand)args;

            switch (operand.Call)
            {
                // 0. HOUSE TUTORIAL COMPLETE
                case VMGenericTS1CallMode.SwapMyAndStackObjectsSlots: //1
                    var cont1 = context.Caller.Container;
                    var cont2 = context.StackObject.Container;
                    var contS1 = context.Caller.ContainerSlot;
                    var contS2 = context.StackObject.ContainerSlot;
                    if (cont1 != null && cont2 != null)
                    {
                        context.Caller.PrePositionChange(context.VM.Context);
                        context.StackObject.PrePositionChange(context.VM.Context);
                        cont1.ClearSlot(contS1);
                        cont2.ClearSlot(contS2);
                        cont1.PlaceInSlot(context.StackObject, contS1, true, context.VM.Context);
                        cont2.PlaceInSlot(context.Caller, contS2, true, context.VM.Context);
                    }
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.SetActionIconToStackObject: //2
                    context.Thread.Queue[0].IconOwner = context.StackObject;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.HouseRadioStationEqualsTemp0:
                    context.VM.SetGlobalValue(31, context.Thread.TempRegisters[0]);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.ReturnZoningTypeOfLotInTemp0:
                    var zones = Content.Content.Get().Neighborhood.ZoningDictionary;
                    short result = 1;
                    if (zones.TryGetValue(context.Thread.TempRegisters[0], out result))
                        context.Thread.TempRegisters[0] = result;
                    else context.Thread.TempRegisters[0] = (short)((context.Thread.TempRegisters[0] >= 81 && context.Thread.TempRegisters[0] <= 89)?2:1);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.ChangeToLotInTemp0:
                    context.VM.SignalLotSwitch((uint)context.Thread.TempRegisters[0]);
                    return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
                case VMGenericTS1CallMode.SelectDowntownLot:
                    break;
                case VMGenericTS1CallMode.BuildTheDowntownSimAndPlaceObjIDInTemp0:
                    //currently downtown sim is just in selected sim id.
                    context.Thread.TempRegisters[0] = context.VM.GetGlobalValue(3);
                    break;
                case VMGenericTS1CallMode.BuildVacationFamilyPutFamilyNumInTemp0:
                    //in our implementation, vacation lots build the family in the same way as normal lots.
                    context.Thread.TempRegisters[0] = context.VM.GetGlobalValue(9);
                    context.Thread.TempRegisters[1] = 1; //set to 1 if we spawned a whole family.
                    context.VM.VerifyFamily();
                    break;
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGenericTS1CallOperand : VMPrimitiveOperand
    {
        public VMGenericTS1CallMode Call;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Call = (VMGenericTS1CallMode)io.ReadByte();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write((byte)Call);
            }
        }
        #endregion
    }
}
