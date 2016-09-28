using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Client.UI.Controls
{
    public class UICustomTooltip : UIElement
    {
        public string Text { get; set; } = null;

        private Color _Background = new Color(74, 108, 146);
        public Color Background { get { return _Background; } set { _Background = value; BackgroundTexture = null; } }
        public TextStyle TextStyle { get; set; }
        public int MaxWidth { get; set; } = 300;
        public int PaddingX { get; set; } = 6;
        public int PaddingY { get; set; } = 4;
        public int LineHeight { get; set; } = 20;
        public Vector2 Offset { get; set; } = new Vector2(5, 5);

        private Texture2D BackgroundTexture;
        
        public UICustomTooltip()
        {
            TextStyle = TextStyle.DefaultLabel.Clone();
            TextStyle.Color = Color.White;
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (string.IsNullOrEmpty(Text) || !Visible) { return; }
            
            var wrapped = UIUtils.WordWrap(Text, MaxWidth - (PaddingX*2), TextStyle, new Vector2(TextStyle.Scale));
            int width = wrapped.MaxWidth + (PaddingX*2);
            int height = (LineHeight * wrapped.Lines.Count) + (PaddingY*2);

            if(BackgroundTexture == null){
                BackgroundTexture = TextureUtils.TextureFromColor(batch.GraphicsDevice, Background);
            }

            DrawLocalTexture(batch, BackgroundTexture, null, Vector2.Zero, new Vector2(width, height));

            var y = PaddingY;
            
            for (int i = 0; i < wrapped.Lines.Count; i++)
            {
                var line = wrapped.Lines[i];
                var lineWidth = TextStyle.SpriteFont.MeasureString(line).X * TextStyle.Scale;
                DrawLocalString(batch, line, new Vector2((width - lineWidth) / 2.0f, y), TextStyle);
                y += LineHeight;
            }
        }
    }

    public class UICustomTooltipContainer : UIElement
    {
        public UICustomTooltip Tooltip;
        private Rectangle Bounds;

        public UICustomTooltipContainer(UICustomTooltip tooltip){
            this.Tooltip = tooltip;

            Bounds = new Rectangle(0, 0, 0, 0);
            ListenForMouse(Bounds, new Common.Rendering.Framework.IO.UIMouseEvent(OnMouse));
        }

        public void HideTooltip()
        {
            Tooltip.Visible = false;
        }

        private void OnMouse(UIMouseEventType type, UpdateState state){
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    Tooltip.Visible = true;
                    break;
                case UIMouseEventType.MouseOut:
                    Tooltip.Visible = false;
                    break;
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (Tooltip.Visible)
            {
                var mousePosition = state.MouseState.Position;
                Console.WriteLine(mousePosition);
                Tooltip.Position = Tooltip.Parent.GlobalPoint(new Vector2(mousePosition.X, mousePosition.Y)) + Tooltip.Offset;
            }
        }

        public void SetSize(int width, int height)
        {
            Bounds.Width = width;
            Bounds.Height = height;
        }

        public override Rectangle GetBounds()
        {
            return Bounds;
        }

        public override void Draw(UISpriteBatch batch)
        {
        }
    }
}
