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
    public class RandomNumberDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Math; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.Done; } }

        public override Type OperandType { get { return typeof(VMRandomNumberOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMRandomNumberOperand)Operand;
            var result = new StringBuilder();

            result.Append(scope.GetVarName(op.DestinationScope, (short)op.DestinationData));
            result.Append(" := between 0 and ");
            result.Append(scope.GetVarName(op.RangeScope, (short)op.RangeData));

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Places a random number between 0 and the specified range into the specified variable scope.")));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Range:", "RangeScope", "RangeData"));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Destination:", "DestinationScope", "DestinationData"));
        }
    }
}
