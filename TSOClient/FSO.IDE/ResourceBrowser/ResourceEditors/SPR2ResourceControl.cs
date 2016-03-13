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
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class SPR2ResourceControl : UserControl, IResourceControl
    {
        public SPR2 GraphicChunk;
        public GameIffResource ActiveIff;

        public Image[][] Graphics;

        public SPR2ResourceControl()
        {
            InitializeComponent();
            ModeCombo.SelectedIndex = 0;
        }

        public void SetActiveObject(GameObject obj)
        {
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            GraphicChunk = (SPR2)chunk;
            ActiveIff = res;

            FrameList.Items.Clear();
            for (int i=0; i< GraphicChunk.Frames.Length / 3; i++)
            {
                FrameList.Items.Add("Rotation " + i);
            }

            bool hasFrames = (FrameList.Items.Count > 0);
            Graphics = null;
            if (hasFrames) FrameList.SelectedIndex = 0;
            else
            {
                SPRBox1.Image = null;
                SPRBox2.Image = null;
                SPRBox3.Image = null;
            }
            DeleteButton.Enabled = hasFrames;
            ImportButton.Enabled = hasFrames;
            ExportButton.Enabled = hasFrames;
            ImportAll.Enabled = hasFrames;
            ExportAll.Enabled = hasFrames;
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
            SPRSelector.SetSelectors(null, GraphicChunk, selectors);
        }

        public void UpdateGraphics()
        {
            int index = FrameList.SelectedIndex;
            Graphics = new Image[3][];

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                Graphics[0] = GraphicChunk.Frames[index].GetPixelAlpha(136, 384);
                Graphics[1] = GraphicChunk.Frames[index+GraphicChunk.Frames.Length/3].GetPixelAlpha(68, 192);
                Graphics[2] = GraphicChunk.Frames[index+(GraphicChunk.Frames.Length / 3) * 2].GetPixelAlpha(34, 96);
            }, GraphicChunk, false));

            SetDisplay();
        }

        public void SetDisplay()
        {
            int mode = ModeCombo.SelectedIndex;
            if (mode == -1 || Graphics == null) return;
            SPRBox1.Image = Graphics[0][mode];
            SPRBox2.Image = Graphics[1][mode];
            SPRBox3.Image = Graphics[2][mode];
        }

        public void ExportSPR(int index, string path)
        {
            var gfx = new Image[3][];

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                gfx[0] = GraphicChunk.Frames[index].GetPixelAlpha(136, 384);
                gfx[1] = GraphicChunk.Frames[index + GraphicChunk.Frames.Length / 3].GetPixelAlpha(68, 192);
                gfx[2] = GraphicChunk.Frames[index + (GraphicChunk.Frames.Length / 3) * 2].GetPixelAlpha(34, 96);
            }, GraphicChunk, false));

            var iffname = ActiveIff.MainIff.Filename;
            var extension = iffname.LastIndexOf('.');
            if (extension != -1) iffname = iffname.Substring(0, extension);

            var baseName = Path.Combine(path, iffname + "_spr" + GraphicChunk.ChunkID + "_r" + index);

            gfx[0][0].Save(baseName + "_near" + "_color.bmp", ImageFormat.Bmp);
            gfx[0][1].Save(baseName + "_near" + "_alpha.bmp", ImageFormat.Bmp);
            gfx[0][2].Save(baseName + "_near" + "_depth.bmp", ImageFormat.Bmp);
            if (!AutoZooms.Checked)
            {
                gfx[1][0].Save(baseName + "_med" + "_color.bmp", ImageFormat.Bmp);
                gfx[1][1].Save(baseName + "_med" + "_alpha.bmp", ImageFormat.Bmp);
                gfx[1][2].Save(baseName + "_med" + "_depth.bmp", ImageFormat.Bmp);
                gfx[2][0].Save(baseName + "_far" + "_color.bmp", ImageFormat.Bmp);
                gfx[2][1].Save(baseName + "_far" + "_alpha.bmp", ImageFormat.Bmp);
                gfx[2][2].Save(baseName + "_far" + "_depth.bmp", ImageFormat.Bmp);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetDisplay();
        }

        private void FrameList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateGraphics();
            SetDisplay();
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.Description = "Choose a folder to export this Sprite Rotation to.";
            FolderBrowse(dialog);

            if (dialog.SelectedPath == "") return;
            var path = dialog.SelectedPath;
            ExportSPR(FrameList.SelectedIndex, path);
        }

        private void ExportAll_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.Description = "Choose a folder to export all Sprite Rotations to.";
            FolderBrowse(dialog);

            if (dialog.SelectedPath == "") return;
            var path = dialog.SelectedPath;

            for (int i = 0; i < FrameList.Items.Count; i++)
                ExportSPR(i, path);
        }

        private void FolderBrowse(FolderBrowserDialog dialog)
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

        private void AutoZooms_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void ImportAll_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Choose a folder to export all Sprite Rotations from.";
            FolderBrowse(dialog);

            if (dialog.SelectedPath == "") return;
            ImportSprites(dialog.SelectedPath, -1);
        }

        private void ImportSprites(string path, int targrot)
        {
            var files = Directory.GetFiles(path);

            // look for files with... the pattern
            // iffname_sprid_rotation_zoom_channel.bmp
            //
            // where rotation is in format r0, r1, r2..
            // zoom is near, med, far, all
            // channel is color, alpha, depth
            //
            // replace all existing rotations and make new rotations if needed

            var ignore = new HashSet<string>();

            foreach (var item in files)
            {
                var split = Path.GetFileName(item).Split('_');
                if (split.Length != 5) continue; //invalid
                if (ignore.Contains(split[0] + "_" + split[1] + "_" + split[2] + "_" + split[3])) continue;
                try
                {
                    var rot = int.Parse(split[2].Substring(1));
                    if (targrot != -1 && targrot != rot) continue;
                    var zoom = split[3];
                    var mode = split[4];

                    var total = GraphicChunk.Frames.Length / 3;
                    if (rot >= total)
                    {
                        NewRotation((rot - total) + 1);
                    }

                    var id = split[0] + "_" + split[1] + "_" + split[2] + "_" + split[3];
                    ignore.Add(id);
                    int zoomOff = 0;
                    switch (zoom)
                    {
                        case "near": zoomOff = 0; break;
                        case "med": zoomOff = 1; break;
                        case "far": zoomOff = 2; break;
                    }

                    if (AutoZooms.Checked && zoomOff > 0) continue;

                    //attempt to load all parts. if one of these fails an exception will be thrown!
                    //TODO: support no depth? no alpha? ideally people shouldnt be doing those...
                    var basePath = Path.Combine(Path.GetDirectoryName(item), id);

                    using (
                        Bitmap pixel = new Bitmap(basePath + "_color.bmp"),
                        alpha = new Bitmap(basePath + "_alpha.bmp"),
                        depth = new Bitmap(basePath + "_depth.bmp")
                        )
                    {
                        ReplaceSprite(new Bitmap[] { pixel, alpha, depth }, rot + zoomOff * (GraphicChunk.Frames.Length / 3));

                        if (AutoZooms.Checked)
                        {
                            //generate med and far zooms
                            ReplaceSprite(new Bitmap[] {
                                SmartDownscale(pixel, 2, Color.FromArgb(255, 255, 255, 0)),
                                SmartDownscale(alpha, 2, Color.FromArgb(0,0,0,255)),
                                SmartDownscale(depth, 2, Color.White) }, 
                                rot + 1 * (GraphicChunk.Frames.Length / 3));

                            ReplaceSprite(new Bitmap[] {
                                SmartDownscale(pixel, 4, Color.FromArgb(255, 255, 255, 0)),
                                SmartDownscale(alpha, 4, Color.FromArgb(0,0,0,255)),
                                SmartDownscale(depth, 4, Color.White) },
                                rot + 2 * (GraphicChunk.Frames.Length / 3));
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            var stream = new MemoryStream();
            GraphicChunk.Write(GraphicChunk.ChunkParent, stream);
            GraphicChunk.ChunkData = stream.ToArray();
            GraphicChunk.ChunkProcessed = false;
            GraphicChunk.Dispose();
            GraphicChunk.ChunkParent.Get<SPR2>(GraphicChunk.ChunkID);
            UpdateGraphics();
        }

        private void ReplaceSprite(Bitmap[] bmps, int frame)
        {
            var pixel = bmps[0];
            //first search pixel for the bounding rectangle.
            var plock = pixel.LockBits(new Rectangle(0, 0, pixel.Width, pixel.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pdat = new byte[plock.Stride * plock.Height];

            Marshal.Copy(plock.Scan0, pdat, 0, pdat.Length);
            int maxX = int.MinValue, maxY = int.MinValue,
                minX = int.MaxValue, minY = int.MaxValue;

            int index = 0;
            for (int y = 0; y < pixel.Height; y++)
            {
                for (int x = 0; x < pixel.Width; x++)
                {
                    if (!(pdat[index] == 0 && pdat[index+1] == 255 && pdat[index+2] == 255))
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                    index += 4;
                }
            }

            pixel.UnlockBits(plock);

            var rect = (minX == int.MaxValue) ? new Rectangle() : new Rectangle(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
            var px = rect.Width * rect.Height;

            var locks = new BitmapData[3];
            var data = new byte[3][];
            var pxOut = new Microsoft.Xna.Framework.Color[px];
            var depthOut = new byte[px];

            for (int i=0; i<3; i++)
            {
                locks[i] = bmps[i].LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                data[i] = new byte[locks[i].Stride * locks[i].Height];
                Marshal.Copy(locks[i].Scan0, data[i], 0, data[i].Length);
            }

            int scanStart = 0;
            int dstidx = 0;
            for (int y=0; y<locks[0].Height; y++)
            {
                int srcidx = scanStart;
                for (int j = 0; j < rect.Width; j++)
                {
                    pxOut[dstidx] = new Microsoft.Xna.Framework.Color(data[0][srcidx + 2], data[0][srcidx + 1], data[0][srcidx], data[1][srcidx]);
                    depthOut[dstidx] = data[2][srcidx];
                    srcidx += 4;
                    dstidx++;
                }
                scanStart += locks[0].Stride;
            }


            //set data first. we also want to get back the a palette we can change
            PALT targ = null;
            Microsoft.Xna.Framework.Color[] used;
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                used = GraphicChunk.Frames[frame].SetData(pxOut, depthOut, rect);

                var ownPalt = GraphicChunk.ChunkParent.Get<PALT>(GraphicChunk.Frames[frame].PaletteID);
                ushort freePalt = GraphicChunk.ChunkID;
                if (GraphicChunk.Frames[frame].PaletteID != 0 && ownPalt != null && ownPalt.References == 1) targ = ownPalt;
                else
                {
                    var palts = GraphicChunk.ChunkParent.List<PALT>();
                    palts = (palts == null) ? new List<PALT>() : palts.OrderBy(x => x.ChunkID).ToList();
                    foreach (var palt in palts)
                    {
                        if (palt.ChunkID == freePalt) freePalt++;
                        if (palt.PalMatch(used) || palt.References == 0)
                        {
                            targ = palt;
                            break;
                        }
                    }
                }

                if (targ != null)
                {
                    //replace existing palt.
                    lock (targ)
                    {
                        if (targ.References == 0 || targ == ownPalt) targ.Colors = used;
                    }
                    GraphicChunk.Frames[frame].SetPalt(targ);
                }
                else
                {
                    //we need to make a new PALT.
                    //a bit hacky for now
                    var nPalt = new PALT()
                    {
                        ChunkID = freePalt,
                        ChunkLabel = GraphicChunk.ChunkLabel + " Auto PALT",
                        AddedByPatch = true,
                        ChunkProcessed = true,
                        ChunkType = "PALT",
                        RuntimeInfo = ChunkRuntimeState.Modified,
                        Colors = used
                    };

                    GraphicChunk.ChunkParent.AddChunk(nPalt);
                    GraphicChunk.Frames[frame].SetPalt(nPalt);
                    Content.Content.Get().Changes.IffChanged(GraphicChunk.ChunkParent);
                }

            }, GraphicChunk));

            for (int i = 0; i < 3; i++)
                bmps[i].UnlockBits(locks[i]);
        }

        private void NewRotation(int num)
        {
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var oldToNew = new Dictionary<int, int>();

                var old = GraphicChunk.Frames;
                var oldLen = old.Length;
                var result = new SPR2Frame[oldLen+num*3];
                var index = 0;

                for (int j = 0; j < 3; j++) {
                    for (int i = 0; i < oldLen / 3; i++)
                    {
                        oldToNew.Add(j * (oldLen / 3) + i, index);
                        result[index++] = old[j * (oldLen / 3) + i];
                    }
                    for (int i=0; i<num; i++)
                        result[index++] = new SPR2Frame(GraphicChunk) { PaletteID = (ushort)GraphicChunk.DefaultPaletteID };
                }
                GraphicChunk.Frames = result;

                foreach (var dgrp in GraphicChunk.ChunkParent.List<DGRP>())
                {
                    foreach (var img in dgrp.Images)
                    {
                        foreach (var spr in img.Sprites)
                        {
                            if (spr.SpriteID == GraphicChunk.ChunkID)
                            {
                                var oldspr = spr.SpriteFrameIndex;
                                if (oldToNew.ContainsKey((int)spr.SpriteFrameIndex))
                                    spr.SpriteFrameIndex = (uint)oldToNew[(int)spr.SpriteFrameIndex];
                                else
                                    spr.SpriteFrameIndex = 0;
                                if (oldspr != spr.SpriteFrameIndex) Content.Content.Get().Changes.ChunkChanged(dgrp);
                            }
                        }
                    }
                }

            }, GraphicChunk));
            SetActiveResource(GraphicChunk, ActiveIff);
            FrameList.SelectedIndex = FrameList.Items.Count - 1;
        }

        private void DeleteRotation(int id)
        {
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                var old = GraphicChunk.Frames;
                var oldLen = old.Length;
                var result = new SPR2Frame[oldLen - 3];
                var oldToNew = new Dictionary<int, int>();
                var index = 0;

                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < oldLen / 3; i++) {
                        oldToNew.Add(j * (oldLen / 3) + i, index);
                        if (i == id) continue;
                        result[index++] = old[j * (oldLen / 3) + i];
                    }
                }
                GraphicChunk.Frames = result;

                foreach (var dgrp in GraphicChunk.ChunkParent.List<DGRP>())
                {
                    foreach (var img in dgrp.Images)
                    {
                        foreach (var spr in img.Sprites)
                        {
                            if (spr.SpriteID == GraphicChunk.ChunkID)
                            {
                                var oldspr = spr.SpriteFrameIndex;
                                if (oldToNew.ContainsKey((int)spr.SpriteFrameIndex))
                                    spr.SpriteFrameIndex = (uint)oldToNew[(int)spr.SpriteFrameIndex];
                                else
                                    spr.SpriteFrameIndex = 0;
                                if (oldspr != spr.SpriteFrameIndex) Content.Content.Get().Changes.ChunkChanged(dgrp);
                            }
                        }
                    }
                }
            }, GraphicChunk));
            
            SetActiveResource(GraphicChunk, ActiveIff);
            if (id != 0) FrameList.SelectedIndex = id - 1;
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Choose a folder to export a Sprite Rotation from.";
            FolderBrowse(dialog);

            if (dialog.SelectedPath == "") return;
            ImportSprites(dialog.SelectedPath, FrameList.SelectedIndex);
        }

        private Bitmap SmartDownscale(Bitmap pixel, int factor, Color skip)
        {
            //generate output
            var output = new Bitmap(pixel.Width / factor, pixel.Height / factor);

            var olock = output.LockBits(new Rectangle(0, 0, output.Width, output.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var odat = new byte[olock.Stride * olock.Height];

            //lock input for read
            var plock = pixel.LockBits(new Rectangle(0, 0, pixel.Width, pixel.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pdat = new byte[plock.Stride * plock.Height];

            Marshal.Copy(plock.Scan0, pdat, 0, pdat.Length);

            bool useSkip = skip != Color.FromArgb(0, 0, 0, 0);

            int index = 0;
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    //we want to sum a factor*factor area of the source and compute an average
                    //instances of the "skip" color are ignored.
                    int sumr = 0, sumg = 0, sumb = 0;
                    int div = 0;
                    for (int ix = 0; ix < factor; ix++)
                    {
                        for (int iy = 0; iy < factor; iy++)
                        {
                            var srcIndex = ((x * factor + ix) + (y * factor + iy) * pixel.Width) * 4;
                            if (!(useSkip && pdat[srcIndex] == skip.B && pdat[srcIndex + 1] == skip.G && pdat[srcIndex + 2] == skip.R))
                            {
                                div++;
                                sumr += pdat[srcIndex + 2];
                                sumg += pdat[srcIndex + 1];
                                sumb += pdat[srcIndex];
                            }
                        }
                    }

                    if (div == 0)
                    {
                        div = 1;
                        sumr = skip.R;
                        sumg = skip.G;
                        sumb = skip.B;
                    }
                    else { }

                    odat[index] = (byte)(sumb / div);
                    odat[index + 1] = (byte)(sumg / div);
                    odat[index + 2] = (byte)(sumr / div);
                    odat[index + 3] = 255;
                    index += 4;
                }
            }

            pixel.UnlockBits(plock);

            Marshal.Copy(odat, 0, olock.Scan0, odat.Length);
            output.UnlockBits(olock);

            return output;
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            NewRotation(1);
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (FrameList.SelectedIndex == -1) return;
            DeleteRotation(FrameList.SelectedIndex);
        }

        private Bitmap[] ChannelFromSlice(Bitmap colDep, Bitmap alpha, Rectangle slice)
        {
            var output = new Bitmap[3];
            var locks = new BitmapData[3];
            var data = new byte[3][];

            for (var i = 0; i < 3; i++)
            {
                output[i] = new Bitmap(slice.Width, slice.Height);
                locks[i] = output[i].LockBits(new Rectangle(0, 0, output[i].Width, output[i].Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                data[i] = new byte[locks[i].Stride * locks[i].Height];
            }

            var plock = colDep.LockBits(slice, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pdat = new byte[plock.Stride * plock.Height];
            Marshal.Copy(plock.Scan0, pdat, 0, pdat.Length);

            var alock = alpha.LockBits(slice, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var adat = new byte[alock.Stride * alock.Height];
            Marshal.Copy(alock.Scan0, adat, 0, pdat.Length);

            for (int y=0; y<slice.Height; y++)
            {
                int sindex = y * plock.Stride;
                int dindex = y * locks[0].Stride;
                for (int x=0; x<slice.Width; x++)
                {
                    data[0][dindex] = pdat[sindex];
                    data[0][dindex + 1] = pdat[sindex+1];
                    data[0][dindex + 2] = pdat[sindex+2]; 
                    data[0][dindex + 3] = 255;

                    data[1][dindex] = adat[sindex];
                    data[1][dindex + 1] = adat[sindex];
                    data[1][dindex + 2] = adat[sindex];
                    data[1][dindex + 3] = 255;

                    data[2][dindex] = pdat[sindex+3];
                    data[2][dindex + 1] = pdat[sindex + 3];
                    data[2][dindex + 2] = pdat[sindex + 3];
                    data[2][dindex + 3] = 255;

                    sindex+=4;
                    dindex+=4;
                }
            }

            for (var i = 0; i < 3; i++)
            {
                Marshal.Copy(data[i], 0, locks[i].Scan0, data[i].Length);
                output[i].UnlockBits(locks[i]);
            }

            colDep.UnlockBits(plock);
            alpha.UnlockBits(alock);
            return output;
        }

        private void SheetImport_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "TGA Sprite Sheets (select non Alpha)|*.tga";
            if (FileBrowse(dialog))
            {
                try
                {
                    var name = dialog.FileName;
                    var tga1 = new Paloma.TargaImage(name).Image;
                    var tga2 = new Paloma.TargaImage(name.Substring(0,name.Length-4)+"Alpha.tga").Image;

                    if (tga1.Width % 238 != 0) {
                        MessageBox.Show("Invalid sheet!");
                    }

                    var rotations = tga1.Width / 238;

                    for (int r=0; r< rotations; r++)
                    {
                        ReplaceSprite(ChannelFromSlice(tga1, tga2, new Rectangle(r * 136, 0, 136, 384)), r + 0 * (GraphicChunk.Frames.Length / 3));
                        ReplaceSprite(ChannelFromSlice(tga1, tga2, new Rectangle(r * 68 + rotations*136, 0, 68, 192)), r + 1 * (GraphicChunk.Frames.Length / 3));
                        ReplaceSprite(ChannelFromSlice(tga1, tga2, new Rectangle(r * 34 + rotations * (136 + 68), 0, 34, 96)), r + 2 * (GraphicChunk.Frames.Length / 3));
                    }

                    var stream = new MemoryStream();
                    GraphicChunk.Write(GraphicChunk.ChunkParent, stream);
                    GraphicChunk.ChunkData = stream.ToArray();
                    GraphicChunk.ChunkProcessed = false;
                    GraphicChunk.Dispose();
                    GraphicChunk.ChunkParent.Get<SPR2>(GraphicChunk.ChunkID);
                    UpdateGraphics();
                }
                catch (Exception ex) {
                    MessageBox.Show("Failed to import TGAs! Make sure ...Alpha.tga is also present.");
                }
            }
        }

        private bool FileBrowse(FileDialog dialog)
        {
            // ༼ つ ◕_◕ ༽つ IMPEACH STAThread ༼ つ ◕_◕ ༽つ
            var wait = new AutoResetEvent(false);
            bool success = false;
            var thread = new Thread(() => {
                success = DialogResult.OK == dialog.ShowDialog();
                wait.Set();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            wait.WaitOne();
            return success;
        }
    }
}
