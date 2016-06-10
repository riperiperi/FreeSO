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
        private GameObject ActiveObj;
        private IffChunk ActiveRes;

        public UnknownResourceControl()
        {
            InitializeComponent();
            Selector.Visible = false;
        }

        public void SetErrorMsg(string text)
        {
            ErrorLabel.Text = text;
        }

        public void SetActiveObject(GameObject obj)
        {
            ActiveObj = obj;
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            ActiveRes = chunk;
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
            if (ActiveObj != null) Selector.SetSelectors(ActiveObj.OBJ, ActiveRes, selectors);
            else Selector.Enabled = false;

            Selector.Visible = Selector.Enabled;
        }
    }
}
