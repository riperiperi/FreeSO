using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UINeighBanner : UIElement, IZIndexable
    {
        private Texture2D SliceBg;
        private Texture2D SliceCol;
        private Texture2D SliceShad;
        private UISpriteBatch Batch;
        private bool Invalid;
        public bool Flip;

        private RenderTarget2D Target;

        public Color BannerColor = new Color(0, 128, 255);
        public int DataID;

        private static Color TextMul = new Color(68, 68, 68);
        private static float TextShadIntensity = 0.20f;

        private static TextStyle MainStyle;

        static UINeighBanner()
        {
            MainStyle = TextStyle.DefaultTitle.Clone();
            MainStyle.Size = 26;
        }

        private string _Caption;
        public string Caption
        {
            get
            {
                return _Caption;
            }
            set
            {
                var newSize = MainStyle.MeasureString(value);
                CaptionWidth = (int)newSize.X;
                CaptionHeight = (int)newSize.Y;
                _Caption = value;
                Invalid = true;
            }
        }

        public float Z { get; set; }

        private int CaptionHeight = 0;
        private int CaptionWidth = 0;

        private float IntScale = 1;

        public override void Removed()
        {
            base.Removed();
            Target?.Dispose();
            Batch?.Dispose();

            Target = null;
            Batch = null;
        }

        public UINeighBanner()
        {
            var ui = Content.Content.Get().CustomUI;

            SliceBg = ui.Get("neighp_bigbanner9s_bg.png").Get(GameFacade.GraphicsDevice);
            SliceCol = ui.Get("neighp_bigbanner9s_col.png").Get(GameFacade.GraphicsDevice);
            SliceShad = ui.Get("neighp_bigbanner9s_shad.png").Get(GameFacade.GraphicsDevice);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            base.PreDraw(batch);
            if (Z > 0) return;
            var newIntScale = (float)((Scale.X > 1) ? Math.Round(Scale.X) : 1 / (Math.Round((1 / Scale.X))));
            if (newIntScale > 5) newIntScale = 5;

            if (IntScale != newIntScale)
            {
                IntScale = newIntScale;
                Invalid = true;
            }
            var width = CaptionWidth + 120;
            var activeRect = new Rectangle((int)(IntScale * width / -2), (int)(-40 * IntScale), (int)(width * IntScale), (int)(80*IntScale));
            if (activeRect.Width == 0) activeRect.Width = 1;
            if (activeRect.Height == 0) activeRect.Height = 1;
            if (Invalid || Target == null || activeRect.Size != new Point(Target.Width, Target.Height))
            {
                var gd = GameFacade.GraphicsDevice;
                if (Batch == null) Batch = new UISpriteBatch(gd, 0);
                Target?.Dispose();
                Target = new RenderTarget2D(gd, activeRect.Width, activeRect.Height);

                //draw the banner
                gd.SetRenderTarget(Target);
                gd.Clear(Color.Transparent);
                var scale = Microsoft.Xna.Framework.Matrix.CreateScale(IntScale);
                Batch.BatchMatrixStack.Push(scale);
                Batch.Begin(transformMatrix: scale);

                var o = Opacity;
                Opacity = 1;
                CalculateOpacity();
                gd.RasterizerState = RasterizerState.CullNone;
                InternalDraw(Batch);
                Opacity = o;
                CalculateOpacity();

                Batch.BatchMatrixStack.Pop();
                Batch.End();
                //batch.Resume();
                gd.SetRenderTarget(null);
                Invalid = false;
            }
            Position = Position;
        }

        private void Draw3Slice(UISpriteBatch batch, Texture2D tex, Rectangle rect, Color tint)
        {
            var width = tex.Width;
            var twidth = rect.Width;
            var slice = width / 3;
            var height = tex.Height;
            var loc = rect.Location.ToVector2();
            var scale = Vector2.One;
            if (Flip)
            {
                SprEffects = SpriteEffects.FlipVertically;
            }
            DrawGlobalTexture(batch, tex, new Rectangle(0, 0, slice, height), loc, scale, tint);
            var mWidth = twidth - slice * 2;
            DrawGlobalTexture(batch, tex, new Rectangle(slice, 0, width - 2 * slice, height), loc + new Vector2(slice, 0), 
                new Vector2((float)(mWidth)/(width-slice*2), scale.Y), tint);
            DrawGlobalTexture(batch, tex, new Rectangle(width-slice, 0, slice, height), loc + new Vector2(twidth-slice, 0), scale, tint);

            if (Flip)
            {
                SprEffects = SpriteEffects.None;
            }
        }

        public void DrawGlobalTexture(SpriteBatch batch, Texture2D texture, Nullable<Rectangle> from, Vector2 to, Vector2 scale, Color blend)
        {
            var pos = FlooredLocalPoint(Vector2.Zero)/_Scale;
            DrawLocalTexture(batch, texture, from, to/_Scale-pos, scale/_Scale, blend);
        }

        public void DrawGlobalString(SpriteBatch batch, string text, Vector2 to, TextStyle style, Rectangle bounds, TextAlignment align)
        {
            var scale = _Scale;
            _Scale = Vector2.One;
            _ScaleX = 1;
            _ScaleY = 1;
            CalculateMatrix();
            var pos = FlooredLocalPoint(Vector2.Zero);
            DrawLocalString(batch, text, to - pos, style, bounds, align);
            _Scale = scale;
            _ScaleX = scale.X;
            _ScaleY = scale.Y;
            CalculateMatrix();
        }

        public void InternalDraw(UISpriteBatch batch)
        {
            var width = CaptionWidth + 120;
            var activeRect = new Rectangle(0, 0, width, 80);

            //the banners have multiple layers to drive smart coloration.
            //shadow layer: drawn under banner, should slightly tint with the target color.
            Draw3Slice(batch, SliceShad, activeRect, Color.Black);
            //bg layer: white background for the banner. Should not tint at all.
            Draw3Slice(batch, SliceBg, activeRect, Color.White);
            //col layer: colour tint to be applied over the banner.
            Draw3Slice(batch, SliceCol, activeRect, Color.Lerp(BannerColor, Color.White, 0.50f));

            //finally draw the text. Shadow then normal.
            var textCol = BannerColor * 0.33f;
            textCol.A = 255;

            var ctr = activeRect.Center;
            MainStyle.Color = textCol;
            DrawGlobalString(batch, Caption, ctr.ToVector2() + new Vector2(0, (Flip?12:-14)), MainStyle, new Rectangle(0, 0, 1, 1), TextAlignment.Middle | TextAlignment.Center);
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            if (Target != null)
            {
                var width = CaptionWidth + 120;
                var activeRect = new Rectangle(width / -2, -40, width, 80);
                
                DrawLocalTexture(batch, Target, null, activeRect.Location.ToVector2(), new Vector2(1/IntScale));
            }
        }
    }
}
