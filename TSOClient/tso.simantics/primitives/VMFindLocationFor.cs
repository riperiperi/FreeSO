using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.model;
using tso.world.components;

namespace TSO.Simantics.primitives
{
    public class VMFindLocationFor : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMFindLocationForOperand>();
            var refObj = (operand.UseLocalAsRef) ? context.VM.GetObjectById((short)context.Locals[operand.Local]) : context.Caller;

            short container = context.StackObject.GetValue(VMStackObjectVariable.ContainerId);
            if (container != 0) context.VM.GetObjectById(container).ClearSlot(context.StackObject.GetValue(VMStackObjectVariable.SlotNumber)); //if object is in a slot, eject it
            
            if (operand.Mode == 0) //todo: detect collisions and place close to intended position if AllowIntersection is false.
            { //default
                //also todo.. a better way of moving objects lol (especially for multitile)
                context.StackObject.SetPosition((short)refObj.Position.X, (short)refObj.Position.Y, (sbyte)refObj.WorldUI.Level, context.StackObject.Direction, context.VM.Context);
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMFindLocationForOperand : VMPrimitiveOperand
    {
        public byte Mode;
        public byte Local;
        public byte Flags;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Mode = io.ReadByte();
                Local = io.ReadByte();
                Flags = io.ReadByte();
            }
        }
        #endregion

        public bool UseLocalAsRef
        {
            get
            {
                return (Flags & 1) == 1;
            }
        }

        public bool AllowIntersection
        {
            get
            {
                return (Flags & 2) == 2;
            }
        }

        public bool UserEditableTilesOnly
        {
            get
            {
                return (Flags & 4) == 4;
            }
        }
    }
}
