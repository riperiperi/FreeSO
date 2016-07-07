/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;
using FSO.Files.Utils;
using FSO.Common.Rendering.Framework.Camera;
using FSO.LotView.Model;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// Similar to SpriteBatch but has additional features that target
    /// world object rendering in this game such as zbuffers.
    /// </summary>
    public class _2DWorldBatch
    {
        protected Matrix World;
        protected Matrix View;
        protected Matrix Projection;

        public GraphicsDevice Device;
        protected Effect Effect;

        protected Dictionary<_2DBatchRenderMode, List<_2DSprite>> Sprites = new Dictionary<_2DBatchRenderMode, List<_2DSprite>>();

        protected int DrawOrder;

        protected ICamera WorldCamera;

        private Vector2 PxOffset;
        private Vector3 WorldOffset;
        private Vector3 TileOffset;
        private short ObjectID;
        
        public bool OutputDepth = false;
        public bool OBJIDMode = false;
        public Texture2D AmbientLight;

        private Vector2 Scroll;
        private int LastWidth;
        private int LastHeight;
        private int ScrollBuffer;
        public void SetScroll(Vector2 scroll)
        {
            Scroll = scroll;
            ResetMatrices(LastWidth, LastHeight, (int)Scroll.X, (int)-Scroll.Y);
        }

        public void OffsetPixel(Vector2 pxOffset)
        {
            this.PxOffset = pxOffset;
        }

        public void OffsetTile(Vector3 tileOffset)
        {
            this.TileOffset = tileOffset;
            this.WorldOffset = WorldSpace.GetWorldFromTile(tileOffset);
        }

        public void SetObjID(short obj)
        {
            this.ObjectID = obj;
        }

        public _2DWorldBatch(GraphicsDevice device, int numBuffers, SurfaceFormat[] surfaceFormats, int scrollBuffer)
        {
            this.Device = device;
            this.Effect = WorldContent._2DWorldBatchEffect;
            //TODO: World size
            Sprites.Add(_2DBatchRenderMode.NO_DEPTH, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.RESTORE_DEPTH, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.WALL, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.Z_BUFFER, new List<_2DSprite>());

            ScrollBuffer = scrollBuffer;

            ResetMatrices(device.Viewport.Width, device.Viewport.Height);

            for (var i = 0; i < numBuffers; i++)
            {
                int width = device.Viewport.Width+scrollBuffer;
                int height = device.Viewport.Height+scrollBuffer;

                switch (i) {
                    case 4: //World2D.BUFFER_OBJID
                        width = 1;
                        height = 1;
                        break;
                    case 0: //World2D.BUFFER_THUMB
                        width = 1024;
                        height = 1024;
                        break;
                }
                Buffers.Add(
                    RenderUtils.CreateRenderTarget(device, 1, 0, surfaceFormats[i], width, height)
                );
            }
        }

        public void Draw(_2DSprite sprite)
        {
            sprite.AbsoluteDestRect = new Rectangle((int)(sprite.DestRect.X + PxOffset.X), (int)(sprite.DestRect.Y + PxOffset.Y), sprite.DestRect.Width, sprite.DestRect.Height);
            sprite.AbsoluteWorldPosition = sprite.WorldPosition + WorldOffset;
            sprite.AbsoluteTilePosition = sprite.TilePosition + TileOffset; 
            sprite.ObjectID = ObjectID;
            sprite.DrawOrder = DrawOrder;
            Sprites[sprite.RenderMode].Add(sprite);
            DrawOrder++;
        }

        public void DrawScrollBuffer(ScrollBuffer buffer, Vector2 offset, Vector3 tileOffset, WorldState state)
        {
            var offsetDiff = buffer.PxOffset - offset;
            var tOff = buffer.WorldPosition - tileOffset;
            var spr = new _2DSprite
            {
                RenderMode = (buffer.Depth == null) ? _2DBatchRenderMode.NO_DEPTH : _2DBatchRenderMode.RESTORE_DEPTH,
                Pixel = buffer.Pixel,
                Depth = buffer.Depth,
                SrcRect = new Rectangle(0, 0, buffer.Pixel.Width, buffer.Pixel.Height),
                DestRect = new Rectangle((int)offsetDiff.X-2, (int)offsetDiff.Y, buffer.Pixel.Width, buffer.Pixel.Height),
            };
            this.Draw(spr);
            spr.AbsoluteWorldPosition = tOff * new Vector3(1, 1.23f, 1) * WorldSpace.WorldUnitsPerTile;
            if (state.Rotation == WorldRotation.TopRight || state.Rotation == WorldRotation.BottomRight) spr.AbsoluteWorldPosition.Y *= -1;
            //why does this even work???
            //i added the 1.23 scaling factor on the y direction because at 1 it was shifting slightly
            //it's still not perfect, which leads me to believe there is a bigger problem...
            //after that I flip the offset if we're looking at it the other way, which inverts the z offset and corrects it somehow...
            //i mean, techically this should work with NO multipliers at all!
        }


        /// <summary>
        /// Reset for a draw loop
        /// </summary>
        public void Begin(ICamera worldCamera)
        {
            this.WorldCamera = worldCamera;
            ((WorldCamera)worldCamera).ProjectionDirty();

            this.Sprites[_2DBatchRenderMode.NO_DEPTH].Clear();
            this.Sprites[_2DBatchRenderMode.Z_BUFFER].Clear();
            this.Sprites[_2DBatchRenderMode.RESTORE_DEPTH].Clear();
            this.Sprites[_2DBatchRenderMode.WALL].Clear();

            this.DrawOrder = 0;
        }

        public void Pause()
        {
            this.End();
        }

        public void Resume()
        {
            this.Begin(this.WorldCamera);
        }

        public _2DWorldRenderPlane WithBuffer(int bufferIndex, ref Promise<Texture2D> output, int depthBufferIndex, ref Promise<Texture2D> depthOutput)
        {
            var promise = new Promise<Texture2D>(x => null);
            output = promise;

            depthOutput = new Promise<Texture2D>(x => null);

            ResetMatrices(Buffers[bufferIndex].Width, Buffers[bufferIndex].Height);

            return new _2DWorldRenderPlaneWithDepth(
                this,
                promise,
                Buffers[bufferIndex],
                depthOutput,
                Buffers[depthBufferIndex]
            );
        }

        public _2DWorldRenderPlane WithBuffer(int bufferIndex, ref Promise<Texture2D> output)
        {
            var promise = new Promise<Texture2D>(x => null);
            output = promise;

            ResetMatrices(Buffers[bufferIndex].Width, Buffers[bufferIndex].Height);

            return new _2DWorldRenderPlane(
                this,
                promise,
                Buffers[bufferIndex]
            );
        }

        private List<RenderTarget2D> Buffers = new List<RenderTarget2D>();


        public void End() { End(null, OutputDepth); }
        /// <summary>
        /// Processes the accumulated draw commands and paints the screen. Optionally outputs to a vertex cache.
        /// </summary>
        public void End(List<_2DDrawGroup> cache, bool outputDepth)
        {
            if (WorldCamera == null) return;
            var effect = this.Effect;
            if (cache == null)
            {
                Device.BlendState = BlendState.AlphaBlend;
                //  set the only parameter this effect takes.
                effect.Parameters["dirToFront"].SetValue(FrontDirForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation));
                effect.Parameters["offToBack"].SetValue(BackOffForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation));
                effect.Parameters["viewProjection"].SetValue(this.View * this.Projection);
                var mat = this.WorldCamera.View * this.WorldCamera.Projection;
                effect.Parameters["worldViewProjection"].SetValue(this.WorldCamera.View * this.WorldCamera.Projection);
                effect.Parameters["rotProjection"].SetValue(((WorldCamera)this.WorldCamera).GetRotationMatrix() * this.WorldCamera.Projection);
                effect.Parameters["ambientLight"].SetValue(AmbientLight);
            }

            if (outputDepth)
            {
                var spritesWithNoDepth = Sprites[_2DBatchRenderMode.NO_DEPTH];
                RenderSpriteList(spritesWithNoDepth, effect, effect.Techniques["drawSimple"], cache);

                var spritesWithDepth = Sprites[_2DBatchRenderMode.Z_BUFFER];
                RenderSpriteList(spritesWithDepth, effect, effect.Techniques["drawZSpriteDepthChannel"], cache);

                var walls = Sprites[_2DBatchRenderMode.WALL];
                RenderSpriteList(walls, effect, effect.Techniques["drawZWallDepthChannel"], cache);
            }
            else
            {
                /**
                 * Render the no depth items first
                 */
                var spritesWithNoDepth = Sprites[_2DBatchRenderMode.NO_DEPTH];
                RenderSpriteList(spritesWithNoDepth, effect, effect.Techniques[(OBJIDMode)?"drawSimpleID":"drawSimple"], cache); //todo: no depth sprites have fixed depth relative to their position
                //the flies object and sim balloons/skill gauges/relationship plusses use this mode

                var spritesWithDepth = Sprites[_2DBatchRenderMode.Z_BUFFER];
                RenderSpriteList(spritesWithDepth, effect, effect.Techniques[(OBJIDMode) ? "drawZSpriteOBJID" : "drawZSprite"], cache);

                var walls = Sprites[_2DBatchRenderMode.WALL];
                RenderSpriteList(walls, effect, effect.Techniques[(OBJIDMode) ? "drawZSpriteOBJID" : "drawZWall"], cache);

                var spritesWithRestoreDepth = Sprites[_2DBatchRenderMode.RESTORE_DEPTH];
                RenderSpriteList(spritesWithRestoreDepth, effect, effect.Techniques["drawSimpleRestoreDepth"], cache);
            }
        }

        public void RenderCache(List<_2DDrawGroup> cache)
        {
            var effect = this.Effect;
            Device.BlendState = BlendState.AlphaBlend;
            //  set the only parameter this effect takes.
            effect.Parameters["dirToFront"].SetValue(FrontDirForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation));
            effect.Parameters["offToBack"].SetValue(BackOffForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation));
            effect.Parameters["viewProjection"].SetValue(this.View * this.Projection);
            var mat = this.WorldCamera.View * this.WorldCamera.Projection;
            effect.Parameters["worldViewProjection"].SetValue(this.WorldCamera.View * this.WorldCamera.Projection);
            effect.Parameters["ambientLight"].SetValue(AmbientLight);

            foreach (var group in cache) RenderDrawGroup(group);
        }

        private List<_2DSpriteTextureGroup> GroupByTexture(List<_2DSprite> sprites)
        {
            var result = new List<_2DSpriteTextureGroup>();
            var map = new Dictionary<Tuple<Texture2D, Texture2D>, _2DSpriteTextureGroup>();

            foreach (var sprite in sprites)
            {
                var tuple = new Tuple<Texture2D, Texture2D>(sprite.Pixel, sprite.Mask);
                _2DSpriteTextureGroup grouping;
                
                if (!map.TryGetValue(tuple, out grouping))
                {
                    grouping = new _2DSpriteTextureGroup
                    {
                        Pixel = sprite.Pixel,
                        Depth = sprite.Depth,
                        Mask = sprite.Mask
                    };
                    map.Add(tuple, grouping);
                    result.Add(grouping);
                }
                grouping.Sprites.Add(sprite);
            }
            return result;
        }

        private Vector3 FrontDirForRot(WorldRotation rot)
        {
            switch (rot)
            {
                case WorldRotation.TopLeft:
                    return new Vector3(3, 0, 3);
                case WorldRotation.TopRight:
                    return new Vector3(3, 0, -3);
                case WorldRotation.BottomRight:
                    return new Vector3(-3, 0, -3);
                case WorldRotation.BottomLeft:
                    return new Vector3(-3, 0, 3);
            }
            return new Vector3(3, 0, 3);
        }

        private Vector4 BackOffForRot(WorldRotation rot)
        {
            switch (rot)
            {
                case WorldRotation.TopLeft:
                    return new Vector4(0, 0, 0, 0);
                case WorldRotation.TopRight:
                    return new Vector4(0, 0, 3, 0);
                case WorldRotation.BottomRight:
                    return new Vector4(3, 0, 3, 0);
                case WorldRotation.BottomLeft:
                    return new Vector4(3, 0, 0, 0);
            }
            return new Vector4(0, 0, 0, 0);
        }

        public Rectangle GetSpriteListBounds()
        {
            List<_2DSprite> all = new List<_2DSprite>();
            for (var i=0; i<Sprites.Count; i++) {
                List<_2DSprite> list = Sprites.Values.ElementAt(i);
                all.AddRange(list);
            }
            return GetSpriteListBounds(all);
        }

        private Rectangle GetSpriteListBounds(List<_2DSprite> sprites)
        {
            int smallX = int.MaxValue;
            int smallY = int.MaxValue;
            int bigX = int.MinValue;
            int bigY = int.MinValue;
            foreach (var sprite in sprites) {
                var rect = sprite.AbsoluteDestRect;
                if (rect.X < smallX) smallX = rect.X;
                if (rect.Y < smallY) smallY = rect.Y;
                if (rect.X + rect.Width > bigX) bigX = rect.X + rect.Width;
                if (rect.Y + rect.Height > bigY) bigY = rect.Y + rect.Height;
            }
            return new Rectangle(smallX, smallY, bigX - smallX, bigY - smallY);
        }

        private void RenderDrawGroup(_2DDrawGroup group)
        {
            var effect = this.Effect;
            effect.Parameters["pixelTexture"].SetValue(group.Pixel);
            if (group.Depth != null) effect.Parameters["depthTexture"].SetValue(group.Depth);
            if (group.Mask != null) effect.Parameters["maskTexture"].SetValue(group.Mask);

            effect.CurrentTechnique = group.Technique;
            EffectPassCollection passes = group.Technique.Passes;
            for (int i = 0; i < passes.Count; i++)
            {
                EffectPass pass = passes[i];
                pass.Apply();
                Device.DrawUserIndexedPrimitives<_2DSpriteVertex>(
                    PrimitiveType.TriangleList, group.Vertices, 0, group.Vertices.Length,
                    group.Indices, 0, group.Indices.Length / 3);
            }
        }

        private void RenderSpriteList(List<_2DSprite> sprites, Effect effect, EffectTechnique technique, List<_2DDrawGroup> cache){
            if (sprites.Count == 0) { return; }

            /** Group by texture **/
            var groupByTexture = GroupByTexture(sprites);
            foreach (var group in groupByTexture)
            {
                var numSprites = group.Sprites.Count;
                var texture = group.Pixel;

                /** Build vertex data **/
                var vertices = new _2DSpriteVertex[4 * numSprites];
                var indices = new short[6 * numSprites];
                var indexCount = 0;
                var vertexCount = 0;

                foreach (var sprite in group.Sprites)
                {
                    //TODO: We want to pre-generate the sprite vertices, to reduce CPU usage.
                    //To do this they'll need to be scrolled by the gpu, all updates to sprite state
                    //will need to regenerate the _2DSpriteVertices, etc.

                    var srcRectangle = sprite.SrcRect;
                    var dstRectangle = sprite.AbsoluteDestRect;

                    indices[indexCount++] = (short)(vertexCount + 0);
                    indices[indexCount++] = (short)(vertexCount + 1);
                    indices[indexCount++] = (short)(vertexCount + 3);
                    indices[indexCount++] = (short)(vertexCount + 1);
                    indices[indexCount++] = (short)(vertexCount + 2);
                    indices[indexCount++] = (short)(vertexCount + 3);
                    // add the new vertices

                    var left = sprite.FlipHorizontally ? srcRectangle.Right : srcRectangle.Left;
                    var right = sprite.FlipHorizontally ? srcRectangle.Left : srcRectangle.Right;
                    var top = sprite.FlipVertically ? srcRectangle.Bottom : srcRectangle.Top;
                    var bot = sprite.FlipVertically ? srcRectangle.Top : srcRectangle.Bottom;

                    vertices[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Left, dstRectangle.Top, 0)
                        , GetUV(texture, left, top), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID, sprite.Room);
                    vertices[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Right, dstRectangle.Top, 0)
                        , GetUV(texture, right, top), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID, sprite.Room);
                    vertices[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Right, dstRectangle.Bottom, 0)
                        , GetUV(texture, right, bot), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID, sprite.Room);
                    vertices[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Left, dstRectangle.Bottom, 0)
                        , GetUV(texture, left, bot), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID, sprite.Room);
                }

                var dg = new _2DDrawGroup()
                {
                    Pixel = group.Pixel,
                    Depth = group.Depth,
                    Mask = group.Mask,

                    Vertices = vertices,
                    Indices = indices,
                    Technique = technique
                };

                if (cache != null) cache.Add(dg);
                else RenderDrawGroup(dg);
                
            }
        }

        private Vector2 GetUV(Texture2D Texture, float x, float y)
        {
            return new Vector2(x / (float)Texture.Width, y / (float)Texture.Height);
        }

        public void ResetMatrices(int width, int height)
        {
            ResetMatrices(width, height, (int)Scroll.X, (int)Scroll.Y);
        }
        public void ResetMatrices(int width, int height, int transX, int transY)
        {
            LastHeight = height;
            LastWidth = width;
            this.World = Matrix.Identity;
            this.View = new Matrix(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, -1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            if (LotView.World.DirectX)
            {
                this.Projection = Matrix.CreateOrthographicOffCenter(
                    transX - 2f, transX + width - 2f, transY - height - 0f, transY - 0f, 0, 1);
            }
            else
            {
                this.Projection = Matrix.CreateOrthographicOffCenter(
                    transX - 1.5f, transX + width - 1.5f, transY - height - 0.5f, transY - 0.5f, 0, 1);
            }
            //offset pixels by a little bit so that the center of them lies on the sample area. Avoids graphical bugs.
        }

        private Dictionary<ITextureProvider, Texture2D> _TextureCache = new Dictionary<ITextureProvider, Texture2D>();

        /// <summary>
        /// Gets a texture from this 2DWorldBatch's texture cache.
        /// </summary>
        /// <param name="item">An ITextureProvider instance.</param>
        /// <returns>A Texture2D instance.</returns>
        public Texture2D GetTexture(ITextureProvider item)
        {
            lock (_TextureCache)
            {
                if (_TextureCache.ContainsKey(item))
                {
                    return _TextureCache[item];
                }

                var texture = item.GetTexture(this.Device);
                _TextureCache.Add(item, texture);
                return texture;
            }
        }

        private Dictionary<IWorldTextureProvider, WorldTexture> _WorldTextureCache = new Dictionary<IWorldTextureProvider, WorldTexture>();
        public WorldTexture GetWorldTexture(IWorldTextureProvider item)
        {
            return item.GetWorldTexture(this.Device);
        }
    }

    public class _2DWorldRenderPlaneWithDepth : _2DWorldRenderPlane
    {
        protected Promise<Texture2D> DepthTexture;
        protected RenderTarget2D DepthTarget;

        public _2DWorldRenderPlaneWithDepth(_2DWorldBatch batch, 
                                            Promise<Texture2D> output, 
                                            RenderTarget2D buffer,
                                            Promise<Texture2D> depthOutput,
                                            RenderTarget2D depthBuffer) : base(batch, output, buffer)
        {
            this.DepthTexture = depthOutput;
            this.DepthTarget = depthBuffer;
        }

        public override bool NextPass()
        {
            if (Pass == 0){
                Batch.Pause();
                GD.SetRenderTargets(Target); //render to multiple targets, 0 is color, 1 is depth!
                GD.Clear(Color.Transparent);
                GD.SetRenderTargets(DepthTarget); //render to multiple targets, 0 is color, 1 is depth!
                GD.Clear(Color.Transparent);

                GD.SetRenderTargets(Target, DepthTarget);
                Batch.OutputDepth = true;
                Batch.Resume();

                Pass++;
                return true;
            }
            return false ;
        }

        protected void ExtractDepthTexture()
        {
            var texture = DepthTarget;
            DepthTexture.SetValue(texture);
        }

        public override void Dispose()
        {
            Batch.Pause();
            Batch.OutputDepth = false;
            GD.SetRenderTarget(null); //need to unbind both before we can extract their textures.
            ExtractPixelTexture();
            ExtractDepthTexture();
            Batch.Resume();
        }
    }

    /// <summary>
    /// Represents a temporary render target so we can render parts of the
    /// world into a texture for caching
    /// </summary>
    public class _2DWorldRenderPlane : IDisposable
    {
        protected GraphicsDevice GD;
        protected RenderTarget2D Target;
        protected Promise<Texture2D> Texture;
        protected _2DWorldBatch Batch;

        protected int Pass = 0;

        public _2DWorldRenderPlane(_2DWorldBatch batch, Promise<Texture2D> output, RenderTarget2D buffer){
            this.GD = batch.Device;
            this.Texture = output;
            this.Batch = batch;
            this.Target = buffer;//batch.GetBuffer();

            /** Switch the render target **/
            
        }

        public virtual bool NextPass()
        {
            if (Pass == 0)
            {
                Batch.Pause();
                GD.SetRenderTarget(Target);
                GD.Clear(Color.Transparent);
                Batch.Resume();

                Pass++;
                return true;
            }
            return false;
        }

        protected void ExtractPixelTexture()
        {
            var texture = Target;
            Texture.SetValue(texture);
        }

        #region IDisposable Members

        public virtual void Dispose(){
            Batch.Pause();
            GD.SetRenderTarget(null);
            ExtractPixelTexture();
            Batch.Resume();
        }

        #endregion
    }



    public enum _2DBatchRenderMode {
        NO_DEPTH,
        Z_BUFFER,
        RESTORE_DEPTH,
        WALL
    }

    public class _2DSpriteTextureGroup
    {
        public Texture2D Pixel;
        public Texture2D Depth;
        public Texture2D Mask;
        public List<_2DSprite> Sprites = new List<_2DSprite>();
    }

    public struct Tuple<T1, T2> //used for texture groups
    {
        public readonly T1 Item1;
        public readonly T2 Item2;
        public Tuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2; }
    }
}
