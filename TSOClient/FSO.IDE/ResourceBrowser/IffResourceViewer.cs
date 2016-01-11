using FSO.Content;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.ResourceBrowser
{
    public partial class IffResourceViewer : Form
    {
        public IffResourceViewer()
        {
            InitializeComponent();
        }

        public IffResourceViewer(string name, GameIffResource iff, GameObject srcObj) : this()
        {
            iffRes.ChangeIffSource(iff);
            iffRes.ChangeActiveObject(srcObj);

            Text = "Iff Editor - " + name;
        }
    }
}
