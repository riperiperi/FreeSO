using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.Common;
using FSO.IDE.EditorComponent;
using FSO.IDE.Managers;
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
    public partial class ObjectWindow : Form, IffResWindow
    {
        public ObjectRegistryEntry ActiveObjTable;
        public GameObject ActiveObj;
        public GameIffResource ActiveIff;
        public string SemiglobalName;

        public ObjectWindow()
        {
            InitializeComponent();
        }

        public bool RegenObjMeta(GameIffResource res)
        {
            var objd = res.List<OBJD>();
            var entries = new List<ObjectRegistryEntry>();

            foreach (var obj in objd)
            {
                ObjectRegistryEntry entry = new ObjectRegistryEntry
                {
                    GUID = obj.GUID,
                    Filename = res.MainIff.Filename,
                    Name = obj.ChunkLabel,
                    Group = (short)obj.MasterID,
                    SubIndex = obj.SubIndex
                };
                entries.Add(entry);
            }

            if (entries.Count == 0) return false;

            entries = entries.OrderBy(x => x.SubIndex).OrderBy(x => x.Group).ToList();

            var GUID = (ActiveObj == null) ? 0 : ActiveObj.OBJ.GUID;
            //populate object selected box with options
            ObjCombo.Items.Clear();
            int i = 0;
            foreach (var item in entries)
            {
                ObjCombo.Items.Add(item);
                if (item.GUID == GUID) ObjCombo.SelectedIndex = i;
                i++;
            }
            if (ObjCombo.SelectedIndex == -1) ObjCombo.SelectedIndex = 0;

            Text = "Edit Object - " + ActiveObjTable.Filename;
            return true;
        }

        public ObjectWindow(GameIffResource iff, GameObject obj) : this()
        {
            DefinitionEditor.Init(null, this);
            IffResView.Init();
            IffResView.ChangeIffSource(iff);
            ActiveObj = obj;
            ActiveIff = iff;
            RegenObjMeta(iff);
        }

        public void ChangeActiveObject(ObjectRegistryEntry obj)
        {
            ActiveObjTable = obj;
            var objd = Content.Content.Get().WorldObjects.Get(obj.GUID);
            if (objd == null) return;
            SetTargetObject(objd);
        }

        public void SetTargetObject(GameObject obj)
        {
            ActiveObj = obj;

            if (ActiveObj != null)
            {
                var sgs = ActiveObj.Resource.List<GLOB>();
                if (sgs != null && sgs.Count > 0)
                {
                    SemiglobalName = sgs[0].Name;
                    SemiGlobalButton.Text = "Semi-Global (" + SemiglobalName + ")";
                    SemiGlobalButton.Enabled = true;
                }
                else
                {
                    SemiglobalName = "";
                    SemiGlobalButton.Text = "Semi-Global";
                    SemiGlobalButton.Enabled = false;
                }
            }
            ObjThumb.ShowObject(obj.OBJ.GUID);

            IffResView.ChangeActiveObject(ActiveObj);
            FuncEditor.SetActiveObject(ActiveObj);
            DrawgroupEdit.SetActiveObject(ActiveObj);
            FSOMEdit.SetActiveObject(ActiveObj);
            XMLEdit.SetActiveObject(ActiveObj);
            PIFFEditor.SetActiveObject(ActiveObj);
            UpgradeEditor.SetActiveObject(ActiveObj);

            //update top var

            ObjNameLabel.Text = obj.OBJ.ChunkLabel;
            ObjDescLabel.Text = "§----";
            if (obj.OBJ.MasterID == 0)
            {
                ObjMultitileLabel.Text = "Single-tile object.";
            }
            else if (obj.OBJ.SubIndex < 0)
            {
                ObjMultitileLabel.Text = "Multitile master object.";
            }
            else
            {
                ObjMultitileLabel.Text = "Multitile part. (" + (obj.OBJ.SubIndex >> 8) + ", " + (obj.OBJ.SubIndex & 0xFF) + ")";
            }

            DefinitionEditor.UpdateOBJD(ActiveObj);
        }

        private void ObjCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChangeActiveObject((ObjectRegistryEntry)ObjCombo.SelectedItem);
        }

        private void GlobalButton_Click(object sender, EventArgs e)
        {
            MainWindow.Instance.IffManager.OpenResourceWindow(EditorScope.Globals.Resource, ActiveObj);
        }

        private void SemiGlobalButton_Click(object sender, EventArgs e)
        {
            var sg = FSO.Content.Content.Get().WorldObjectGlobals.Get(SemiglobalName);
            if (sg == null)
            {
                MessageBox.Show("Error: Semi-Global iff '"+sg+"' could not be found!");
                return;
            }
            MainWindow.Instance.IffManager.OpenResourceWindow(sg.Resource, ActiveObj);
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

        public void GotoResource(IffChunk chunk)
        {
            GotoResource(chunk.GetType(), chunk.ChunkID);
        }

        public void GotoResource(Type type, ushort id)
        {
            objPages.SelectTab(1);
            IffResView.GotoResource(type, ActiveObj.OBJ.CatalogStringsID);
        }

        private void CTSSButton_Click(object sender, EventArgs e)
        {
            GotoResource(typeof(CTSS), ActiveObj.OBJ.CatalogStringsID);
        }

        private void ObjectWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            MainWindow.Instance.IffManager.CloseResourceWindow(ActiveObj.Resource);
        }

        private void NewOBJD_Click(object sender, EventArgs e)
        {
            var iff = ActiveIff.MainIff;
            var objDialog = new NewObjectDialog(iff, false);
            objDialog.ShowDialog();
            if (objDialog.DialogResult == DialogResult.OK)
            {
                RegenObjMeta(ActiveIff);
                MainWindow.Instance.IffManager.OpenResourceWindow(Content.Content.Get().WorldObjects.Get(objDialog.ResultGUID));
            }
        }

        private void DeleteOBJD_Click(object sender, EventArgs e)
        {
            if (ActiveObj == null) return; //???
            var confirm = MessageBox.Show("Are you sure that you want to delete the object \"" + ActiveObj.OBJ.ChunkLabel + "\"? ", 
                "Confirm Deletion?", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.Yes)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    ActiveObj.OBJ.ChunkParent.FullRemoveChunk(ActiveObj.OBJ);
                    Content.Content.Get().Changes.ChunkChanged(ActiveObj.OBJ);
                    Content.Content.Get().WorldObjects.RemoveObject(ActiveObj.OBJ.GUID);
                }));

                if (!RegenObjMeta(ActiveIff))
                {
                    Close();
                }
            }
        }

        private void SGChangeButton_Click(object sender, EventArgs e)
        {
            var textInput = new GenericTextInput("Enter the name of the Semi-Global IFF. (without .iff)", SemiglobalName);
            textInput.ShowDialog();
            if (textInput.DialogResult == DialogResult.OK && ActiveObj != null)
            {
                var name = textInput.StringResult;
                SemiglobalName = name;
                var sgs = ActiveObj.Resource.List<GLOB>();
                if (sgs != null && sgs.Count > 0)
                {
                    //modify existing
                    Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                    {
                        sgs[0].Name = name;
                    }, sgs[0]));
                } else
                {
                    //make one!

                    Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                    {
                        var glob = new GLOB()
                        {
                            ChunkID = 1,
                            ChunkLabel = "Semi-Globals",
                            AddedByPatch = true,
                            ChunkProcessed = true,
                            ChunkType = "GLOB",
                            RuntimeInfo = ChunkRuntimeState.Modified,
                            Name = name
                        };

                        ActiveObj.Resource.MainIff.AddChunk(glob);
                        Content.Content.Get().Changes.ChunkChanged(glob);
                    }));
                }

                if (SemiglobalName != "")
                {
                    SemiGlobalButton.Text = "Semi-Global (" + SemiglobalName + ")";
                    SemiGlobalButton.Enabled = true;
                }
                else
                {
                    SemiGlobalButton.Text = "Semi-Global";
                    SemiGlobalButton.Enabled = false;
                }
            }
        }
    }
}
