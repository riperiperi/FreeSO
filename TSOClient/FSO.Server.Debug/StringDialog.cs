using System;
using System.Windows.Forms;

namespace tso.debug.network
{
    public partial class StringDialog : Form
    {
        public StringDialogResult Result;

        public StringDialog(string title, string description)
        {
            this.Text = title;
            InitializeComponent();
            this.description.Text = description;
        }

        private void StringModal_Load(object sender, EventArgs e)
        {
        }

        private void txtValue_Enter(object sender, EventArgs e)
        {
            btnOk.PerformClick();
        }

        private void txtValue_TextChanged(object sender, EventArgs e)
        {
            this.btnOk.Enabled = txtValue.Text.Length > 0;
        }

        private void StringDialog_Deactivate(object sender, EventArgs e)
        {
            
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SetResult();
        }

        private void SetResult()
        {
            Result = new StringDialogResult { Value = txtValue.Text };
        }
    }

    public class StringDialogResult
    {
        public string Value;
    }

}
