using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.IFF;
using System.IO;

namespace FSO.IDE.ResourceBrowser
{
    public partial class DGRPEditor : UserControl
    {
        public GameObject ActiveObject;
        public GameIffResource ActiveIff;
        public DGRP ActiveDGRP;
        public DGRPImage[] ActiveDGRPImages;
        public DGRPSprite[] ActiveDGRPSprites;

        private DGRP OldDGRP;
        private Dictionary<ListViewItem, ushort> DGRPChunkIDs;
        private bool InternalChange;

        public DGRPEditor()
        {
            InitializeComponent();
        }

        public void SetActiveObject(GameObject active)
        {
            ActiveObject = active;
            ActiveIff = active.Resource;
            ActiveDGRP = null;

            SelectSpriteBox.Enabled = false;
            groupBox3.Enabled = false;

            int sprID = active.OBJ.DynamicSpriteBaseId;
            var spr2 = ActiveIff.Get<SPR2>((ushort)sprID);

            if (spr2 != null) FirstDynLabel.Text = spr2.ChunkLabel;
            else if (sprID == 0) FirstDynLabel.Text = "None Selected";
            else FirstDynLabel.Text = "Unknown SPR2#" + sprID;

            sprID = active.OBJ.DynamicSpriteBaseId+active.OBJ.NumDynamicSprites-1;
            if (active.OBJ.NumDynamicSprites == 0) sprID = 0;
            spr2 = ActiveIff.Get<SPR2>((ushort)sprID);

            if (spr2 != null) LastDynLabel.Text = spr2.ChunkLabel;
            else if (sprID < 1) LastDynLabel.Text = "None Selected";
            else LastDynLabel.Text = "Unknown SPR2#" + sprID;

            DGRPEdit.ShowObject(active.OBJ.GUID);

            UpdateDGRPList(true);
            var allowEdit = (ActiveObject.OBJ.ObjectType != OBJDType.Person);
            groupBox1.Enabled = allowEdit;
            DGRPBox.Enabled = allowEdit;
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
                var relativeIndex = dgrp.ChunkID-ActiveObject.OBJ.BaseGraphicID;
                bool notUsed = relativeIndex < 0 || relativeIndex >= ActiveObject.OBJ.NumGraphics;

                var listItem = new ListViewItem(new string[] { (notUsed) ? ("("+relativeIndex+")") : relativeIndex.ToString(), dgrp.ChunkLabel });

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

        private void UpdateImage()
        {
            var oldIndex = SpriteList.SelectedIndex;
            DGRPEdit.ChangeWorld((int)RotationTrack.Value, (int)ZoomTrack.Value);
            DGRPEdit.ForceUpdate();
            if (ActiveDGRP == null)
            {
                SelectSpriteBox.Enabled = false;
                groupBox3.Enabled = false;
                return;
            }
            else
            {
                //todo: verify that images can be zoom synced

                if (AutoRot.Checked)
                {
                    var dgrps = new List<DGRPImage>();
                    for (int i=0; i<4; i++)
                    {
                        dgrps.Add(ActiveDGRP.GetImage(0x10, 3, (uint)i));
                        dgrps.Add(ActiveDGRP.GetImage(0x10, 2, (uint)i));
                        dgrps.Add(ActiveDGRP.GetImage(0x10, 1, (uint)i));
                    }
                    ActiveDGRPImages = dgrps.ToArray();
                }
                else if (AutoZoom.Checked)
                {
                    ActiveDGRPImages = new DGRPImage[] {
                        ActiveDGRP.GetImage(0x10, 3, (uint)RotationTrack.Value),
                        ActiveDGRP.GetImage(0x10, 2, (uint)RotationTrack.Value),
                        ActiveDGRP.GetImage(0x10, 1, (uint)RotationTrack.Value)
                    };
                }
                else
                {
                    ActiveDGRPImages = new DGRPImage[] {
                        ActiveDGRP.GetImage(0x10, (uint)(3 - ZoomTrack.Value), (uint)RotationTrack.Value)
                    };
                }

                //populate sprite list

                SelectSpriteBox.Enabled = true;
                groupBox3.Enabled = true;

                SpriteList.Items.Clear();
                foreach (var frame in ActiveDGRPImages[0].Sprites)
                {
                    //attempt to get spr
                    var sprID = (ushort)frame.SpriteID;
                    var baseID = ActiveObject.OBJ.DynamicSpriteBaseId;
                    var isDyn = (sprID >= baseID && sprID < baseID + ActiveObject.OBJ.NumDynamicSprites);

                    var spr = ActiveIff.Get<SPR2>(sprID);
                    var dyn = (isDyn) ? ("(^" + (sprID - baseID) + ") "):"";
                    var name = (spr != null) ? spr.ChunkLabel : ("SPR#" + frame.SpriteID);
                    SpriteList.Items.Add(dyn+name+" (" + frame.SpriteOffset.X + "," + frame.SpriteOffset.Y+")");
                }
                if (SpriteList.Items.Count == 0)
                    SelectSpriteBox.Enabled = false;
                else
                {
                    SelectSpriteBox.Enabled = true;
                    if (OldDGRP == ActiveDGRP)
                        SpriteList.SelectedIndex = Math.Max(0, Math.Min(SpriteList.Items.Count - 1, oldIndex));
                    else
                        SpriteList.SelectedIndex = 0;
                }
                OldDGRP = ActiveDGRP;
            }          
        }

        public void UpdateSprite()
        {
            if (SpriteList.SelectedIndex == -1 || ActiveDGRPImages == null) return;

            InternalChange = true;
            ActiveDGRPSprites = new DGRPSprite[ActiveDGRPImages.Length];

            for (int i = 0; i < ActiveDGRPSprites.Length; i++)
            {
                ActiveDGRPSprites[i] = ActiveDGRPImages[i].Sprites[SpriteList.SelectedIndex];
            }

            //if dynamic, set the dynamic sprite flags to 0
            var sprID = (ushort)ActiveDGRPSprites[0].SpriteID;
            var baseID = ActiveObject.OBJ.DynamicSpriteBaseId;
            if (sprID >= baseID && sprID < baseID + ActiveObject.OBJ.NumDynamicSprites)
            {
                DGRPEdit.SetDynamic(sprID-baseID);
            }

            xPx.Value = (int)ActiveDGRPSprites[0].SpriteOffset.X;
            yPx.Value = (int)ActiveDGRPSprites[0].SpriteOffset.Y;

            xPhys.Text = ActiveDGRPSprites[0].ObjectOffset.X.ToString();
            yPhys.Text = ActiveDGRPSprites[0].ObjectOffset.Y.ToString();
            zPhys.Text = ActiveDGRPSprites[0].ObjectOffset.Z.ToString();

            RotationCombo.Items.Clear();

            var spr2 = ActiveIff.Get<SPR2>(sprID);

            if (spr2 != null)
            {
                for (int i = 0; i < spr2.Frames.Length / 3; i++)
                    RotationCombo.Items.Add("Rotation " + i);
                RotationCombo.SelectedIndex = (int)(ActiveDGRPSprites[0].SpriteFrameIndex % (spr2.Frames.Length / 3));

                SPRLabel.Text = spr2.ChunkLabel;
            } else
            {
                SPRLabel.Text = "Unknown SPR2#"+ActiveDGRPSprites[0].SpriteID;
            }

            FlipCheck.Checked = ActiveDGRPSprites[0].Flip;

            InternalChange = false;
        }

        private void DGRPList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DGRPList.SelectedIndices.Count == 0 || !DGRPChunkIDs.ContainsKey(DGRPList.SelectedItems[0])) {
                return;
            }
            ActiveDGRP = ActiveIff.Get<DGRP>(DGRPChunkIDs[DGRPList.SelectedItems[0]]);
            
