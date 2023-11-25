using System;
using System.Windows.Forms;

namespace FSO.IDE.ResourceBrowser
{
    public partial class GenericTextInput : Form
    {
        public string StringResult;
        public GenericTextInput()
        {
            InitializeComponent();
        }

        public GenericTextInput(string description, string origValue) : this()
        {
            DescLabel.Text = description;
            TextInput.Text = origValue;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (TextInput.Text == "")
            {
                MessageBox.Show("Please enter a valid name.");
            }
            else
            {
                StringResult = TextInput.Text;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
