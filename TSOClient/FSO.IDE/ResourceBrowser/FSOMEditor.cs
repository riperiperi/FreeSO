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
using System.Threading;
using System.IO;
using FSO.Files.RC;
using FSO.Common.Utils;
using FSO.Common;

namespace FSO.IDE.ResourceBrowser
{
    public partial class FSOMEditor : UserControl
    {
        public GameObject ActiveObject;
        public GameIffResource ActiveIff;
        public DGRP ActiveDGRP;
        public FSOR ActiveFSOR;
        public bool IffMode;
        public DGRPRCParams ActiveParams = new DGRPRCParams();

        private Dictionary<ListViewItem, ushort> DGRPChunkIDs;

        public FSOMEditor()
        {
            InitializeComponent();
        }

        public void SetActiveObject(GameObject active)
        {
            ActiveObject = active;
            ActiveIff = active.Resource;
            ActiveDGRP = null;

            Debug3D.ShowObject(active.OBJ.GUID);

            UpdateDGRPList(true);
            var allowEdit = (ActiveObject.OBJ.ObjectType != OBJDType.Person);
            DGRPBox.Enabled = allowEdit;

            var lower = active.OBJ.ChunkParent.Filename.ToLowerInvariant();
            if (!DGRP3DMesh.ParamsByIff.TryGetValue(lower, out ActiveParams))
            {
                ActiveParams = new DGRPRCParams();
            }

            ActiveFSOR = ActiveIff.List<FSOR>()?.FirstOrDefault();
            InternalChange = true;
            if (ActiveFSOR == null) IffMode = false;
            else
            {
                IffMode = true;
                ActiveParams = ActiveFSOR.Params;
            }

            SimpleCheck.Checked = ActiveParams.Simplify;
            DoorCheck.Checked = ActiveParams.DoorFix;
            CounterCheck.Checked = ActiveParams.CounterFix;
            BlenderCheck.Checked = ActiveParams.BlenderTweak;
            Rot1.Checked = ActiveParams.Rotations[0];
            Rot2.Checked = ActiveParams.Rotations[1];
            Rot3.Checked = ActiveParams.Rotations[2];
            Rot4.Checked = ActiveParams.Rotations[3];

            IffCheck.Checked = IffMode;

            InternalChange = false;
        }

        public void UpdateDGRPList(bool selectBase)
        {
            DGRPChunkIDs = new Dictionary<ListViewItem, ushort>();

            var baseList = ActiveIff.List<DGRP>();
            if (baseList == null)
            {
                DGRPList.Items.Clear();
                return;
            }

            var dgrps = baseList.OrderBy(x => x.ChunkID);
            var oldIndex = (DGRPList.SelectedIndices.Count == 0) ? -1 : DGRPList.SelectedIndices[0];
            DGRPList.Items.Clear();

            var baseG = -1;
            var lastG = -1;

            foreach (var dgrp in dgrps)
            {
                var relativeIndex = dgrp.ChunkID - ActiveObject.OBJ.BaseGraphicID;
                bool notUsed = relativeIndex < 0 || relativeIndex >= ActiveObject.OBJ.NumGraphics;

                var listItem = new ListViewItem(new string[] { (notUsed) ? ("(" + relativeIndex + ")") : relativeIndex.ToString(), dgrp.ChunkLabel });

                if (relativeIndex == 0)
                {
                    baseG = DGRPList.Items.Count;
                    listItem.BackColor = Color.LightSeaGreen;
                }
                else if (relativeIndex == ActiveObject.OBJ.NumGraphics - 1)
                    listItem.BackColor = Color.LightSalmon;

                if (notUsed)
                    listItem.BackColor = Color.LightGray;
                else
                    lastG = DGRPList.Items.Count;

                DGRPList.Items.Add(listItem);
                DGRPChunkIDs.Add(listItem, dgrp.ChunkID);
            }
            if (baseG != -1)
            {
                if (selectBase || oldIndex == -1)
                {
                    DGRPList.Items[baseG].Selected = true;
                    if (lastG > -1) DGRPList.EnsureVisible(lastG);
                }
            }

            if (!selectBase && oldIndex != -1)
            {
                var newInd = Math.Min(oldIndex, DGRPList.Items.Count - 1);
                DGRPList.Items[newInd].Selected = true;
                DGRPList.EnsureVisible(newInd);
            }
        }

