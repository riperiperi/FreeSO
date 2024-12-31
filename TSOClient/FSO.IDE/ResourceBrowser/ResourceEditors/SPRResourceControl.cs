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
using FSO.IDE.Common;
using System.Reflection;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public partial class SPRResourceControl : UserControl, IResourceControl
    {
        private static bool FramesMode;

        public SPR GraphicChunk;
        public GameIffResource ActiveIff;

        public Image[][] Graphics;

        public SPRResourceControl()
        {
            InitializeComponent();
            ModeCombo.SelectedIndex = 0;
        }

        public void SetActiveObject(GameObject obj)
        {
        }

        public void SetActiveResource(IffChunk chunk, GameIffResource res)
        {
            GraphicChunk = (SPR)chunk;
            ActiveIff = res;

            FrameList.Items.Clear();
            if (!FramesMode)
            {
                for (int i = 0; i < (GraphicChunk.Frames.Count / 2) / 3; i++)
                {
                    FrameList.Items.Add("Rotation " + i);
                }
                FramesButton.Text = "Frames";
                FramesLabel.Text = "Rotations:";
            }
            else
            {
                for (int i = 0; i < GraphicChunk.Frames.Count; i++)
                {
                    FrameList.Items.Add("Frame " + i);
                }
                FramesButton.Text = "Rotations";
                FramesLabel.Text = "Frames:";
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
            DeleteButton.Enabled = false;// hasFrames;
            ImportButton.Enabled = false;// hasFrames;
            ExportButton.Enabled = hasFrames;
            ImportAll.Enabled = false;// hasFrames;
            ExportAll.Enabled = hasFrames;
        }

        public void SetOBJDAttrs(OBJDSelector[] selectors)
        {
            SPRSelector.SetSelectors(null, GraphicChunk, selectors);
        }

        void RenderResourceModification(Image[][] Graphics, int index, bool depthFrame = false) => 
            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                int step = (GraphicChunk.Frames.Count / 2) / 3;
                if (depthFrame)
                    index += GraphicChunk.Frames.Count / 2;
                if (FramesMode)
                    step = 0;

                Graphics[0] = SpriteEncoderUtils.GetPixelAlpha(GraphicChunk.Frames.ElementAt(index), 136, 384, depthFrame);
                Graphics[1] = SpriteEncoderUtils.GetPixelAlpha(GraphicChunk.Frames.ElementAt(index + step), 68, 192, depthFrame);
                Graphics[2] = SpriteEncoderUtils.GetPixelAlpha(GraphicChunk.Frames.ElementAt(index + (step * 2)), 34, 96, depthFrame);
            }, GraphicChunk, false));

        public void UpdateGraphics()
        {
            int index = FrameList.SelectedIndex;
            Graphics = new Image[3][];
            
            bool depthFrame = ModeCombo.SelectedIndex == 2;
            RenderResourceModification(Graphics, index, depthFrame);
        }

        public void SetDisplay()
        {
            int mode = ModeCombo.SelectedIndex;
            if (mode == -1 || Graphics == null || Graphics[0] == null) return;
            if (mode == 2) mode = 0; // workaround for SPR using separate depth frames
            SPRBox1.Image = Graphics[0][mode];
            SPRBox2.Image = Graphics[1][mode];
            SPRBox3.Image = Graphics[2][mode];
        }

        public void ExportSPR(int index, string path)
        {
            var gfx = new Image[3][];

            var iffname = ActiveIff.MainIff.Filename;
            var extension = iffname.LastIndexOf('.');
            if (extension != -1) iffname = iffname.Substring(0, extension);

            var baseName = Path.Combine(path, iffname + "_spr1" + GraphicChunk.ChunkID + "_r" + index);

            RenderResourceModification(gfx, index, false);
            gfx[0][0].Save(baseName + "_near" + "_color.bmp", ImageFormat.Bmp);
            gfx[0][1].Save(baseName + "_near" + "_alpha.bmp", ImageFormat.Bmp);
            if (!AutoZooms.Checked)
            {
                gfx[1][0].Save(baseName + "_med" + "_color.bmp", ImageFormat.Bmp);
                gfx[1][1].Save(baseName + "_med" + "_alpha.bmp", ImageFormat.Bmp);
                gfx[2][0].Save(baseName + "_far" + "_color.bmp", ImageFormat.Bmp);
                gfx[2][1].Save(baseName + "_far" + "_alpha.bmp", ImageFormat.Bmp);
            }
            RenderResourceModification(gfx, index, true);
            gfx[0][0].Save(baseName + "_near" + "_depth.bmp", ImageFormat.Bmp);
            if (!AutoZooms.Checked)
            {
                gfx[1][0].Save(baseName + "_med" + "_depth.bmp", ImageFormat.Bmp);
                gfx[2][0].Save(baseName + "_far" + "_depth.bmp", ImageFormat.Bmp);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int mode = ModeCombo.SelectedIndex;
            if (GraphicChunk != default && mode != 1)
                UpdateGraphics(); // Ineffective for the Alpha channel since it doesn't exist
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
            throw new NotImplementedException();
        }

        private void ReplaceSprite(Bitmap[] bmps, int frame)
        {
            
        }

        private void NewRotation(int num)
        {
            throw new NotImplementedException();
        }

        private void DeleteRotation(int id)
        {
            throw new NotImplementedException();
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Choose a folder to export a Sprite Rotation from.";
            FolderBrowse(dialog);

            if (dialog.SelectedPath == "") return;
            ImportSprites(dialog.SelectedPath, FrameList.SelectedIndex);
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
            NewRotation(1);
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
            if (FrameList.SelectedIndex == -1) return;
            DeleteRotation(FrameList.SelectedIndex);
        }

        private void SheetImport_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
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

        private void FramesButton_Click(object sender, EventArgs e)
        {
            FramesMode = !FramesMode;
            SetActiveResource(GraphicChunk, ActiveIff);
        }
    }
}
