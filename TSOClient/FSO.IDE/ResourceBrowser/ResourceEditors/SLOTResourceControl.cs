using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Content;
using FSO.Files.Formats.IFF;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class SLOTResourceControl : UserControl, IResourceControl
    {
        private Dictionary<NumericUpDown, string> SLOTNumberEntry;
        private Dictionary<CheckBox, PropFlagCombo> SLOTFlagEntries;
        private Dictionary<ComboBox, string> SLOTComboEntry;

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

        private SLOT ActiveSLOT;
        public STR ActiveSLOTLabel;
        private SLOTItem ActiveItem;
        public GameObject ActiveObject;

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            ActiveSLOT = (SLOT)chunk;
            ActiveSLOTLabel = res.Get<STR>(257);
            UpdateStrings();
        }

        public void SetActiveObject(GameObject obj)
        {
            ActiveObject = obj;
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
            Selector.SetSelectors(ActiveObject.OBJ, ActiveSLOT, selectors);
        }

        private NameValueCombo[] FacingTypes =
        {
            new NameValueCombo("Anywhere", -3),
            new NameValueCombo("Towards Object", -2),
            new NameValueCombo("Away From Object", -1),
            new NameValueCombo("North", 0),
            new NameValueCombo("North East", 1),
            new NameValueCombo("East", 2),
            new NameValueCombo("South East", 3),
            new NameValueCombo("South", 4),
            new NameValueCombo("South West", 5),
            new NameValueCombo("West", 6),
            new NameValueCombo("North West", 7),
        };

        private NameValueCombo[] SLOTTypes =
        {
            new NameValueCombo("Container (0)", 0),
            new NameValueCombo("Unknown (1)", 1),
            new NameValueCombo("Unknown (2)", 2),
            new NameValueCombo("Routing (3)", 3),
        };

        private bool OwnChange;

        public SLOTResourceControl()
        {
            InitializeComponent();

            SLOTNumberEntry = new Dictionary<NumericUpDown, string>()
            {
                { StandingEntry, "Standing" },
                { SittingEntry, "Sitting" },
                { GroundEntry, "Ground" },
                { OffsetXEntry, "OffsetX" },
                { OffsetYEntry, "OffsetY" },
                { OffsetZEntry, "OffsetZ" },
                { GradientEntry, "Gradient" },
                { ResolutionEntry, "Resolution" },
                { MinEntry, "MinProximity" },
                { OptimalEntry, "OptimalProximity" },
                { MaxEntry, "MaxProximity" },

                { SnapSlotEntry, "SnapTargetSlot" },
            };

            SLOTFlagEntries = new Dictionary<CheckBox, PropFlagCombo>()
            {
                { AbsoluteCheck, new PropFlagCombo("Rsflags", 9) },
                { RandomCheck, new PropFlagCombo("Rsflags", 13) },
                { IgnoreCheck, new PropFlagCombo("Rsflags", 11) },
                { MultitileCheck, new PropFlagCombo("Rsflags", 16) },
                { SnapDirCheck, new PropFlagCombo("Rsflags", 12) },
                { FailureCheck, new PropFlagCombo("Rsflags", 14) },
                { AltsCheck, new PropFlagCombo("Rsflags", 15) },

                { Nc, new PropFlagCombo("Rsflags", 0) },
                { NEc, new PropFlagCombo("Rsflags", 1) },
                { Ec, new PropFlagCombo("Rsflags", 2) },
                { SEc, new PropFlagCombo("Rsflags", 3) },
                { Sc, new PropFlagCombo("Rsflags", 4) },
                { SWc, new PropFlagCombo("Rsflags", 5) },
                { Wc, new PropFlagCombo("Rsflags", 6) },
                { NWc, new PropFlagCombo("Rsflags", 7) },
            };

            SLOTComboEntry = new Dictionary<ComboBox, string>()
            {
                { TypeCombo, "Type" },
                { FacingCombo, "Facing" },
            };
            
            foreach (var entry in SLOTNumberEntry)
            {
                var ui = entry.Key;
                ui.ValueChanged += NumberEntry_ValueChanged;
            }

            foreach (var entry in SLOTFlagEntries)
            {
                var ui = entry.Key;
                ui.CheckedChanged += SLOTCheck_CheckedChanged;
            }

            foreach (var entry in SLOTComboEntry)
            {
                var ui = entry.Key;
                ui.SelectedIndexChanged += GenericCombo_SelectedIndexChanged;
            }

            FacingCombo.Items.Clear();
            FacingCombo.Items.AddRange(FacingTypes);

            TypeCombo.Items.Clear();
            TypeCombo.Items.AddRange(SLOTTypes);

            UpdateSLOTItem();
        }

        public void UpdateStrings()
        {
            StringList.SelectedItems.Clear();
            StringList.Items.Clear();
            for (int i = 0; i < ActiveSLOT.Chronological.Count; i++)
            {
                var type = ActiveSLOT.Chronological[i].Type;
                StringList.Items.Add(new ListViewItem(new string[] {
                    Convert.ToString(i),
                    (ActiveSLOTLabel == null) ? "SLOT" : ActiveSLOTLabel.GetString(i),
                    SLOTTypes.FirstOrDefault(x => x.Value == type)?.Name ?? type.ToString(),
                    (ActiveSLOTLabel == null) ? "" : ActiveSLOTLabel.GetComment(i)

                }));
            }
            StringList_SelectedIndexChanged(StringList, new EventArgs());
        }

        private void StringList_SelectedIndexChanged(object sender, EventArgs e)
        {
            //change selected string
            var ind = SelectedStringInd;

            bool enableMod = (ind != -1);

            RemoveButton.Enabled = enableMod;
            UpButton.Enabled = enableMod;
            DownButton.Enabled = enableMod;
            NameBox.Enabled = enableMod;

            OwnChange = true;
            if (ind == -1)
            {
                ActiveItem = null;
                NameBox.Text = "";
            }
            else
            {
                ActiveItem = ActiveSLOT.Chronological[ind];
                NameBox.Enabled = (ActiveSLOTLabel != null);
                NameBox.Text = ActiveSLOTLabel?.GetString(ind) ?? "";
            }
            UpdateSLOTItem();
            OwnChange = false;
        }

        private void UpdateSLOTItem()
        {
            OwnChange = true;
            var isNull = (ActiveItem == null);
            foreach (var entry in SLOTNumberEntry)
            {
                entry.Key.Enabled = !isNull;
            }

            foreach (var entry in SLOTFlagEntries)
            {
                entry.Key.Enabled = !isNull;
            }

            foreach (var combo in SLOTComboEntry)
            {
                combo.Key.Enabled = !isNull;
            }
            if (isNull)
            {
                OwnChange = false;
                return;
            }

            foreach (var entry in SLOTNumberEntry)
            {
                var ui = entry.Key;
                try
                {
                    if (ui.DecimalPlaces > 0)
                    {
                        ui.Value = (decimal)GetPropertyByName<float>(ActiveItem, entry.Value);
                    }
                    else
                    {
                        ui.Value = GetPropertyByName<int>(ActiveItem, entry.Value);
                    }
                } catch
                {
                    ui.Value = ui.Minimum;
                }
            }

            foreach (var entry in SLOTFlagEntries)
            {
                var ui = entry.Key;
                ui.Checked = (GetPropertyByName<int>(ActiveItem, entry.Value.Property) & (1 << entry.Value.Flag)) > 0;
            }

            foreach (var combo in SLOTComboEntry)
            {
                var targetValue = GetPropertyByName<int>(ActiveItem, combo.Value);
                foreach (NameValueCombo item in combo.Key.Items)
                {
                    if (item.Value == targetValue) combo.Key.SelectedItem = item;
                }
            }

            OwnChange = false;
        }

        private void GenericCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var combo = (ComboBox)sender;
            var item = (NameValueCombo)combo.SelectedItem;
            var prop = SLOTComboEntry[combo];

            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
            {
                if (prop == "Type") ActiveSLOT.Slots[ActiveItem.Type].Remove(ActiveItem);
                SetPropertyByName(ActiveItem, prop, item.Value);
                if (prop == "Type")
                {
                    if (!ActiveSLOT.Slots.ContainsKey(ActiveItem.Type))
                        ActiveSLOT.Slots[ActiveItem.Type] = new List<SLOTItem>();
                    ActiveSLOT.Slots[ActiveItem.Type].Add(ActiveItem);
                }
            }, ActiveSLOT));
        }

        private void SLOTCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var ui = (CheckBox)sender;
            var check = ui.Checked;
            var target = SLOTFlagEntries[ui];

            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
            {
                ushort value = GetPropertyByName<ushort>(ActiveItem, target.Property);
                ushort flag = (ushort)(~(1 << target.Flag));

                SetPropertyByName(ActiveItem, target.Property, (value & flag) | (check ? (1 << target.Flag) : 0));
            }, ActiveSLOT));
        }

        private void NumberEntry_ValueChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var ui = (NumericUpDown)sender;
            var target = SLOTNumberEntry[ui];

            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
            {
                SetPropertyByName(ActiveItem, target, ui.Value);
            }, ActiveSLOT));
        }

        public T GetPropertyByName<T>(object obj, string name)
        {
            Type me = obj.GetType();
            var prop = me.GetProperty(name);
            return (T)Convert.ChangeType(prop.GetValue(obj, null), typeof(T));
        }

        public void SetPropertyByName(object obj, string name, object value)
        {
            Type me = obj.GetType();
            var prop = me.GetProperty(name);
            try
            {
                value = Convert.ChangeType(value, prop.PropertyType);
            }
            catch
            {
                value = Enum.Parse(prop.PropertyType, value.ToString());
            }
            prop.SetValue(obj, value, null);
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            int ind = -1;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ind = ActiveSLOT.Chronological.Count;
                var item = new SLOTItem();
                ActiveSLOT.Chronological.Add(item);
                if (!ActiveSLOT.Slots.ContainsKey(0)) ActiveSLOT.Slots[0] = new List<SLOTItem>();
                ActiveSLOT.Slots[0].Add(item);
            }, ActiveSLOT));
            UpdateStrings();
            SelectedStringInd = ind;
        }

        private void NameBox_TextChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var text = NameBox.Text;
            var ind = SelectedStringInd;
            if (ActiveSLOTLabel != null)
            {
                Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
                {
                    while (ind >= ActiveSLOTLabel.Length)
                        ActiveSLOTLabel.InsertString(ActiveSLOTLabel.Length, new STRItem(""));
                    ActiveSLOTLabel.SetString(ind, text);
                }, ActiveSLOTLabel));
            }
        }
    }
}
