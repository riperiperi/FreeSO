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
    public class SetMotiveChangeDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Sim; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.Done; } }

        public override Type OperandType { get { return typeof(VMSetMotiveChangeOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMSetMotiveChangeOperand)Operand;
            var result = new StringBuilder();

            if (op.ClearAll) result.Append("Clear all");
            else
            {
                result.Append(scope.GetVarScopeDataName(VMVariableScope.MyMotives, (short)op.Motive));
                result.Append(" += ");
                result.Append(scope.GetVarName(op.DeltaOwner, op.DeltaData));

                if (!op.Once) result.Append(" (per hr)");

                result.Append("\r\n Stop at ");
                result.Append(scope.GetVarName(op.MaxOwner, op.MaxData));
            }

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Sets a motive to change at a specific delta per hour until it hits the specified value. You can clear all active changes by setting the Clear All flag.")));

            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Motive:", "Motive", new OpStaticNamedPropertyProvider(escope.GetVarScopeDataNames(VMVariableScope.MyMotives))));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Delta:", "DeltaOwner", "DeltaData"));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Max:", "MaxOwner", "MaxData"));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Apply Change Once", "Once"),
                new OpFlag("Clear All", "ClearAll"),
                }));
        }
    }
}
