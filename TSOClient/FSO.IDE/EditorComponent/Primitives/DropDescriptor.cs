using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class DropDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Position; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMGrabOperand); } }

        public override string GetBody(EditorScope scope)
        {
            return "my slot 0 onto the ground";
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, 
                new OpStaticTextProvider("Places the object in the Caller's 0th slot on the ground near them, starting in front."
                + "If space cannot be found or there is no object in slot 0, returns False. Returns True if successful.")));
        }
    }
}
