using System;
using System.Linq;
using System.Windows.Forms;
using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

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
                    (ActiveConstLabel == null) ? "Constant "+i : ActiveConstLabel.Entries[i].Label,
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

            if (ActiveConstLabel != null && ind != -1)
            {
                NameBox.Text = ActiveConstLabel.Entries[ind].Label;
                StringBox.Text = ActiveConstLabel.Entries[ind].Comment;
            }
            else
            {
                StringBox.Text = "";
                if (ind != -1)
                    NameBox.Text = "Constant " + ind;
                else
                    NameBox.Text = "";
            }

            if (ind == -1)
            {
                
                StringBox.Enabled = false;
                NameBox.Enabled = false;
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

        private void CheckDeleteLabels()
        {
            if (ActiveConstLabel == null)
                return;
            if (ActiveConst.Constants.Length <= 0)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    ActiveConstLabel.ChunkParent.FullRemoveChunk(ActiveConstLabel);
                    Content.Content.Get().Changes.ChunkChanged(ActiveConstLabel);
                }));
                ActiveConstLabel = null;
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
            if (ActiveConstLabel == null)
            {
                if (label != "" || comment != "")
                {
                    var newLabels = new TRCN();
                    newLabels.ChunkType = "TRCN";
                    newLabels.ChunkProcessed = true;
                    newLabels.ChunkParent = ActiveConst.ChunkParent;
                    Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                    {
                        newLabels.ChunkID = ActiveConst.ChunkID;
                        newLabels.ChunkLabel = ActiveConst.ChunkLabel;

                        ActiveConst.ChunkParent.AddChunk(newLabels);
                        newLabels.AddedByPatch = true;
                        newLabels.RuntimeInfo = ChunkRuntimeState.Modified;
                        var newEntries = new TRCNEntry[ActiveConst.Constants.Length];
                        for(var i=0;i<ActiveConst.Constants.Length;i++)
                        {
                            newEntries[i] = new TRCNEntry();
                            newEntries[i].Comment = "";
                            newEntries[i].Label = "";
                        }
                        newLabels.Entries = newEntries;
                    }, newLabels));
                    ActiveConstLabel = newLabels;
                }
            }
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
            var ind = SelectedStringInd+1;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var c = ActiveConst.Constants.ToList();
                c.Add(0);
                ActiveConst.Constants = c.ToArray();
            }, ActiveConst));
            if (ActiveConstLabel != null)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    var constEntries = new TRCNEntry[ActiveConst.Constants.Length];
                    Array.Copy(ActiveConstLabel.Entries, constEntries, ActiveConstLabel.Entries.Length);
                    var newEntry = new TRCNEntry();
                    newEntry.Label = "Constant " + (ActiveConst.Constants.Length - 1);
                    newEntry.Comment = "";
                    constEntries[ActiveConst.Constants.Length - 1] = newEntry;
                    ActiveConstLabel.Entries = constEntries;
                }, ActiveConstLabel));
            }
            UpdateStrings();
            SelectedStringInd = StringList.Items.Count-1;
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var constantList = ActiveConst.Constants.ToList();
                constantList.RemoveAt(ind);
                ActiveConst.Constants = constantList.ToArray();
            }, ActiveConst));
            if (ActiveConstLabel != null)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    var constantLabelList = ActiveConstLabel.Entries.ToList();
                    constantLabelList.RemoveAt(ind);
                    ActiveConstLabel.Entries = constantLabelList.ToArray();
                }, ActiveConstLabel));
            }
            CheckDeleteLabels();
            UpdateStrings();
            SelectedStringInd = Math.Max(0, ind - 1);
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd;
            if (ind == 0) return;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var old = ActiveConst.Constants[ind - 1];
                ActiveConst.Constants[ind - 1] = ActiveConst.Constants[ind];
                ActiveConst.Constants[ind] = old;
            }, ActiveConst));
            if (ActiveConstLabel != null)
            {
                var old = ActiveConstLabel.Entries[ind - 1];
                ActiveConstLabel.Entries[ind - 1] = ActiveConstLabel.Entries[ind];
                ActiveConstLabel.Entries[ind] = old;
            }
            UpdateStrings();
            SelectedStringInd = ind - 1;
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd;
            if (ind == StringList.Items.Count - 1) return;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var old = ActiveConst.Constants[ind + 1];
                ActiveConst.Constants[ind + 1] = ActiveConst.Constants[ind];
                ActiveConst.Constants[ind] = old;
            }, ActiveConst));
            if (ActiveConstLabel != null)
            {
                var old = ActiveConstLabel.Entries[ind + 1];
                ActiveConstLabel.Entries[ind + 1] = ActiveConstLabel.Entries[ind];
                ActiveConstLabel.Entries[ind] = old;
            }
            UpdateStrings();
            SelectedStringInd = ind + 1;
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
            Selector.SetSelectors(ActiveObject.OBJ, ActiveConst, selectors);
        }
    }
}
