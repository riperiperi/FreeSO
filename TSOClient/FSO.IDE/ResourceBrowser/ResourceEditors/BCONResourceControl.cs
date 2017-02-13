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
    public partial class BCONResourceControl : UserControl, IResourceControl
    {
        public BCON ActiveConst;
        public TRCN ActiveConstLabel;
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
                if (StringList.Items.Count > value && value != -1) StringList.Items[value].Selected = true;
            }
        }

        public BCONResourceControl()
        {
            InitializeComponent();
        }

        public void SetActiveObject(GameObject obj)
        {
            ActiveObject = obj;
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            ActiveConst = (BCON)chunk;
            ActiveConstLabel = res.Get<TRCN>(chunk.ChunkID);
            UpdateStrings();
        }

        public void UpdateStrings()
        {
            StringList.SelectedItems.Clear();
            StringList.Items.Clear();
            for (int i=0; i<ActiveConst.Constants.Length; i++) { 
                StringList.Items.Add(new ListViewItem(new string[] {
                    Convert.ToString(i), 
                    (ActiveConstLabel == null) ? "" : ActiveConstLabel.Entries[i].Label,
                    ActiveConst.Constants[i].ToString(),
                    (ActiveConstLabel == null) ? "" : ActiveConstLabel.Entries[i].Comment

                }));
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
            NameBox.Enabled = enableMod;
            ValueBox.Enabled = enableMod;
            RemoveButton.Enabled = enableMod;
            UpButton.Enabled = enableMod;
            DownButton.Enabled = enableMod;
            SaveButton.Enabled = enableMod;

            if (ind == -1 || ActiveConstLabel == null)
            {
                StringBox.Text = "";
                NameBox.Text = "";
                StringBox.Enabled = false;
                NameBox.Enabled = false;
            }
            else
            {
                NameBox.Text = ActiveConstLabel.Entries[ind].Label;
                StringBox.Text = ActiveConstLabel.Entries[ind].Comment;
            }

            if (enableMod) ValueBox.Value = (short)ActiveConst.Constants[ind];
            else ValueBox.Value = 0;

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
            var value = (ushort)ValueBox.Value;
            var comment = StringBox.Text;
            var label = NameBox.Text;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveConst.Constants[ind] = value;
            }, ActiveConst));
            if (ActiveConstLabel != null) {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    ActiveConstLabel.Entries[ind].Label = label;
                    ActiveConstLabel.Entries[ind].Comment = comment;
                }, ActiveConstLabel));
                UpdateStrings();
            }
            SelectedStringInd = ind;
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            /*
            var ind = SelectedStringInd+1;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveConst.InsertString(ind, new STRItem());
            }, ActiveConst));
            UpdateStrings();
            SelectedStringInd = StringList.Items.Count-1;
            */
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            /*
            var ind = SelectedStringInd;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveConst.RemoveString(ind);
            }, ActiveConst));
            UpdateStrings();
            SelectedStringInd = Math.Max(0, ind-1);
            */
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            /*
            var ind = SelectedStringInd;
            if (ind == 0) return;

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var old = ActiveConst.GetStringEntry(ind - 1);
                ActiveConst.SwapString(ind, ind - 1);
            }, ActiveConst));
            UpdateStrings();
            SelectedStringInd = ind - 1;
            */
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            /*
            var ind = SelectedStringInd;
            if (ind == StringList.Items.Count-1) return;
            
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var old = ActiveConst.GetStringEntry(ind);
                ActiveConst.SwapString(ind, ind + 1);
            }, ActiveConst));
            UpdateStrings();
            SelectedStringInd = ind + 1;
            */
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
            Selector.SetSelectors(ActiveObject.OBJ, ActiveConst, selectors);
        }
    }
}
