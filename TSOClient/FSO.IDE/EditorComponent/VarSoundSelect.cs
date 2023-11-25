using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.HIT;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent
{
    public partial class VarSoundSelect : Form
    {
        public int SelectedFWAV = 0;
        private List<FWAV> FWAVTable;
        private IffFile SourceIff;
        private bool InternalChange;
        public VarSoundSelect()
        {
            InitializeComponent();
        }
        private HITSound LastSound = null;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            LastSound?.RemoveOwner(-1);
        }

        public void PlaySound(string evt)
        {
            LastSound?.RemoveOwner(-1);
            LastSound = HIT.HITVM.Get().PlaySoundEvent(evt);
            LastSound?.AddOwner(-1);
        }

        public VarSoundSelect(IffFile sourceIff, int oldSel) : this()
        {
            SourceIff = sourceIff;
            RefreshAllList();
            SelectedFWAV = oldSel;
            RefreshAnimTable();
            //if (MyList.Items.Count > 0) MyList.SelectedIndex = oldSel;
        }

        public void RefreshAllList()
        {
            var searchString = new Regex(".*" + SearchBox.Text.ToLowerInvariant() + ".*");

            AllList.Items.Clear();
            var events = Content.Content.Get().Audio.Events.Keys.OrderBy(x => x);
            foreach (var sound in events)
            {
                var name = sound;
                if (searchString.IsMatch(name)) AllList.Items.Add(name); //keys are names
            }
        }
        
        public void RefreshAnimTable()
        {
            InternalChange = true;
            FWAVTable = SourceIff.List<FWAV>();
            if (FWAVTable == null) FWAVTable = new List<FWAV>();

            MyList.Items.Clear();
            if (FWAVTable != null)
            {
                foreach (var item in FWAVTable)
                {
                    MyList.Items.Add(item.Name);
                }

                MyList.SelectedItem = FWAVTable.FindIndex(x => x.ChunkID == SelectedFWAV);
            }

            InternalChange = false;
        }

        private void MyList_SelectedIndexChanged(object sender, EventArgs e)
        {
            //set animation displayed to selected
            if (MyList.SelectedItem == null || InternalChange) return;
            SelectedFWAV = FWAVTable[MyList.SelectedIndex].ChunkID;
            var name = (string)MyList.SelectedItem;
            PlaySound(name);
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
            PlaySound((string)AllList.SelectedItem);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (AllList.SelectedItem == null) return;
            string name = (string)AllList.SelectedItem;

            var fwav = new FWAV();
            fwav.Name = name;
            fwav.ChunkParent = SourceIff;
            fwav.ChunkProcessed = true;
            fwav.ChunkType = "FWAV";

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ushort resultID = 0;
                for (ushort i=1; i<ushort.MaxValue; i++)
                {
                    if (!FWAVTable.Any(x => x.ChunkID == i))
                    {
                        resultID = i;
                        break;
                    }
                }
                fwav.ChunkID = resultID;
                fwav.ChunkLabel = name;

                SourceIff.AddChunk(fwav);
                fwav.AddedByPatch = true;
                fwav.RuntimeInfo = ChunkRuntimeState.Modified;
            }, fwav));
            RefreshAnimTable();
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (MyList.SelectedIndex < 0) return;

            var chunk = FWAVTable.FirstOrDefault(x => x.ChunkID == SelectedFWAV);
            if (chunk == null) return;

            int id = MyList.SelectedIndex;
            FWAVTable.Remove(chunk);

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                chunk.ChunkParent.FullRemoveChunk(chunk);
                Content.Content.Get().Changes.ChunkChanged(chunk);
            }));

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
