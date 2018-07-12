using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent
{
    public partial class VarAnimSelect : Form
    {
        public int SelectedAnim = 0;
        private STR AnimTable;
        private bool InternalChange;
        public VarAnimSelect()
        {
            InitializeComponent();
        }
        
        public VarAnimSelect(STR animTable, int oldSel) : this()
        {
            AnimDisplay.ShowAnim("a2o-standing-loop");
            RefreshAllList();
            AnimTable = animTable;
            SelectedAnim = oldSel;
            RefreshAnimTable();
            if (MyList.Items.Count > 0) MyList.SelectedIndex = oldSel;

            Text = "Select Animation - " + (AnimTable?.ChunkLabel ?? "Missing") + " (#" + ((AnimTable?.ChunkID)?.ToString() ?? "?") + ")";
        }

        public void RefreshAllList()
        {
            var searchString = new Regex(".*" + SearchBox.Text.ToLowerInvariant() + ".*");

            AllList.Items.Clear();
            var anims = (Content.Content.Get().AvatarAnimations as AvatarAnimationProvider)?.AnimationsByName.Keys.ToList();
            if (anims == null)
                anims = (Content.Content.Get().AvatarAnimations as Content.TS1.TS1BCFAnimationProvider)?.BaseProvider.ListAllAnimations();
            if (anims != null)
            {
                foreach (var anim in anims)
                {
                    var name = anim.Substring(0, anim.Length - 5).ToLowerInvariant();
                    if (searchString.IsMatch(name)) AllList.Items.Add(name); //keys are names
                }
            }
        }
        
        public void RefreshAnimTable()
        {
            InternalChange = true;

            MyList.Items.Clear();
            if (AnimTable != null)
            {
                for (int i = 0; i < AnimTable.Length; i++)
                {
                    MyList.Items.Add((i == 0) ? "Stop Animation" : AnimTable.GetString(i, STRLangCode.EnglishUS));
                }

                MyList.SelectedItem = SelectedAnim;
            }

            InternalChange = false;
        }

        private void MyList_SelectedIndexChanged(object sender, EventArgs e)
        {
            //set animation displayed to selected
            if (MyList.SelectedItem == null || InternalChange) return;
            SelectedAnim = MyList.SelectedIndex;
            var name = (string)MyList.SelectedItem;
            AnimDisplay.ShowAnim((name == "Stop Animation")? "a2o-standing-loop" : name);
            SelectAnim.Text = "Select \"" + name+"\"";
        }

        private void AllList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AllList.SelectedItem == null || InternalChange)
            {
                AddButton.Enabled = false;
                return;
            }
            AddButton.Enabled = true;
            AnimDisplay.ShowAnim((string)AllList.SelectedItem);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (AllList.SelectedItem == null) return;
            string name = (string)AllList.SelectedItem;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                AnimTable.InsertString(AnimTable.Length, new STRItem() { Value = name });
            }, AnimTable));
            RefreshAnimTable();
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (MyList.SelectedIndex < 1) return;
            int id = MyList.SelectedIndex;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                AnimTable.RemoveString(id);
            }, AnimTable));

            MyList.SelectedIndex--;
            RefreshAnimTable();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            RefreshAllList();
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void SelectAnim_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
