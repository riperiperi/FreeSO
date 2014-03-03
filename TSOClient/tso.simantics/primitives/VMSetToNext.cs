using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;

namespace TSO.Simantics.primitives
{
    public class VMSetToNext : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMSetToNextOperand>();
            var targetValue = VMMemory.GetVariable(context, operand.GetTargetOwner(), operand.GetTargetData());

            if (operand.SearchType == VMSetToNextSearchType.ObjectOfType){
                //TODO: Implement!
                return VMPrimitiveExitCode.GOTO_FALSE;
            }
            throw new Exception("Unknown search type");
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
