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
using FSO.Content.Upgrades.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.OTF;
using Newtonsoft.Json;
using FSO.IDE.Utils;

namespace FSO.IDE.ResourceBrowser
{
    public partial class UpgradeEditor : UserControl
    {
        public GameObject ActiveObj;
        public GameObjectResource ActiveRes;
        public UpgradeIff ActiveUpgrades;
        public UpgradeLevel ActiveLevel;
        public UpgradeSubstitution ActiveSub;
        public UpgradeGroup ActiveGroup;
        public int ActiveGroupInd;
        public ObjectUpgradeConfig ActiveConfig;

        public TreeNode ActiveGroupNode => GroupTree.SelectedNode?.Parent ?? GroupTree.SelectedNode;
        public TreeNode ActiveGroupIndNode => (GroupTree.SelectedNode?.Parent == null) ? null : GroupTree.SelectedNode;

        public bool GroupsTabActive => LevelsTabControl.SelectedTab == GroupPage;

        public List<TuningEntry> TuningEntries;
        public int LevelTabStart = 2;

        private bool InternalChange;

        public UpgradeEditor()
        {
            InitializeComponent();
            LevelsTabControl.SelectedIndex = 0;
        }

        public void SetActiveObject(GameObject obj)
        {
            ActiveObj = obj;
            ActiveRes = obj.Resource;
            ActiveUpgrades = Content.Content.Get().Upgrades?.GetFile(ActiveRes.MainIff.Filename);
            IffSpecificBox.Text = ActiveRes.MainIff.Filename;

            Render();
        }

        public void Render()
        {
            if (Content.Content.Get().Upgrades?.Editable == true)
            {
                if (ActiveUpgrades == null)
                {
                    UpgradeStatusLabel.Text = "This file does not yet have an upgrade table.";
                    IffSpecificBox.Enabled = false;
                    ObjectSpecificBox.Enabled = false;
                    CopyButton.Enabled = false;
                    PasteButton.Enabled = false;
                    SaveButton.Enabled = false;
                    EntryCRDButton.Enabled = true;
                }
                else
                {
                    UpgradeStatusLabel.Text = "Upgrade Table is present.";
                    IffSpecificBox.Enabled = true;
                    ObjectSpecificBox.Enabled = true;
                    CopyButton.Enabled = true;
                    PasteButton.Enabled = true;
                    SaveButton.Enabled = true;
                    EntryCRDButton.Enabled = true;

                    UpdateObjectProperties();
                    PrepareTabs();

                }
            }
            else
            {
                UpgradeStatusLabel.Text = "Upgrade editing is not available when online.";
                IffSpecificBox.Enabled = false;
                ObjectSpecificBox.Enabled = false;
                CopyButton.Enabled = false;
                PasteButton.Enabled = false;
                SaveButton.Enabled = false;
                EntryCRDButton.Enabled = false;
            }
        }

        private void PrepareTabs()
        {
            //remove old tab pages
            var oldSelectedIndex = LevelsTabControl.SelectedIndex;
            var pages = LevelsTabControl.TabPages;
            pages.Clear();
            pages.Add(ConstantPage);
            pages.Add(GroupPage);
            int i = 0;
            foreach (var level in ActiveUpgrades.Upgrades)
            {
                pages.Add(ParseName(level.Name));
            }
            pages.Add("Add...");
            LevelsTabControl.SelectedIndex = Math.Min(oldSelectedIndex, pages.Count - 2);
            UpdateSubsList();
        }

        public string ParseName(string name)
        {
            int id;
            if (int.TryParse(name, out id) && id >= 0 && id < UpgradeNameText.Items.Count)
            {
                return UpgradeNameText.Items[id].ToString();
            }
            return name;
        }

        private void UpdateFile()
        {
            Content.Content.Get().Upgrades.UpdateFile(ActiveUpgrades, true);
            InternalChange = true;
            if (ConstantSubList.SelectedItem != null) ConstantSubList.Items[ConstantSubList.SelectedIndex] = ConstantSubList.SelectedItem;
            if (UpgradeSubList.SelectedItem != null) UpgradeSubList.Items[UpgradeSubList.SelectedIndex] = UpgradeSubList.SelectedItem;
            InternalChange = false;
        }

        private void Save()
        {
            Content.Content.Get().Upgrades.SaveJSONTuning();
        }

