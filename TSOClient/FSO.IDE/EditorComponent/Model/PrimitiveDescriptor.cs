using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Model
{
    public abstract class PrimitiveDescriptor
    {
        public ushort PrimID;
        public VMPrimitiveOperand Operand;

        public abstract PrimitiveGroup Group { get; }
        public abstract PrimitiveReturnTypes Returns { get; }
        public abstract Type OperandType { get; }

        public virtual string GetTitle(EditorScope scope) {
            var primName = EditorScope.Behaviour.Get<STR>(139).GetString(PrimID);
            return (primName == null)?"Primitive #"+PrimID:primName;
        }
        public abstract string GetBody(EditorScope scope);

        public virtual void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpUnknownControl());
        }

        //TODO: modifiable operand models, special form controls for specific types.
    }

    public enum PrimitiveReturnTypes
    {
        TrueFalse,
        Done,
    }
}
