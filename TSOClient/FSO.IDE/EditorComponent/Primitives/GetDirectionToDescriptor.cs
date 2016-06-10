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
    public class GetDirectionToDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Math; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.Done; } }

        public override Type OperandType { get { return typeof(VMGetDirectionToOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMGetDirectionToOperand)Operand;
            var result = new StringBuilder();

            result.Append("From object with ID: ");
            result.Append(scope.GetVarName(op.ObjectScope, (short)op.OScopeData));
            result.Append("\r\nto the Stack Object\r\nStore in: ");
            result.Append(scope.GetVarName(op.ResultOwner, (short)op.ResultData));

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Finds the direction between the specified object to the Stack Object and stores it in the specified Variable Scope. Result is 0-7 inclusive, 0 being North and the following going clockwise in 45 degree increments.")));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "From Object:", "ObjectScope", "OScopeData"));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Store in:", "ResultOwner", "ResultData"));
        }
    }
}
