﻿using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.ResourceBrowser
{
    public partial class IffNameDialog : Form
    {
        private IffChunk Chunk;
        private bool NewChunk;

        public IffNameDialog(IffChunk chunk, bool newChunk)
        {
            InitializeComponent();

            Chunk = chunk;
            ChunkLabelEntry.Text = chunk.ChunkLabel;
            NewChunk = newChunk;

            if (chunk is BHAV || chunk is BCON)
            {
                switch (chunk.ChunkParent.RuntimeInfo.UseCase)
                {
                    case IffUseCase.Object:
                        ChunkIDEntry.Minimum = 4096;
                        break;
                    case IffUseCase.Global:
                        if (chunk.ChunkParent.Filename == "global.iff")
                            ChunkIDEntry.Minimum = 256;
                        else
                            ChunkIDEntry.Minimum = 8192;
                        break;
                }
            }
            ChunkIDEntry.Value = Math.Max(ChunkIDEntry.Minimum, Math.Min(ChunkIDEntry.Maximum, chunk.ChunkID));
            if (newChunk)
            {
                for (var i = ChunkIDEntry.Minimum; i < ChunkIDEntry.Maximum; i++)
                {
                    MethodInfo method = typeof(IffFile).GetMethod("Get");
                    MethodInfo generic = method.MakeGenericMethod(Chunk.GetType());
                    var iff = Chunk.ChunkParent;
                    var chnk = (IffChunk)generic.Invoke(iff, new object[] { (ushort)i });
                    if (chnk == null)
                    {
                        ChunkIDEntry.Value = i;
                        break;
                    }
                }
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            var iff = Chunk.ChunkParent;

            MethodInfo method = typeof(IffFile).GetMethod("Get");
            MethodInfo generic = method.MakeGenericMethod(Chunk.GetType());
            var chunk = (IffChunk)generic.Invoke(iff, new object[] { (ushort)ChunkIDEntry.Value });

            if (chunk != null && chunk != Chunk)
            {
                MessageBox.Show("The specified ID is already in use!", "Yikes!");
            }
            else
            {
                var id = (ushort)ChunkIDEntry.Value;
                var label = ChunkLabelEntry.Text;
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    Chunk.ChunkID = id;
                    Chunk.ChunkLabel = label;

                    if (!NewChunk) iff.RemoveChunk(Chunk);
                    iff.AddChunk(Chunk);
                    if (NewChunk) Chunk.AddedByPatch = true;
                    Chunk.RuntimeInfo = ChunkRuntimeState.Modified;
                }, Chunk));

                DialogResult = DialogResult.OK;
                this.Close();

            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