        public string GetTuningVariableLabel(GameIffResource res, uint tableID, uint keyID)
        {
            var bcon = res.Get<BCON>((ushort)(tableID));
            if (bcon != null)
            {
                var trcn = res.Get<TRCN>((ushort)(tableID));
                if (trcn != null && keyID < trcn.Entries.Length) return trcn.Entries[keyID].Label;
                return bcon.ChunkLabel + " #" + keyID;
            }

            var tuning = res.Get<OTFTable>((ushort)(tableID));
            if (tuning != null)
            {
                return tuning.GetKey((int)keyID)?.Label ?? "";
            }
            return tableID + ":" + keyID;
        }

        public string GetTuningVariableLabel(uint tableID, uint keyID)
        {
            int mode = 0;
            if (tableID < 4096) mode = 2;
            else if (tableID < 8192) mode = 0;
            else if (ActiveRes.SemiGlobal != null) mode = 1;

            BCON bcon;
            OTFTable tuning;
            /** This could be in a BCON or an OTF **/
            switch (mode)
            {
                case 0:
                    return GetTuningVariableLabel(ActiveRes, tableID, keyID);
                case 1:
                    return GetTuningVariableLabel(ActiveRes.SemiGlobal, tableID, keyID);
                case 2:
                    return GetTuningVariableLabel(Content.Content.Get().WorldObjectGlobals.Get("global").Resource, tableID, keyID);
            }

            return tableID + ":" + keyID;
        }

        private void AddTuningEntry(KeyValuePair<uint, short> tuning, bool withReplacement)
        {
            var tableID = tuning.Key >> 16;
            var keyID = tuning.Key & 0xFFFF;

            var entry = new TuningEntry();
            entry.Label = GetTuningVariableLabel(tableID, keyID);
            entry.Identifier = tableID + ":" + keyID;
            entry.Value = tuning.Value;

            if (withReplacement)
            {
                //search for a replacement
                var replacement = ActiveUpgrades.Subs.FirstOrDefault(x => x.Old == entry.Identifier);
                //TODO: use it
                //if (replacement != null) entry.Value = replacement.New;
            }
            TuningEntries.Add(entry);
        }

        public void UpdateTuningEntries(bool withReplacement, bool withGroups)
        {
            TuningEntries = new List<TuningEntry>();
            if (withGroups)
            {
                TuningEntries.AddRange(ActiveUpgrades.Groups.Select(x =>
                    new TuningEntry()
                    {
                        Label = "Group '" + x.Name + "'",
                        Identifier = "G" + ActiveUpgrades.Groups.IndexOf(x),
                        Value = 0
                    }));
            }

            foreach (var tuning in ActiveRes.TuningCache)
                AddTuningEntry(tuning, withReplacement);

            if (ActiveRes.SemiGlobal != null)
            {
                foreach (var tuning in ActiveRes.SemiGlobal.TuningCache)
                    AddTuningEntry(tuning, withReplacement);
            }

            foreach (var tuning in Content.Content.Get().WorldObjectGlobals.Get("global").Resource.TuningCache)
                AddTuningEntry(tuning, withReplacement);

            SubFromTuning.Items.Clear();
            if (withGroups)
            {
                /*
                var groups = 
                SubFromTuning.Items.AddRange(ActiveUpgrades.Groups.Select(x =>
                    new TuningEntry()
                    {
                        Label = "Group '" + x.Name + "'",
                        Identifier = "G" + ActiveUpgrades.Groups.IndexOf(x),
                        Value = 0
                    }).ToArray());*/
            }
            SubFromTuning.Items.AddRange(TuningEntries.ToArray());
            SubTargetTuning.Items.Clear();
            SubTargetTuning.Items.AddRange(TuningEntries.Where(x => x.Identifier.Length < 1 || x.Identifier[0] != 'G').ToArray());
        }

        public void UpdateObjectProperties()
        {
            ObjectUpgradeConfig config = ActiveUpgrades.Config.FirstOrDefault(x => x.GUID.ToLowerInvariant() == ActiveObj.GUID.ToString("x8"));
            if (config == null)
            {
                config = new ObjectUpgradeConfig();
                config.GUID = ActiveObj.GUID.ToString("x8");
                ActiveUpgrades.Config.Add(config);
                UpdateFile();
            }
            ActiveConfig = config;

            var upgradeNames = ActiveUpgrades.Upgrades.Select(x => ParseName(x.Name));
            StartLevelCombo.Items.Clear();
            EndLevelCombo.Items.Clear();
            StartLevelCombo.Items.AddRange(upgradeNames.ToArray());
            EndLevelCombo.Items.AddRange(upgradeNames.ToArray());

            StartLevelCombo.SelectedIndex = Math.Min(upgradeNames.Count() - 1, config.Level);
            EndLevelCombo.SelectedIndex = Math.Min(config.Limit ?? -1, upgradeNames.Count() - 1);

            FlagOriginal.Checked = config.Special == true;

            ObjectSpecificBox.Text = $"Object Properties ({ActiveObj.OBJ.ChunkLabel})";
        }

