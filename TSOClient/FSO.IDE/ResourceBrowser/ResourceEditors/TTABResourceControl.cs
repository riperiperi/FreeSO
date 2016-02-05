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
using FSO.IDE.EditorComponent;
using System.Threading;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class TTABResourceControl : UserControl, IResourceControl
    {
        public TTAB ActiveTTAB;
        public GameIffResource Resource;
        public TTAs Strings;
        public GameObject Object;

        private bool InternalChange;

        private static string[] MotiveNames =
        {
            "Energy: ",
            "Comfort: ",
            "Hunger: ",
            "Hygiene: ",
            "Bladder: ",
            "Room: ",
            "Social: ",
            "Fun: ",
            "Mood: "
        };

        private static string[] VaryNames =
        {
            "None",
            "Nice",
            "Grouchy",
            "Active",
            "Lazy",
            "Generous",
            "Selfish",
            "Playful",
            "Serious",
            "Outgoing",
            "Shy",
            "Neat",
            "Sloppy",
            "Cleaning Skill",
            "Cooking Skill",
            "Social Skill",
            "Repair Skill",
            "Gardening Skill",
            "Music Skill",
            "Creative Skill",
            "Literacy Skill",
            "Physical Skill",
            "Logic Skill"
        };

        private Dictionary<CheckBox, string> FlagNames;

        private int SelectedIndex;
        private TTABInteraction Selected;

        private Dictionary<TreeNode, int> PieToInteraction;
        private Dictionary<int, int> TTAToListIndex;

        public TTABResourceControl()
        {
            InitializeComponent();
            LanguageCombo.Items.Clear();
            LanguageCombo.Items.AddRange(STR.LanguageSetNames);
            LanguageCombo.SelectedIndex = 0;
            MotivePersonality.Items.Clear();
            MotivePersonality.Items.AddRange(VaryNames);

            FlagNames = new Dictionary<CheckBox, string>();
            FlagNames.Add(AllowVisitors, "AllowVisitors");
            FlagNames.Add(AllowFriends, "AllowFriends");
            FlagNames.Add(AllowRoomies, "AllowRoommates");
            FlagNames.Add(AllowOwner, "AllowObjectOwner");
            FlagNames.Add(AllowCSRs, "AllowCSRs");
            FlagNames.Add(AllowGhosts, "AllowGhosts");
            FlagNames.Add(AllowCats, "AllowCats");
            FlagNames.Add(AllowDogs, "AllowDogs");

            //FLAGS
            FlagNames.Add(FlagDebug, "Debug");

            FlagNames.Add(FlagLeapfrog, "Leapfrog");
            FlagNames.Add(FlagMustRun, "MustRun");
            FlagNames.Add(FlagAutoFirst, "AutoFirst");
            FlagNames.Add(FlagRunImmediately, "RunImmediately");
            FlagNames.Add(FlagConsecutive, "AllowConsecutive");


            FlagNames.Add(FlagCarrying, "Carrying");
            FlagNames.Add(FlagRepair, "Repair");
            FlagNames.Add(FlagCheck, "AlwaysCheck");
            FlagNames.Add(FlagDead, "WhenDead");
        }

        public void SetActiveObject(GameObject obj)
        {
            Object = obj;
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            Resource = res;
            Strings = res.Get<TTAs>(chunk.ChunkID);
            ActiveTTAB = (TTAB)chunk;

            if (Strings == null)
            {
                //we have a problem... make us some strings!
                Strings = new TTAs();
                Strings.ChunkLabel = chunk.ChunkLabel;
                Strings.ChunkID = chunk.ChunkID;
                Strings.ChunkProcessed = true;
                res.MainIff.AddChunk(Strings);
            }

            UseButton.Enabled = Object.OBJ.TreeTableID != ActiveTTAB.ChunkID;
            UseButton.Text = (UseButton.Enabled)? "Use this TTAB" : "TTAB for this Object";

            UpdateListing();
            UpdateSelection(-1);
        }

        public BHAV GetBHAV(ushort id)
        {
            if (id >= 8192 && Object != null) return Object.Resource.SemiGlobal.Get<BHAV>(id); //semiglobal
            else if (id >= 4096) return Resource.Get<BHAV>(id); //private
            else return EditorScope.Globals.Resource.Get<BHAV>(id); //global
        }

        public string GetTTA(uint index)
        {
            if (Strings == null || Strings.Length <= index) return "???";
            return Strings.GetString((int)index);
        }

        public void UpdateListing()
        {
            InteractionList.Items.Clear();
            TTAToListIndex = new Dictionary<int, int>();
            int i = 0;
            foreach (var entry in ActiveTTAB.Interactions)
            {
                BHAV test = GetBHAV(entry.TestFunction);
                BHAV action = GetBHAV(entry.ActionFunction);
                InteractionList.Items.Add(
                    new ListViewItem(new string[] { entry.TTAIndex.ToString(),
                        (test == null)?"---":test.ChunkLabel,
                        (action == null)?"---":action.ChunkLabel
                    }));
                TTAToListIndex.Add((int)entry.TTAIndex, i++);
            }

            var sortActions = ActiveTTAB.Interactions.OrderBy(x => GetTTA(x.TTAIndex));
            int prevDepth = 0;
            TreeNode curNode = null;
            TreeNodeCollection curDepth = PieView.Nodes;
            string category = "";
            curDepth.Clear();
            PieToInteraction = new Dictionary<TreeNode, int>();
            foreach (var entry in sortActions)
            {
                BHAV test = GetBHAV(entry.TestFunction);
                BHAV action = GetBHAV(entry.ActionFunction);
                var name = GetTTA(entry.TTAIndex);
                var split = name.Split('/');
                var node = new TreeNode(split[split.Length - 1] + " (" + entry.TTAIndex.ToString() + " / " +
                        ((test == null) ? "---" : test.ChunkLabel) + " / " +
                        ((action == null) ? "---" : action.ChunkLabel) + ")");
                PieToInteraction.Add(node, (int)entry.TTAIndex);

                while (split.Length - 1 < prevDepth || (prevDepth > 0 && category != split[prevDepth - 1]))
                {
                    curNode = curNode.Parent;
                    category = (curNode == null) ? "" : curNode.Text;
                    curDepth = (curNode == null) ? PieView.Nodes : curNode.Nodes;
                    prevDepth--;
                }

                while (split.Length-1 > prevDepth)
                {
                    var newName = split[prevDepth++];
                    var newNode = new TreeNode(newName); //add new category
                    category = newName;
                    curDepth.Add(newNode);
                    curNode = newNode;
                    curDepth = newNode.Nodes;
                }

                curDepth.Add(node);
            }
        }

        public void UpdateSelection(int sel)
        {
            InternalChange = true;
            SelectedIndex = sel;
            bool enabled = true;
            try
            {
                Selected = ActiveTTAB.InteractionByIndex[(uint)sel];
            }
            catch (Exception) {

                Selected = new TTABInteraction() { TTAIndex = 0, MotiveEntries = new TTABMotiveEntry[MotiveNames.Length] };
                enabled = false;
                //disable everything and set Selected to a dummy.
            }

            AllowBox.Enabled = enabled;
            ActionButton.Enabled = enabled;
            CheckButton.Enabled = enabled;
            FlagsBox.Enabled = enabled;
            MetaBox.Enabled = enabled;
            MotiveBox.Enabled = enabled;

            InteractionPathName.Text = GetTTA(Selected.TTAIndex);
            AutonomyInput.Value = Selected.AutonomyThreshold;
            AttenuationCombo.Text = Selected.AttenuationValue.ToString();
            JoinInput.Value = Selected.JoiningIndex;

            UpdateMotiveList();

            foreach (var checkAtt in FlagNames)
            {
                var check = checkAtt.Key;
                var att = checkAtt.Value;
                var property = Selected.GetType().GetProperty(att);
                check.Checked = (bool)property.GetValue(Selected);
            }

            if (InteractionList.Items.Count > 0)
            {
                var int1 = PieToInteraction.FirstOrDefault(x => x.Value == sel);
                PieView.SelectedNode = int1.Key;
                var int2 = -1;
                TTAToListIndex.TryGetValue(sel, out int2);
                if (int2 != -1 && !InteractionList.SelectedIndices.Contains(int2))
                {
                    InteractionList.SelectedIndices.Clear();
                    InteractionList.SelectedIndices.Add(int2);
                }
            }
            InternalChange = false;
            var oldInd = MotiveList.SelectedIndex;
            MotiveList.SelectedIndex = -1;
            MotiveList.SelectedIndex = oldInd;
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void PieView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (InternalChange) return;
            int action = -1;
            PieToInteraction.TryGetValue(e.Node, out action);
            if (action != -1) UpdateSelection(action);
        }

        private void InteractionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InternalChange || InteractionList.SelectedIndices.Count == 0) return;
            UpdateSelection(Convert.ToInt32(InteractionList.SelectedItems[0].Text));
        }

        private void FlagCheckedChanged(object sender, EventArgs e)
        {
            if (InternalChange) return;
            var me = (CheckBox)sender;
            var param = FlagNames[me];
            var property = Selected.GetType().GetProperty(param);
            var sel = Selected;
            bool value = me.Checked;
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                property.SetValue(sel, value);
            }, ActiveTTAB));
        }

        private void InteractionPathName_TextChanged(object sender, EventArgs e)
        {
            if (InternalChange || Strings == null || SelectedIndex == -1) return;
            var ind = (int)Selected.TTAIndex;
            var value = InteractionPathName.Text;
            var wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                Strings.SetString(ind, value);
            }, Strings, wait));
            wait.WaitOne(); //wait for changes to propagate
            UpdateListing();
            UpdateSelection(ind);
        }

        private void UpdateMotiveList()
        {
            var oldInternal = InternalChange;
            InternalChange = true;

            var oldInd = MotiveList.SelectedIndex;
            MotiveList.Items.Clear();
            for (int i = 0; i < MotiveNames.Length; i++)
            {
                var item = Selected.MotiveEntries[i];
                bool hasEffect = (item.EffectRangeMinimum > 0 || item.EffectRangeMaximum > 0);
                string vary = (item.PersonalityModifier == 0) ? "" : (", " + VaryNames[Math.Min(VaryNames.Length - 1, item.PersonalityModifier)]);
                MotiveList.Items.Add(MotiveNames[i] + (hasEffect ? (item.EffectRangeMinimum + ".." + item.EffectRangeMaximum + vary) : "N/A"));
            }
            MotiveList.SelectedIndex = oldInd;

            InternalChange = oldInternal;
        }

        private void MotiveList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InternalChange || MotiveList.SelectedIndex == -1) return;

            var ind = MotiveList.SelectedIndex;
            MinMotive.Value = Selected.MotiveEntries[ind].EffectRangeMinimum;
            MaxMotive.Value = Selected.MotiveEntries[ind].EffectRangeMaximum;
            MotivePersonality.SelectedIndex = Selected.MotiveEntries[ind].PersonalityModifier;
        }

        private void MinMotive_ValueChanged(object sender, EventArgs e)
        {
            if (InternalChange || MotiveList.SelectedIndex == -1) return;

            var ind = MotiveList.SelectedIndex;
            var sel = Selected;
            var value = (short)MinMotive.Value;
            var wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                sel.MotiveEntries[ind].EffectRangeMinimum = value;
            }, ActiveTTAB, wait));
            wait.WaitOne();
            UpdateMotiveList();
        }

        private void MaxMotive_ValueChanged(object sender, EventArgs e)
        {
            if (InternalChange || MotiveList.SelectedIndex == -1) return;

            var ind = MotiveList.SelectedIndex;
            var sel = Selected;
            var value = (short)MaxMotive.Value;
            var wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                sel.MotiveEntries[ind].EffectRangeMaximum = value;
            }, ActiveTTAB, wait));
            wait.WaitOne();
            UpdateMotiveList();
        }

        private void MotivePersonality_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InternalChange || MotiveList.SelectedIndex == -1) return;

            var ind = MotiveList.SelectedIndex;
            var sel = Selected;
            var value = (ushort)Math.Max(0,MotivePersonality.SelectedIndex);
            var wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                sel.MotiveEntries[ind].PersonalityModifier = value;
            }, ActiveTTAB, wait));
            wait.WaitOne();
            UpdateMotiveList();
        }

        private void ActionButton_Click(object sender, EventArgs e)
        {
            var dialog = new SelectTreeDialog(Resource);
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var sel = Selected;
                var value = dialog.ResultID;
                var wait = new AutoResetEvent(false);
                Content.Content.Get().QueueResMod(new ResAction(() =>
                {
                    sel.ActionFunction = value;
                }, ActiveTTAB, wait));
                wait.WaitOne();
                UpdateListing();
            }
        }

        private void CheckButton_Click(object sender, EventArgs e)
        {
            var dialog = new SelectTreeDialog(Resource);
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var sel = Selected;
                var value = dialog.ResultID;
                var wait = new AutoResetEvent(false);
                Content.Content.Get().QueueResMod(new ResAction(() =>
                {
                    sel.TestFunction = value;
                }, ActiveTTAB, wait));
                wait.WaitOne();
                UpdateListing();
            }
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            var sel = Selected;
            var wait = new AutoResetEvent(false);
            int TTAIndex = 0;
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                TTAIndex = Strings.Length;
                Strings.InsertString(Strings.Length, new STRItem { Value = "New Interaction" });
            }, Strings, wait));
            wait.WaitOne();
            
            wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                var action = new TTABInteraction() { TTAIndex = (uint)TTAIndex, MotiveEntries = new TTABMotiveEntry[MotiveNames.Length] };
                ActiveTTAB.InsertInteraction(action, 
                    (sel == null)?ActiveTTAB.Interactions.Length:(Array.IndexOf(ActiveTTAB.Interactions, sel)+1));
            }, ActiveTTAB, wait));
            wait.WaitOne();

            UpdateListing();
        }

        private void RemoveBtn_Click(object sender, EventArgs e)
        {
            var sel = Selected;
            var wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                Strings.RemoveString((int)sel.TTAIndex);
            }, Strings, wait));
            wait.WaitOne();

            wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                ActiveTTAB.DeleteInteraction(Array.IndexOf(ActiveTTAB.Interactions, sel));
            }, ActiveTTAB, wait));
            wait.WaitOne();

            UpdateListing();
        }

        private void MoveUpBtn_Click(object sender, EventArgs e)
        {
            var sel = Selected;
            var wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                var ind = Array.IndexOf(ActiveTTAB.Interactions, sel);
                if (ind == 0) return;
                ActiveTTAB.DeleteInteraction(ind);
                ActiveTTAB.InsertInteraction(sel, ind - 1);
            }, ActiveTTAB, wait));
            wait.WaitOne();

            UpdateListing();
        }

        private void MoveDownBtn_Click(object sender, EventArgs e)
        {
            var sel = Selected;
            var wait = new AutoResetEvent(false);
            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                var ind = Array.IndexOf(ActiveTTAB.Interactions, sel);
                if (ind == ActiveTTAB.Interactions.Length-1) return;
                ActiveTTAB.DeleteInteraction(ind);
                ActiveTTAB.InsertInteraction(sel, ind+1);
            }, ActiveTTAB, wait));
            wait.WaitOne();

            UpdateListing();
        }

        private void UseButton_Click(object sender, EventArgs e)
        {
            if (Object == null) return; //???
            Object.OBJ.TreeTableID = ActiveTTAB.ChunkID;
            UseButton.Enabled = false;
            UseButton.Text = "TTAB for this Object";
        }
    }
}
