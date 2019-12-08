using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Utils;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Utils;
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
                    context.Thread.ActiveAction.IconOwner = context.StackObject;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                // 3. PullDownTaxiDialog
                case VMGenericTS1CallMode.AddToFamily: //4
                    if (context.VM.TS1State.CurrentFamily == null || context.VM.TS1State.CurrentFamily.FamilyGUIDs.Length >= 8)
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    var fneigh = Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID);
                    if (fneigh == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    AddToFamily(context.VM.TS1State.CurrentFamily, fneigh, context.VM);
                    var runtime = context.VM.TS1State.CurrentFamily.RuntimeSubset.ToList();
                    runtime.Add(fneigh.GUID);
                    context.VM.TS1State.CurrentFamily.RuntimeSubset = runtime.ToArray();
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.CombineAssetsOfFamilyInTemp0: //5
                    //adds the family in temp 0's assets to our budget. (for move in)
                    var family = Content.Content.Get().Neighborhood.GetFamily((ushort)context.Thread.TempRegisters[0]);
                    context.VM.TS1State.CurrentFamily.Budget += family.ValueInArch + family.Budget;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.RemoveFromFamily: //6
                    if (context.VM.TS1State.CurrentFamily == null)
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    fneigh = Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID);
                    if (fneigh == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    fneigh.PersonData[(int)VMPersonDataVariable.TS1FamilyNumber] = 0;
                    var runtimef = GetRuntimeNeigh(context.VM, (ushort)context.StackObjectID);
                    runtimef?.SetPersonData(VMPersonDataVariable.TS1FamilyNumber, 0);

                    var guids = context.VM.TS1State.CurrentFamily.FamilyGUIDs.ToList();
                    guids.Remove(fneigh.GUID);
                    context.VM.TS1State.CurrentFamily.FamilyGUIDs = guids.ToArray();
                    TryDeleteFamily(context.VM.TS1State.CurrentFamily);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                /*
               MakeNewNeighbor = 7, //this one is "depracated". there's a dedicated primitive for this.
               FamilyTutorialComplete = 8,
               ArchitectureTutorialComplete = 9, */

                case VMGenericTS1CallMode.DisableBuildBuy: //10
                    context.VM.Context.Architecture.BuildBuyEnabled = false;
                    context.VM.SignalGenericVMEvt(VMEventType.TS1BuildBuyChange, 0);
                    break;
                case VMGenericTS1CallMode.EnableBuildBuy: //11
                    context.VM.Context.Architecture.BuildBuyEnabled = true;
                    context.VM.SignalGenericVMEvt(VMEventType.TS1BuildBuyChange, 1);
                    break;
                /*
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
                    var switchLotId = (uint)context.Thread.TempRegisters[0];
                    var vacation = switchLotId >= 40 && switchLotId < 50;
                    var crossData = Content.Content.Get().Neighborhood.GameState;
                    crossData.ActiveFamily = context.VM.TS1State.CurrentFamily;
                    crossData.DowntownSimGUID = context.Caller.Object.OBJ.GUID;
                    crossData.LotTransitInfo = (vacation) ? (short)1 : context.VM.GetGlobalValue(34);
                    var people = new List<VMAvatar>();

                    people.Add((VMAvatar)context.Caller);
                    if (crossData.LotTransitInfo >= 1)
                    {
                        foreach (VMAvatar person in context.VM.Context.ObjectQueries.Avatars)
                        {
                            if (person.GetPersonData(VMPersonDataVariable.TS1FamilyNumber) == crossData.ActiveFamily.ChunkID && person != context.Caller)
                                people.Add(person);
                        }
                    }

                    int pi = 0;
                    foreach (var person in people)
                    {
                        var nid = person.GetPersonData(VMPersonDataVariable.NeighborId);
                        var dtInv = InitInventory(nid);

                        SaveIData(dtInv, 0, person.GetMotiveData(VMMotive.Bladder));
                        SaveIData(dtInv, 1, person.GetMotiveData(VMMotive.Comfort));
                        SaveIData(dtInv, 2, person.GetMotiveData(VMMotive.Energy));
                        SaveIData(dtInv, 3, person.GetMotiveData(VMMotive.Fun));
                        SaveIData(dtInv, 4, person.GetMotiveData(VMMotive.Hunger));
                        SaveIData(dtInv, 5, person.GetMotiveData(VMMotive.Hygiene));
                        SaveIData(dtInv, 6, person.GetMotiveData(VMMotive.Social));

                        if (crossData.LotTransitInfo > 1)
                            SaveIData(dtInv, 9, (short)context.VM.TS1State.CurrentFamily.FamilyGUIDs.Length);

                        if (pi++ == 0)
                        {
                            SaveIData(dtInv, 7, (short)context.VM.Context.Clock.Hours);
                            SaveIData(dtInv, 8, (short)context.VM.Context.Clock.Minutes);
                        }
                    }

                    //the original game sends avatar motive data along in their inventory under type 2
                    //this is called "inventory sim data effects"


                    context.VM.SignalLotSwitch(switchLotId);
                    return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
                case VMGenericTS1CallMode.BuildTheDowntownSimAndPlaceObjIDInTemp0: //18
                    //spawn downtown sim out of world

                    var crossDataDT = Content.Content.Get().Neighborhood.GameState;

                    var control = context.VM.Context.CreateObjectInstance(crossDataDT.DowntownSimGUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH)?.BaseObject;
                    ((VMAvatar)control).AvatarState.Permissions = Model.TSOPlatform.VMTSOAvatarPermissions.Owner;
                    context.VM.SetGlobalValue(3, control.ObjectID);
                    context.VM.SendCommand(new VMNetChangeControlCmd() { TargetID = control.ObjectID });
                    crossDataDT.ActiveFamily.SelectOneMember(crossDataDT.DowntownSimGUID);
                    context.VM.TS1State.ActivateFamily(context.VM, crossDataDT.ActiveFamily);

                    context.Thread.TempRegisters[0] = context.VM.GetGlobalValue(3);
                    if (VM.UseWorld) context.VM.Context.World.CenterTo((AvatarComponent)(context.VM.GetObjectById(context.VM.GetGlobalValue(3))?.WorldUI));
                    break;
                case VMGenericTS1CallMode.SpawnDowntownDateOfPersonInTemp0: //18
                    //spawn our autofollow sim
                    var neighbourhood = Content.Content.Get().Neighborhood;
                    var ntarget = (VMAvatar)context.VM.GetObjectById(context.Thread.TempRegisters[0]);
                    context.Thread.TempRegisters[0] = -1;
                    if (ntarget == null) return VMPrimitiveExitCode.GOTO_FALSE; //vacation?
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
                case VMGenericTS1CallMode.SpawnInventorySimDataEffects:
                    //for the caller? stack object?
                    //do caller for now
                    var eperson = (VMAvatar)context.VM.GetObjectById(context.Thread.TempRegisters[0]);
                    var eInv = InitInventory(eperson.GetPersonData(VMPersonDataVariable.NeighborId));

                    if (eInv.Count(x => x.Type == 2) == 0)
                        return VMPrimitiveExitCode.GOTO_TRUE;

                    eperson.SetMotiveData(VMMotive.Bladder, GetIData(eInv, 0));
                    eperson.SetMotiveData(VMMotive.Comfort, GetIData(eInv, 1));
                    eperson.SetMotiveData(VMMotive.Energy, GetIData(eInv, 2));
                    eperson.SetMotiveData(VMMotive.Fun, GetIData(eInv, 3));
                    eperson.SetMotiveData(VMMotive.Hunger, GetIData(eInv, 4));
                    eperson.SetMotiveData(VMMotive.Hygiene, GetIData(eInv, 5));
                    eperson.SetMotiveData(VMMotive.Social, GetIData(eInv, 6));

                    //remove the effects since we've used em
                    //7 and 8, time, were used to start the lot. they arent really used on return
                    //9 is not used by fso

                    eInv.RemoveAll(x => x.Type == 2 && x.GUID < 7 && x.GUID >= 0);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.SelectDowntownLot: //22
                    //TODO: this is a pre-unleashed system I believe. likely need to add this if we want to support older TS1 versions (need testers)
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.GetDowntownTimeFromSOInventory: //23
                    //eperson = (VMAvatar)context.StackObject;
                    //eInv = InitInventory(eperson.GetPersonData(VMPersonDataVariable.NeighborId));
                    //context.Thread.TempRegisters[0] = GetIData(eInv, 7);
                    //context.Thread.TempRegisters[1] = GetIData(eInv, 8);
                    return VMPrimitiveExitCode.GOTO_TRUE; //UNUSED?
                case VMGenericTS1CallMode.HotDateChangeSuitsPermanentlyCall:
                    //temp 0: outfit type
                    //temp 1: outfit index
                    context.Thread.TempRegisters[1] = VMTS1PurchasableOutfitHelper.SetSuit(
                        (VMAvatar)context.Caller, 
                        context.Thread.TempRegisters[0],
                        context.Thread.TempRegisters[1]);

                    return VMPrimitiveExitCode.GOTO_TRUE;

                // 25. SaveSimPersistentData (motives, relationships?)
                case VMGenericTS1CallMode.SaveSimPersistentData:
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.BuildVacationFamilyPutFamilyNumInTemp0: //26
                    //in our implementation, vacation lots build the family in the same way as normal lots.
                    var crossData2 = Content.Content.Get().Neighborhood.GameState;
                    if (crossData2.LotTransitInfo >= 1)
                    {
                        crossData2.ActiveFamily.SelectWholeFamily();
                        context.VM.TS1State.ActivateFamily(context.VM, crossData2.ActiveFamily);
                        context.Thread.TempRegisters[0] = context.VM.GetGlobalValue(9);

                        //set to 1 if we spawned a whole family.
                        //seems to be from globals 34 on the lot we exited. Magic town uses 0 for a single sim, and 1 for whole family 
                        //(blimp, though 1 is still set for whole family when theres only one person in it!)

                        context.VM.TS1State.VerifyFamily(context.VM);
                        context.VM.SendCommand(new VMNetChangeControlCmd() { TargetID = context.VM.Context.ObjectQueries.GetObjectsByGUID(crossData2.DowntownSimGUID).FirstOrDefault()?.ObjectID ?? 0 });
                    }
                    else
                    {
                        var control2 = context.VM.Context.CreateObjectInstance(crossData2.DowntownSimGUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH)?.BaseObject;
                        ((VMAvatar)control2).AvatarState.Permissions = Model.TSOPlatform.VMTSOAvatarPermissions.Owner;
                        context.VM.SetGlobalValue(3, control2.ObjectID);
                        context.VM.SendCommand(new VMNetChangeControlCmd() { TargetID = control2.ObjectID });
                        crossData2.ActiveFamily.SelectOneMember(crossData2.DowntownSimGUID);
                        context.VM.TS1State.ActivateFamily(context.VM, crossData2.ActiveFamily);

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

                case VMGenericTS1CallMode.SetStackObjectsSuit:
                    context.Thread.TempRegisters[1] = VMTS1PurchasableOutfitHelper.SetSuit(
                        (VMAvatar)context.StackObject, 
                        context.Thread.TempRegisters[0],
                        context.Thread.TempRegisters[1]);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.GetStackObjectsSuit:
                    context.Thread.TempRegisters[1] = VMTS1PurchasableOutfitHelper.GetSuitIndex((VMAvatar)context.StackObject, context.Thread.TempRegisters[0]);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.CountStackObjectSuits:
                    var validSuits = VMTS1PurchasableOutfitHelper.GetValidOutfits((VMAvatar)context.StackObject, context.Thread.TempRegisters[0]);
                    context.Thread.TempRegisters[0] = (short)validSuits.Length;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                // 32. CreatePurchasedPetsNearOwner
                case VMGenericTS1CallMode.CreatePurchasedPetsNearOwner:
                    context.VM.TS1State.CurrentFamily.SelectWholeFamily();
                    context.VM.TS1State.VerifyFamily(context.VM);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTS1CallMode.AddToFamilyInTemp0:
                    family = Content.Content.Get().Neighborhood.GetFamily((ushort)context.Thread.TempRegisters[0]);
                    if (family == null || family.FamilyGUIDs.Length >= 8)
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    fneigh = Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID);
                    if (fneigh == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    AddToFamily(context.VM.TS1State.CurrentFamily, fneigh, context.VM);
                    return VMPrimitiveExitCode.GOTO_TRUE;
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
                case VMGenericTS1CallMode.MakeTemp0SelectedSim:
                    //right now assume there's only one ts1 client, and that's us.
                    var vm = context.VM;
                    var target = vm.GetObjectById(context.Thread.TempRegisters[0]);
                    if (target == null || target is VMGameObject) return VMPrimitiveExitCode.GOTO_FALSE;

                    var caller = vm.GetAvatarByPersist(vm.MyUID);
                    if (caller != null)
                    {
                        //relinquish previous control
                        vm.Context.ObjectQueries.RemoveAvatarPersist(caller.PersistID);
                        caller.PersistID = 0;
                    }

                    target.PersistID = vm.MyUID;
                    vm.Context.ObjectQueries.RegisterAvatarPersist((VMAvatar)target, target.PersistID);
                    vm.SetGlobalValue(3, target.ObjectID);
                    
                    return VMPrimitiveExitCode.GOTO_TRUE;
                // 43. FamilySpellsIntoController
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }

        private short GetIData(List<InventoryItem> inventory, uint guid)
        {
            return (short)(inventory.FirstOrDefault(x => x.Type == 2 && x.GUID == guid)?.Count ?? 0);
        }

        private void SaveIData(List<InventoryItem> inventory, uint guid, short data)
        {
            var replace = inventory.FirstOrDefault(x => x.Type == 2 && x.GUID == guid);
            if (replace == null)
            {
                replace = new InventoryItem() { Type = 2, GUID = guid };
                inventory.Add(replace);
            }
            replace.Count = (ushort)data;
        }

        private List<InventoryItem> InitInventory(short neighbour)
        {
            var neighbourhood = Content.Content.Get().Neighborhood;
            var inventory = neighbourhood.GetInventoryByNID(neighbour);
            if (inventory == null)
            {
                //set up this neighbour's inventory...
                inventory = new List<InventoryItem>();
                neighbourhood.SetInventoryForNID(neighbour, inventory);
            }
            return inventory;
        }

        private void TryDeleteFamily(FAMI family)
        {
            //delete the family if there's no people in it
            if (family.FamilyGUIDs.Length == 0)
            {
                family.ChunkParent.FullRemoveChunk(family);
            }
        }

        private void AddToFamily(FAMI family, Neighbour neigh, VM vm)
        {
            //was the neighbor already in a family?
            if (neigh.PersonData != null) {
                var famID = neigh.PersonData[(int)VMPersonDataVariable.TS1FamilyNumber];
                var oldFam = Content.Content.Get().Neighborhood.GetFamily((ushort)famID);
                if (oldFam != null && oldFam.ChunkID != 0)
                {
                    var oguids = oldFam.FamilyGUIDs.ToList();
                    oguids.Remove(neigh.GUID);
                    oldFam.FamilyGUIDs = oguids.ToArray();
                    TryDeleteFamily(oldFam);
                }
                neigh.PersonData[(int)VMPersonDataVariable.TS1FamilyNumber] = (short)family.ChunkID;
            }

            var guids = family.FamilyGUIDs.ToList();
            guids.Add(neigh.GUID);
            family.FamilyGUIDs = guids.ToArray();

            //if the sim is on the lot, change their runtime person data to reflect the new family id.
            var runtime = (VMAvatar)vm.Context.ObjectQueries.Avatars.FirstOrDefault(x => ((VMAvatar)x).GetPersonData(VMPersonDataVariable.NeighborId) == neigh.NeighbourID);
            runtime?.SetPersonData(VMPersonDataVariable.TS1FamilyNumber, (short)family.ChunkID);
        }

        private VMAvatar GetRuntimeNeigh(VM vm, ushort neighborID)
        {
            return (VMAvatar)vm.Context.ObjectQueries.Avatars.FirstOrDefault(x => ((VMAvatar)x).GetPersonData(VMPersonDataVariable.NeighborId) == neighborID);
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
