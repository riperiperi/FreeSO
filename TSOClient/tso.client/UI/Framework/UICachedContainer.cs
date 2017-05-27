using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Framework
{
    public class UICachedContainer : UIContainer
    {
        public bool Invalidated;
        private RenderTarget2D Target;
        public UIContainer DynamicOverlay = new UIContainer();
        public Point BackOffset;

        public UICachedContainer()
        {
            Add(DynamicOverlay);
            InvalidationParent = this;
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            if (!Visible)
            {
                return;
            }

            var gd = batch.GraphicsDevice;
            if (Invalidated)
            {
                var size = Size;
                if (Target == null || (int)size.X != Target.Width || (int)size.Y != Target.Height)
                {
                    Target?.Dispose();
                    Target = new RenderTarget2D(gd, (int)size.X, (int)size.Y, false, SurfaceFormat.Color, DepthFormat.None);
                }

                lock (Children)
                {
                    foreach (var child in Children)
                    {
                        if (child == DynamicOverlay) continue;
                        child.PreDraw(batch);
                    }
                }

                batch.End();
                gd.SetRenderTarget(Target);
                gd.Clear(Color.Transparent);
                var pos = LocalPoint(0, 0);

                batch.Begin(transformMatrix: Microsoft.Xna.Framework.Matrix.CreateTranslation(-(pos.X-BackOffset.X), -(pos.Y-BackOffset.Y), 0), blendState: BlendState.AlphaBlend, sortMode: SpriteSortMode.Deferred);
                batch.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                lock (Children)
                {
                    foreach (var child in Children)
                    {
                        if (child == DynamicOverlay) continue;
                        child.Draw(batch);
                    }
                }
                batch.End();
                gd.SetRenderTarget(null);
                Invalidated = false;
            }
            DynamicOverlay.PreDraw(batch);
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            DrawLocalTexture(batch, Target, -BackOffset.ToVector2());
            DynamicOverlay.Draw(batch);
        }

        private Vector2 _Size;
        public override Vector2 Size
        {
            get
            {
                return _Size;
            }

            set
            {
                Invalidate();
                _Size = value;
            }
        }

        public override void Removed()
        {
            Target?.Dispose();
            Target = null;
            base.Removed();
        }
    }
}
