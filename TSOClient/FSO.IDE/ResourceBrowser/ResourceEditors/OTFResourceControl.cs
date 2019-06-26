using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.Files.Formats.IFF;
using FSO.Content;
using FSO.Files.Formats.OTF;
using System.Xml;
using System.IO;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class OTFResourceControl : UserControl, IResourceControl
    {
        public OTFResourceControl()
        {
            InitializeComponent();
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            OTFFile tuning = null;
            if (res is GameObjectResource)
            {
                tuning = ((GameObjectResource)res).Tuning;
            }
            else if (res is GameGlobalResource)
            {
                tuning = ((GameGlobalResource)res).Tuning;
            }
            if (tuning == null)
            {
                XMLDisplay.Text = "No OTF is present for this iff.";
            }
            else
            {
                using (var stream = new StringWriter())
                {
                    var writer = new XmlTextWriter(stream);
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 2;

                    tuning.Document.Save(writer);

                    XMLDisplay.Text = stream.ToString();
                }
            }
        }

        public void SetActiveObject(GameObject obj)
        {

        }
        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {

        }
    }
}
