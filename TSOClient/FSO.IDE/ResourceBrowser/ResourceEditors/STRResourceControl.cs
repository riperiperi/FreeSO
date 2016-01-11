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
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class STRResourceControl : UserControl, IResourceControl
    {
        public STR ActiveString;
        private int SelectedStringInd
        {
            get
            {
                return (StringList.SelectedIndices == null || StringList.SelectedIndices.Count == 0) ? -1 : StringList.SelectedIndices[0];
            }
            set
            {
                if (StringList.Items.Count > value) StringList.Items[value].Selected = true;
            }
        }

        public STRResourceControl()
        {
            InitializeComponent();

            LanguageBox.Items.Add("English");
            LanguageBox.SelectedIndex = 0;

            SelectButton.Enabled = false;
        }

        public void SetActiveObject(GameObject obj)
        {
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            ActiveString = (STR)chunk;
            UpdateStrings();
        }

        public void UpdateStrings()
        {
            StringList.SelectedItems.Clear();
            StringList.Items.Clear();
            for (int i=0; i<ActiveString.Length; i++) { 
                StringList.Items.Add(new ListViewItem(new string[] { Convert.ToString(i), ActiveString.GetString(i) }));
            }
            StringList_SelectedIndexChanged(StringList, new EventArgs());
        }

        private void StringList_SelectedIndexChanged(object sender, EventArgs e)
        {
            //change selected string
            var ind = SelectedStringInd;
            if (ind == -1)
            {
                StringBox.Text = "";
                StringBox.Enabled = false;
                RemoveButton.Enabled = false;
            } else
            {
                StringBox.Text = ActiveString.GetString(ind);
                StringBox.Enabled = true;
                RemoveButton.Enabled = true;
            }
        }
    }
}
