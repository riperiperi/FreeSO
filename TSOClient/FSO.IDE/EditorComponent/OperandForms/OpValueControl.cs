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

namespace FSO.IDE.EditorComponent.OperandForms
{
    public partial class OpValueControl : UserControl, IOpControl
    {
        private OpValueBoundsProvider BoundsProvider;
        private BHAVEditor Master;
        private VMPrimitiveOperand Operand;
        private EditorScope Scope;
        private string Property;

        private bool IgnoreSet;

        public OpValueControl()
        {
            InitializeComponent();
        }

        public OpValueControl(BHAVEditor master, EditorScope scope, VMPrimitiveOperand operand, string title, string property, OpValueBoundsProvider bounds)
        {
            InitializeComponent();
            Master = master;
            Scope = scope;
            Operand = operand;
            TitleLabel.Text = title;
            Property = property;
            BoundsProvider = bounds;
            this.Dock = DockStyle.Fill;
            OperandUpdated();
        }

        public void OperandUpdated()
        {
            var bounds = BoundsProvider.GetBounds(Scope, Operand);
            ValueEntry.Minimum = bounds[0];
            ValueEntry.Maximum = bounds[1];

            var prop = OpUtils.GetOperandProperty(Operand, Property);
            if (prop.GetType() == typeof(uint)) prop = unchecked((int)Convert.ToUInt32(prop));
            int value = Convert.ToInt32(prop);

            IgnoreSet = true;
            ValueEntry.Value = value;
            IgnoreSet = false;
        }

        private void ValueEntry_ValueChanged(object sender, EventArgs e)
        {
            if (IgnoreSet) return;
            OpUtils.SetOperandProperty(Operand, Property, ValueEntry.Value);

            Master.SignalOperandUpdate();
        }

    }
}
