using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;
using Microsoft.Xna.Framework;

namespace TSO.Simantics.primitives
{
    public class VMSetToNext : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMSetToNextOperand>();
            var targetValue = VMMemory.GetVariable(context, operand.GetTargetOwner(), operand.GetTargetData());
            var entities = context.VM.Entities;

            VMEntity Pointer = context.VM.GetObjectById(targetValue);

            //re-evaluation of what this actually does:
            //tries to find the next object id (from the previous) that meets a specific condition.
            //the previous object id is supplied via the target variable
            //
            //we should take the first result with object id > targetValue.

            if (operand.SearchType == VMSetToNextSearchType.PartOfAMultipartTile) {
                var target = context.VM.GetObjectById(targetValue);
                if (target == null || target.MultitileGroup == null) return VMPrimitiveExitCode.GOTO_FALSE; //single part
                else
                {
                    var group = target.MultitileGroup.Objects;
                    bool found = false;
                    short bestID = 0;
                    short smallestID = 0;
                    for (int i = 0; i < group.Count; i++)
                    {
                        var temp = group[i];
                        if (temp.ObjectID < smallestID || smallestID == 0) smallestID = temp.ObjectID;
                        if (temp.ObjectID > targetValue)
                        {
                            if ((!found) || (temp.ObjectID < bestID))
                            {
                                found = true;
                                bestID = temp.ObjectID;
                            }
                        }
                    }
                    if (found)
                    {
                        VMMemory.SetVariable(context, operand.GetTargetOwner(), operand.GetTargetData(), bestID);
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    else
                    {
                        VMMemory.SetVariable(context, operand.GetTargetOwner(), operand.GetTargetData(), smallestID);
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                }
            } else {
                bool loop = (operand.SearchType == VMSetToNextSearchType.ObjectOnSameTile);
                VMEntity first = null;
                for (int i=0; i<entities.Count; i++) //generic search through all objects
                {
                    var temp = entities[i];
                    bool found = false;
                    if (temp.ObjectID > targetValue || loop)
                    {
                        VMEntity temp2; //used in some places

                        switch (operand.SearchType)
                        { //search types
                            case VMSetToNextSearchType.Object:
                                found = true;
                                break;
                            case VMSetToNextSearchType.Person:
                                found = (temp.GetType() == typeof(VMAvatar));
                                break;
                            case VMSetToNextSearchType.NonPerson:
                                found = (temp.GetType() == typeof(VMGameObject));
                                break;
                            case VMSetToNextSearchType.ObjectOfType:
                                found = (temp.Object.OBJ.GUID == operand.GUID);
                                break;
                            case VMSetToNextSearchType.NeighborId:
                                throw new Exception("Not implemented!");
                            case VMSetToNextSearchType.ObjectWithCategoryEqualToSP0:
                                found = (temp.Object.OBJ.FunctionFlags == context.Args[0]); //I'm assuming that means "Stack parameter 0", that category means function and that it needs to be exactly the same (no subsets)
                                break;
                            case VMSetToNextSearchType.NeighborOfType:
                                throw new Exception("Not implemented!");
                            case VMSetToNextSearchType.ObjectOnSameTile:
                                temp2 = Pointer; //.VM.GetObjectById((short)context.Locals[operand.Local]); //sure, it doesn't have this in the name, but it seems like the object is chosen from a local.
                                found = ((int)temp.Position.X == (int)temp2.Position.X && (int)temp.Position.Y == (int)temp2.Position.Y);
                                break;
                            case VMSetToNextSearchType.ObjectAdjacentToObjectInLocal:
                                temp2 = context.VM.GetObjectById((short)context.Locals[operand.Local]);
                                found = ((Math.Abs(Math.Floor(temp.Position.X) - Math.Floor(temp2.Position.X)) == 1) ^ (Math.Abs(Math.Floor(temp.Position.Y) - Math.Floor(temp2.Position.Y)) == 1));
                                break;
                            case VMSetToNextSearchType.Career:
                                throw new Exception("Not implemented!");
                            case VMSetToNextSearchType.ClosestHouse:
                                throw new Exception("Not implemented!");
                        }
                        if (temp.ObjectID <= targetValue && found)
                        {
                            //remember the first element in case we need to loop back to it (set to next tile on same location)
                            if (first == null) first = temp; 
                            found = false;
                        }
                    }
                    if (found)
                    {
                        VMMemory.SetVariable(context, operand.GetTargetOwner(), operand.GetTargetData(), temp.ObjectID);
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                }

                if (loop)
                {
                    if (first == null) return VMPrimitiveExitCode.GOTO_FALSE; //no elements of this kind at all.
                    else
                    {
                        VMMemory.SetVariable(context, operand.GetTargetOwner(), operand.GetTargetData(), first.ObjectID); //set to loop, so go back to lowest obj id.
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    //loop around
                }

            }
            return VMPrimitiveExitCode.GOTO_FALSE; //ran out of objects to test
        }


    }

    public class VMSetToNextOperand : VMPrimitiveOperand
    {
        public uint GUID;
        public byte Flags;
        public VMVariableScope TargetOwner;
        public byte Local;
        public ushort TargetData;
        public VMSetToNextSearchType SearchType;

        public VMVariableScope GetTargetOwner(){
            if ((Flags & 0x80) == 0x80){
                return TargetOwner;
            }
            return VMVariableScope.StackObjectID;
        }

        public ushort GetTargetData(){
            if ((Flags & 0x80) == 0x80){
                return TargetData;
            }
            return 0;
        }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){

                this.GUID = io.ReadUInt32();
                //132 was object of type
                this.Flags = io.ReadByte();
                this.TargetOwner = (VMVariableScope)io.ReadByte();
                this.Local = io.ReadByte();
                this.TargetData = io.ReadByte();

                this.SearchType = (VMSetToNextSearchType)(this.Flags & 0x7F);

            }
        }
        #endregion
    }


    public enum VMSetToNextSearchType
    {
        Object = 0,
        Person = 1,
        NonPerson = 2,
        PartOfAMultipartTile = 3,
        ObjectOfType = 4,
        NeighborId = 5,
        ObjectWithCategoryEqualToSP0 = 6,
        NeighborOfType = 7,
        ObjectOnSameTile = 8,
        ObjectAdjacentToObjectInLocal = 9,
        Career = 10,
        ClosestHouse = 11
    }
}
