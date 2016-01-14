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
        private string OldStr;
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
            saveButton.Enabled = false;

            bool enableMod = (ind != -1);

            StringBox.Enabled = enableMod;
            RemoveButton.Enabled = enableMod;
            UpButton.Enabled = enableMod;
            DownButton.Enabled = enableMod;

            if (ind == -1)
                StringBox.Text = "";
            else
                StringBox.Text = ActiveString.GetString(ind);
            OldStr = StringBox.Text;
        }

        private void StringBox_TextChanged(object sender, EventArgs e)
        {
            if (!OldStr.Equals(StringBox.Text))
            {
                saveButton.Enabled = true;
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            OldStr = StringBox.Text;
            var ind = SelectedStringInd;
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                ActiveString.SetString(ind, StringBox.Text);
            }, ActiveString));
            StringList.Items[ind].SubItems[1].Text = StringBox.Text;
            saveButton.Enabled = false;
        }

        private void NewButton_Click(object sender, EventArgs e)
        {

        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {

        }

        private void UpButton_Click(object sender, EventArgs e)
        {

        }

        private void DownButton_Click(object sender, EventArgs e)
        {

        }
    }
}
