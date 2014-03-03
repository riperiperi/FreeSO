using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TSO.Simantics.emulator
{
    public partial class Console : RichTextBox
    {
        public Console()
        {
            InitializeComponent();

            this.BackColor = Color.FromArgb(0xFF, 0x1B, 0x1D, 0x1E);
        }
    }
}
