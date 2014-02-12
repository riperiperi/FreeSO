using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tso.simantics;

namespace tso.debug.content.preview
{
    public partial class VMRoutineInspector : Form
    {
        private VMRoutine Routine;
        public VMRoutineInspector(VMRoutine routine)
        {
            this.Routine = routine;
            InitializeComponent();
        }

        private void VMRoutineInspector_Load(object sender, EventArgs e)
        {
            display.Routine = Routine;
        }
    }
}