            if (ActiveDGRP != null)
            {
                //change selected graphic
                DGRPEdit.ChangeGraphic(ActiveDGRP.ChunkID);
                UpdateImage();
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void ZoomTrack_Scroll(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void SpriteList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSprite();
        }

        private void ChangeSPR_Click(object sender, EventArgs e)
        {
            if (ActiveDGRPSprites == null) return;
            var sprSel = new SPR2SelectorDialog(ActiveIff, ActiveObject);
            sprSel.ShowDialog();
            if (sprSel.DialogResult == DialogResult.OK)
            {
                var id = sprSel.ChosenID;
                var zoom = (int)(3-ActiveDGRPImages[0].Zoom);
                var rot = (!AutoRot.Checked)?0:((ActiveDGRPImages[0].Direction + 2) % 4);
                string name = "";
                float zFactor = 1.0f;

                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    foreach (var dgrpSpr in ActiveDGRPSprites)
                    {
                        dgrpSpr.SpriteID = id;
                        //default back to rotation 0
                        var spr = ActiveIff.Get<SPR2>(id);
                        if (spr == null) continue;
                        name = spr.ChunkLabel;
                        dgrpSpr.SpriteFrameIndex = (uint)((spr == null) ? 0 : (zoom * spr.Frames.Length / 3) + rot);
                        AutoOffset(dgrpSpr, spr, zFactor);

                        zoom++;
                        zFactor /= 2;
                        if (zoom > 2)
                        {
                            zoom = (int)(3 - ActiveDGRPImages[0].Zoom);
                            zFactor = 1.0f;
                            rot = (rot + 1) % 4;
                        }
                    }
                }, ActiveDGRP));

