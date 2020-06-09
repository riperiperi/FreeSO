using FSO.Client;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.Common;
using FSO.IDE.Utils;
using FSO.SimAntics;
using FSO.Vitaboy;
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
            try
            {
                var searchString = new Regex(".*" + SearchBox.Text.ToLowerInvariant() + ".*");

                AllList.Items.Clear();
                var anims = (Content.Content.Get().AvatarAnimations as AvatarAnimationProvider)?.Names;
                if (anims == null)
                    anims = (Content.Content.Get().AvatarAnimations as Content.TS1.TS1BCFAnimationProvider)?.BaseProvider.ListAllAnimations();
                if (anims != null)
                {
                    foreach (var anim in anims)
                    {
                        string name = anim.Substring(0, anim.Length - 5);
                        if (!Content.Content.Get().TS1) name = name.ToLowerInvariant();
                        if (searchString.IsMatch(name)) AllList.Items.Add(name); //keys are names
                    }
                }
            } catch (Exception)
            {

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
                    MyList.Items.Add((i == 0) ? "Stop Animation" : AnimTable.GetString(i, STRLangCode.EnglishUS) ?? "");
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

        private void FBXButton_Click(object sender, EventArgs e)
        {
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var interactive = AnimDisplay.Renderer;
                var ava = (VMAvatar)interactive.TargetOBJ.BaseObject;

                var exp = new GLTFExporter();
                var scn = exp.SceneGroup(ava.Avatar.Bindings.Select(x => x.Mesh).ToList(),
                    new List<Animation> { ava.Animations[0].Anim },
                    ava.Avatar.Bindings.Select(x => x.Texture?.Get(GameFacade.GraphicsDevice)).ToList(),
                    ava.Avatar.BaseSkeleton);

                scn.SaveGLTF("C:/Users/Rhys/Desktop/fsoexport/testsim.gltf");

                /*
                var scn = MeshExporter.SceneGroup(
                    ava.Avatar.Bindings.Select(x => x.Mesh).ToList(),
                    new List<Animation> { ava.Animations[0].Anim },
                    ava.Avatar.Bindings.Select(x => x.Texture?.Get(GameFacade.GraphicsDevice)).ToList(),
                    ava.Avatar.BaseSkeleton);

                MeshExporter.ExportToFBX(scn, @"C:\Users\Rhys\Desktop\fsoexport\test.dae");
                */
            }));
        }

        private static int RuntimeID;

        private void glTFImportButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a glTF/glb file.";
            FormsUtils.StaExecute(() =>
            {
                dialog.ShowDialog();
            });
            if (dialog.FileName == null) return;
            var importer = new GLTFImporter();
            importer.Process(dialog.FileName);
            var anims = importer.Animations;

            var animC = Content.Content.Get().AvatarAnimations as AvatarAnimationProvider;
            foreach (var anim in anims) {
                animC.Runtime.Add(anim.Name + "-runtime.anim", ulong.MaxValue - (ulong)(RuntimeID++), anim);
            }
            RefreshAllList();
        }
    }
}
