using FSO.Files.Utils;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Scopes;
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
                    if (state.WriteTemp0) context.Thread.TempRegisters[0] = state.Temp0Value;
                    return (state.Success) ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
                }
                else return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }

            switch (operand.Mode)
            {
                case VMInventoryOpMode.GetRemainingCapacity:
                    context.Thread.TempRegisters[0] = 255; //TODO: hard object limit imposed by db.
                    break;
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
                                WriteTemp0 = false
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
                                vm.ForwardCommand(new VMNetSendToInventoryCmd()
                                {
                                    Verified = true,
                                    Success = true,
                                    ObjectPID = pid,
                                    ActorUID = mypid
                                });
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
                                    WriteTemp0 = false
                                }));
                            });
                    }
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
                case VMInventoryOpMode.CountObjectsOfType:
                    context.Thread.BlockingState = new VMInventoryOpState();
                    if (context.VM.GlobalLink != null)
                    {
                        //get id and vm now to avoid race conditions
                        var id = context.Caller.ObjectID; //this thread's object id.
                        var vm = context.VM;
                        context.VM.GlobalLink.ConsumeInventory(context.VM, context.Caller.PersistID, operand.GUID, 0, 0, 
                            (bool success, int count) =>
                        {
                            vm.SendCommand(new VMNetAsyncResponseCmd(id, new VMInventoryOpState
                            {
                                Responded = true,
                                Success = success,
                                WriteTemp0 = true,
                                Temp0Value = (short)count
                            }));
                        });
                    }
                    return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMInventoryOperationsOperand : VMPrimitiveOperand
    {
        public uint GUID;
        public VMInventoryOpMode Mode;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                GUID = io.ReadUInt32();
                Mode = (VMInventoryOpMode)io.ReadByte();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(GUID);
                io.Write((byte)Mode);
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
    }

    public class VMInventoryOpState : VMAsyncState
    {
        public bool Success;
        public bool WriteTemp0;
        public short Temp0Value;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Success = reader.ReadBoolean();
            WriteTemp0 = reader.ReadBoolean();
            Temp0Value = reader.ReadInt16();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Success);
            writer.Write(WriteTemp0);
            writer.Write(Temp0Value);
        }
    }
}
