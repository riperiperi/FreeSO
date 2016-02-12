using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.Common;
using FSO.IDE.EditorComponent;
using FSO.IDE.ResourceBrowser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE
{
    public partial class ObjectWindow : Form
    {
        public ObjectRegistryEntry ActiveObjTable;
        public GameObject ActiveObj;
        public string SemiglobalName;

        private Dictionary<NumericUpDown, string> OBJDNumberEntry;
        private Dictionary<CheckBox, PropFlagCombo> OBJDFlagEntries;
        private Dictionary<ComboBox, string> OBJDComboEntry;
        private bool OwnChange;

        private NameValueCombo[] Cardinal =
        {
            new NameValueCombo("North", 0),
            new NameValueCombo("East", 1),
            new NameValueCombo("South", 2),
            new NameValueCombo("West", 3),
        };

        private NameValueCombo[] ShadowTypes =
        {
            new NameValueCombo("Default", 0),
            new NameValueCombo("Square", 16)
        };

        public ObjectWindow()
        {
            InitializeComponent();
        }

        public ObjectWindow(List<ObjectRegistryEntry> df, uint GUID) : this()
        {
            if (df.Count == 0)
            {
                MessageBox.Show("Not a valid object!");
                this.Close();
                return;
            }

            //populate object selected box with options
            ObjCombo.Items.Clear();
            int i = 0;
            foreach (var master in df)
            {
                ObjCombo.Items.Add(master);
                if (master.GUID == GUID) ObjCombo.SelectedIndex = i;
                i++;
                foreach (var child in master.Children)
                {
                    ObjCombo.Items.Add(child);
                    if (child.GUID == GUID) ObjCombo.SelectedIndex = i;
                    i++;
                }
            }
            if (ObjCombo.SelectedIndex == -1) ObjCombo.SelectedIndex = 0;

            Text = "Edit Object - " + ActiveObjTable.Filename;

            OBJDNumberEntry = new Dictionary<NumericUpDown, string>()
            {
                { VersionEntry, "ObjectVersion" },
                { LevelOffset, "LevelOffset" },
                { SalePrice, "SalePrice" },
                { MotiveBladder, "RatingBladder" },
                { MotiveComfort, "RatingComfort" },
                { MotiveEnergy, "RatingEnergy" },
                { MotiveFun, "RatingFun" },
                { MotiveHunger, "RatingHunger" },
                { MotiveHygiene, "RatingHygiene" },
                { TileWidth, "TileWidth" },
                { ShadowEntry, "ShadowBrightness" },
                { DeprInitial, "InitialDepreciation" },
                { DeprDaily, "DailyDepreciation" },
                { DeprLimit, "DepreciationLimit" }
            };

            OBJDFlagEntries = new Dictionary<CheckBox, PropFlagCombo>()
            {
                { CatMoney, new PropFlagCombo("LotCategories", 1) },
                { CatOffbeat, new PropFlagCombo("LotCategories", 2) },
                { CatRomance, new PropFlagCombo("LotCategories", 3) },
                { CatServices, new PropFlagCombo("LotCategories", 4) },
                { CatShopping, new PropFlagCombo("LotCategories", 5) },
                { CatSkills, new PropFlagCombo("LotCategories", 6) },
                { CatWelcome, new PropFlagCombo("LotCategories", 7) },
                { CatGames, new PropFlagCombo("LotCategories", 8) },
                { CatEntertainment, new PropFlagCombo("LotCategories", 9) },
                { CatResidence, new PropFlagCombo("LotCategories", 10) },
                { SklCooking, new PropFlagCombo("RatingSkillFlags", 0) },
                { SklMechanical, new PropFlagCombo("RatingSkillFlags", 1) },
                { SklLogic, new PropFlagCombo("RatingSkillFlags", 2) },
                { SklBody, new PropFlagCombo("RatingSkillFlags", 3) },
                { SklCreativity, new PropFlagCombo("RatingSkillFlags", 4) },
                { SklCharisma, new PropFlagCombo("RatingSkillFlags", 5) },
                { GlobalSim, new PropFlagCombo("Global", 0) }
            };

            OBJDComboEntry = new Dictionary<ComboBox, string>()
            {
                { FrontDir, "FrontDirection" },
                { MultiGroupCombo, "MasterID" },
                { ShadowType, "Shadow" },
            };

            foreach (var entry in OBJDNumberEntry)
            {
                var ui = entry.Key;
                ui.ValueChanged += NumberEntry_ValueChanged;
            }

            foreach (var entry in OBJDFlagEntries)
            {
                var ui = entry.Key;
                ui.CheckedChanged += OBJDCheck_CheckedChanged;
            }

            FrontDir.Items.Clear();
            FrontDir.Items.AddRange(Cardinal);
            ShadowType.Items.Clear();
            ShadowType.Items.AddRange(ShadowTypes);

            UpdateOBJD();
        }

        private void OBJDCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var ui = (CheckBox)sender;
            var check = ui.Checked;
            var target = OBJDFlagEntries[ui];

            Content.Content.Get().QueueResMod(new ResAction(() => {
                ushort value = ActiveObj.OBJ.GetPropertyByName<ushort>(target.Property);
                ushort flag = (ushort)(~(1 << target.Flag));

                ActiveObj.OBJ.SetPropertyByName(target.Property, (value & flag) | (check?(1<<target.Flag):0));
            }, ActiveObj.OBJ));
        }

        private void NumberEntry_ValueChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var ui = (NumericUpDown)sender;
            var target = OBJDNumberEntry[ui];

            Content.Content.Get().QueueResMod(new ResAction(() => {
                ActiveObj.OBJ.SetPropertyByName(target, ui.Value);
            }, ActiveObj.OBJ));
        }

        public void UpdateOBJD()
        {
            if (OBJDNumberEntry == null) return;
            OwnChange = true;

            NameEntry.Text = ActiveObj.OBJ.ChunkLabel;
            GUIDEntry.Text = ActiveObj.OBJ.GUID.ToString("x8");

            foreach (var entry in OBJDNumberEntry)
            {
                var ui = entry.Key;
                ui.Value = ActiveObj.OBJ.GetPropertyByName<decimal>(entry.Value);
            }

            foreach (var entry in OBJDFlagEntries)
            {
                var ui = entry.Key;
                ui.Checked = (ActiveObj.OBJ.GetPropertyByName<ushort>(entry.Value.Property) & (1 << entry.Value.Flag)) > 0;
            }

            var ctss = ActiveObj.Resource.Get<CTSS>(ActiveObj.OBJ.CatalogStringsID);
            CatalogNameLabel.Text = (ctss == null) ? "<NO CTSS>" : ctss.GetString(0);
            CTSSIDLabel.Text = "(CTSS #" + ActiveObj.OBJ.CatalogStringsID + ")";

            //set up multitile box

            MultiGroupCombo.Items.Clear();
            MultiGroupCombo.Items.Add(new NameValueCombo("Single-Tile", 0, true));
            MultiGroupCombo.SelectedIndex = 0;
            var i = 1;
            foreach (var obj in ActiveObj.Resource.List<OBJD>())
            {
                if (obj.MasterID != 0 && obj.SubIndex == -1)
                {
                    MultiGroupCombo.Items.Add(new NameValueCombo(obj.ChunkLabel, obj.MasterID, true));
                    if (obj.MasterID == ActiveObj.OBJ.MasterID) MultiGroupCombo.SelectedIndex = i;
                    i++;
                }
            }

            //set up collision
            FootprintNorth.Value = ActiveObj.OBJ.FootprintMask & 15;
            FootprintEast.Value = (ActiveObj.OBJ.FootprintMask>>4) & 15;
            FootprintSouth.Value = (ActiveObj.OBJ.FootprintMask>>8) & 15;
            FootprintWest.Value = (ActiveObj.OBJ.FootprintMask>>12) & 15;

            var isMaster = ActiveObj.OBJ.SubIndex == -1;
            if (isMaster)
            {
                XOffset.Value = 0;
                YOffset.Value = 0;
            }
            else
            {
                XOffset.Value = (sbyte)ActiveObj.OBJ.SubIndex;
                YOffset.Value = (sbyte)(ActiveObj.OBJ.SubIndex >> 8);
            }

            XOffset.Enabled = !isMaster;
            YOffset.Enabled = !isMaster;

            foreach (var combo in OBJDComboEntry)
            {
                var targetValue = ActiveObj.OBJ.GetPropertyByName<ushort>(combo.Value);
                foreach (NameValueCombo item in combo.Key.Items)
                {
                    if (item.Value == targetValue) combo.Key.SelectedItem = item;
                }
            }

            OwnChange = false;
        }

        public void ChangeActiveObject(ObjectRegistryEntry obj)
        {
            ActiveObjTable = obj;
            ActiveObj = Content.Content.Get().WorldObjects.Get(obj.GUID);

            if (ActiveObj != null)
            {
                var sgs = ActiveObj.Resource.List<GLOB>();
                if (sgs != null && sgs.Count>0)
                {
                    SemiglobalName = sgs[0].Name;
                    SemiGlobalButton.Text = "Semi-Global ("+SemiglobalName+")";
                    SemiGlobalButton.Enabled = true;
                }
                else
                {
                    SemiglobalName = "";
                    SemiGlobalButton.Text = "Semi-Global";
                    SemiGlobalButton.Enabled = false;
                }
            }
            ObjThumb.ShowObject(obj.GUID);
            ObjectView.ShowObject(obj.GUID);
            DGRPEdit.ShowObject(obj.GUID);

            if (IffResView.ActiveIff == null) IffResView.ChangeIffSource(ActiveObj.Resource);
            IffResView.ChangeActiveObject(ActiveObj);

            //update top var

            ObjNameLabel.Text = obj.Name;
            ObjDescLabel.Text = "§----";
            if (obj.Group == 0)
            {
                ObjMultitileLabel.Text = "Single-tile object.";
            }
            else if (obj.SubIndex < 0)
            {
                ObjMultitileLabel.Text = "Multitile master object.";
            }
            else
            {
                ObjMultitileLabel.Text = "Multitile part. (" + (obj.SubIndex >> 8) + ", " + (obj.SubIndex & 0xFF) + ")";
            }

            UpdateOBJD();
        }

        private void ObjCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChangeActiveObject((ObjectRegistryEntry)ObjCombo.SelectedItem);
        }

        private void GlobalButton_Click(object sender, EventArgs e)
        {
            var globalWindow = new IffResourceViewer("global", EditorScope.Globals.Resource, ActiveObj);
            globalWindow.Show();
        }

        private void SemiGlobalButton_Click(object sender, EventArgs e)
        {
            var sg = FSO.Content.Content.Get().WorldObjectGlobals.Get(SemiglobalName);
            if (sg == null)
            {
                MessageBox.Show("Error: Semi-Global iff '"+sg+"' could not be found!");
                return;
            }
            var globalWindow = new IffResourceViewer(SemiglobalName, sg.Resource, ActiveObj);
            globalWindow.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var test = FSO.Files.Formats.PiffEncoder.GeneratePiff(ActiveObj.Resource.Iff);
            var filename = "Content/Patch/"+test.Filename;
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            using (var stream = new FileStream(filename, FileMode.Create))
                test.Write(stream);
        }

        private void iffButton_Click(object sender, EventArgs e)
        {
            var test = ActiveObj.Resource.Iff;
            var filename = "Content/Objects/" + test.Filename;
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            using (var stream = new FileStream(filename, FileMode.Create))
                test.Write(stream);
        }

        private void ShadowType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void CTSSButton_Click(object sender, EventArgs e)
        {
            objPages.SelectTab(1);
            IffResView.GotoResource(typeof(CTSS), ActiveObj.OBJ.CatalogStringsID);
        }

        /** -------------------- **/
        /** OBJD Mod Tab         **/
        /** -------------------- **/

        private void NameEntry_TextChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            ObjNameLabel.Text = NameEntry.Text;
            var name = NameEntry.Text;

            Content.Content.Get().QueueResMod(new ResAction(() => {
                ActiveObj.OBJ.ChunkLabel = name;
            }, ActiveObj.OBJ));
        }

        private void GUIDEntry_TextChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            uint id = ActiveObj.OBJ.GUID;
            try {
                id = Convert.ToUInt32(GUIDEntry.Text, 16);
            } catch (Exception)
            {

            }

            Content.Content.Get().QueueResMod(new ResAction(() => {
                ActiveObj.OBJ.GUID = id;
            }, ActiveObj.OBJ));
        }
         
        private void GenericCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var combo = (ComboBox)sender;
            var item = (NameValueCombo)combo.SelectedItem;
            var prop = OBJDComboEntry[combo];

            Content.Content.Get().QueueResMod(new ResAction(() => {
                ActiveObj.OBJ.SetPropertyByName(prop, item.Value);
            }, ActiveObj.OBJ));
        }

        private void XOffset_ValueChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var value = (byte)XOffset.Value;

            Content.Content.Get().BlockingResMod(new ResAction(() => {
                ActiveObj.OBJ.SubIndex = (short)((ActiveObj.OBJ.SubIndex & 0xFF00) | value);
            }, ActiveObj.OBJ));

            ObjectView.ShowObject(ActiveObj.OBJ.GUID);
            DGRPEdit.ShowObject(ActiveObj.OBJ.GUID);
        }

        private void YOffset_ValueChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var value = (byte)YOffset.Value;

            Content.Content.Get().BlockingResMod(new ResAction(() => {
                ActiveObj.OBJ.SubIndex = (short)((ActiveObj.OBJ.SubIndex & 0x00FF) | (value<<8));
            }, ActiveObj.OBJ));

            ObjectView.ShowObject(ActiveObj.OBJ.GUID);
            DGRPEdit.ShowObject(ActiveObj.OBJ.GUID);
        }

        private void NewMultitile_Click(object sender, EventArgs e)
        {
            //find new Master ID that has not been taken.
            var list = ActiveObj.Resource.List<OBJD>();
            var objs = list.OrderBy(x => x.MasterID);
            ushort lastMaster = 0;
            foreach (var obj in objs)
            {
                if (obj.MasterID > lastMaster+1)
                {
                    //there is a space in front of lastMaster
                    break;
                }
                lastMaster = obj.MasterID;
            }
            //new id is lastmaster + 1
            //todo: handle 65535 groups??

            ushort newGroup = (ushort)(lastMaster + 1);

            Content.Content.Get().BlockingResMod(new ResAction(() => {
            }, ActiveObj.OBJ));
            
        }
    }

    public class NameValueCombo
    {
        public string Name;
        public int Value;
        public bool Important;

        public NameValueCombo(string name, int value, bool important)
        {
            Name = name;
            Value = value;
            Important = important;
        }

        public NameValueCombo(string name, int value) : this(name, value, false) { }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PropFlagCombo
    {
        public string Property;
        public int Flag;

        public PropFlagCombo(string prop, int flag)
        {
            Property = prop;
            Flag = flag;
        }
    }
}
