using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tso.content;

namespace tso.debug
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            Content.Init(@"C:\Program Files\Maxis\The Sims Online\TSOClient\", null);
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new Vitaboy().Show();
        }
    }
}
