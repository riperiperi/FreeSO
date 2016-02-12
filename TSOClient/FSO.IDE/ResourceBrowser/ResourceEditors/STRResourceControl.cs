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
using System.Threading;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class STRResourceControl : UserControl, IResourceControl
    {
        public STR ActiveString;
        public GameObject ActiveObject;
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
        }

        public void SetActiveObject(GameObject obj)
        {
            ActiveObject = obj;
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
            SaveButton.Enabled = false;

            bool enableMod = (ind != -1);

            StringBox.Enabled = enableMod;
            RemoveButton.Enabled = enableMod;
            UpButton.Enabled = enableMod;
            DownButton.Enabled = enableMod;
            SaveButton.Enabled = enableMod;

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
                SaveButton.Enabled = true;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            OldStr = StringBox.Text;
            var ind = SelectedStringInd;
            Content.Content.Get().BlockingResMod(new ResAction(() =>
            {
                ActiveString.SetString(ind, OldStr);
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = ind;
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd+1;
            Content.Content.Get().BlockingResMod(new ResAction(() =>
            {
                ActiveString.InsertString(ind, new STRItem());
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = StringList.Items.Count-1;
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd;
            Content.Content.Get().BlockingResMod(new ResAction(() =>
            {
                ActiveString.RemoveString(ind);
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = Math.Max(0, ind-1);
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd;
            if (ind == 0) return;

            Content.Content.Get().BlockingResMod(new ResAction(() =>
            {
                var old = ActiveString.GetStringEntry(ind - 1);
                ActiveString.RemoveString(ind-1);
                ActiveString.InsertString(ind, old);
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = ind - 1;
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd;
            if (ind == StringList.Items.Count-1) return;
            
            Content.Content.Get().BlockingResMod(new ResAction(() =>
            {
                var old = ActiveString.GetStringEntry(ind);
                ActiveString.RemoveString(ind);
                ActiveString.InsertString(ind+1, old);
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = ind + 1;
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
            Selector.SetSelectors(ActiveObject.OBJ, ActiveString, selectors);
        }
    }
}
