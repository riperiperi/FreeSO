using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Model
{
    struct PixelMaskTuple
    {
        public Texture2D Pixel;
        public Texture2D Mask;

        public PixelMaskTuple(Texture2D px, Texture2D mask)
        {
            Pixel = px;
            Mask = mask;
        }

        public override int GetHashCode()
        {
            return (Pixel?.GetHashCode()??0) ^ (Mask?.GetHashCode()??0);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (PixelMaskTuple)obj;
            return (Pixel == other.Pixel && Mask == other.Mask);
        }
    }
}
