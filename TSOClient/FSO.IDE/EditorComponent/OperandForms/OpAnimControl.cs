using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.SimAntics.Engine;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.Primitives;
using FSO.IDE.EditorComponent.Primitives;

namespace FSO.IDE.EditorComponent.OperandForms
{
    public partial class OpAnimControl : UserControl, IOpControl
    {
        private BHAVEditor Master;
        private VMAnimateSimOperand Operand;
        private EditorScope Scope;

        private string IndexProperty;
        private string SourceProperty;

        public OpAnimControl()
        {
            InitializeComponent();
        }

        public OpAnimControl(BHAVEditor master, EditorScope scope, VMPrimitiveOperand operand, string title)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            Master = master;
            Scope = scope;
            Operand = (VMAnimateSimOperand)operand;
            TitleLabel.Text = title;

            OperandUpdated();
        }

        public void OperandUpdated()
        {
            var op = Operand;
            if (op.AnimationID == 0)
                ObjectLabel.Text = "Stop Animation";
            else
                ObjectLabel.Text = AnimateSimDescriptor.GetAnimationName(Scope, op.Source, op.AnimationID);
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            var popup = new VarAnimSelect(AnimateSimDescriptor.GetAnimTable(Scope, Operand.Source), Operand.AnimationID);
            popup.ShowDialog();
            if (popup.DialogResult == DialogResult.OK)
            {
                Operand.AnimationID = (ushort)popup.SelectedAnim;
            }
            Master.SignalOperandUpdate();
        }

        private void TitleLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
