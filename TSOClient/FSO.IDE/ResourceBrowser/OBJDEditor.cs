using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.Content;
using FSO.IDE.Common;
using FSO.Files.Formats.IFF.Chunks;
using System.IO;
using FSO.UI.Utils;
using FSO.Client;
using FSO.Windows;
using System.Runtime.InteropServices;
using FSO.Files.Formats.IFF;
using System.Threading;

namespace FSO.IDE.ResourceBrowser
{
    public partial class OBJDEditor : UserControl
    {
        public GameObject ActiveObj;
        public ObjectWindow Master;

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

        public OBJDEditor()
        {
            InitializeComponent();
        }

        public void Init(GameObject obj, ObjectWindow master)
        {
            Master = master;
            ActiveObj = obj;
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
                { DeprLimit, "DepreciationLimit" },

                { FootprintNorth, "FootprintNorth" },
                { FootprintEast, "FootprintEast" },
                { FootprintSouth, "FootprintSouth" },
                { FootprintWest, "FootprintWest" },

                { InteractionGroup, "InteractionGroupID" },
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
                { CatCommunity, new PropFlagCombo("LotCategories", 11) },
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
                { TypeCombo, "ObjectType" }
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

            foreach (var entry in OBJDComboEntry)
            {
                var ui = entry.Key;
                ui.SelectedIndexChanged += GenericCombo_SelectedIndexChanged;
            }

            FrontDir.Items.Clear();
            FrontDir.Items.AddRange(Cardinal);
            ShadowType.Items.Clear();
            ShadowType.Items.AddRange(ShadowTypes);

            MultitileList.DrawItem += MultitileList_DrawItem;

            UpdateOBJD(obj);
        }

        private void MultitileList_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            Brush myBrush = Brushes.Black;

            if (e.Index == -1) return;
            var item = (OBJD)MultitileList.Items[e.Index];

            var isMaster = (item.MasterID == 0 || item.SubIndex == -1);
           
            string display = ((isMaster) ? "^" : "  ") + item.ChunkLabel;
            if (isMaster) e.Graphics.FillRectangle(new SolidBrush(((e.State & DrawItemState.Selected) > 0)?Color.Orange:Color.Gold), e.Bounds);
            else
            {
                display += " (";
                display += item.SubIndex & 0xFF;
                display += ", ";
                display += item.SubIndex >> 8;
                display += ")";
            }

            e.Graphics.DrawString(display,
                e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
            // If the ListBox has focus, draw a focus rectangle around the selected item.
            e.DrawFocusRectangle();
        }

        public void UpdateOBJD(GameObject tobj)
        {
            ActiveObj = tobj;
            if (ActiveObj == null || OBJDNumberEntry == null) return;
            OwnChange = true;

            bool isPhysical = (ActiveObj.OBJ.MasterID == 0 || ActiveObj.OBJ.SubIndex > -1); //NOT a multitile master. eg. has collision bounds.
            bool isMaster = (ActiveObj.OBJ.MasterID == 0 || ActiveObj.OBJ.SubIndex == -1); //is single tile or multitile master

            CatalogBox.Enabled = isMaster;
            MotiveBox.Enabled = isMaster;
            ThumbnailBox.Enabled = isMaster;
            PhysicalBox.Enabled = isPhysical;

            ObjectView.ShowObject(ActiveObj.OBJ.GUID);
            NameEntry.Text = ActiveObj.OBJ.ChunkLabel;
            GUIDButton.Text = ActiveObj.OBJ.GUID.ToString("X8");

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
            UpdateMultitileGroup();

            TypeCombo.Items.Clear();
            foreach (var num in Enum.GetValues(typeof(OBJDType)))
            {
                TypeCombo.Items.Add(new NameValueCombo(num.ToString(), Convert.ToInt16(num), true));
            }

            foreach (var combo in OBJDComboEntry)
            {
                var targetValue = ActiveObj.OBJ.GetPropertyByName<ushort>(combo.Value);
                foreach (NameValueCombo item in combo.Key.Items)
                {
                    if (item.Value == targetValue) combo.Key.SelectedItem = item;
                }
            }

            var thumb = ActiveObj.Resource.Get<BMP>(ActiveObj.OBJ.CatalogStringsID);
            ThumbSave.Enabled = false;
            if (thumb != null)
            {
                var mem = new MemoryStream(thumb.data);
                ThumbnailPic.Image = Image.FromStream(mem);
                ThumbSave.Enabled = true;
            }

            OwnChange = false;
        }

