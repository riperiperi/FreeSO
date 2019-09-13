using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    class TSOInventoryOperationsDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Object; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMInventoryOperationsOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMInventoryOperationsOperand)Operand;
            var result = new StringBuilder();

            var obj = Content.Content.Get().WorldObjects.Get(op.GUID);
            result.Append("Mode: " + op.Mode.ToString() + "\r\n");
            result.Append("Target Data: " + scope.GetVarName(op.FSOScope, op.FSOData) + "\r\n");

            if (op.GUID != 0)
            {
                result.Append("Object: ");
                result.Append((obj == null) ? ("0x" + Convert.ToString(op.GUID.ToString("x8"))) : obj.OBJ.ChunkLabel);
            }

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand,
                new OpStaticTextProvider("Inventory operations in TSO. Many of these have been added in FreeSO.")));

            panel.Controls.Add(new OpObjectControl(master, escope, Operand, "Object Type:", "GUID"));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Mode:", "Mode", new OpStaticNamedPropertyProvider(typeof(VMInventoryOpMode))));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Target Variable", "FSOScope", "FSOData"));
        }
    }
}
