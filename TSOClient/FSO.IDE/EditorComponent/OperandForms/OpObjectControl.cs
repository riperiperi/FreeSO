using System;
using System.Windows.Forms;
using FSO.SimAntics.Engine;

namespace FSO.IDE.EditorComponent.OperandForms
{
    public partial class OpObjectControl : UserControl, IOpControl
    {
        private BHAVEditor Master;
        private VMPrimitiveOperand Operand;
        private EditorScope Scope;

        private string GUIDProperty;

        public OpObjectControl()
        {
            
            InitializeComponent();
        }

        public OpObjectControl(BHAVEditor master, EditorScope scope, VMPrimitiveOperand operand, string title, string guidProperty)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            Master = master;
            Scope = scope;
            Operand = operand;
            TitleLabel.Text = title;
            GUIDProperty = guidProperty;

            OperandUpdated();
        }

        public void OperandUpdated()
        {
            uint guid = Convert.ToUInt32(OpUtils.GetOperandProperty(Operand, GUIDProperty));
            var obj = Content.Content.Get().WorldObjects.Get(guid);
            ObjectLabel.Text = (obj == null) ? ("0x"+guid.ToString("X8")) : obj.OBJ.ChunkLabel;
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            var popup = new VarObjectSelect();
            popup.ShowDialog();
            if (popup.DialogResult == DialogResult.OK)
            {
                OpUtils.SetOperandProperty(Operand, GUIDProperty, popup.GUIDResult);
            }
            Master.SignalOperandUpdate();
        }
    }
}