        public void UpdateMultitileGroup()
        {
            bool isMaster = (ActiveObj.OBJ.MasterID == 0 || ActiveObj.OBJ.SubIndex == -1);
            var own = OwnChange;
            OwnChange = true;
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

            MultitileList.Items.Clear();
            MultitileList.Items.AddRange(
                ActiveObj.Resource.List<OBJD>()
                .Where(x => x.MasterID == ActiveObj.OBJ.MasterID)
                .OrderBy(x => x.SubIndex)
                .ToArray());

            var isMultitileMaster = ActiveObj.OBJ.MasterID > 0 && ActiveObj.OBJ.SubIndex == -1;
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
            LevelOffset.Enabled = !isMaster;

            OwnChange = own;
        }

        private void CTSSButton_Click(object sender, EventArgs e)
        {
            Master.GotoResource(typeof(CTSS), ActiveObj.OBJ.CatalogStringsID);
        }

        /** -------------------- **/
        /** OBJD Mod Tab         **/
        /** -------------------- **/

        private void NameEntry_TextChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            //ObjNameLabel.Text = NameEntry.Text;
            var name = NameEntry.Text;

            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
            {
                ActiveObj.OBJ.ChunkLabel = name;
            }, ActiveObj.OBJ));
        }

        private void GUIDEntry_TextChanged(object sender, EventArgs e)
        {
            /*
            if (OwnChange) return;
            uint id = ActiveObj.OBJ.GUID;
            try
            {
                id = Convert.ToUInt32(GUIDEntry.Text, 16);
            }
            catch (Exception)
            {

            }

            Content.Content.Get().QueueResMod(new ResAction(() =>
            {
                ActiveObj.OBJ.GUID = id;
            }, ActiveObj.OBJ));
            */
        }

        private void GenericCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var combo = (ComboBox)sender;
            var item = (NameValueCombo)combo.SelectedItem;
            var prop = OBJDComboEntry[combo];

            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
            {
                ActiveObj.OBJ.SetPropertyByName(prop, item.Value);
            }, ActiveObj.OBJ));

            if (combo == MultiGroupCombo)
            {
                UpdateMultitileGroup();
            }
        }

        private void XOffset_ValueChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var value = (byte)XOffset.Value;

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveObj.OBJ.SubIndex = (short)((ActiveObj.OBJ.SubIndex & 0xFF00) | value);
            }, ActiveObj.OBJ));

            ObjectView.ShowObject(ActiveObj.OBJ.GUID);
            //DGRPEdit.ShowObject(ActiveObj.OBJ.GUID);
        }

        private void YOffset_ValueChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var value = (byte)YOffset.Value;

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveObj.OBJ.SubIndex = (short)((ActiveObj.OBJ.SubIndex & 0x00FF) | (value << 8));
            }, ActiveObj.OBJ));

            ObjectView.ShowObject(ActiveObj.OBJ.GUID);
            //DGRPEdit.ShowObject(ActiveObj.OBJ.GUID);
        }

        private void NewMultitile_Click(object sender, EventArgs e)
        {
            //find new Master ID that has not been taken.
            var list = ActiveObj.Resource.List<OBJD>();
            var objs = list.OrderBy(x => x.MasterID);
            ushort lastMaster = 0;
            foreach (var obj in objs)
            {
                if (obj.MasterID > lastMaster + 1)
                {
                    //there is a space in front of lastMaster
                    break;
                }
                lastMaster = obj.MasterID;
            }
            //new id is lastmaster + 1
            //todo: handle 65535 groups??

            ushort newGroup = (ushort)(lastMaster + 1);

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                //must signal to parent
                ActiveObj.OBJ.MasterID = newGroup;
                ActiveObj.OBJ.SubIndex = -1;
            }, ActiveObj.OBJ));
            UpdateMultitileGroup();
        }
        private void OBJDCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var ui = (CheckBox)sender;
            var check = ui.Checked;
            var target = OBJDFlagEntries[ui];

            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
            {
                ushort value = ActiveObj.OBJ.GetPropertyByName<ushort>(target.Property);
                ushort flag = (ushort)(~(1 << target.Flag));

                ActiveObj.OBJ.SetPropertyByName(target.Property, (value & flag) | (check ? (1 << target.Flag) : 0));
            }, ActiveObj.OBJ));
        }

        private void NumberEntry_ValueChanged(object sender, EventArgs e)
        {
            if (OwnChange) return;
            var ui = (NumericUpDown)sender;
            var target = OBJDNumberEntry[ui];

            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
            {
                ActiveObj.OBJ.SetPropertyByName(target, ui.Value);
            }, ActiveObj.OBJ));
        }

        private void RegenThumb_Click(object sender, EventArgs e)
        {
            Bitmap thumbBMP = null;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var thumb = CatThumbGenerator.GenerateThumb(ObjectView.ExtObj, ObjectView.ExtVM);

                thumbBMP = new Bitmap(thumb.Width, thumb.Height, PixelFormat.Format32bppArgb);

                var raw = new byte[thumb.Width * thumb.Height * 4];
                thumb.GetData(raw, 0, (GameFacade.DirectX) ? raw.Length : raw.Length / 4);

                for (int i = 0; i < raw.Length; i += 4)
                {
                    var swap = raw[i];
                    raw[i] = raw[i + 2];
                    raw[i + 2] = swap;
                }

                var bmpData = thumbBMP.LockBits(new Rectangle(0, 0, thumbBMP.Width, thumbBMP.Height), ImageLockMode.WriteOnly, thumbBMP.PixelFormat);
                IntPtr ptr = bmpData.Scan0;

                Marshal.Copy(raw, 0, ptr, bmpData.Stride * bmpData.Height);
                thumbBMP.UnlockBits(bmpData);

                thumb.Dispose();
            }));

            ThumbnailPic.Image = thumbBMP;
            SaveThumbImg(thumbBMP);
        }

        private void SaveThumbImg(Image img)
        {
            ThumbSave.Enabled = true;
            byte[] bdata;
            using (var mem = new MemoryStream())
            {
                img.Save(mem, ImageFormat.Bmp);
                bdata = mem.ToArray();
            }

            var existing = ActiveObj.Resource.Get<BMP>(ActiveObj.OBJ.CatalogStringsID);
            var isNew = (existing == null);
            if (isNew)
            {
                existing = new BMP();
                existing.ChunkParent = ActiveObj.Resource.MainIff;
                existing.ChunkProcessed = true;
                existing.ChunkID = ActiveObj.OBJ.CatalogStringsID;
                existing.ChunkLabel = "";
            }

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                existing.data = bdata;
                existing.ChunkParent.AddChunk(existing);
                if (isNew) existing.AddedByPatch = true;
                existing.RuntimeInfo = ChunkRuntimeState.Modified;
            }, existing));
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select an object thumbnail. (bmp)";
            SaveFile(dialog);
            try
            {
                Stream str;
                if ((str = dialog.OpenFile()) != null)
                {
                    var img = Bitmap.FromStream(str);
                    ThumbnailPic.Image = img;
                    SaveThumbImg(img);
                }
            }
            catch
            {

            }
        }

        private void ThumbSave_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Saving Object Thumbnail...";
            dialog.FileName = ActiveObj.OBJ.CatalogStringsID + ".bmp";
            SaveFile(dialog);

            Stream str;
            if ((str = dialog.OpenFile()) != null)
            {
                ThumbnailPic.Image.Save(str, ImageFormat.Bmp);
                str.Close();
            }
        }

        private void SaveFile(FileDialog dialog)
        {
            // ༼ つ ◕_◕ ༽つ IMPEACH STAThread ༼ つ ◕_◕ ༽つ
            var wait = new AutoResetEvent(false);
            var thread = new Thread(() => {
                dialog.ShowDialog();
                wait.Set();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            wait.WaitOne();
            return;
        }

        // MouseDown instead of MouseClick for right-click handling
        private void GUIDButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                WinFormsClipboard clipboard = new WinFormsClipboard();
                clipboard.Set((sender as Button).Text);
            }
        }

        private void LeadMultitile_Click(object sender, EventArgs e)
        {

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
