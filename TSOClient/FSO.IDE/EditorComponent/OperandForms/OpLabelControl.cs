using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.OperandForms
{
    public class OpLabelControl : Label, IOpControl
    {
        private OpTextProvider TextProvider;
        private VMPrimitiveOperand Operand;
        private EditorScope Scope;

        public OpLabelControl()
        {
            this.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.AutoSize = true;
        }

        public OpLabelControl(BHAVEditor master, EditorScope scope, VMPrimitiveOperand operand, OpTextProvider textP) : this()
        {
            Scope = scope;
            Operand = operand;
            TextProvider = textP;
            OperandUpdated();
        }

        public void OperandUpdated()
        {
            this.Text = TextProvider.GetText(Scope, Operand);
        }
    }
}
