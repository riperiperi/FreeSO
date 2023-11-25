using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class GrabDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Position; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMGrabOperand); } }

        public override string GetBody(EditorScope scope)
        {
            return "the Stack Object, place in my slot 0";
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, 
                new OpStaticTextProvider("Places the Stack Object in the Caller object's 0th slot. "
                + "Usually used by Avatars. Returns False on failure, True on success.")));
        }
    }
}
