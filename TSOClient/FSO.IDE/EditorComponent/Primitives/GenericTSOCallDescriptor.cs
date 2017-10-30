using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class GenericTSOCallDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Control; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMGenericTSOCallOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMGenericTSOCallOperand)Operand;
            return op.Call.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand,
                new OpStaticTextProvider("Run a TSO specific function. Can use state from this scope to perform an action.")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Call:", "Call", new OpStaticNamedPropertyProvider(typeof(VMGenericTSOCallMode))));
        }
    }
}
