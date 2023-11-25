using FSO.LotView.Effects;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// A group of sprites grouped by texture. In SoftwareDepth mode, each buffer has more than one of these. Otherwise it just tends to be one.
    /// </summary>
    public class _2DDrawBuffer : IDisposable
    {
        public List<_2DDrawGroup> Groups = new List<_2DDrawGroup>();

        public void Dispose()
        {
            foreach (var group in Groups)
            {
                group.Dispose();
            }
        }
    }

    public class _2DDrawGroup : IDisposable
    {
        public int Primitives;
        public VertexBuffer VertBuf;
        public IndexBuffer IndexBuf;
        public short[] Indices;
        public _2DSpriteVertex[] Vertices;

        public Texture2D Pixel;
        public Texture2D Depth;
        public Texture2D Mask;
        public WorldBatchTechniques Technique;
        public bool Floors;

        public void Dispose()
        {
            if (VertBuf == null) return;
            VertBuf.Dispose();
            IndexBuf.Dispose();
        }
    }
}
