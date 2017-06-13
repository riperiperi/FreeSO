using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class UnknownPrimitiveDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveRegistry.GetGroupOf((byte)PrimID); } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMSubRoutineOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMSubRoutineOperand)Operand;
            return (op.Arg0 & 0xFF).ToString("x2") + " " + (op.Arg0 >> 8).ToString("x2") + " " +
                (op.Arg1 & 0xFF).ToString("x2") + " " + (op.Arg1 >> 8).ToString("x2") + " " +
                (op.Arg2 & 0xFF).ToString("x2") + " " + (op.Arg2 >> 8).ToString("x2") + " " +
                (op.Arg3 & 0xFF).ToString("x2") + " " + (op.Arg3 >> 8).ToString("x2") + " ";
        }

    }
}
