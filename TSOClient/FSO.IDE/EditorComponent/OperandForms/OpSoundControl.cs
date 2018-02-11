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
using FSO.SimAntics.Primitives;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.IDE.EditorComponent.OperandForms
{
    public partial class OpSoundControl : UserControl, IOpControl
    {
        private BHAVEditor Master;
        private VMPlaySoundOperand Operand;
        private EditorScope Scope;

        private string IndexProperty;
        private string SourceProperty;

        public OpSoundControl()
        {
            InitializeComponent();
        }

        public OpSoundControl(BHAVEditor master, EditorScope scope, VMPrimitiveOperand operand, string title)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            Master = master;
            Scope = scope;
            Operand = (VMPlaySoundOperand)operand;
            TitleLabel.Text = title;

            OperandUpdated();
        }

        public void OperandUpdated()
        {
            var op = Operand;
            var fwav = Scope.GetResource<FWAV>(op.EventID, ScopeSource.Private);
            ObjectLabel.Text = (fwav == null) ? "*Event Missing!*" : fwav.Name;
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            var popup = new VarSoundSelect(Scope.Object.Resource.MainIff, Operand.EventID);
            popup.ShowDialog();
            if (popup.DialogResult == DialogResult.OK)
            {
                Operand.EventID = (ushort)popup.SelectedFWAV;
            }
            Master.SignalOperandUpdate();
        }

        private void TitleLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
