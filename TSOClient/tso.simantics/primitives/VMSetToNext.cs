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

            if (operand.SearchType == VMSetToNextSearchType.Object) //find next object
            {
                if (context.SetToNextPointer == -1) context.SetToNextPointer = (entities.IndexOf(context.Callee)+1)%entities.Count;
                context.StackObject = entities[context.SetToNextPointer++]; //pick next object, serve it back.
                if (context.SetToNextPointer >= entities.Count) context.SetToNextPointer = 0; //loop around if hit the end
                if (context.StackObject == context.Callee)
                {
                    context.SetToNextPointer = -1;
                    return VMPrimitiveExitCode.GOTO_FALSE;
                }
                return VMPrimitiveExitCode.GOTO_TRUE;

            } else if (operand.SearchType == VMSetToNextSearchType.PartOfAMultipartTile) {
                if (context.Callee.MultitileGroup == null) return VMPrimitiveExitCode.GOTO_FALSE; //single part
                else
                {
                    var group = context.Callee.MultitileGroup;
                    if (context.SetToNextPointer == -1) context.SetToNextPointer = (group.IndexOf(context.Callee)+1)%group.Count; //start at me

                    context.StackObject = group[context.SetToNextPointer++];
                    if (context.SetToNextPointer >= context.Callee.MultitileGroup.Count) context.SetToNextPointer = 0; //loop around when we hit the end
                    if (context.StackObject == context.Callee) return VMPrimitiveExitCode.GOTO_FALSE; //back at original
                    return VMPrimitiveExitCode.GOTO_TRUE;
                }
            } else {
                while (true) //generic search through all objects
                {
                    if (context.SetToNextPointer == -1) context.SetToNextPointer = 0; //not sure about these, as some may not include the original object

                    if (context.SetToNextPointer >= entities.Count)
                    {
                        context.SetToNextPointer = 0;
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                    var temp = entities[context.SetToNextPointer++];
                    VMEntity temp2; //used in some places
                    bool found = false;

                    switch (operand.SearchType) { //search types
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
                            temp2 = context.Caller; //.VM.GetObjectById((short)context.Locals[operand.Local]); //sure, it doesn't have this in the name, but it seems like the object is chosen from a local.
                            found = (Math.Round(temp.Position.X) == Math.Round(temp2.Position.X) && Math.Round(temp.Position.Y) == Math.Round(temp2.Position.Y));
                            break;
                        case VMSetToNextSearchType.ObjectAdjacentToObjectInLocal:
                            temp2 = context.VM.GetObjectById((short)context.Locals[operand.Local]);
                            found = (Math.Abs(Math.Round(temp.Position.X) - Math.Round(temp2.Position.X)) < 2 && Math.Abs(Math.Round(temp.Position.Y) - Math.Round(temp2.Position.Y)) < 2);
                            break;
                        case VMSetToNextSearchType.Career:
                            throw new Exception("Not implemented!");
                        case VMSetToNextSearchType.ClosestHouse:
                            throw new Exception("Not implemented!");
                    }
                    if (found)
                    {
                        VMMemory.SetVariable(context, operand.GetTargetOwner(), operand.GetTargetData(), temp.ObjectID);
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                }     
            }
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
