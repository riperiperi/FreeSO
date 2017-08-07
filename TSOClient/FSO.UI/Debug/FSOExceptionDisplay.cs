using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.Client.Debug
{
    public partial class FSOExceptionDisplay : Form
    {
        public FSOExceptionDisplay()
        {
            InitializeComponent();
        }

        public FSOExceptionDisplay(string trace) : this()
        {
            ExceptionBox.Text = trace;
        }

        private void ContinueButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            var text = ExceptionBox.Text;
            var t = new Thread(() =>
            {
                Clipboard.SetText(text);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
}
