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

namespace FSO.IDE.EditorComponent.OperandForms
{
    public partial class OpFlagsControl : FlowLayoutPanel, IOpControl
    {
        private OpFlag[] Flags;
        private BHAVEditor Master;
        private VMPrimitiveOperand Operand;
        private EditorScope Scope;
        private CheckBox[] FlagChecks;

        private bool IgnoreSet;

        private Dictionary<CheckBox, OpFlag> CheckToFlag = new Dictionary<CheckBox, OpFlag>();

        public OpFlagsControl()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
        }

        public OpFlagsControl(BHAVEditor master, EditorScope scope, VMPrimitiveOperand operand, string title, OpFlag[] flags)
        {
            InitializeComponent();
            this.FlagsPanel.Controls.Clear();
            Master = master;
            Scope = scope;
            Operand = operand;
            Flags = flags;
            FlagChecks = new CheckBox[flags.Length];

            int i = 0;
            foreach (var flag in flags)
            {
                var check = new CheckBox();
                check.AutoSize = true;
                check.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
                check.Size = new System.Drawing.Size(195, 17);
                check.Text = flag.Title;
                check.UseVisualStyleBackColor = true;
                check.CheckedChanged += Check_CheckedChanged;

                FlagChecks[i++] = check;
                CheckToFlag.Add(check, flag);
                this.FlagsPanel.Controls.Add(check);
            }

            OperandUpdated();
        }

        private void Check_CheckedChanged(object sender, EventArgs e)
        {
            if (IgnoreSet) return;
            var check = (CheckBox)sender;
            var flag = CheckToFlag[check];

            OpUtils.SetOperandProperty(Operand, flag.Property, check.Checked);

            Master.SignalOperandUpdate();
        }

        public void OperandUpdated()
        {
            IgnoreSet = true;
            for (int i=0; i<Flags.Length; i++) {
                var flag = Flags[i];
                var ui = FlagChecks[i];

                var property = Operand.GetType().GetProperty(flag.Property);
                bool value = Convert.ToBoolean(property.GetValue(Operand));

                ui.Checked = value;
            }
            IgnoreSet = false;
        }
    }

    public class OpFlag
    {
        public string Title;
        public string Property;

        public OpFlag(string title, string property)
        {
            Title = title;
            Property = property;
        }
    }
}
