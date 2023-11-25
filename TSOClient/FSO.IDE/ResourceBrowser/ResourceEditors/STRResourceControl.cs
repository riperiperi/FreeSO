using System;
using System.Windows.Forms;
using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class STRResourceControl : UserControl, IResourceControl
    {
        public STR ActiveString;
        public GameObject ActiveObject;

        private STRLangCode ActiveLanguage = STRLangCode.EnglishUS;
        private string OldStr;
        private string OldComment;
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

        public STRResourceControl()
        {
            InitializeComponent();

            LanguageBox.Items.Clear();
            LanguageBox.Items.AddRange(STR.LanguageSetNames);
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
                StringList.Items.Add(new ListViewItem(new string[] { Convert.ToString(i), ActiveString.GetString(i, ActiveLanguage) }));
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
            CommentBox.Enabled = enableMod;
            RemoveButton.Enabled = enableMod;
            UpButton.Enabled = enableMod;
            DownButton.Enabled = enableMod;
            SaveButton.Enabled = enableMod;

            if (ind == -1)
            {
                StringBox.Text = "";
                CommentBox.Text = "";
            }
            else
            {
                StringBox.Text = ActiveString.GetString(ind, ActiveLanguage);
                CommentBox.Text = ActiveString.GetComment(ind, ActiveLanguage);
            }
                
            OldStr = StringBox.Text;
            OldComment = CommentBox.Text;
        }

        private void StringBox_TextChanged(object sender, EventArgs e)
        {
            if (!OldStr.Equals(StringBox.Text) || !OldComment.Equals(CommentBox.Text))
            {
                SaveButton.Enabled = true;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            OldStr = StringBox.Text;
            OldComment = CommentBox.Text;
            var ind = SelectedStringInd;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveString.SetString(ind, OldStr, ActiveLanguage);
                //todo: set comment
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = ind;
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd+1;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveString.InsertString(ind, new STRItem());
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = StringList.Items.Count-1;
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
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

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var old = ActiveString.GetStringEntry(ind - 1);
                ActiveString.SwapString(ind, ind - 1);
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = ind - 1;
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            var ind = SelectedStringInd;
            if (ind == StringList.Items.Count-1) return;
            
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var old = ActiveString.GetStringEntry(ind);
                ActiveString.SwapString(ind, ind + 1);
            }, ActiveString));
            UpdateStrings();
            SelectedStringInd = ind + 1;
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
            Selector.SetSelectors(ActiveObject.OBJ, ActiveString, selectors);
        }

        private void LanguageBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int oldSel = SelectedStringInd;
            int index = LanguageBox.SelectedIndex;
            string chosenName = STR.LanguageSetNames[index];
            bool langExists = false;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                langExists = ActiveString.IsSetInit((STRLangCode)(index + 1));
            }, ActiveString));

            if (!langExists)
            {
                var result = MessageBox.Show("This language has not been initialized for this string resource yet. Initialize '" + chosenName + "'?", 
                    "Language not initialized!", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                    {
                        ActiveString.InitLanguageSet((STRLangCode)(index + 1));
                    }, ActiveString));
                }
                else
                {
                    LanguageBox.SelectedIndex = 0;
                    return;
                }
            }
            ActiveLanguage = (STRLangCode)(index + 1);
            UpdateStrings();
            SelectedStringInd = oldSel;
        }
    }
}