        private void DGRPList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DGRPList.SelectedIndices.Count == 0 || !DGRPChunkIDs.ContainsKey(DGRPList.SelectedItems[0]))
            {
                return;
            }
            ActiveDGRP = ActiveIff.Get<DGRP>(DGRPChunkIDs[DGRPList.SelectedItems[0]]);

            if (ActiveDGRP != null)
            {
                //change selected graphic
                Debug3D.ChangeGraphic(ActiveDGRP.ChunkID);
                //UpdateImage();
            }
            SetCustomMode(false);
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            var mesh = Debug3D.Mesh;
            var dialog = new SaveFileDialog();
            dialog.FileName = mesh.Name + ".obj";
            dialog.Title = "Saving OBJ for " + mesh.Name + "...";
            FolderSave(dialog);

            Stream str;
            if ((str = dialog.OpenFile()) != null)
            {
                mesh.SaveOBJ(str, Path.GetFileNameWithoutExtension(dialog.FileName));
                var dirname = Path.GetDirectoryName(dialog.FileName);
                var ext = Path.GetExtension(dialog.FileName);
                using (var io = File.Open(dialog.FileName.Substring(0, dialog.FileName.Length - ext.Length) + ".mtl", FileMode.Create))
                    mesh.SaveMTL(io, dirname);
                str.Close();
            }
        }

        private void FolderSave(SaveFileDialog dialog)
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

        private void FolderOpen(OpenFileDialog dialog)
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

        private void ImportButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select an OBJ file.";
            FolderOpen(dialog);
            var dgrp = ActiveDGRP;

            try
            {
                Stream str;
                if ((str = dialog.OpenFile()) != null)
                {
                    var obj = new OBJ(str);
                    //identify and copy replacement textures
                    //only happens when this iff doesnt have a 3d model

                    foreach (var mtl in obj.FacesByObjgroup.Keys)
                    {
                        var split = mtl.Split('_');
                        if (split.Length < 3 || split[1] == "SPR" || split[0] == "DEPTH") continue;

                        var baseDir = Path.GetDirectoryName(dialog.FileName);
                        var copyname = "TEX_" + split[2] + ".png";
                        if (IffMode)
                        {
                            var texID = ushort.Parse(split[2]);
                            var tex = ActiveObject.Resource.Get<MTEX>(texID);
                            if (tex == null)
                            {
                                tex = new MTEX();
                                tex.ChunkLabel = "OBJ Import Texture";
                                tex.ChunkID = texID;
                                tex.ChunkProcessed = true;
                                tex.ChunkType = "MTEX";
                                tex.AddedByPatch = true;
                                (ActiveObject.Resource.Sprites ?? ActiveObject.Resource.MainIff).AddChunk(tex);
                            }

                            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                            {
                                tex.SetData(File.ReadAllBytes(Path.Combine(baseDir, copyname)));
                            }, tex));
                        }
                        else
                        {
                            var texname = ActiveDGRP.ChunkParent.Filename.Replace('.', '_').Replace("spf", "iff") + "_";
                            texname += copyname;

                            File.Copy(Path.Combine(baseDir, copyname), Path.Combine(FSOEnvironment.ContentDir, "MeshReplace/", texname), true);
                        }
                    }

                    GameThread.NextUpdate(x =>
                    {
                        var mesh = new DGRP3DMesh(ActiveDGRP, obj, Client.GameFacade.GraphicsDevice);
                        if (IffMode)
                        {
                            var fsom = ActiveObject.Resource.Get<FSOM>(ActiveDGRP.ChunkID);
                            if (fsom == null)
                            {
                                fsom = new FSOM();
                                fsom.ChunkLabel = "OBJ Import Mesh";
                                fsom.ChunkID = ActiveDGRP.ChunkID;
                                fsom.ChunkProcessed = true;
                                fsom.ChunkType = "FSOM";
                                fsom.AddedByPatch = true;
                                (ActiveObject.Resource.Sprites ?? ActiveObject.Resource.MainIff).AddChunk(fsom);
                            }
                            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
                            {
                                fsom.SetMesh(mesh);
                                Content.Content.Get().RCMeshes.ClearCache(ActiveDGRP);
                                Debug3D.ForceUpdate();
                            }, fsom));
                        }
                        else
                        {
                            Content.Content.Get().RCMeshes.Replace(ActiveDGRP, mesh);
                            Debug3D.ForceUpdate();
                        }
                    });

                    str.Close();
                }
            } catch (Exception)
            {

            }
        }

        private void SetCustomMode(bool mode)
        {
            if (mode)
            {
                CustomBox.Show();
                ReconBox.Hide();
            } else
            {
                CustomBox.Hide();
                ReconBox.Show();
            }
        }

        private void ToCustom_Click(object sender, EventArgs e)
        {
            SetCustomMode(true);
        }

        private void ToRecon_Click(object sender, EventArgs e)
        {
            SetCustomMode(false);
        }

        private bool InternalChange;
        private void UpdateFSOR(object sender, EventArgs e)
        {
            if (InternalChange) return;

            Content.Content.Get().Changes.QueueResMod(new ResAction(() =>
            {
                UpdateParams();
                if (!IffMode)
                {
                    var lower = ActiveObject.OBJ.ChunkParent.Filename.ToLowerInvariant();
                    DGRP3DMesh.ParamsByIff[lower] = ActiveParams;
                }
                Content.Content.Get().RCMeshes.ClearCache(ActiveDGRP);
                Debug3D.ForceUpdate();
            }, ActiveFSOR));
        }

        private void UpdateParams()
        {
            ActiveParams.Simplify = SimpleCheck.Checked;
            ActiveParams.DoorFix = DoorCheck.Checked;
            ActiveParams.CounterFix = CounterCheck.Checked;
            ActiveParams.BlenderTweak = BlenderCheck.Checked;
            ActiveParams.Rotations[0] = Rot1.Checked;
            ActiveParams.Rotations[1] = Rot2.Checked;
            ActiveParams.Rotations[2] = Rot3.Checked;
            ActiveParams.Rotations[3] = Rot4.Checked;
        }

        private void UpdateUseIFF(object sender, EventArgs e)
        {
            if (InternalChange) return;

            IffMode = IffCheck.Checked;

            if (IffMode)
            {
                //try create an FSOR descriptor
                ActiveFSOR = ActiveObject.Resource.List<FSOR>()?.FirstOrDefault();
                if (ActiveFSOR == null)
                {
                    ActiveFSOR = new FSOR();
                    ActiveFSOR.ChunkLabel = "3D Reconstruction info";
                    ActiveFSOR.ChunkID = 1;
                    ActiveFSOR.ChunkProcessed = true;
                    ActiveFSOR.ChunkType = "FSOR";
                    ActiveFSOR.AddedByPatch = true;
                    ActiveObject.Resource.MainIff.AddChunk(ActiveFSOR);
                }
                ActiveParams = ActiveFSOR.Params;

            }
        }

        private void UpdateChecks()
        {
            InternalChange = true;
            SimpleCheck.Checked = ActiveParams.Simplify;
            DoorCheck.Checked = ActiveParams.DoorFix;
            CounterCheck.Checked = ActiveParams.CounterFix;
            BlenderCheck.Checked = ActiveParams.BlenderTweak;
            Rot1.Checked = ActiveParams.Rotations[0];
            Rot2.Checked = ActiveParams.Rotations[1];
            Rot3.Checked = ActiveParams.Rotations[2];
            Rot4.Checked = ActiveParams.Rotations[3];
            InternalChange = false;
        }
    }
}
