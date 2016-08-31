using FSO.Common;
using FSO.Files.Formats.IFF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.Common
{
    public partial class NewIffDialog : Form
    {
        public IffFile InitIff = null;
        public NewIffDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            var name = Path.Combine(FSOEnvironment.ContentDir, "Objects/" +NameEntry.Text+".iff");
            var objProvider = Content.Content.Get().WorldObjects;
            if (NameEntry.Text == "")
            {
                MessageBox.Show("Name cannot be empty!", "Invalid IFF Name");
            }
            else
            {
                //search for duplicates.
                lock (objProvider.Entries)
                {
                    foreach (var obj in objProvider.Entries.Values)
                    {
                        if (obj.FileName == name)
                        {
                            MessageBox.Show("Name "+name+" already taken!", "Invalid IFF Name");
                            return;
                        }
                    }
                }
                //we're good. Create the IFF and add it. Don't drop the lock, so changes cannot be made between this check.
                var iff = new IffFile();
                iff.RuntimeInfo.Path = name;
                iff.RuntimeInfo.State = IffRuntimeState.Standalone;
                iff.Filename = NameEntry.Text;

                DialogResult = DialogResult.OK;
                InitIff = iff;
                Close();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
