using System;
using System.Windows.Forms;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Scopes;

namespace FSO.IDE.EditorComponent.OperandForms
{
    public partial class OpScopeControl : UserControl, IOpControl
    {
        private BHAVEditor Master;
        private VMPrimitiveOperand Operand;
        private EditorScope Scope;

        private string SourceProperty;
        private string DataProperty;

        public OpScopeControl()
        {
            
            InitializeComponent();
        }

        public OpScopeControl(BHAVEditor master, EditorScope scope, VMPrimitiveOperand operand, string title, string sourceProperty, string dataProperty)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            Master = master;
            Scope = scope;
            Operand = operand;
            TitleLabel.Text = title;
            SourceProperty = sourceProperty;
            DataProperty = dataProperty;

            OperandUpdated();
        }

        public void OperandUpdated()
        {
            int value = Convert.ToInt32(OpUtils.GetOperandProperty(Operand, SourceProperty));
            ScopeLabel.Text = Scope.GetVarName((VMVariableScope)value, Convert.ToInt16(OpUtils.GetOperandProperty(Operand, DataProperty)));
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            var popup = new VarScopeSelect(Scope, Convert.ToByte(OpUtils.GetOperandProperty(Operand, SourceProperty)), Convert.ToInt16(OpUtils.GetOperandProperty(Operand, DataProperty)));
            popup.ShowDialog();
            if (popup.DialogResult == DialogResult.OK)
            {
                OpUtils.SetOperandProperty(Operand, SourceProperty, popup.SelectedSource);
                OpUtils.SetOperandProperty(Operand, DataProperty, popup.SelectedData);
            }
            Master.SignalOperandUpdate();
        }
    }
}
