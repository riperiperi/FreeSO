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
using FSO.IDE.EditorComponent;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class BHAVResourceControl : UserControl, IResourceControl
    {
        public BHAV ActiveChunk;
        public TPRP ActiveMeta;
        public GameIffResource ActiveResource;

        public GameObject ActiveObject;

        public BHAVResourceControl()
        {
            InitializeComponent();
        }

        public void SetActiveObject(GameObject obj)
        {
            ActiveObject = obj;
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            ActiveChunk = (BHAV)chunk;
            ActiveMeta = res.Get<TPRP>(chunk.ChunkID);
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            bool meta = (ActiveMeta != null);

            TPRPButton.Enabled = !meta;
            //DescriptionBox.Enabled = meta;

            //populate param and local lists
            ParamList.Items.Clear();
            LocalList.Items.Clear();
            if (meta)
            {
                foreach (var param in ActiveMeta.ParamNames)
                    ParamList.Items.Add(param);
                foreach (var local in ActiveMeta.LocalNames)
                    LocalList.Items.Add(local);
            }
            else
            {
                for (int i = 0; i < ActiveChunk.Args; i++)
                    ParamList.Items.Add("Parameter " + i);
                for (int i = 0; i < ActiveChunk.Locals; i++)
                    LocalList.Items.Add("Local " + i);
            }

            ParamList_SelectedIndexChanged(ParamList, new EventArgs());
            LocalList_SelectedIndexChanged(LocalList, new EventArgs());
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            var bhav = ActiveChunk;
            MainWindow.Instance.BHAVManager.OpenEditor(bhav, ActiveObject);
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
        }

        private void ParamRenameBtn_Click(object sender, EventArgs e)
        {
            if (ParamList.SelectedIndex == -1 || ActiveMeta == null) return;
            int index = ParamList.SelectedIndex;
            var input = new GenericTextInput("Enter a new name for this Parameter.", ParamList.Items[index].ToString());
            input.ShowDialog();
            if (input.DialogResult == DialogResult.OK) {
                string name = input.StringResult;
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    ActiveMeta.ParamNames[index] = name;
                }, ActiveMeta));
                RefreshDisplay();
            }
        }

        private void LocalRenameBtn_Click(object sender, EventArgs e)
        {
            if (LocalList.SelectedIndex == -1 || ActiveMeta == null) return;
            int index = LocalList.SelectedIndex;
            var input = new GenericTextInput("Enter a new name for this Local.", LocalList.Items[index].ToString());
            input.ShowDialog();
            if (input.DialogResult == DialogResult.OK)
            {
                string name = input.StringResult;
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    ActiveMeta.LocalNames[index] = name;
                }, ActiveMeta));
                RefreshDisplay();
            }
        }

        private void ParamAddBtn_Click(object sender, EventArgs e)
        {
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveChunk.Args++;
            }, ActiveChunk));

            if (ActiveMeta != null)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    var newN = new string[ActiveMeta.ParamNames.Length + 1];
                    Array.Copy(ActiveMeta.ParamNames, newN, newN.Length - 1);
                    newN[newN.Length - 1] = "Parameter " + newN.Length;
                    ActiveMeta.ParamNames = newN;
                }, ActiveMeta));
            }

            RefreshDisplay();
        }

        private void LocalRemoveBtn_Click(object sender, EventArgs e)
        {
            int selected = LocalList.SelectedIndex;
            if (selected == -1 && ActiveMeta != null) return;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveChunk.Locals--;
            }, ActiveChunk));

            if (ActiveMeta != null)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    var newN = new string[ActiveMeta.LocalNames.Length - 1];
                    Array.Copy(ActiveMeta.LocalNames, newN, selected);
                    Array.Copy(ActiveMeta.LocalNames, selected + 1, newN, selected, newN.Length - selected);
                    ActiveMeta.LocalNames = newN;
                }, ActiveMeta));
            }

            RefreshDisplay();
        }

        private void LocalAddBtn_Click(object sender, EventArgs e)
        {
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveChunk.Locals++;
            }, ActiveChunk));

            if (ActiveMeta != null)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    var newN = new string[ActiveMeta.LocalNames.Length + 1];
                    Array.Copy(ActiveMeta.LocalNames, newN, newN.Length - 1);
                    newN[newN.Length - 1] = "Local " + newN.Length;
                    ActiveMeta.LocalNames = newN;
                }, ActiveMeta));
            }

            RefreshDisplay();
        }

        private void ParamRemoveBtn_Click(object sender, EventArgs e)
        {
            int selected = ParamList.SelectedIndex;
            if (selected == -1 && ActiveMeta != null) return;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveChunk.Args--;
            }, ActiveChunk));

            if (ActiveMeta != null)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    var newN = new string[ActiveMeta.ParamNames.Length - 1];
                    Array.Copy(ActiveMeta.ParamNames, newN, selected);
                    Array.Copy(ActiveMeta.ParamNames, selected + 1, newN, selected, newN.Length - selected);
                    ActiveMeta.ParamNames = newN;
                }, ActiveMeta));
            }

            RefreshDisplay();
        }

        private void TPRPButton_Click(object sender, EventArgs e)
        {

        }

        private void ParamList_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool enableButtons = ParamList.SelectedIndex != -1;
            ParamRemoveBtn.Enabled = enableButtons;
            ParamRenameBtn.Enabled = enableButtons && ActiveMeta != null;
        }

        private void LocalList_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool enableButtons = LocalList.SelectedIndex != -1;
            LocalRemoveBtn.Enabled = enableButtons;
            LocalRenameBtn.Enabled = enableButtons && ActiveMeta != null;
        }
    }
}
