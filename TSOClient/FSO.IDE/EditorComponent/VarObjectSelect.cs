using System;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent
{
    public partial class VarObjectSelect : Form
    {
        public uint GUIDResult;

        public VarObjectSelect()
        {
            InitializeComponent();
            Browser.SelectedChanged += Browser_SelectedChanged;
            Browser.RefreshTree();
        }

        private void Browser_SelectedChanged()
        {
            SelectButton.Enabled = (Browser.SelectedObj != null);
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            if (Browser.SelectedObj != null)
            {
                DialogResult = DialogResult.OK;
                GUIDResult = Browser.SelectedObj.GUID;
            }
            else
                DialogResult = DialogResult.Cancel;

            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
