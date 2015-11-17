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
    public class BreakpointDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Debug; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMBreakPointOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMBreakPointOperand)Operand;
            var result = new StringBuilder();

            result.Append("when ");
            result.Append(scope.GetVarName(op.Scope, (short)op.Data));
            result.Append(" != 0");

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Breakpoints when the specified Variable Scope contains a non-zero value.")));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Variable:", "Scope", "Data"));
        }
    }
}
