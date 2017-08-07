using FSO.Files.Utils;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
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
                    else context.Thread.TempRegisters[0] = (short)((context.Thread.TempRegisters[0] >= 81 && context.Thread.TempRegisters[0] <= 89) ? 2 : 1);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.ChangeToLotInTemp0:
                    //-1 is this family's home lot
                    var crossData = Content.Content.Get().Neighborhood.GameState;
                    crossData.ActiveFamily = context.VM.CurrentFamily;
                    crossData.DowntownSimGUID = context.Caller.Object.OBJ.GUID;
                    crossData.LotTransitInfo = context.VM.GetGlobalValue(34);
                    context.VM.SignalLotSwitch((uint)context.Thread.TempRegisters[0]);
                    return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
                case VMGenericTS1CallMode.SelectDowntownLot:
                    break;
                case VMGenericTS1CallMode.BuildTheDowntownSimAndPlaceObjIDInTemp0:

                    //spawn downtown sim out of world

                    var crossDataDT = Content.Content.Get().Neighborhood.GameState;

                    var control = context.VM.Context.CreateObjectInstance(crossDataDT.DowntownSimGUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH)?.BaseObject;
                    ((Model.TSOPlatform.VMTSOAvatarState)control.TSOState).Permissions = Model.TSOPlatform.VMTSOAvatarPermissions.Owner;
                    control.TSOState.Budget.Value = 1000000;
                    context.VM.SetGlobalValue(3, control.ObjectID);
                    context.VM.SendCommand(new VMNetChangeControlCmd() { TargetID = control.ObjectID });
                    crossDataDT.ActiveFamily.SelectOneMember(crossDataDT.DowntownSimGUID);
                    context.VM.ActivateFamily(crossDataDT.ActiveFamily);

                    context.Thread.TempRegisters[0] = context.VM.GetGlobalValue(3);
                    if (VM.UseWorld) context.VM.Context.World.CenterTo((AvatarComponent)(context.VM.GetObjectById(context.VM.GetGlobalValue(3))?.WorldUI));
                    break;
                case VMGenericTS1CallMode.BuildVacationFamilyPutFamilyNumInTemp0:
                    //in our implementation, vacation lots build the family in the same way as normal lots.
                    var crossData2 = Content.Content.Get().Neighborhood.GameState;
                    if (crossData2.LotTransitInfo >= 1)
                    {
                        crossData2.ActiveFamily.SelectWholeFamily();
                        context.VM.ActivateFamily(crossData2.ActiveFamily);
                        context.Thread.TempRegisters[0] = context.VM.GetGlobalValue(9);

                        //set to 1 if we spawned a whole family.
                        //seems to be from globals 34 on the lot we exited. Magic town uses 0 for a single sim, and 1 for whole family 
                        //(blimp, though 1 is still set for whole family when theres only one person in it!)

                        context.VM.VerifyFamily();
                        context.VM.SendCommand(new VMNetChangeControlCmd() { TargetID = context.VM.Context.ObjectQueries.GetObjectsByGUID(crossData2.DowntownSimGUID).FirstOrDefault()?.ObjectID ?? 0 });
                    } else
                    {
                        var control2 = context.VM.Context.CreateObjectInstance(crossData2.DowntownSimGUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH)?.BaseObject;
                        ((Model.TSOPlatform.VMTSOAvatarState)control2.TSOState).Permissions = Model.TSOPlatform.VMTSOAvatarPermissions.Owner;
                        control2.TSOState.Budget.Value = 1000000;
                        context.VM.SetGlobalValue(3, control2.ObjectID);
                        context.VM.SendCommand(new VMNetChangeControlCmd() { TargetID = control2.ObjectID });
                        crossData2.ActiveFamily.SelectOneMember(crossData2.DowntownSimGUID);
                        context.VM.ActivateFamily(crossData2.ActiveFamily);

                        context.Thread.TempRegisters[0] = context.VM.GetGlobalValue(3);
                    }

                    context.Thread.TempRegisters[1] = (short)((crossData2.LotTransitInfo >= 1)?1:0);
                    break;
                case VMGenericTS1CallMode.TakeTaxiHook:
                    //unused past hot date?
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
