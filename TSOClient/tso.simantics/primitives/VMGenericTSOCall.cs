﻿/*
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
using FSO.SimAntics.Model;
using System.IO;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Drivers;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics.Primitives
{

    public class VMGenericTSOCall : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGenericTSOCallOperand)args;

            switch (operand.Call)
            {
                // 0. HOUSE TUTORIAL COMPLETE
                case VMGenericTSOCallMode.SwapMyAndStackObjectsSlots: //1
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
                case VMGenericTSOCallMode.SetActionIconToStackObject: //2
                    context.Thread.ActiveAction.IconOwner = context.StackObject;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                // 3. DO NOT USE
                case VMGenericTSOCallMode.IsStackObjectARoommate: //4 
                    return (context.StackObject is VMGameObject 
                        || ((VMAvatar)context.StackObject).AvatarState.Permissions < VMTSOAvatarPermissions.Roommate)
                        ? VMPrimitiveExitCode.GOTO_FALSE : VMPrimitiveExitCode.GOTO_TRUE;
                // 5. Combine Assets of family in Temp0
                // 6. Remove From Family
                // 7. Make New Neighbour
                // 8. Family Sims1 Tutorial Complete
                // 9. Architecture Sims1 Tutorial Complete
                // 10. Disable Build/Buy
                // 11. Enable Build/Buy
                // 12. Distance to camera in Temp0
                // 13. Abort Interactions
                case VMGenericTSOCallMode.AbortInteractions:
                    return VMPrimitiveExitCode.GOTO_TRUE;

                // 14. House Radio Station equals Temp0 (TODO)
                case VMGenericTSOCallMode.HouseRadioStationEqualsTemp0:
                    context.VM.SetGlobalValue(31, context.Thread.TempRegisters[0]);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                // 15. My Routing Footprint equals Temp0
                // 16. Change Normal Output
                case VMGenericTSOCallMode.GetInteractionResult: //17
                    //if our current interaction result is -1, then we need to start the process.
                    if (context.ActionTree && context.Caller is VMAvatar && ((VMAvatar)context.Caller).PersistID != 0)
                    {
                        var interaction = context.Thread.ActiveAction;
                        if (interaction.InteractionResult == -1) interaction.InteractionResult = 0;
                        else interaction.ResultCheckCounter++;

                        if (interaction.InteractionResult > 0 || interaction.ResultCheckCounter < 30 * 10)
                            context.Thread.TempRegisters[0] = interaction.InteractionResult; //0=waiting, 1=reject, 2=accept, 3=timeout
                        else context.Thread.TempRegisters[0] = 3;
                    } else
                        context.Thread.TempRegisters[0] = 2;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.SetInteractionResult: //18
                    //todo: set interaction result to value of temp 0. UNUSED.
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.DoIOwnThisObject: //19
                    context.Thread.TempRegisters[0] = (context.StackObject is VMAvatar
                    || ((VMTSOObjectState)context.StackObject.TSOState).OwnerID != context.Caller.PersistID)
                    ? (short)0 : (short)1;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.DoesTheLocalMachineSimOwnMe: //20
                    context.Thread.TempRegisters[1] = 1; //TODO
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.MakeMeStackObjectsOwner: //21
                    if (context.StackObject is VMAvatar) return VMPrimitiveExitCode.GOTO_TRUE;
                    foreach (var owned in context.StackObject.MultitileGroup.Objects)
                        ((VMTSOObjectState)owned.TSOState).OwnerID = context.Caller.PersistID;

                    if (context.VM.IsServer)
                        context.VM.GlobalLink.UpdateObjectPersist(context.VM, context.StackObject.MultitileGroup, (worked, objid) => { });
                    //TODO: immediately persist? what to do when new owner has hit their object limit?
                    return VMPrimitiveExitCode.GOTO_TRUE;
                //TODO: may need to update in global server
                // 22. Get Permissions (TODO)
                // 23. Set Permissions (TODO)
                case VMGenericTSOCallMode.AskStackObjectToBeRoommate: //24
                    //0 = initiate. 1 = accept. 2 = reject.
                    if (context.VM.GlobalLink != null) context.VM.GlobalLink.RequestRoommate(context.VM, context.StackObject.PersistID, context.Thread.TempRegisters[0], 0);
                    return VMPrimitiveExitCode.GOTO_TRUE;

                case VMGenericTSOCallMode.LeaveLot: //25
                    ((VMAvatar)context.Caller).UserLeaveLot();
                    if (context.VM.GlobalLink != null)
                    {
                        context.VM.GlobalLink.LeaveLot(context.VM, (VMAvatar)context.Caller);
                    }
                    else
                    {
                        // use our stub to remove the sim and potentially disconnect the client.
                        context.VM.CheckGlobalLink.LeaveLot(context.VM, (VMAvatar)context.Caller);
                    }
                    return VMPrimitiveExitCode.GOTO_TRUE;

                // 26. UNUSED
                case VMGenericTSOCallMode.KickoutRoommate:
                    if (context.VM.GlobalLink != null) context.VM.GlobalLink.RemoveRoommate(context.VM, (VMAvatar)context.StackObject);
                    return VMPrimitiveExitCode.GOTO_TRUE;

                case VMGenericTSOCallMode.KickoutVisitor:
                    if (context.VM.GlobalLink != null)
                    {
                        var server = (VMServerDriver)context.VM.Driver;
                        server.DropAvatar(context.StackObject as VMAvatar);
                    }
                    ((VMAvatar)context.StackObject).UserLeaveLot();
                    return VMPrimitiveExitCode.GOTO_TRUE;

                case VMGenericTSOCallMode.StackObjectOwnerID: //29
                    //attempt to find owner on lot. null stack object if not present
                    if (context.StackObject is VMAvatar) context.Thread.TempRegisters[0] = 0;
                    else
                    {
                        var owner = context.VM.GetObjectByPersist(((VMTSOObjectState)context.StackObject.TSOState).OwnerID);
                        context.Thread.TempRegisters[0] = (owner == null) ? (short)0 : owner.ObjectID;
                    }
                    return VMPrimitiveExitCode.GOTO_TRUE;

                //30. Create Cheat Neighbour
                case VMGenericTSOCallMode.IsTemp0AvatarIgnoringTemp1Avatar: //31
                    context.Thread.TempRegisters[0] = (short)(((VMTSOAvatarState)(context.VM.GetObjectById(context.Thread.TempRegisters[0]).TSOState))
                        .IgnoredAvatars.Contains(context.VM.GetObjectById(context.Thread.TempRegisters[1]).PersistID) ? 1 : 0);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                //32. Play Next Song on Radio Station in Temp 0 (TODO)
                case VMGenericTSOCallMode.Temp0AvatarUnignoreTemp1Avatar:
                    var avatar = context.VM.GetObjectById(context.Thread.TempRegisters[0]);
                    if (avatar.PersistID == context.VM.MyUID)
                    {
                        context.VM.SignalGenericVMEvt(VMEventType.TSOUnignore, (uint)context.VM.GetObjectById(context.Thread.TempRegisters[1]).PersistID);
                    }
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.GlobalRepairCostInTempXL0: //34
                    var mech = ((VMAvatar)context.Caller).GetPersonData(VMPersonDataVariable.MechanicalSkill);
                    var discount = (Math.Min((int)mech, 1000) * 50) / 1000 + (Math.Max(0, mech - 1000) * 2 / 100);
                    context.Thread.TempXL[0] = ((context.StackObject.MultitileGroup.InitialPrice / 10) * (100-discount)) / 100;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.GlobalRepairObjectState: //35
                    //repairs the stack object
                    if (context.StackObject is VMGameObject)
                    {
                        var state = (context.StackObject.MultitileGroup.BaseObject.TSOState as VMTSOObjectState);
                        var wearRecovery = 0; //in quarter percents

                        if (context.Caller is VMAvatar) {
                            var rava = (VMAvatar)context.Caller;
                            var logicFactor = Math.Min((ulong)rava.GetPersonData(VMPersonDataVariable.LogicSkill), 1000);
                            if (context.Caller.PersistID > 0) {
                                wearRecovery = (int)(((ulong)rava.GetPersonData(VMPersonDataVariable.MechanicalSkill) *
                                    (100 + (context.VM.Context.NextRandom(80) * (1000 - logicFactor) + 100 * logicFactor) / 1000)) / 2000);
                            } else
                            {
                                wearRecovery = 40; //10% recovery for repairman
                            }
                        }
                        state.QtrDaysSinceLastRepair = 0;
                        foreach (var objr in context.StackObject.MultitileGroup.Objects)
                        {
                            ((VMGameObject)objr).DisableParticle(256);
                        }
                        state.Wear = (ushort)Math.Max(20 * 4, state.Wear - wearRecovery);
                    }
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.IsGlobalBroken: //36
                    return ((context.StackObject.MultitileGroup.BaseObject.TSOState as VMTSOObjectState)?.Broken == true)?VMPrimitiveExitCode.GOTO_TRUE:VMPrimitiveExitCode.GOTO_FALSE;
                // 37. UNUSED
                case VMGenericTSOCallMode.MayAddRoommate: //38
                    // CONDITIONS, where stack object is desired roommate: (TODO: support extensions)
                    // - Avatar we're asking must be resident of less lots than the maximum (currently 1)
                    // - This lot must have less than (MAX_ROOMIES) roommates. (currently 8)
                    // - Caller must be lot owner.
                    short result = 0;
                    if (context.Caller is VMAvatar && context.Callee is VMAvatar)
                    {
                        var caller = (VMAvatar)context.Caller;
                        var callee = (VMAvatar)context.Callee;
                        if (caller.AvatarState.Permissions == VMTSOAvatarPermissions.Owner && context.VM.TSOState.Roommates.Count < 8
                            && (((VMTSOAvatarState)callee.TSOState).Flags & VMTSOAvatarFlags.CanBeRoommate) > 0)
                        {
                            result = 2;
                        }
                    }
                    context.Thread.TempRegisters[0] = result;
                    // 2 is "true". not sure what 1 is. (interaction shows up, but fails on trying to run it. likely "guessed" state for client)
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.ReturnLotCategory: //39
                    context.Thread.TempRegisters[0] = context.VM.TSOState.PropertyCategory; //skills lot. see #Lot Types in global.iff
                    //TODO: set based on lot state
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.TestStackObject: //40
                    return (context.StackObject != null) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                case VMGenericTSOCallMode.GetCurrentValue: //41
                    //stack object price in TempXL 0
                    context.Thread.TempXL[0] = context.StackObject.MultitileGroup.Price;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.IsRegionEmpty: //42
                    //is temp0 radius (in full tiles) around stack object empty?
                    //used for resurrect. TODO.
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.SetSpotlightStatus:
                    if (context.VM.GlobalLink != null) context.VM.GlobalLink.SetSpotlightStatus(context.VM, context.Thread.TempRegisters[0] == 1);
                    return VMPrimitiveExitCode.GOTO_TRUE;
                //44. Is Full Refund (TODO: small grace period after purchase/until user exits buy mode)
                //45. Refresh buy/build (TODO? we probably don't need this)
                case VMGenericTSOCallMode.GetLotOwner: //46
                    // TODO! global lot state not in yet
                    context.Thread.TempRegisters[0] = context.Caller.ObjectID;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.CopyDynObjNameFromTemp0ToTemp1: //47
                    var obj1 = context.VM.GetObjectById(context.Thread.TempRegisters[0]);
                    var obj2 = context.VM.GetObjectById(context.Thread.TempRegisters[1]);
                    obj2.Name = obj1.Name;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.GetIsPendingDeletion: //48
                    if (context.StackObject == null || context.StackObject.Dead) return VMPrimitiveExitCode.GOTO_TRUE;
                    return VMPrimitiveExitCode.GOTO_FALSE;
                //49. Pet Ran Away
                //50. Set Original Purchase Price (TODO: parrot, snowman. should set to temp0)
                case VMGenericTSOCallMode.HasTemporaryID: //51
                    //persist ID should be 0 til we get one.
                    return (context.StackObject.PersistID == 0 && context.StackObject.GetValue(VMStackObjectVariable.Reserved75) == 0)?VMPrimitiveExitCode.GOTO_TRUE:VMPrimitiveExitCode.GOTO_FALSE;
                case VMGenericTSOCallMode.SetStackObjOwnerToTemp0: //52
                    var obj = context.VM.GetObjectById(context.Thread.TempRegisters[0]); 
                    if (context.StackObject is VMAvatar || obj == null) return VMPrimitiveExitCode.GOTO_TRUE;

                    foreach (var owned in context.StackObject.MultitileGroup.Objects)
                        ((VMTSOObjectState)owned.TSOState).OwnerID = obj.PersistID;

                    if (context.VM.IsServer)
                        context.VM.GlobalLink.UpdateObjectPersist(context.VM, context.StackObject.MultitileGroup, (worked, objid) => { });

                    return VMPrimitiveExitCode.GOTO_TRUE;
                //53. Is On Editable Tile
                //54. Set Stack Object's Crafter Name To Avatar in Temp 0
                case VMGenericTSOCallMode.CalcHarvestComponents: //55
                    var tableq = Content.Content.Get().GlobalTuning?.EntriesByName["harvest component table"];
                    if (tableq == null) return VMPrimitiveExitCode.GOTO_FALSE;
                    var table = tableq.Value;
                    int[] componentTable = new int[] { 0, 0, 0 };
                    var objb = context.StackObject;
                    uint guid = objb.Object.OBJ.GUID;
                    if (objb.MasterDefinition != null) guid = objb.MasterDefinition.GUID;
                    var catalog = Content.Content.Get().WorldCatalog;
                    var item = catalog.GetItemByGUID(guid);
                    if (item != null)
                    {
                        string compString = null;
                        if (table.KeyValues.TryGetValue(item.Value.Category.ToString(), out compString))
                        {
                            var commentInd = compString.IndexOf(';');
                            if (commentInd == -1) commentInd = compString.Length;
                            var substr = compString.Substring(0, commentInd);
                            componentTable = substr.Split(',').Select(x => int.Parse(x)).ToArray();
                        }
                    }
                    var value = context.StackObject.MultitileGroup.Price;

                    //TODO: scale with wear, object age?
                    var ava = (VMAvatar)context.Caller;
                    float scale = 0.50f + ava.GetPersonData(VMPersonDataVariable.CreativitySkill) / 2000f + ava.GetPersonData(VMPersonDataVariable.MechanicalSkill) / 2000f;
                    value = (int)(value * scale);

                    context.Thread.TempRegisters[0] = (short)((7500 + context.VM.Context.NextRandom((ulong)(value*componentTable[0])))/10000); //wood
                    context.Thread.TempRegisters[1] = (short)((7500 + context.VM.Context.NextRandom((ulong)(value * componentTable[1]))) / 10000); //cloth
                    context.Thread.TempRegisters[2] = (short)((7500 + context.VM.Context.NextRandom((ulong)(value * componentTable[2]))) / 10000); //parts
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.IsStackObjectForSale: //56.
                    return (((context.StackObject as VMGameObject)?.Disabled)?.HasFlag(VMGameObjectDisableFlags.ForSale) ?? false) ?
                        VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;

                // === FREESO SPECIFIC ===
                case VMGenericTSOCallMode.FSOLightRGBFromTemp012: //128
                    context.StackObject.LightColor = new Color((int)context.Thread.TempRegisters[0], (int)context.Thread.TempRegisters[1],
                        (int)context.Thread.TempRegisters[2], (int)1);
                    if (context.StackObject.GetValue(VMStackObjectVariable.LightingContribution) > 0)
                        context.VM.Context.RefreshLighting(context.VM.Context.GetObjectRoom(context.StackObject), true, new HashSet<ushort>());
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.FSOAbortAllInteractions: //129
                    //aborts all of the interactions of the stack object
                    //a few rules:
                    //if we're interacting with the go here object, set us as interruptable (since by default, go here is not)
                    var sthread = context.StackObject.Thread;
                    if (sthread.ActiveAction?.Callee?.Object?.GUID == 0x000007C4)
                    {
                        (context.StackObject as VMAvatar)?.SetPersonData(VMPersonDataVariable.NonInterruptable, 0);
                    }

                    var actionClone = new List<VMQueuedAction>(sthread.Queue);
                    foreach (var action in actionClone)
                    {
                        sthread.CancelAction(action.UID);
                    }
                    return VMPrimitiveExitCode.GOTO_TRUE;
                case VMGenericTSOCallMode.FSOClearStackObjRelationships:
                    context.StackObject.MeToObject.Clear();
                    context.StackObject.MeToPersist.Clear();
                    return VMPrimitiveExitCode.GOTO_TRUE;
                default:
                    return VMPrimitiveExitCode.GOTO_TRUE;
            }
        }
    }

    public class VMGenericTSOCallOperand : VMPrimitiveOperand
    {
        public VMGenericTSOCallMode Call { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Call = (VMGenericTSOCallMode)io.ReadByte();
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
}
