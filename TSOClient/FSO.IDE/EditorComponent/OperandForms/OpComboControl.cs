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
    public partial class OpComboControl : UserControl, IOpControl
    {
        private BHAVEditor Master;
        private OpNamedPropertyProvider ValueProvider;
        private VMPrimitiveOperand Operand;
        private EditorScope Scope;
        private string Property;

        private Dictionary<int, string> ValueNames; //cached from the value provider

        private bool IgnoreSet;

        public OpComboControl()
        {
            
            InitializeComponent();
        }

        public OpComboControl(BHAVEditor master, EditorScope scope, VMPrimitiveOperand operand, string title, string property, OpNamedPropertyProvider valueProvider)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            Master = master;
            Scope = scope;
            Operand = operand;
            TitleLabel.Text = title;
            Property = property;
            ValueProvider = valueProvider;
            OperandUpdated();
        }

        public void OperandUpdated()
        {
            ValueNames = ValueProvider.GetNamedProperties(Scope, Operand);
            int value = Convert.ToInt32(OpUtils.GetOperandProperty(Operand, Property));

            IgnoreSet = true;

            ComboSelect.Items.Clear();
            bool found = false;
            foreach (var pair in ValueNames)
            {
                ComboSelect.Items.Add(new ComboNameValuePair(pair));
                if (pair.Key == value)
                {
                    ComboSelect.SelectedIndex = ComboSelect.Items.Count - 1;
                    found = true;
                }
            }
            IgnoreSet = false;
            ComboSelect.Enabled = ComboSelect.Items.Count > 0;
            if (ComboSelect.Enabled && (!found)) ComboSelect.SelectedIndex = 0; //force update with new index
        }

        private class ComboNameValuePair {
            public string Name;
            public int Value;

            public ComboNameValuePair(KeyValuePair<int, string> pair)
            {
                Value = pair.Key;
                Name = pair.Value;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private void ComboSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IgnoreSet) return;
            OpUtils.SetOperandProperty(Operand, Property, ((ComboNameValuePair)ComboSelect.SelectedItem).Value);

            Master.SignalOperandUpdate();
        }
    }
}
