using FSO.Common.Model;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// A group of sprites grouped by texture. In SoftwareDepth mode, each buffer has more than one of these. Otherwise it just tends to be one.
    /// </summary>
    public class _2DDrawBuffer
    {
        public List<_2DDrawGroup> Groups = new List<_2DDrawGroup>();
    }

    public class _2DDrawGroup
    {
        public _2DSpriteVertex[] Vertices;
        public short[] Indices;

        public Texture2D Pixel;
        public Texture2D Depth;
        public Texture2D Mask;
        public EffectTechnique Technique;
    }
}
