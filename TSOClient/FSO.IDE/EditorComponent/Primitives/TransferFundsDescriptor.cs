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
    public class TransferFundsDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Sim; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMTransferFundsOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMTransferFundsOperand)Operand;
            return op.TransferType.ToString() + " - " + op.ExpenseType.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand,
                new OpStaticTextProvider("Transfers funds from one object to another (either can be 'maxis', essentially an infinite bank). Both must be in the database.")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Transfer Type:", "TransferType", new OpStaticNamedPropertyProvider(typeof(VMTransferFundsType))));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Expense Type:", "ExpenseType", new OpStaticNamedPropertyProvider(typeof(VMTransferFundsExpenseType))));
        }
    }
}
