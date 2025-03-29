using FSO.Client;
using FSO.Content;
using FSO.Content.TS1;
using FSO.Files.Utils;
using FSO.IDE.Utils;
using FSO.SimAntics;
using FSO.SimAntics.Engine.Scopes;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FSO.IDE.ContentEditors
{
    public partial class AvatarTool : Form
    {
        private static Dictionary<string, string> SkeletonToPrefix = new Dictionary<string, string>()
        {
            { "adult", "a" },
            { "cat", "k" },
            { "kat", "k" },
            { "dog", "d" },
            { "child", "c" },
            { "object", "o" },
            { "effects1", "e" }
        };

        private Outfit SelectedOutfit;
        private List<AvatarToolAnimation> SceneAnimations = new List<AvatarToolAnimation>();
        private List<AvatarToolRuntimeMesh> SceneMeshes = new List<AvatarToolRuntimeMesh>();
        private List<AvatarToolRuntimeMesh> BoundRuntimeMeshes = new List<AvatarToolRuntimeMesh>();
        private bool ImportMode;

        public AvatarTool()
        {
            InitializeComponent();
            SetImportMode(false, null);
            Animator.ShowAnim("a2o-standing-loop");
            RefreshSkeletonCombo();
            RefreshAnimList();
            RefreshOutfitList();
            RefreshAccessoryList();
        }

        private void TestAllMesh()
        {
            var meshes = Content.Content.Get().AvatarMeshes.List();
            var real = meshes.Select(x => x.Get()).ToList();
        }
        
        private string SearchString(string str)
        {
            return ".*" + str + ".*";
        }

        private string StartsWithRegex(string regex, params string[] str)
        {
            return $"^({string.Join("|", str)}){regex}";
        }

        private string DoesNotStartWithRegex(string regex, params string[] str)
        {
            return $"^(?i)(?!({string.Join("|", str)})){regex}";
        }

        public void SetImportMode(bool import, string name)
        {
            if (import)
            {
                if (name != null) Text = "Avatar Tool - Import Scene " + name;
                else Text = "Avatar Tool - Import";
            }
            else
            {
                if (name != null) Text = "Avatar Tool - Export Scene " + name;
                else Text = "Avatar Tool - Preparing Export";
            }

            ImportMeshButton.Enabled = import;
            ImportSkeletonButton.Enabled = false; //import;
            MeshImportBox.Enabled = import;
            ExportGLTFButton.Enabled = !import;
            AnimationAdd.Enabled = !import;

            if (import)
            {
                ImportAnimButton.Text = "Import Selected Animations";
            }
            else
            {
                ImportAnimButton.Text = "Remove Selected from Export";
            }
            ImportMode = import;
        }

        public void ClearScene()
        {
            UnbindRuntimeMesh();
            
            //wait for the animator to clear
            for (int i = 0; i < 2; i++)
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                }));
            }

            foreach (var anim in SceneAnimations)
            {
                if (anim.Runtime)
                {
                    //remove from runtime provider
                    var anims = Content.Content.Get().AvatarAnimations;
                    var tso = anims as Content.Framework.TSOAvatarContentProvider<Animation>;
                    if (tso != null)
                    {
                        tso.Runtime.Remove(anim.InternalName + ".anim");
                    }
                }
            }
            SceneAnimations.Clear();

            foreach (var mesh in SceneMeshes)
            {
                if (mesh.IsOutfit)
                {
                    var outfits = Content.Content.Get().AvatarOutfits;
                    var tso = outfits as Content.Framework.TSOAvatarContentProvider<Outfit>;
                    if (tso != null)
                    {
                        tso.Runtime.Remove(mesh.Name + ".oft");
                    }
                }
                else
                {
                    var appearances = Content.Content.Get().AvatarAppearances;
                    var tso = appearances as Content.Framework.TSOAvatarContentProvider<Vitaboy.Appearance>;
                    if (tso != null)
                    {
                        tso.Runtime.Remove(mesh.Name + ".apr");
                    }
                }
            }
            SceneMeshes.Clear();

            AnimationImportBox.Items.Clear();
            MeshImportBox.Items.Clear();
        }

        public void AddList(ListBox list, List<string> items, Regex search)
        {
            list.BeginUpdate();
            list.Items.Clear();

            if (items != null)
            {
                foreach (var item in items)
                {
                    var name = item.ToLowerInvariant();
                    var dot = name.LastIndexOf('.');
                    if (dot > -1) name = name.Substring(0, dot);
                    if (search.IsMatch(name)) list.Items.Add(name); //keys are names
                }
            }
            list.EndUpdate();
        }

        public void UnbindRuntimeMesh()
        {
            ImportMeshButton.Enabled = false;
            foreach (var mesh in BoundRuntimeMeshes)
            {
                Animator.RemoveAccessory(mesh.Name);
            }
            BoundRuntimeMeshes.Clear();
        }

        public void RefreshSkeletonCombo()
        {
            var provider = Content.Content.Get().AvatarSkeletons;

            SkeletonCombo.BeginUpdate();

            if (provider is AvatarSkeletonProvider avaSkel)
            {
                var skels = avaSkel.Names;
                SkeletonCombo.Items.Clear();
                foreach (var skel in skels)
                {
                    SkeletonCombo.Items.Add(skel.Substring(0, skel.Length - 5));
                }
            }
            else if (provider is TS1BCFSkeletonProvider ts1Skel)
            {
                var skels = ts1Skel.BaseProvider.SkelHostBCF.Keys;
                SkeletonCombo.Items.Clear();
                foreach (var skel in skels)
                {
                    SkeletonCombo.Items.Add(skel);
                }
            }

            SkeletonCombo.EndUpdate();
        }

        public void RefreshAnimList()
        {
            var searchString = SearchString(AnimationSearch.Text.ToLowerInvariant());
            var anims = (Content.Content.Get().AvatarAnimations as AvatarAnimationProvider)?.Names;
            if (anims == null)
                anims = (Content.Content.Get().AvatarAnimations as Content.TS1.TS1BCFAnimationProvider)?.BaseProvider.ListAllAnimations();

            if (!AllAnimsCheck.Checked)
            {
                string starts;
                var skel = (SkeletonCombo.SelectedItem as string) ?? "adult";
                var skelExtInd = skel.LastIndexOf('.');
                if (skelExtInd != -1) skel = skel.Substring(0, skelExtInd);
                if (SkeletonToPrefix.TryGetValue(skel, out starts)) {
                    searchString = StartsWithRegex(searchString, starts);
                }
            }

            AddList(AnimationList, anims, new Regex(searchString));
        }

        public void RefreshAccessoryList()
        {
            var accessories = Content.Content.Get().AvatarAppearances;
            var searchString = SearchString(AccessorySearch.Text.ToLowerInvariant());

            List<string> aprs;

            if (accessories is AvatarAppearanceProvider aprProvider)
            {
                aprs = aprProvider?.Names;
            }
            else if (accessories is TS1BCFAppearanceProvider ts1Provider)
            {
                aprs = ts1Provider.BaseProvider.SkinHostBCF.Keys.ToList();
            }
            else
            {
                aprs = new List<string>();
            }

            //exclude outfit accessories
            searchString = DoesNotStartWithRegex(searchString, new string[]
            {
                "fab", "mab", "fam", "mam", "fah", "mah", "uaa"
            });
            /*
            if (anims == null)
                anims = (Content.Content.Get().AvatarAnimations as Content.TS1.TS1BCFAnimationProvider)?.BaseProvider.ListAllAnimations();
                */

            AddList(AccessoryList, aprs, new Regex(searchString));
        }

        public void RefreshOutfitList()
        {
            var accessories = Content.Content.Get().AvatarOutfits;
            var searchString = SearchString(OutfitSearch.Text.ToLowerInvariant());
            var outfits = (accessories as AvatarOutfitProvider)?.Names;
            /*
            if (anims == null)
                anims = (Content.Content.Get().AvatarAnimations as Content.TS1.TS1BCFAnimationProvider)?.BaseProvider.ListAllAnimations();
                */

            AddList(OutfitList, outfits, new Regex(searchString));
        }

        public void RefreshSceneAnims()
        {
            AnimationImportBox.BeginUpdate();

            AnimationImportBox.Items.Clear();
            foreach (var anim in SceneAnimations)
            {
                AnimationImportBox.Items.Add(anim);
            }

            AnimationImportBox.EndUpdate();
        }

        public void RefreshSceneMeshes()
        {
            MeshImportBox.BeginUpdate();

            MeshImportBox.Items.Clear();
            foreach (var mesh in SceneMeshes)
            {
                MeshImportBox.Items.Add(mesh);
            }

            MeshImportBox.EndUpdate();
        }

        private VMPersonSuits GuessOutfitType(string name, Outfit oft)
        {
            if (name.StartsWith("uad") && name.Length > 9)
            {
                switch (name[8])
                {
                    case 'b':
                        return VMPersonSuits.DecorationBack;
                    case 'h':
                        return VMPersonSuits.DecorationHead;
                    case 's':
                        return VMPersonSuits.DecorationShoes;
                    case 't':
                        return VMPersonSuits.DecorationTail;
                }
            }
            if (name.StartsWith("uaa")) return VMPersonSuits.Head;
            if (oft.Region == 18) return VMPersonSuits.DefaultDaywear;
            return VMPersonSuits.Head;
        }

        public void UpdateSelectedOutfit()
        {
            var name = OutfitList.SelectedItem as string;
            if (name == null)
            {
                SelectedOutfit = null;
            }
            // get the content
            SelectedOutfit = Content.Content.Get().AvatarOutfits.Get(name + ".oft");

            if (SelectedOutfit != null)
            {
                var type = GuessOutfitType(name, SelectedOutfit);
                OutfitSet.Text = "Set as " + type.ToString();
            }
        }

        private void AnimationSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshAnimList();
        }

        private void AccessorySearch_TextChanged(object sender, EventArgs e)
        {
            RefreshAccessoryList();
        }

        private void OutfitSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshOutfitList();
        }

        private void AnimationList_SelectedIndexChanged(object sender, EventArgs e)
        {
            Animator.ShowAnim((string)AnimationList.SelectedItem);
        }

        private void AccessoryList_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void OutfitList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectedOutfit();
        }

        private void AnimationAdd_Click(object sender, EventArgs e)
        {
            var name = AnimationList.SelectedItem as string;
            if (name != null)
            {
                var anim = Content.Content.Get().AvatarAnimations.Get(name + ".anim");
                if (anim != null)
                {
                    SceneAnimations.Add(new AvatarToolAnimation(anim));
                    RefreshSceneAnims();
                }
            }
        }

        private void AccessoryAdd_Click(object sender, EventArgs e)
        {
            var accessory = AccessoryList.SelectedItem as string;
            if (accessory != null)
            {
                Animator.AddAccessory(accessory);
            }
        }

        private void AccessoryRemove_Click(object sender, EventArgs e)
        {
            var accessory = AccessoryList.SelectedItem as string;
            if (accessory != null)
            {
                Animator.RemoveAccessory(accessory);
            }
        }

        private void AccessoryClear_Click(object sender, EventArgs e)
        {
            Animator.ClearAccessories();
        }

        private void OutfitSet_Click(object sender, EventArgs e)
        {
            if (SelectedOutfit != null)
            {
                Animator.BindOutfit(GuessOutfitType(OutfitList.SelectedItem as string, SelectedOutfit), SelectedOutfit);
            }
        }

        private void AllAnimsCheck_CheckedChanged(object sender, EventArgs e)
        {
            RefreshAnimList();
            RefreshOutfitList();
        }

        private void NewSceneButton_Click(object sender, EventArgs e)
        {
            ClearScene();
            SetImportMode(false, null);
        }

        private void ExportGLTFButton_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Save the scene as a glTF/glb file.";
            dialog.Filter = "glTF Binary Package|*.glb|glTF Separate|*.gltf";
            dialog.DefaultExt = "glb";
            dialog.AddExtension = true;
            FormsUtils.StaExecute(() =>
            {
                dialog.ShowDialog();
            });

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var interactive = Animator.Renderer;
                var ava = (VMAvatar)interactive.TargetOBJ.BaseObject;

                var exp = new GLTFExporter();
                ava.Avatar.Skeleton = ava.Avatar.BaseSkeleton.Clone();
                ava.Avatar.ReloadSkeleton();
                var scn = exp.SceneGroup(ava.Avatar.Bindings.Select(x => x.Mesh).ToList(),
                    SceneAnimations.Select(x => x.Anim).ToList(),
                    ava.Avatar.Bindings.Select(x => x.Texture?.Get(GameFacade.GraphicsDevice)).ToList(),
                    ava.Avatar.BaseSkeleton);

                if (dialog.FileName == "") return;
                if (dialog.FileName.EndsWith(".gltf")) scn.SaveGLTF(dialog.FileName);
                else scn.SaveGLB(dialog.FileName);
            }));

            SetImportMode(false, Path.GetFileName(dialog.FileName));
        }

        private static ulong RuntimeID = 0;
        private void ImportGLTFButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "glTF Binary Package|*.glb|glTF Separate/Embedded|*.gltf";
            dialog.Title = "Select a glTF/glb file.";
            FormsUtils.StaExecute(() =>
            {
                dialog.ShowDialog();
            });
            if (dialog.FileName == "") return;
            var importer = new GLTFImporter();
            importer.Process(dialog.FileName);
            var anims = importer.Animations;

            // add to runtime
            ClearScene();

            var animC = Content.Content.Get().AvatarAnimations as AvatarAnimationProvider;
            foreach (var anim in anims)
            {
                animC.Runtime.Add(anim.Name + "-runtime.anim", ulong.MaxValue - (RuntimeID++), anim);
                SceneAnimations.Add(new AvatarToolAnimation(anim, anim.Name + "-runtime"));
            }

            var generator = new AppearanceGenerator();
            foreach (var mesh in importer.Meshes)
            {
                generator.GenerateAppearanceTSO(new List<ImportMeshGroup>() { mesh }, mesh.Name + "-runtime", true);
                SceneMeshes.Add(new AvatarToolRuntimeMesh(false, mesh.Name + "-runtime", mesh));
            }

            SetImportMode(true, Path.GetFileName(dialog.FileName));
            RefreshSceneAnims();
            RefreshSceneMeshes();
        }

        private void AnimationImportBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {

        }

        private void AnimationImportBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = AnimationImportBox.SelectedItem as AvatarToolAnimation;
            if (item != null)
            {
                Animator.ShowAnim(item.InternalName);
            }
        }

        private void MeshImportBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UnbindRuntimeMesh();
            var items = MeshImportBox.SelectedItems.Cast<AvatarToolRuntimeMesh>();
            foreach (var item in items)
            {
                Animator.AddAccessory(item.Name);
                BoundRuntimeMeshes.Add(item);
            }
            if (items.Count() > 0) ImportMeshButton.Enabled = true;
        }

        private void ImportAnimButton_Click(object sender, EventArgs e)
        {
            if (ImportMode)
            {
                var animContent = Content.Content.Get().AvatarAnimations;
                var selected = AnimationImportBox.CheckedItems.Cast<AvatarToolAnimation>().ToList();
                var dupes = selected.Where(x =>
                {
                    var existing = animContent.Get(x.ToString() + ".anim");
                    return existing != null;
                });
                if (dupes.Count() > 0)
                {
                    var result = MessageBox.Show($"The following animations are already present in the game: \r\n{String.Join(", ", dupes.Select(x => x.ToString()))}\r\nDo you want to overwrite them?",
                        "Duplicate Animations", MessageBoxButtons.YesNo);
                    if (result == DialogResult.No) return;
                }

                //import the animations to the runtime AND content folder
                //TODO: ts1
                var provider = (animContent as Content.Framework.TSOAvatarContentProvider<Animation>);
                foreach (var anim in selected)
                {
                    using (var mem = new MemoryStream())
                    {
                        using (var writer = IoWriter.FromStream(mem, ByteOrder.BIG_ENDIAN))
                        {
                            anim.Anim.Write(writer, false);
                            provider.CreateFile(anim.ToString() + ".anim", anim.Anim, mem.ToArray(), false);
                        }
                    }
                    
                }

                MessageBox.Show($"Successfully imported animations: \r\n{String.Join(", ", selected.Select(x => x.ToString()))}",
                       "Imported Animations!", MessageBoxButtons.OK);

            }
            else
            {
                //remove animations from export list
                var selected = AnimationImportBox.CheckedItems.Cast<AvatarToolAnimation>().ToList();
                foreach (var item in selected)
                {
                    SceneAnimations.Remove(item);
                    AnimationImportBox.Items.Remove(item);
                }
            }
        }

        private void ImportMeshButton_Click(object sender, EventArgs e)
        {
            var importDialog = new AvatarUtils.AddAppearanceWindow(BoundRuntimeMeshes.Select(x => x.Mesh).ToList(), new List<ImportMeshGroup>());
            importDialog.ShowDialog();
        }

        private void ImportSkeletonButton_Click(object sender, EventArgs e)
        {

        }

        private void AvatarTool_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearScene();
        }

        private void SkeletonCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshAnimList();
            RefreshOutfitList();
        }
    }

    class AvatarToolAnimation
    {
        public Animation Anim;
        public string InternalName;
        public bool Runtime;

        public AvatarToolAnimation(Animation anim)
        {
            Anim = anim;
            InternalName = this.ToString();
        }

        public AvatarToolAnimation(Animation anim, string internalName)
        {
            Anim = anim;
            InternalName = internalName;
            Runtime = true;
        }

        public override string ToString()
        {
            return Anim.Name ?? Anim.XSkillName ?? "a2o-unnamed";
        }
    }

    class AvatarToolRuntimeMesh
    {
        public bool IsOutfit;
        public string Name; //appearance or outfit name for the runtime instance
        public ImportMeshGroup Mesh;

        public AvatarToolRuntimeMesh(bool isOutfit, string name, ImportMeshGroup mesh) {
            IsOutfit = isOutfit;
            Name = name;
            Mesh = mesh;
        }

        public override string ToString()
        {
            return Mesh.Name;
        }
    }
}
