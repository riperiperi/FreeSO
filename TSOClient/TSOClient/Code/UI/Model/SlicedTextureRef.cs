using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Framework;

namespace TSOClient.Code.UI.Model
{
    public class SlicedTextureRef : ITextureRef
    {
        public Texture2D Texture;
        public Rectangle Margins;

        private NineSliceMargins NineSlice;

        public SlicedTextureRef(Texture2D texture, Rectangle margins)
        {
            this.Texture = texture;
            this.Margins = margins;

            NineSlice = new NineSliceMargins {
                Left = margins.Left,
                Top = margins.Top,
                Right = margins.Width,
                Bottom = margins.Height
            };
            NineSlice.CalculateOrigins(texture);
        }

        public void Draw(SpriteBatch SBatch, UIElement element, float x, float y, float width, float height)
        {
            //TODO: Cache scales for various sizes?
            NineSlice.CalculateScales(width, height);
            NineSlice.DrawOntoPosition(SBatch, element, Texture, width, height, new Vector2(x, y));
        }
    }


    public interface ITextureRef
    {
        void Draw(SpriteBatch SBatch, UIElement element, float x, float y, float width, float height);
    }
}
