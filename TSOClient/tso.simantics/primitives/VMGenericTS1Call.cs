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

            var inventoryInd = 10;
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
                // 3. PullDownTaxiDialog
                /*
                 * AddToFamily = 4,
                   CombineAssetsOfFamilyInTemp0 = 5,
                   RemoveFromFamily = 6,
                   MakeNewNeighbor = 7, //this one is "depracated"
                   FamilyTutorialComplete = 8,
                   ArchitectureTutorialComplete = 9,
                   DisableBuildBuy = 10,
                   EnableBuildBuy = 11,
                   GetDistanceToCameraInTemp0 = 12,
                   AbortInteractions = 13, //abort all interactions associated with the stack object
                 **/
                case VMGenericTS1CallMode.HouseRadioStationEqualsTemp0: //14
                    context.VM.SetGlobalValue(31, context.Thread.TempRegisters[0]);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.MyRoutingFootprintEqualsTemp0: //15
                    //todo: change the avatar's routing footprint (need to find out how exactly this is changed in the normal game)
                    break;
                // 16. Change Normal Outfit
                case VMGenericTS1CallMode.ChangeToLotInTemp0: //17
                    //-1 is this family's home lot
                    var crossData = Content.Content.Get().Neighborhood.GameState;
                    crossData.ActiveFamily = context.VM.CurrentFamily;
                    crossData.DowntownSimGUID = context.Caller.Object.OBJ.GUID;
                    crossData.LotTransitInfo = context.VM.GetGlobalValue(34);
                    context.VM.SignalLotSwitch((uint)context.Thread.TempRegisters[0]);
                    return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
                case VMGenericTS1CallMode.BuildTheDowntownSimAndPlaceObjIDInTemp0: //18
                    //spawn downtown sim out of world

                    var crossDataDT = Content.Content.Get().Neighborhood.GameState;

                    var control = context.VM.Context.CreateObjectInstance(crossDataDT.DowntownSimGUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH)?.BaseObject;
                    ((Model.TSOPlatform.VMTSOAvatarState)control.TSOState).Permissions = Model.TSOPlatform.VMTSOAvatarPermissions.Owner;
                    context.VM.SetGlobalValue(3, control.ObjectID);
                    context.VM.SendCommand(new VMNetChangeControlCmd() { TargetID = control.ObjectID });
                    crossDataDT.ActiveFamily.SelectOneMember(crossDataDT.DowntownSimGUID);
                    context.VM.ActivateFamily(crossDataDT.ActiveFamily);

                    context.Thread.TempRegisters[0] = context.VM.GetGlobalValue(3);
                    if (VM.UseWorld) context.VM.Context.World.CenterTo((AvatarComponent)(context.VM.GetObjectById(context.VM.GetGlobalValue(3))?.WorldUI));
                    break;
                case VMGenericTS1CallMode.SpawnDowntownDateOfPersonInTemp0: //18
                    //spawn our autofollow sim
                    var neighbourhood = Content.Content.Get().Neighborhood;
                    var ntarget = (VMAvatar)context.VM.GetObjectById(context.Thread.TempRegisters[0]);
                    var neighbour = ntarget.GetPersonData(Model.VMPersonDataVariable.NeighborId);
                    var inventory = neighbourhood.GetInventoryByNID(neighbour);
                    if (inventory != null)
                    {
                        var toSpawn = inventory.FirstOrDefault(x => x.Type == 2 && x.GUID == inventoryInd)?.Count;
                        if (toSpawn != null)
                        {
                            var spawntarg = neighbourhood.GetNeighborByID((short)toSpawn);
                            var autofollow = context.VM.Context.CreateObjectInstance(spawntarg.GUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH)?.BaseObject;
                            context.Thread.TempRegisters[0] = autofollow.ObjectID;
                            inventory.RemoveAll(x => x.Type == 2 && x.GUID == inventoryInd);
                        }
                    }
                    break;
                case VMGenericTS1CallMode.SpawnTakeBackHomeDataOfPersonInTemp0:
                    inventoryInd = 11;
                    goto case VMGenericTS1CallMode.SpawnDowntownDateOfPersonInTemp0;
                // 21. SpawnInventorySimDataEffects
                case VMGenericTS1CallMode.SelectDowntownLot: //22
                    //TODO: this is a pre-unleashed system I believe
                    return VMPrimitiveExitCode.GOTO_TRUE;
                // 23. GetDowntownTimeFromSOInventory (the time it was in downtown, from stack objects inventory)
                // 24. HotDateChangeSuitsPermanentlyCall
                // 25. SaveSimPersistentData (motives, relationships)
                case VMGenericTS1CallMode.BuildVacationFamilyPutFamilyNumInTemp0: //26
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
                    }
                    else
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

                    context.Thread.TempRegisters[1] = (short)((crossData2.LotTransitInfo >= 1) ? 1 : 0);
                    break;
                case VMGenericTS1CallMode.ReturnNumberOfAvaiableVacationLotsInTemp0: //27
                    //TODO: vacation lots are disabled when other people have saved the game there! that's what this primitive checks.
                    context.Thread.TempRegisters[0] = 9;
                    break;
                case VMGenericTS1CallMode.ReturnZoningTypeOfLotInTemp0: //28
                    var zones = Content.Content.Get().Neighborhood.ZoningDictionary;
                    short result = 1;
                    if (zones.TryGetValue(context.Thread.TempRegisters[0], out result))
                        context.Thread.TempRegisters[0] = result;
                    else context.Thread.TempRegisters[0] = (short)((context.Thread.TempRegisters[0] >= 81 && context.Thread.TempRegisters[0] <= 89) ? 2 : 1);
                    return VMPrimitiveExitCode.GOTO_TRUE;

                // 29. SetStackObjectsSuit //suit type in temp0, suit index in temp1. Returns old index in temp1. (where are these saved?)
                // 30. GetStackObjectsSuit //suit type in temp0, suit index in temp1.
                // 31. CountStackObjectSuits
                // 32. CreatePurchasedPetsNearOwner
                // 33. AddToFamilyInTemp0 
                // 34. PromoteFameIfNeeded
                case VMGenericTS1CallMode.TakeTaxiHook: //35
                    //not sure where this one is called, seems to have been added for studiotown
                    break;
                // 36. DemoteFameIfNeeded
                // 37. CancelPieMenu
                // 38. GetTokensFromString (MM)
                // 39. ChildToAdult (let's make this at least keep their skin colour, maybe)
                // 40. PetToAdult
                // 41. HeadFlush
                // 42. MakeTemp0SelectedSim,
                // 43. FamilySpellsIntoController
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
