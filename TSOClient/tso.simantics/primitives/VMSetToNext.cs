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
        public short searchPosition = 0; //stored by the primitive so we know it on future runs! The assumption is that the BHAV will be reassembled on each run.
                                         //We probably won't do this when we start optimizing the server, so it will need to be handled another way.

        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMSetToNextOperand>();
            var targetValue = VMMemory.GetVariable(context, operand.GetTargetOwner(), operand.GetTargetData());
            var entities = context.VM.Entities;

            if (searchPosition >= entities.Count) {
                searchPosition = 0;
                return VMPrimitiveExitCode.GOTO_FALSE;
            }

            if (operand.SearchType == VMSetToNextSearchType.Object) //find next object
            {
                context.StackObject = entities[searchPosition++]; //pick next object, serve it back.
                return VMPrimitiveExitCode.GOTO_TRUE;
            } else {
                while (true) //generic search through all objects
                {
                    if (searchPosition >= entities.Count)
                    {
                        searchPosition = 0;
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                    var temp = entities[searchPosition++];
                    VMEntity temp2; //used in some places
                    bool found = false;

                    switch (operand.SearchType) { //search types
                        case VMSetToNextSearchType.Person:
                            found = (temp.GetType() == typeof(VMAvatar));
                            break;
                        case VMSetToNextSearchType.NonPerson:
                            found = (temp.GetType() == typeof(VMGameObject));
                            break;
                        case VMSetToNextSearchType.PartOfAMultipartTile:
                            throw new Exception("Not implemented!");
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
                            temp2 = context.VM.GetObjectById((short)context.Locals[operand.Local]); //sure, it doesn't have this in the name, but it seems like the object is chosen from a local.
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

            return VMPrimitiveExitCode.GOTO_FALSE;
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
