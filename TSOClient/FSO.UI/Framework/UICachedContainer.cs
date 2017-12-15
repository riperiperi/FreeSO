﻿using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common;

namespace FSO.Client.UI.Framework
{
    public class UICachedContainer : UIContainer
    {
        public bool UseMultisample;
        public bool Invalidated;
        protected RenderTarget2D Target;
        public UIContainer DynamicOverlay = new UIContainer();
        public Point BackOffset;
        public Color ClearColor = Color.TransparentBlack;
        public bool UseZ;

        public UICachedContainer()
        {
            Add(DynamicOverlay);
            InvalidationParent = this;
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            //If our matrix is dirty, recalculate it
            if (_MtxDirty)
            {
                CalculateMatrix();
            }

            if (!Visible)
            {
                return;
            }

            var gd = batch.GraphicsDevice;
            if (Invalidated)
            {
                var size = Size * Scale;
                if (Target == null || (int)size.X != Target.Width || (int)size.Y != Target.Height)
                {
                    Target?.Dispose();
                    Target = new RenderTarget2D(gd, (int)size.X, (int)size.Y, false, SurfaceFormat.Color, (UseZ)?DepthFormat.Depth24:DepthFormat.None, (UseMultisample && !FSOEnvironment.DirectX)?4:0, RenderTargetUsage.PreserveContents);
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
                gd.Clear(ClearColor);
                var pos = LocalPoint(0, 0);

                batch.Begin(transformMatrix:
                    Microsoft.Xna.Framework.Matrix.CreateTranslation(-(pos.X), -(pos.Y), 0) *
                    Microsoft.Xna.Framework.Matrix.CreateScale(1f / FSOEnvironment.DPIScaleFactor) *
                    Microsoft.Xna.Framework.Matrix.CreateTranslation(BackOffset.X, BackOffset.Y, 0)
                    , blendState: BlendState.AlphaBlend, sortMode: SpriteSortMode.Deferred);
                batch.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                InternalDraw(batch);
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

        public virtual void InternalDraw(UISpriteBatch batch)
        {

        }

        public override void Update(UpdateState state)
        {
            BaseUpdate(state);
            lock (Children)
            {
                var chCopy = new List<UIElement>(Children);
                //todo: why are all these locks here, and what kind of problems might that cause
                //also find a cleaner way to allow modification of an element's children by its own children.
                foreach (var child in chCopy)
                {
                    if (child != DynamicOverlay)
                        child.Update(state);
                }
            }
            DynamicOverlay.Update(state);
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            if (Target != null)
            {
                DrawLocalTexture(batch, Target, null, -BackOffset.ToVector2(), new Vector2(1/(ScaleX), 1/(ScaleY)));
            }
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
                Invalidated = true;
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
