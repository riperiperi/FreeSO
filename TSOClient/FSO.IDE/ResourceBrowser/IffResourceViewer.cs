using FSO.Content;
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

        private void piffDebugButton_Click(object sender, EventArgs e)
        {
            var test = FSO.Files.Formats.PiffEncoder.GeneratePiff(iffRes.ActiveIff.MainIff);
            var filename = "Content/Patch/" + test.Filename;
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            using (var stream = new FileStream(filename, FileMode.Create))
                test.Write(stream);
        }
    }
}