        private void RenderSubs(ListBox target, List<UpgradeSubstitution> subs)
        {
            target.Items.Clear();
            target.Items.AddRange(subs.Select(x => new SubEntry() { Sub = x, Owner = this }).ToArray());
        }

        private void RenderSub()
        {
            if (InternalChange) return;
            if (ActiveSub == null)
            {
                if (ActiveGroup != null && ActiveGroupInd != -1)
                {
                    //group target
                    SubSpecificBox.Enabled = true;
                    SubTargetValueRadio.Enabled = false;
                    SubTargetTuningRadio.Enabled = false;
                    SubTargetValue.Enabled = false;
                    SubTargetTuning.Enabled = false;
                    UpdateTuningEntries(false, false);

                    var tuningInd = TuningEntries.FindIndex(x => x.Identifier == ActiveGroup.Tuning[ActiveGroupInd]);
                    SubFromTuning.SelectedIndex = tuningInd;
                }
                else
                {
                    SubSpecificBox.Enabled = false;
                }
                return;
            }
            SubSpecificBox.Enabled = true;
            SubTargetValueRadio.Enabled = true;
            SubTargetTuningRadio.Enabled = true;
            UpdateTuningEntries(ActiveLevel != null, true);

            SubFromTuning.SelectedIndex = TuningEntries.FindIndex(x => x.Identifier == ActiveSub.Old);

            var isValue = ActiveSub.New[0] == 'V';
            SubTargetValueRadio.Checked = isValue;
            SubTargetTuningRadio.Checked = !isValue;
            SubTargetValue.Enabled = isValue;
            SubTargetTuning.Enabled = !isValue;

            if (isValue)
            {
                SubTargetTuning.SelectedIndex = -1;
                SubTargetValue.Value = short.Parse(ActiveSub.New.Substring(1));
            } else
            {
                SubTargetTuning.SelectedIndex = TuningEntries.FindIndex(x => x.Identifier == ActiveSub.New.Substring(1));
                SubTargetValue.Value = 0;
            }
        }

        private void UpdateSubsList()
        {
            ActiveSub = null;
            RenderSub();
            if (ActiveLevel == null)
            {
                RenderSubs(ConstantSubList, ActiveUpgrades.Subs);
            }
            else
            {
                UpgradeContainer.Parent = LevelsTabControl.SelectedTab;
                UpgradeContainer.BackColor = Color.White;

                int parsedLevelName;
                if (int.TryParse(ActiveLevel.Name, out parsedLevelName))
                {
                    UpgradeNameText.SelectedIndex = parsedLevelName;
                }
                else
                {
                    UpgradeNameText.Text = ActiveLevel.Name;
                }

                DescriptionText.Text = ActiveLevel.Description;
                AdText.Text = ActiveLevel.Ad;

                var literal = ActiveLevel.Price[0] == 'R' || ActiveLevel.Price[0] == '$';
                if (literal)
                {
                    PriceValueRadio.Checked = true;
                    UpgradeValue.Value = int.Parse(ActiveLevel.Price.Substring(1));
                    RelativeCheck.Checked = ActiveLevel.Price[0] == 'R';
                }
                else
                {
                    PriceObjectRadio.Checked = true;
                }
                UpdatePriceObject();

                RenderSubs(UpgradeSubList, ActiveLevel.Subs);
            }
        }

        private void UpdatePriceObject()
        {
            uint guid;
            if (!uint.TryParse(ActiveLevel.Price, System.Globalization.NumberStyles.HexNumber, null, out guid))
            {
                UpgradeObjectButton.Text = "Select Object";
            }
            else
            {
                var obj = Content.Content.Get().WorldObjects.Get(guid);
                if (obj == null) UpgradeObjectButton.Text = "Select Object";
                else UpgradeObjectButton.Text = obj.OBJ.ChunkLabel;
            }
        }

