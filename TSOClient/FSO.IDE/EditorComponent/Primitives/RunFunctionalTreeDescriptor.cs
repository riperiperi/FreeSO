using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine.Primitives;
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
    public class RunFunctionalTreeDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Control; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMRunFunctionalTreeOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMRunFunctionalTreeOperand)Operand;
            var result = new StringBuilder();
            var nextObject = EditorScope.Behaviour.Get<STR>(201).GetString((int)op.Function);
            result.Append(nextObject);

            if (op.ChangeIcon) result.Append("(Change Action Icon)");

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Runs the specified entry point on the Stack Object, like a subroutine. Returns the result of the tree.")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Function: ", "Function", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(201), 0)));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Change Action Icon", "ChangeIcon"),
                }));
        }
    }
}
