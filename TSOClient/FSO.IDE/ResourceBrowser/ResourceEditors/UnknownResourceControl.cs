using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.Content;
using FSO.Files.Formats.IFF;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class UnknownResourceControl : UserControl, IResourceControl
    {
        public UnknownResourceControl()
        {
            InitializeComponent();
        }

        public void SetErrorMsg(string text)
        {
            ErrorLabel.Text = text;
        }

        public void SetActiveObject(GameObject obj)
        {
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
        }
    }
}
