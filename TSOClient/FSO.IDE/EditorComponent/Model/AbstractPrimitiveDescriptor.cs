using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent.Model
{
    public abstract class AbstractPrimitiveDescriptor
    {
        public abstract PrimitiveGroup Group { get; }
        public abstract Type OperandType { get; }

        public abstract string GetTitle(EditorScope scope);
        public abstract string GetBody(EditorScope scope);

        //TODO: modifiable operand models, special form controls for specific types.
    }

    public enum PrimitiveReturnTypes
    {
        TrueFalse,
        Done,
    }
}