        private void UpgradeNameText_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UpgradeNameText.SelectedIndex == UpgradeNameText.Items.Count - 1)
            {
                //custom name entry dialog
                // TODO
            }
            else
            {
                ActiveLevel.Name = UpgradeNameText.SelectedIndex.ToString();
            }
            LevelsTabControl.SelectedTab.Text = ParseName(ActiveLevel.Name);
        }

        private void AdText_TextChanged(object sender, EventArgs e)
        {
            ActiveLevel.Ad = AdText.Text;
            UpdateFile();
        }

        private void AdObjectButton_Click(object sender, EventArgs e)
        {
            //open object browser
        }

        private void PriceObjectRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (PriceObjectRadio.Checked)
            {
                UpgradeObjectButton.Enabled = true;
                UpgradeValue.Enabled = false;
                PriceValueRadio.Checked = false;
                RelativeCheck.Enabled = false;
            }
        }

        private void PriceValueRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (PriceValueRadio.Checked)
            {
                UpgradeValue.Enabled = true;
                UpgradeObjectButton.Enabled = false;
                PriceObjectRadio.Checked = false;
                RelativeCheck.Enabled = true;
                if (!(ActiveLevel.Price[0] == 'R' || ActiveLevel.Price[0] == '$'))
                {
                    ActiveLevel.Price = "$0";
                    UpgradeValue.Value = 0;
                    RelativeCheck.Checked = false;
                    UpdatePriceObject();
                    UpdateFile();
                }
            }
        }

        private void RelativeCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (ActiveLevel == null) return;

            if (!(ActiveLevel.Price[0] == 'R' || ActiveLevel.Price[0] == '$'))
            {
                ActiveLevel.Price = "$0";
                UpgradeValue.Value = 0;
                RelativeCheck.Checked = false;
            }

            ActiveLevel.Price = (RelativeCheck.Checked ? "R" : "$") + ActiveLevel.Price.Substring(1);
            UpdateFile();
        }

        private void UpgradeObjectButton_Click(object sender, EventArgs e)
        {
            PriceObjectRadio.Checked = true;
            var popup = new EditorComponent.VarObjectSelect();
            popup.ShowDialog();
            //open object window...
            if (popup.DialogResult == DialogResult.OK) {
                ActiveLevel.Price = popup.GUIDResult.ToString("x8");
            }
            UpdatePriceObject();
        }

        private void UpgradeValue_ValueChanged(object sender, EventArgs e)
        {
            ActiveLevel.Price = ((RelativeCheck.Checked) ? "R" : "$") + UpgradeValue.Value.ToString();
            UpdateFile();
        }

        private void DescriptionText_TextChanged(object sender, EventArgs e)
        {
            ActiveLevel.Description = DescriptionText.Text;
        }

        private void UpgradeSubList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = UpgradeSubList.SelectedItem;
            ActiveSub = (item as SubEntry)?.Sub;
            RenderSub();
        }

        private void AddSubButton_Click(object sender, EventArgs e)
        {
            if (GroupsTabActive)
            {
                if (ActiveGroup != null)
                {
                    ActiveGroup.Tuning.Add("0:0");
                    RenderGroups();
                }
            }
            else
            {
                var newSub = new UpgradeSubstitution();
                if (ActiveLevel == null)
                {
                    ActiveUpgrades.Subs.Add(newSub);
                }
                else
                {
                    ActiveLevel.Subs.Add(newSub);
                }
                UpdateSubsList();
            }
            UpdateFile();
        }

        private void RemoveSubButton_Click(object sender, EventArgs e)
        {
            if (GroupsTabActive)
            {
                if (ActiveGroup != null)
                {
                    if (ActiveGroupInd != -1)
                    {
                        ActiveGroup.Tuning.RemoveAt(ActiveGroupInd);
                        ActiveGroupInd = -1;
                    }
                    RenderGroups();
                }
            }
            else
            {
                if (ActiveSub == null) return;
                if (ActiveLevel == null)
                {
                    ActiveUpgrades.Subs.Remove(ActiveSub);
                }
                else
                {
                    ActiveLevel.Subs.Remove(ActiveSub);
                }
                UpdateSubsList();
            }
            UpdateFile();
        }

        private void SubFromTuning_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = SubFromTuning.SelectedItem as TuningEntry;
            if (item == null) return;
            if (ActiveSub == null)
            {
                if (ActiveGroup != null && ActiveGroupInd != -1)
                {
                    ActiveGroup.Tuning[ActiveGroupInd] = item.Identifier;
                    var node = ActiveGroupIndNode;
                    if (node != null)
                    {
                        node.Text = item.ToString();
                    }
                    UpdateFile();
                }
                return;
            }
            ActiveSub.Old = item.Identifier;
            UpdateFile();
        }

        private void SubTargetTuningRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (SubTargetTuningRadio.Checked)
            {
                SubTargetValueRadio.Checked = false;
                SubTargetTuning.Enabled = true;
                SubTargetValue.Enabled = false;
            }
        }

        private void SubTargetTuning_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ActiveSub == null) return;
            var item = SubTargetTuning.SelectedItem as TuningEntry;
            if (item == null) return;
            ActiveSub.New = "C" + item.Identifier;
            UpdateFile();
        }

        private void SubTargetValueRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (SubTargetValueRadio.Checked)
            {
                SubTargetTuningRadio.Checked = false;
                SubTargetTuning.Enabled = false;
                SubTargetValue.Enabled = true;
            }
        }

        private void SubTargetValue_ValueChanged(object sender, EventArgs e)
        {
            if (ActiveSub == null || !SubTargetValueRadio.Checked) return;
            ActiveSub.New = "V" + SubTargetValue.Value.ToString();
        }

        private void StartLevelCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ActiveConfig == null || StartLevelCombo.SelectedIndex == -1) return;
            ActiveConfig.Level = StartLevelCombo.SelectedIndex;
        }

        private void EndLevelCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ActiveConfig == null || EndLevelCombo.SelectedIndex == -1) return;
            ActiveConfig.Limit = EndLevelCombo.SelectedIndex;
        }

        private void FlagOriginal_CheckedChanged(object sender, EventArgs e)
        {
            if (ActiveConfig == null) return;
            ActiveConfig.Special = FlagOriginal.Checked ? true : (bool?)null;
            UpdateFile();
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            var levels = ActiveUpgrades.Upgrades;
            var text = JsonConvert.SerializeObject(levels, Formatting.Indented);
            FormsUtils.StaExecute(() =>
            {
                Clipboard.SetText(text);
            });
        }

        private void PasteButton_Click(object sender, EventArgs e)
        {
            try
            {
                var oldLevels = ActiveUpgrades.Upgrades;
                string text = null;
                FormsUtils.StaExecute(() =>
                {
                    text = Clipboard.GetText();
                });
                var levels = JsonConvert.DeserializeObject<List<UpgradeLevel>>(text);

                if (oldLevels.Count > 0)
                {
                    var result = MessageBox.Show("This operation will overwrite existing upgrade levels. Continue?", "Warning", MessageBoxButtons.YesNo);
                    if (result == DialogResult.No) return;
                }

                ActiveUpgrades.Upgrades = levels;
                UpdateFile();
                Render();
            }
            catch
            {
                MessageBox.Show("The clipboard did not contain valid upgrade data. Try paste it somewhere else and make sure it's at least valid JSON.", "Hey!");
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void EntryCRDButton_Click(object sender, EventArgs e)
        {
            ActiveUpgrades = new UpgradeIff();
            ActiveUpgrades.Name = ActiveRes.MainIff.Filename;
            UpdateFile();
            Save();
            Render();
        }

        private void LevelsTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var ind = LevelsTabControl.SelectedIndex;
            if (ind == -1) return;
            ActiveGroup = null;
            ActiveGroupInd = -1;
            ActiveSub = null;
            if (ind < LevelTabStart)
            {
                ActiveLevel = null;
                if (LevelsTabControl.SelectedTab == GroupPage)
                {
                    UpdateTuningEntries(false, false);
                    RenderGroups();
                }
            }
            else if (ind == LevelsTabControl.TabPages.Count - 1 && LevelsTabControl.TabPages.Count > LevelTabStart)
                AddUpgrade();
            else
                ActiveLevel = ActiveUpgrades.Upgrades[ind - LevelTabStart];
            UpdateSubsList();
        }

        private void AddUpgrade()
        {
            if (ActiveUpgrades == null) return;
            var newLevel = new UpgradeLevel();
            ActiveUpgrades.Upgrades.Add(newLevel);
            PrepareTabs();
            UpdateFile();
        }

        private void RemoveUpgradeButton_Click(object sender, EventArgs e)
        {
            if (ActiveUpgrades == null || ActiveLevel == null) return;
            ActiveUpgrades.Upgrades.Remove(ActiveLevel);
            PrepareTabs();
            UpdateFile();
        }

        private void RenderGroups()
        {
            GroupTree.Nodes.Clear();
            foreach (var group in ActiveUpgrades.Groups)
            {
                var node = new TreeNode(group.Name);
                foreach (var target in group.Tuning)
                {
                    var child = new TreeNode(TuningEntries.FirstOrDefault(x => x.Identifier == target)?.ToString() ?? target);
                    node.Nodes.Add(child);
                }
                GroupTree.Nodes.Add(node);
            }
            GroupTree.ExpandAll();
        }

        private void UpdateSelectedGroupItem()
        {
            GroupNameBox.Text = ActiveGroup?.Name ?? "";
            RenderSub();
        }

        private void GroupTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //select a group, optionally an index
            var node = GroupTree.SelectedNode;
            if (node == null)
            {
                ActiveGroup = null;
                ActiveGroupInd = -1;
            }
            else
            {
                var parent = node.Parent;
                if (parent == null)
                {
                    //group only
                    var groupInd = GroupTree.Nodes.IndexOf(node);
                    if (groupInd < ActiveUpgrades.Groups.Count)
                    {
                        ActiveGroup = ActiveUpgrades.Groups[groupInd];
                        ActiveGroupInd = -1;
                    }
                }
                else
                {
                    var tuningInd = parent.Nodes.IndexOf(node);
                    var groupInd = GroupTree.Nodes.IndexOf(parent);
                    if (groupInd < ActiveUpgrades.Groups.Count)
                    {
                        ActiveGroup = ActiveUpgrades.Groups[groupInd];
                        ActiveGroupInd = tuningInd;
                    }
                }
            }
            UpdateSelectedGroupItem();
        }

        private void GroupAdd_Click(object sender, EventArgs e)
        {
            ActiveUpgrades.Groups.Add(new UpgradeGroup() { Name = "New Group" });
            RenderGroups();
            GroupTree.SelectedNode = GroupTree.Nodes[GroupTree.Nodes.Count - 1];
            UpdateFile();
        }

        private void GroupRemove_Click(object sender, EventArgs e)
        {
            if (ActiveGroup == null) return;
            ActiveUpgrades.Groups.Remove(ActiveGroup);
            ActiveGroup = null;
            ActiveGroupInd = -1;
            RenderGroups();
            UpdateFile();
        }

        private void GroupNameBox_TextChanged(object sender, EventArgs e)
        {
            if (ActiveGroup == null) return;
            ActiveGroup.Name = GroupNameBox.Text;
            var node = ActiveGroupNode;
            if (node != null)
            {
                node.Text = GroupNameBox.Text;
            }
        }
    }

    public class TuningEntry {
        public string Label;
        public string Identifier;
        public short Value;

        public override string ToString()
        {
            return $"{Label} ({Identifier} | {Value})";
        }
    }

    public class SubEntry
    {
        public UpgradeSubstitution Sub;
        public UpgradeEditor Owner;

        public override string ToString()
        {
            string target = null;
            if (Sub.Old.Length > 1 && Sub.Old[0] == 'G')
            {
                //parse group
                int grpID = 0;
                int.TryParse(Sub.Old.Substring(1), out grpID);

                var groups = Owner.ActiveUpgrades.Groups;
                if (grpID < 0 || grpID >= groups.Count) target = "Group " + grpID;
                else target = "Group '"+groups[grpID].Name+"'";
            } else {
                //parse old
                var oldSplit = Sub.Old.Split(':');
                if (oldSplit.Length != 2) throw new Exception("Tuning to substitute invalid: " + Sub.Old);
                uint table;
                uint index;

                uint.TryParse(oldSplit[0], out table);
                uint.TryParse(oldSplit[1], out index);

                target = Owner.GetTuningVariableLabel(table, index);
            }

            //parse new
            if (Sub.New.Length == 0) throw new Exception("Substitution value cannot be empty.");
            string value;
            switch (Sub.New[0])
            {
                case 'C':
                    //lookup constant in the file.
                    var newSplit = Sub.New.Substring(1).Split(':');
                    if (newSplit.Length != 2) throw new Exception("Substitution value tuning ref invalid: " + Sub.New);
                    uint ntable;
                    uint nindex;
                    uint.TryParse(newSplit[0], out ntable);
                    uint.TryParse(newSplit[1], out nindex);

                    value = Owner.GetTuningVariableLabel(ntable, nindex);
                    break;
                case 'V':
                    //it's just a value.
                    value = Sub.New.Substring(1);
                    break;
                default:
                    throw new Exception("Invalid substitution value: " + Sub.New);
            }

            return $"{target} -> {value}";
        }
    }
}