                UpdateImage();
            }
        }

        private void AutoOffset(DGRPSprite dgrpSpr, SPR2 spr, float zFactor)
        {
            dgrpSpr.Flip = false;
            var frameN = dgrpSpr.SpriteFrameIndex;

            //generate a default offset using the cage setup in the SPR2.
            //68 is half width of cage
            //348 is distance from baseline to top of cage;
            var frame = spr.Frames[frameN];
            dgrpSpr.SpriteOffset = new Microsoft.Xna.Framework.Vector2(
                (int)((-68 * zFactor) + frame.Position.X),
                (-348 * zFactor) + frame.Height + frame.Position.Y
                );
        }

        private void xPhys_TextChanged(object sender, EventArgs e)
        {
            if (ActiveDGRPSprites == null) return;
            float value = 0;
            float.TryParse(xPhys.Text, out value);
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                foreach (var dgrpSpr in ActiveDGRPSprites) dgrpSpr.ObjectOffset.X = value;
            }, ActiveDGRP));
            DGRPEdit.ForceUpdate();
        }

        private void yPhys_TextChanged(object sender, EventArgs e)
        {
            if (ActiveDGRPSprites == null || InternalChange) return;
            float value = 0;
            float.TryParse(yPhys.Text, out value);
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                foreach (var dgrpSpr in ActiveDGRPSprites) dgrpSpr.ObjectOffset.Y = value;
            }, ActiveDGRP));
            DGRPEdit.ForceUpdate();
        }

        private void zPhys_TextChanged(object sender, EventArgs e)
        {
            if (ActiveDGRPSprites == null || InternalChange) return;
            float value = 0;
            float.TryParse(zPhys.Text, out value);
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                foreach (var dgrpSpr in ActiveDGRPSprites) dgrpSpr.ObjectOffset.Z = value;
            }, ActiveDGRP));
            DGRPEdit.ForceUpdate();
        }

        private void xPx_ValueChanged(object sender, EventArgs e)
        {
            if (ActiveDGRPSprites == null || InternalChange) return;
            int value = (int)xPx.Value;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                float zoom = 1f;
                foreach (var dgrpSpr in ActiveDGRPSprites) {
                    dgrpSpr.SpriteOffset.X = (int)(value * zoom);
                    zoom /= 2;
                }
            }, ActiveDGRP));
            UpdateImage();
        }

        private void yPx_ValueChanged(object sender, EventArgs e)
        {
            if (ActiveDGRPSprites == null || InternalChange) return;
            int value = (int)yPx.Value;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                float zoom = 1f;
                foreach (var dgrpSpr in ActiveDGRPSprites)
                {
                    dgrpSpr.SpriteOffset.Y = (int)(value * zoom);
                    zoom /= 2;
                }
            }, ActiveDGRP));
            UpdateImage();
        }

        private void RotationCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ActiveDGRPSprites == null || RotationCombo.SelectedIndex == -1 || InternalChange) return;
            var rot = RotationCombo.SelectedIndex;
            float zFactor = 1f;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var spr = ActiveIff.Get<SPR2>((ushort)ActiveDGRPSprites[0].SpriteID);
                int numRot = 0;
                if (spr != null)
                    numRot = spr.Frames.Length / 3;
                int frame = rot;
                foreach (var dgrpSpr in ActiveDGRPSprites) {
                    dgrpSpr.SpriteFrameIndex = (uint)frame;
                    AutoOffset(dgrpSpr, spr, zFactor);
                    zFactor /= 2;
                    frame += numRot;
                }
                zFactor /= 2;
            }, ActiveDGRP));
            UpdateImage();
        }

        private void FlipCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (ActiveDGRPSprites == null || InternalChange) return;
            var flip = FlipCheck.Checked;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                float zFactor = 1f;
                foreach (var dgrpSpr in ActiveDGRPSprites)
                {
                    dgrpSpr.Flip = flip;

                    //flip the x offset to flip the sprite in its cage
                    var spr = ActiveIff.Get<SPR2>((ushort)dgrpSpr.SpriteID);
                    if (spr == null) continue;
                    var frame = spr.Frames[dgrpSpr.SpriteFrameIndex];
                    dgrpSpr.SpriteOffset.X = (-dgrpSpr.SpriteOffset.X)-frame.Width;

                    new Microsoft.Xna.Framework.Vector2(
                        (int)((-68 * zFactor) + frame.Position.X),
                        (-348) + frame.Height + frame.Position.Y
                        );
                    zFactor /= 2;
                }
            }, ActiveDGRP));
            UpdateImage();
        }

        private void AddSPR_Click(object sender, EventArgs e)
        {
            if (ActiveDGRPImages == null) return;
            var ind = SpriteList.SelectedIndex;
            if (ind == -1) ind = SpriteList.Items.Count - 1;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                foreach (var dgrpSpr in ActiveDGRPImages)
                {
                    var old = dgrpSpr.Sprites;
                    dgrpSpr.Sprites = new DGRPSprite[old.Length + 1];
                    Array.Copy(old, 0, dgrpSpr.Sprites, 0, ind+1);
                    dgrpSpr.Sprites[ind+1] = new DGRPSprite(ActiveDGRP);
                    if (ind != old.Length - 1) Array.Copy(old, ind+1, dgrpSpr.Sprites, ind+2, (old.Length - ind)-1);
                }
            }, ActiveDGRP));

            UpdateImage();
            if (SpriteList.SelectedIndex < SpriteList.Items.Count-1) SpriteList.SelectedIndex++;
        }

        private void RemoveSPR_Click(object sender, EventArgs e)
        {
            if (ActiveDGRPImages == null) return;
            var ind = SpriteList.SelectedIndex;
            if (ind == -1) ind = SpriteList.Items.Count - 1;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                foreach (var dgrpSpr in ActiveDGRPImages)
                {
                    var old = dgrpSpr.Sprites;
                    dgrpSpr.Sprites = new DGRPSprite[old.Length - 1];
                    if (ind != 0) Array.Copy(old, 0, dgrpSpr.Sprites, 0, ind);
                    if (ind != old.Length - 1) Array.Copy(old, ind+1, dgrpSpr.Sprites, ind, (old.Length-ind)-1);
                }
            }, ActiveDGRP));

            InternalChange = true;
            if (SpriteList.SelectedIndex > 0) SpriteList.SelectedIndex--;
            InternalChange = false;
            UpdateImage();
        }

        private void MoveUpSPR_Click(object sender, EventArgs e)
        {
            if (ActiveDGRPImages == null || SpriteList.SelectedIndex < 1) return;
            var ind = SpriteList.SelectedIndex;
            if (ind == -1) ind = SpriteList.Items.Count - 1;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                foreach (var dgrpSpr in ActiveDGRPImages)
                {
                    var temp = dgrpSpr.Sprites[ind-1];
                    dgrpSpr.Sprites[ind - 1] = dgrpSpr.Sprites[ind];
                    dgrpSpr.Sprites[ind] = temp;
                }
            }, ActiveDGRP));

            InternalChange = true;
            SpriteList.SelectedIndex--;
            InternalChange = false;
            UpdateImage();
        }

        private void MoveDownSPR_Click(object sender, EventArgs e)
        {
            if (ActiveDGRPImages == null || SpriteList.SelectedIndex == SpriteList.Items.Count-1) return;
            var ind = SpriteList.SelectedIndex;
            if (ind == -1) ind = SpriteList.Items.Count - 1;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                foreach (var dgrpSpr in ActiveDGRPImages)
                {
                    var temp = dgrpSpr.Sprites[ind + 1];
                    dgrpSpr.Sprites[ind + 1] = dgrpSpr.Sprites[ind];
                    dgrpSpr.Sprites[ind] = temp;
                }
            }, ActiveDGRP));

            InternalChange = true;
            SpriteList.SelectedIndex++;
            InternalChange = false;
            UpdateImage();
        }

        private void AutoZoom_CheckedChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void FirstDGRP_Click(object sender, EventArgs e)
        {
            if (ActiveDGRP == null) return;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveObject.OBJ.BaseGraphicID = ActiveDGRP.ChunkID;
            }, ActiveObject.OBJ));
            SetActiveObject(ActiveObject);
        }

        private void LastDGRP_Click(object sender, EventArgs e)
        {
            if (ActiveDGRP == null) return;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveObject.OBJ.NumGraphics = (ushort)((ActiveDGRP.ChunkID - ActiveObject.OBJ.BaseGraphicID) + 1);
            }, ActiveObject.OBJ));
            SetActiveObject(ActiveObject);
        }

        private void FirstDynButton_Click(object sender, EventArgs e)
        {
            var sprSel = new SPR2SelectorDialog(ActiveIff, ActiveObject);
            sprSel.ShowDialog();
            var id = sprSel.ChosenID;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveObject.OBJ.DynamicSpriteBaseId = id;
            }, ActiveObject.OBJ));
            SetActiveObject(ActiveObject);
        }

        private void LastDynButton_Click(object sender, EventArgs e)
        {
            var sprSel = new SPR2SelectorDialog(ActiveIff, ActiveObject);
            sprSel.ShowDialog();

            var id = sprSel.ChosenID;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveObject.OBJ.NumDynamicSprites = (ushort)Math.Max(0, (id - ActiveObject.OBJ.DynamicSpriteBaseId) + 1);
            }, ActiveObject.OBJ));
            SetActiveObject(ActiveObject);
        }

        private void DGRPUp_Click(object sender, EventArgs e)
        {
            if (ActiveDGRP == null) return;
            var shifted = true;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var chunks = ActiveDGRP.ChunkParent.List<DGRP>().OrderBy(x => x.ChunkID).ToList();
                var myIndex = chunks.IndexOf(ActiveDGRP);

                //find closest dgrp above us. 
                ushort targID = (ushort)Math.Max(1, ActiveDGRP.ChunkID - 1);
                if (myIndex != -1 && myIndex != 0)
                {
                    var above = chunks[Math.Max(0, myIndex - 1)];
                    targID = above.ChunkID;
                    if (ActiveDGRP.ChunkID - targID > 1)
                    {
                        targID++; //place in space instead of swap
                        shifted = false;
                    }
                }
                var targ = ActiveDGRP.ChunkParent.Get<DGRP>(targID);
                if (targ != null) Content.Content.Get().Changes.ChunkChanged(targ);

                ActiveDGRP.ChunkParent.MoveAndSwap(ActiveDGRP, targID);
            }, ActiveDGRP));
            UpdateDGRPList(false);
            if (shifted) ShiftDGRP(-1);
        }

        private void DGRPDown_Click(object sender, EventArgs e)
        {
            if (ActiveDGRP == null) return;
            var shifted = true;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var chunks = ActiveDGRP.ChunkParent.List<DGRP>().OrderBy(x => x.ChunkID).ToList();
                var myIndex = chunks.IndexOf(ActiveDGRP);
                //find closest dgrp below us. 
                ushort targID = (ushort)Math.Min(65535, (int)ActiveDGRP.ChunkID + 1);
                if (myIndex != 1 && myIndex != chunks.Count-1)
                {
                    var below = chunks[Math.Min(chunks.Count-1, myIndex + 1)];
                    targID = below.ChunkID;
                    if (ActiveDGRP.ChunkID - targID < -1)
                    {
                        targID--; //place in space instead of swap
                        shifted = false;
                    }
                }

                var targ = ActiveDGRP.ChunkParent.Get<DGRP>(targID);
                if (targ != null) Content.Content.Get().Changes.ChunkChanged(targ);
                ActiveDGRP.ChunkParent.MoveAndSwap(ActiveDGRP, targID);
            }, ActiveDGRP));
            UpdateDGRPList(false);
            if (shifted) ShiftDGRP(1);
        }

        private void ShiftDGRP(int i)
        {
            if (DGRPList.SelectedIndices.Count == 0) return;
            int newInd = Math.Min(DGRPList.Items.Count - 1, Math.Max(0, DGRPList.SelectedIndices[0] + i));
            DGRPList.Items[newInd].Selected = true;
            DGRPList.Items[newInd].Focused = true;
        }

        private void AddNewDGRP(DGRP newDGRP)
        {
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                //first find a Drawgroup Group ( ;) ) to try add the sprite to.
                //if we have no base graphic or this fails, make a new group.
                //these are indexed by 100.
                ushort chosenID = 0;

                var dgrps = (ActiveDGRP == null) ? null : ActiveDGRP.ChunkParent.List<DGRP>();
                dgrps = (dgrps != null) ? dgrps.OrderBy(x => x.ChunkID).ToList() : new List<DGRP>();
                if (ActiveObject.OBJ.BaseGraphicID != 0)
                {
                    ushort useGroup = (ushort)((ActiveObject.OBJ.BaseGraphicID / 100) * 100);
                    chosenID = useGroup;
                    //find a space in group
                    foreach (var dgrp in dgrps)
                    {
                        if ((dgrp.ChunkID / 100) * 100 == useGroup)
                        {
                            if (dgrp.ChunkID == chosenID) chosenID++;
                        }
                    }
                }
                else
                {
                    //find an empty group
                    chosenID = 100;
                    foreach (var dgrp in dgrps)
                    {
                        if ((dgrp.ChunkID / 100) * 100 == chosenID) chosenID += 100;
                    }

                }
                newDGRP.ChunkID = chosenID;

                var iff = ActiveIff.MainIff;
                if (ActiveIff is GameObjectResource && ((GameObjectResource)ActiveIff).Sprites != null)
                    iff = ((GameObjectResource)ActiveIff).Sprites;

                iff.AddChunk(newDGRP);
                Content.Content.Get().Changes.IffChanged(iff);
            }));
        }

        private void AddDGRP_Click(object sender, EventArgs e)
        {
            var newDGRP = new DGRP()
            {
                ChunkLabel = "New Graphic",
                AddedByPatch = true,
                ChunkProcessed = true,
                ChunkType = "DGRP",
                RuntimeInfo = ChunkRuntimeState.Modified
            };

            newDGRP.Images = new DGRPImage[12];

            var i = 0;
            for (int r = 0; r < 4; r++)
            {
                for (uint z = 1; z < 4; z++)
                {
                    newDGRP.Images[i++] = new DGRPImage(newDGRP) { Sprites = new DGRPSprite[0], Direction = (uint)(1 << (r * 2)), Zoom = z };
                }
            }

            AddNewDGRP(newDGRP);
            UpdateDGRPList(false);
        }

        private void RemoveDGRP_Click(object sender, EventArgs e)
        {
            if (ActiveDGRP == null) return;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                ActiveDGRP.ChunkParent.FullRemoveChunk(ActiveDGRP);
                Content.Content.Get().Changes.ChunkChanged(ActiveDGRP);
            }));
            UpdateDGRPList(true);
        }

        private void RenameDGRP_Click(object sender, EventArgs e)
        {
            if (ActiveDGRP == null) return;
            var dialog = new IffNameDialog(ActiveDGRP, false);
            dialog.ShowDialog();
            UpdateDGRPList(false);
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (ActiveDGRP == null) return;
            var data = new MemoryStream();
            lock (ActiveDGRP)
                ActiveDGRP.Write(ActiveDGRP.ChunkParent, data);

            var newDGRP = new DGRP()
            {
                ChunkLabel = "New Graphic",
                AddedByPatch = true,
                ChunkData = data.ToArray(),
                ChunkProcessed = false,
                ChunkType = "DGRP",
                RuntimeInfo = ChunkRuntimeState.Modified
            };
            data.Close();

            AddNewDGRP(newDGRP);
            UpdateDGRPList(false);
        }

        private void AutoRot_CheckedChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }
    }
}
