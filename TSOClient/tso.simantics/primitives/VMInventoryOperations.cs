using FSO.Files.Utils;
using FSO.LotView.Model;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Model.Platform;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Model.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Primitives
{
    //inventory operations for TSO
    public class VMInventoryOperations : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMInventoryOperationsOperand)args;

            //first of all... are we in an async wait state?
            if (context.Thread.BlockingState != null && context.Thread.BlockingState is VMInventoryOpState)
            {
                var state = (VMInventoryOpState)context.Thread.BlockingState; //crash here if not intended state.
                if (state.Responded)
                {
                    context.Thread.BlockingState = null;
                    if (state.ObjectPersistID > 0)
                    {
                        var persistObj = context.VM.GetObjectByPersist(state.ObjectPersistID);
                        state.Temp0Value = persistObj?.ObjectID ?? 0;
                        if (persistObj != null && operand.Mode == VMInventoryOpMode.FSOCopyObjectOfTypeOOW)
                        {
                            //make sure when this object is deleted, it does not delete the original. (it's a copy)
                            context.VM.Context.ObjectQueries.RemoveMultitilePersist(context.VM, state.ObjectPersistID);
                            foreach (var obj in persistObj.MultitileGroup.Objects)
                            {
                                obj.PersistID = 0;
                            }
                        }
                    }
                    if (state.WriteResult) VMMemory.SetVariable(context, state.WriteScope, state.WriteData, state.Temp0Value);
                    return (state.Success) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                }
                else return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }

            switch (operand.Mode)
            {
                case VMInventoryOpMode.GetRemainingCapacity:
                    context.Thread.TempRegisters[0] = 255; //TODO: hard object limit imposed by db.
                    break;
                case VMInventoryOpMode.FSOSaveStackObj:
                case VMInventoryOpMode.AddStackObjToInventory:
                    // vm initiated inventory transfer. 
                    // TODO: should this force owner? Crafting Bench uses separate command to change owner before doing this.
                    context.Thread.BlockingState = new VMInventoryOpState();
                    if (context.VM.GlobalLink != null)
                    {
                        var vm = context.VM;
                        var id = context.Caller.ObjectID; //this thread's object id.
                        var mypid = context.Caller.PersistID;
                        var opid = context.StackObject.PersistID;
                        var oid = context.StackObject.ObjectID;
                        vm.GlobalLink.MoveToInventory(vm, context.StackObject.MultitileGroup, (bool success, uint pid) =>
                        {
                            vm.SendCommand(new VMNetAsyncResponseCmd(id, new VMInventoryOpState
                            {
                                Responded = true,
                                Success = success,
                                WriteResult = false
                            }));
                            if (success)
                            {
                                if (opid == 0)
                                {
                                    //we got a persist id when we got added to inventory. quickly register it before the send to inventory is run.
                                    vm.ForwardCommand(new VMNetUpdatePersistStateCmd()
                                    {
                                        ObjectID = oid,
                                        PersistID = pid
                                    });
                                }
                                if (operand.Mode != VMInventoryOpMode.FSOSaveStackObj)
                                {
                                    vm.ForwardCommand(new VMNetSendToInventoryCmd()
                                    {
                                        Verified = true,
                                        Success = true,
                                        ObjectPID = pid,
                                        ActorUID = mypid
                                    });
                                }
                            }
                        });
                    }
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                case VMInventoryOpMode.RemoveTemp0ObjOfTypeFromInventory:
                    //needs a rather complex db operation.
                    //must first select x objects. must then delete them all.
                    //if deleted# != selected#, reverse transaction and declare it failed.
                    //same for selected# being < x.
                    context.Thread.BlockingState = new VMInventoryOpState();
                    if (context.VM.GlobalLink != null)
                    {
                        //get id and vm now to avoid race conditions
                        var id = context.Caller.ObjectID; //this thread's object id.
                        var vm = context.VM;
                        context.VM.GlobalLink.ConsumeInventory(context.VM, context.Caller.PersistID, operand.GUID, 1, context.Thread.TempRegisters[0],
                            (bool success, int count) =>
                            {
                                vm.SendCommand(new VMNetAsyncResponseCmd(id, new VMInventoryOpState
                                {
                                    Responded = true,
                                    Success = success,
                                    WriteResult = false
                                }));
                            });
                    }
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                case VMInventoryOpMode.CountObjectsOfType:
                case VMInventoryOpMode.FSOCountAllObjectsOfType:
                    context.Thread.BlockingState = new VMInventoryOpState();
                    if (context.VM.GlobalLink != null)
                    {
                        //get id and vm now to avoid race conditions
                        var id = context.Caller.ObjectID; //this thread's object id.
                        var vm = context.VM;
                        var all = operand.Mode == VMInventoryOpMode.FSOCountAllObjectsOfType;
                        context.VM.GlobalLink.ConsumeInventory(context.VM, context.Caller.PersistID, operand.GUID, all?2:0, 0, 
                            (bool success, int count) =>
                        {
                            vm.SendCommand(new VMNetAsyncResponseCmd(id, new VMInventoryOpState
                            {
                                Responded = true,
                                Success = success,
                                WriteResult = true,
                                WriteScope = VMVariableScope.Temps,
                                Temp0Value = (short)count
                            }));
                        });
                    }
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                case VMInventoryOpMode.FSOCreateObjectOfTypeOOW:
                case VMInventoryOpMode.FSOCopyObjectOfTypeOOW:
                    context.Thread.BlockingState = new VMInventoryOpState();
                    if (context.VM.GlobalLink != null)
                    {
                        var id = context.Caller.ObjectID; //this thread's object id.
                        var reserve = operand.Mode == VMInventoryOpMode.FSOCreateObjectOfTypeOOW;
                        var index = VMMemory.GetBigVariable(context, operand.FSOScope, operand.FSOData);
                        context.VM.GlobalLink.RetrieveFromInventoryByType(context.VM, context.Caller.PersistID, operand.GUID, index, reserve, (data) =>
                        {
                            context.VM.SendCommand(new VMNetAsyncResponseCmd(id, new VMInventoryOpState
                            {
                                Responded = true,
                                Success = data.GUID != 0,
                                ObjectPersistID = data?.PersistID ?? 0,
                                WriteResult = data.GUID != 0,
                                WriteScope = VMVariableScope.StackObjectID,
                                WriteData = 0
                            }));

                            if (data.GUID != 0)
                            {
                                data.RestoreType = reserve ? VMInventoryRestoreType.CreateOOW : VMInventoryRestoreType.CopyOOW;
                                var inventoryCmd = new VMNetPlaceInventoryCmd()
                                {
                                    ObjectPID = data.PersistID,
                                    Verified = true,
                                    dir = Direction.NORTH,
                                    level = 1,
                                    x = -32768,
                                    y = -32768,
                                    Info = data,
                                    Mode = PurchaseMode.Normal
                                };
                                context.VM.ForwardCommand(inventoryCmd);
                            }
                        });
                    }
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMInventoryOperationsOperand : VMPrimitiveOperand
    {
        public uint GUID { get; set; }
        public VMInventoryOpMode Mode { get; set; }
        public VMVariableScope FSOScope { get; set; }
        public short FSOData { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                GUID = io.ReadUInt32();
                Mode = (VMInventoryOpMode)io.ReadByte();
                FSOScope = (VMVariableScope)io.ReadByte();
                FSOData = io.ReadInt16();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(GUID);
                io.Write((byte)Mode);
                io.Write((byte)FSOScope);
                io.Write(FSOData);
            }
        }
        #endregion
    }

    public enum VMInventoryOpMode : byte
    {
        GetRemainingCapacity = 0, //result in temp 0
        AddStackObjToInventory = 1, //true/false. Original game doesn't expect false?
        RemoveTemp0ObjOfTypeFromInventory = 2, //true/false. Original game doesnt expect false?
        CountObjectsOfType = 3, //result in temp 0

        //these create an inventory object out of world.
        FSOCreateObjectOfTypeOOW = 4, // scope/data is index to access. if index is out of range, goes to last object. if no object, returns false. id in temp 0
        FSOCopyObjectOfTypeOOW = 5, // same as above, but does not claim the object. id in temp 0.
        FSOSaveStackObj = 6, //same as add, but does not delete the object afterwards
        FSOCountAllObjectsOfType = 7, //includes placed objects

        //tokens not implemented yet
        //return false if the object does not behave like a token.

        FSOTokenEnsureStackObjExists = 32, //ensure one object with this guid exists. if it doesn't, create it similar to AddStackObjToInventory
        //access token attribute[temp 0] for the zeroth object of this type. Value to read or set is in scope/data.
        FSOTokenGetAttributeTemp0 = 33,
        FSOTokenSetAttributeTemp0 = 34,
        FSOTokenModifyAttributeTemp0 = 35,
    }

    public class VMInventoryOpState : VMAsyncState
    {
        public bool Success;
        public bool WriteResult;
        public short Temp0Value;
        public uint ObjectPersistID;
        public VMVariableScope WriteScope = VMVariableScope.INVALID;
        public short WriteData;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Success = reader.ReadBoolean();
            WriteResult = reader.ReadBoolean();
            Temp0Value = reader.ReadInt16();
            if (Version > 34)
            {
                ObjectPersistID = reader.ReadUInt32();
                WriteScope = (VMVariableScope)reader.ReadByte();
                WriteData = reader.ReadInt16();
            }
            else
            {
                if (WriteResult)
                {
                    WriteScope = VMVariableScope.Temps;
                    WriteData = 0;
                }
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Success);
            writer.Write(WriteResult);
            writer.Write(Temp0Value);

            writer.Write(ObjectPersistID);
            writer.Write((byte)WriteScope);
            writer.Write(WriteData);
        }
    }
}
