using FSO.Client.UI.Controls;
using System;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.UI.Controls
{
    public class UINineSliceButton : UIButton
    {
        private NineSliceMargins Margins;

        public UINineSliceButton() : base(StandardButton)
        {

        }

        public UINineSliceButton(Texture2D Texture) : base(Texture)
        {

        }

        public void SetNineSlice(int marginLeft, int marginRight, int marginTop, int marginBottom)
        {
            Margins = new NineSliceMargins()
            {
                Left = marginLeft,
                Right = marginRight,
                Top = marginTop,
                Bottom = marginBottom,
                States = 4
            };
            Margins.CalculateOrigins(Texture);
        }

        private float _Height;
        public float Height
        {
            get { return _Height; }
            set
            {
                _Height = value;
                if (ClickHandler != null)
                {
                    ClickHandler.Region.Height = (int)value;
                }
            }
        }

        public override void Draw(UISpriteBatch SBatch)
        {
            if (!Visible) { return; }

            /** Draw the button as a 3 slice **/
            var frame = CurrentFrameIndex;
            if (Disabled)
            {
                frame = 3;
            }
            if (Selected)
            {
                frame = 1;
            }
            if (ForceState > -1) frame = ForceState;
            frame = Math.Min(ImageStates - 1, frame);
            int offset = frame * Texture.Width / ImageStates;
            Margins.CalculateScales(Width, Height);
            Margins.DrawOntoPositionSlice(SBatch, this, Texture, Width, Height, Vector2.Zero, new Point(offset, 0));
        }
    }
}
