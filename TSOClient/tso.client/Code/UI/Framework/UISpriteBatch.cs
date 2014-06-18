/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using Microsoft.Xna.Framework;
using TSO.Common.utils;

namespace TSOClient.Code.UI.Framework
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
        public UISpriteBatch(GraphicsDevice gd, int numBuffers)
            : base(gd)
        {
            for (var i = 0; i < numBuffers; i++)
            {
                Buffers.Add(
                    RenderUtils.CreateRenderTarget(gd, 1, SurfaceFormat.Color, gd.Viewport.Width, gd.Viewport.Height)
                );
            }

            base.GraphicsDevice.DeviceReset += new EventHandler<EventArgs>(gd_DeviceReset);
        }

        public static bool Invalidated = false;

        private void gd_DeviceReset(object sender, EventArgs e)
        {
            Invalidated = true;

            Buffers.Clear();
            for (var i = 0; i < 3; i++)
            {
                Buffers.Add(
                    RenderUtils.CreateRenderTarget(base.GraphicsDevice, 1, SurfaceFormat.Color,
                    base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height)
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
