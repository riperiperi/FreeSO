using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent;
using FSO.IDE.EditorComponent.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE
{
    public partial class BHAVEditor : Form
    {
        public BHAVEditor(BHAV bhav, EditorScope scope)
        {
            InitializeComponent();

            Text = scope.GetFilename(scope.GetScopeFromID(bhav.ChunkID))+"::"+bhav.ChunkLabel.Trim('\0');
            EditorControl.InitBHAV(bhav, scope);
        }
    }
}
