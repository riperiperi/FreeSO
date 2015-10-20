/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.Utils;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;

namespace FSO.Client.UI.Framework
{
    public class UISpriteBatch : SpriteBatch
    {
        /// <summary>
        /// Creates a UISpriteBatch. Same as spritebatch with some extra functionality
        /// required by the UI system of this game.
        /// 
        /// NumBuffers refers to a number of RenderTarget2D objects to create up front.
        /// These should be used for temp rendering for special effects. E.g. rendering
        /// part of the GUI to a texture and then painting it with opacity onto the main
        /// target.
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="numBuffers">The number of rendering buffers to pre-alloc</param>
        public UISpriteBatch(GraphicsDevice gd, int numBuffers, int width, int height, int multisample)
            : base(gd)
        {
            _Width = width;
            _Height = height;

            for (var i = 0; i < numBuffers; i++)
            {
                Buffers.Add(
                    RenderUtils.CreateRenderTarget(gd, 1, multisample, SurfaceFormat.Color, width, height)
                );
            }

            base.GraphicsDevice.DeviceReset += new EventHandler<EventArgs>(gd_DeviceReset);
        }

        public UISpriteBatch(GraphicsDevice gd, int numBuffers) : this(gd, numBuffers, gd.Viewport.Width, gd.Viewport.Height, 16) { }
        public UISpriteBatch(GraphicsDevice gd, int numBuffers, int width, int height) : this(gd, numBuffers, width, height, 0) { }

        public static bool Invalidated = false;

        private int _Width;
        public int Width
        {
            get
            {
                return _Width;
            }
        }

        private int _Height;
        public int Height
        {
            get
            {
                return _Height;
            }
        }

        private void gd_DeviceReset(object sender, EventArgs e)
        {
            Invalidated = true;

            Buffers.Clear();
            for (var i = 0; i < 3; i++)
            {
                Buffers.Add(
                    RenderUtils.CreateRenderTarget(base.GraphicsDevice, 1, 0, SurfaceFormat.Color,
                    Width, Height)
                );
            }

            Invalidated = false;
        }

        private BlendState _BlendMode;
        private SpriteSortMode _SortMode;

        /**
         * SpriteBatches which can be used to render
         * parts of the UI to a texture, then manipulated before
         * being added to the main batch. E.g. to do alpha blending
         */
        private List<RenderTarget2D> Buffers = new List<RenderTarget2D>();

        public void UIBegin(BlendState blendMode, SpriteSortMode sortMode)
        {
            this._BlendMode = blendMode;
            this._SortMode = sortMode;

            this.Begin(sortMode, blendMode);
        }

        public void Pause()
        {
            this.End();
        }

        public void Resume()
        {
            this.Begin(_SortMode, _BlendMode);
        }

        public RenderTarget2D GetBuffer()
        {
            if (Buffers.Count > 0)
            {
                var item = Buffers[0];
                Buffers.RemoveAt(0);
                return item;
            }
            return null;
        }

        public void FreeBuffer(RenderTarget2D buffer)
        {
            Buffers.Add(buffer);
        }

        public UIRenderPlane WithBuffer(ref Promise<Texture2D> output)
        {
            var promise = new Promise<Texture2D>(x => null);
            output = promise;

            return new UIRenderPlane(
                this,
                promise
            );
        }
    }

    /// <summary>
    /// Temporary rendering target so you can do visual
    /// effects on the rendered output. Bitmap effects,
    /// Alpha blending etc
    /// </summary>
    public class UIRenderPlane : IDisposable
    {
        private GraphicsDevice GD;
        private RenderTarget2D Target;
        private Promise<Texture2D> Texture;
        private UISpriteBatch Batch;

        public UIRenderPlane(UISpriteBatch batch, Promise<Texture2D> texture)
        {
            this.GD = batch.GraphicsDevice;
            this.Target = batch.GetBuffer();
            this.Texture = texture;
            this.Batch = batch;
            
            if(!UISpriteBatch.Invalidated)
            {
                /** Switch the render target **/
                Batch.Pause();
                GD.SetRenderTarget(Target);
                GD.Clear(Color.Transparent);
                Batch.Resume();
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!UISpriteBatch.Invalidated)
            {
                Batch.Pause();

                GD.SetRenderTarget(null);
                Texture.SetValue(Target);
                Batch.Resume();

                Batch.FreeBuffer(Target);
            }
        }

        #endregion
    }
}
