﻿/*
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
using FSO.Common;
using FSO.LotView.Effects;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// Similar to SpriteBatch but has additional features that target
    /// world object rendering in this game such as zbuffers.
    /// </summary>
    public class _2DWorldBatch : IDisposable
    {
        public static SurfaceFormat[] BUFFER_SURFACE_FORMATS = new SurfaceFormat[] {
            /** Static Buffers **/
            SurfaceFormat.Color,
            SurfaceFormat.Color, //depth, using a 24-bit packed format

            /** Object ID buffer **/
            SurfaceFormat.Color,

            /** Obj thumbnail buffers **/
            SurfaceFormat.Color,
            SurfaceFormat.Color, //depth, using a 24-bit packed format

            /** Lot Thumbnail Buffer **/
            SurfaceFormat.Color
        };

        public static bool[] FORMAT_ALWAYS_DEPTHSTENCIL = new bool[] {
            /** Static Object Buffers **/
            true,
            false, //depth, using a 24-bit packed format

            /** Object ID buffer **/
            true,

            //Thumbnail depth
            true,
            false,

            //lot thumb
            true,
        };

        public static readonly int NUM_2D_BUFFERS = 6;
        public static readonly int BUFFER_STATIC = 0;
        public static readonly int BUFFER_STATIC_DEPTH = 1;
        public static readonly int BUFFER_OBJID = 2;
        public static readonly int BUFFER_THUMB = 3; //used for drawing thumbnails
        public static readonly int BUFFER_THUMB_DEPTH = 4; //used for drawing thumbnails
        public static readonly int BUFFER_LOTTHUMB = 5;

        public static readonly int SCROLL_BUFFER = 512; //resolution to add to render size for scroll reasons

        protected Matrix World;
        protected Matrix View;
        protected Matrix Projection;
        protected Matrix ViewProjection;

        public GraphicsDevice Device;
        protected WorldBatchEffect Effect;

        protected List<_2DSpriteGroup> Sprites = new List<_2DSpriteGroup>();
        private List<_2DSprite> SpritePool = new List<_2DSprite>();
        private int SpriteIndex = 0;

        protected int DrawOrder;

        protected WorldCamera WorldCamera;

        private Vector2 PxOffset;
        private Vector3 WorldOffset;
        private Vector3 TileOffset;
        private short ObjectID;
        
        public bool OutputDepth = false;
        public bool OBJIDMode = false;
        public Texture2D AmbientLight;
        public Texture2D AdvLight;
        public IndexBuffer SpriteIndices;

        private Vector2 Scroll;
        public int LastWidth;
        public int LastHeight;
        private int ScrollBuffer;

        public float PreciseZoom;
        private float ScreenWidth;
        private float ScreenHeight;

        private List<RenderTarget2D> Buffers = new List<RenderTarget2D>();

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

        private int NumBuffers;
        private SurfaceFormat[] SurfaceFormats;
        private bool[] AlwaysDS;
        public _2DWorldBatch(GraphicsDevice device, int numBuffers, SurfaceFormat[] surfaceFormats, bool[] alwaysDS, int scrollBuffer)
        {
            this.Device = device;
            this.Effect = WorldContent._2DWorldBatchEffect;
            Effect.OutsideDark = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            Effect.WorldToLightFactor = new Vector3(1f / (3 * 75), 1f / (3 * 2.95f), 1f / (3 * 75));
            //TODO: World size
            Sprites = new List<_2DSpriteGroup>();

            ScrollBuffer = scrollBuffer;
            NumBuffers = numBuffers;
            SurfaceFormats = surfaceFormats;
            AlwaysDS = alwaysDS;

            GenBuffers(device.Viewport.Width, device.Viewport.Height);
            SpriteIndices = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, 6, BufferUsage.None);
            SpriteIndices.SetData(_2DStandaloneSprite.indices);
        }

        public void GenBuffers(int bwidth, int bheight)
        {
            foreach (var buffer in Buffers)
            {
                buffer.Dispose();
            }
            Buffers.Clear();

            ResetMatrices(bwidth, bheight);

            ScreenWidth = bwidth;
            ScreenHeight = bheight;

            for (var i = 0; i < NumBuffers; i++)
            {

                int width = bwidth + ScrollBuffer;
                int height = bheight + ScrollBuffer;

                switch (i)
                {
                    case 2: //World2D.BUFFER_OBJID
                        width = 1;
                        height = 1;
                        break;
                    case 3: //World2D.BUFFER_THUMB
                    case 4:
                        width = 1024;
                        height = 1024;
                        break;
                    case 5:
                        width = 576;
                        height = 576;
                        break;
                }

                if (NumBuffers == 2) width = height = 1024; //special case, thumb only. 
                var depthformat = FSOEnvironment.SoftwareDepth ? DepthFormat.Depth24Stencil8 : DepthFormat.Depth24; //stencil is used for software depth
                Buffers.Add(
                    PPXDepthEngine.CreateRenderTarget(Device, 1, 0, SurfaceFormats[i], width, height,
                    (AlwaysDS[i] || (!FSOEnvironment.UseMRT && !FSOEnvironment.SoftwareDepth)) ? depthformat : DepthFormat.None)
                );
            }
        }
        
        public _2DSprite NewSprite(_2DBatchRenderMode mode)
        {
            if (SpriteIndex >= SpritePool.Count)
            {
                var spr = new _2DSprite() { RenderMode = mode };
                SpritePool.Add(spr);
                SpriteIndex++;
                return spr;
            } else
            {
                var spr = SpritePool[SpriteIndex++];
                spr.Repurpose();
                spr.RenderMode = mode;
                return spr;
            }
        }

        public void Draw(_2DSprite sprite)
        {
            var x = sprite.DestRect.X;
            var y = sprite.DestRect.Y;
            var width = sprite.DestRect.Width;
            var height = sprite.DestRect.Height;

            sprite.AbsoluteDestRect = new Rectangle((int)(x + PxOffset.X), (int)(y + PxOffset.Y), width, height);
            sprite.AbsoluteWorldPosition = sprite.WorldPosition + WorldOffset;
            if (sprite.ObjectID == 0) sprite.ObjectID = ObjectID;
            sprite.DrawOrder = DrawOrder;

            bool added = false;
            int i = 0;
            while (!added)
            {
                if (i >= Sprites.Count) { Sprites.Add(new _2DSpriteGroup(FSOEnvironment.SoftwareDepth)); }
                if (FSOEnvironment.SoftwareDepth && Sprites[i].SprRectangles.SearchForIntersect(sprite.AbsoluteDestRect))
                    i++; //intersects with a sprite in this list. advance to next.
                else
                {
                    if (FSOEnvironment.SoftwareDepth) Sprites[i].SprRectangles.Add(sprite.AbsoluteDestRect);
                    Sprites[i].Sprites[sprite.RenderMode].Add(sprite);
                    added = true;
                }
            }

            DrawOrder++;
        }

        public void DrawScrollBuffer(ScrollBuffer buffer, Vector2 offset, Vector3 tileOffset, WorldState state)
        {
            var offsetDiff = (buffer.PxOffset - offset) * state.PreciseZoom;
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
            spr.AbsoluteWorldPosition = tOff * WorldSpace.WorldUnitsPerTile;
            var y = spr.AbsoluteWorldPosition.Z;
            spr.AbsoluteWorldPosition.Z = spr.AbsoluteWorldPosition.Y;
            spr.AbsoluteWorldPosition.Y = y;
        }


        /// <summary>
        /// Reset for a draw loop
        /// </summary>
        public void Begin(WorldCamera camera2D)
        {
            this.WorldCamera = camera2D;
            camera2D.ProjectionDirty();

            this.Sprites.Clear();
            SpriteIndex = 0;

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

            if (Buffers.Count == 2) { bufferIndex = 0; depthBufferIndex = 1; }

            if (depthBufferIndex > Buffers.Count) depthBufferIndex = Buffers.Count - 1;

            if (bufferIndex >= Buffers.Count) bufferIndex = 0;

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

        public void ResizeBuffer(int bufferIndex, int width, int height)
        {
            var buffer = Buffers[bufferIndex];
            if (buffer.Width != width || buffer.Height != height)
            {
                Buffers[bufferIndex] = PPXDepthEngine.CreateRenderTarget(Device, 1, 0, buffer.Format, width, height, buffer.DepthStencilFormat);
                buffer.Dispose();
            }
        }

        public void PrepareImmediate(WorldBatchTechniques technique)
        {
            var effect = this.Effect;
            Device.BlendState = BlendState.AlphaBlend;
            //  set the only parameter this effect takes.
            var frontDir = FrontDirForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation);
            effect.dirToFront = frontDir;
            effect.offToBack = BackOffForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation);
            //frontDir /= 3;
            //WorldContent._2DWorldBatchEffect.Parameters["LightOffset"].SetValue(new Vector2(frontDir.X / (6 * 75), frontDir.Z / (6 * 75)));
            var mat = this.WorldCamera.View * this.WorldCamera.Projection;
            effect.worldViewProjection = mat;
            var inv = Matrix.Invert(mat);
            effect.iWVP = inv;
            effect.rotProjection = ((WorldCamera)this.WorldCamera).GetRotationMatrix() * this.WorldCamera.Projection;

            effect.PxOffset = new Vector2();
            effect.WorldOffset = new Vector4();

            effect.SetTechnique(technique);
            Device.Indices = SpriteIndices;
        }

        public void EnsureIndices()
        {
            if (Device.Indices != SpriteIndices)
            {
                Device.Indices = SpriteIndices;
            }
        }

        public void SetShaderOffsets(Vector2 pxOffset, Vector3 worldOffset)
        {
            var effect = this.Effect;
            effect.PxOffset = pxOffset;
            effect.WorldOffset = new Vector4(worldOffset, 0);
        }

        public void DrawImmediate(_2DStandaloneSprite sprite)
        {
            var effect = this.Effect;
            if (!FSOEnvironment.DirectX)
            {
                Device.Indices = null; //monogame why
                Device.Indices = SpriteIndices;
            }
            PPXDepthEngine.RenderPPXDepth(effect, false, (depth) =>
            {
                effect.pixelTexture = sprite.Pixel;
                if (sprite.Depth != null) effect.depthTexture = sprite.Depth;
                if (sprite.Mask != null) effect.maskTexture = sprite.Mask;
                
                EffectPassCollection passes = effect.CurrentTechnique.Passes;

                EffectPass pass = passes[Math.Min(passes.Count - 1, WorldConfig.Current.DirPassOffset)];
                pass.Apply();
                if (sprite.GPUVertices != null)
                {
                    Device.SetVertexBuffer(sprite.GPUVertices);
                    Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
                }
            });
        }

        public void EndImmediate()
        {
            var effect = this.Effect;
            effect.PxOffset = new Vector2();
            effect.WorldOffset = new Vector4();
        }

        public void End() { End(null, true); }
        /// <summary>
        /// Processes the accumulated draw commands and paints the screen. Optionally outputs to a vertex cache.
        /// </summary>
        public void End(List<_2DDrawBuffer> cache, bool outputDepth)
        {
            if (WorldCamera == null) return;
            var effect = this.Effect;
            if (cache == null)
            {
                Device.BlendState = BlendState.AlphaBlend;
                //  set the only parameter this effect takes.
                var frontDir = FrontDirForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation);
                effect.dirToFront = frontDir;
                effect.offToBack = BackOffForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation);
                //frontDir /= 3;
                //WorldContent._2DWorldBatchEffect.Parameters["LightOffset"].SetValue(new Vector2(frontDir.X / (6 * 75), frontDir.Z / (6 * 75)));
                var mat = this.WorldCamera.View * this.WorldCamera.Projection;
                effect.worldViewProjection = mat;
                var inv = Matrix.Invert(mat);
                effect.iWVP = inv;
                effect.rotProjection = ((WorldCamera)this.WorldCamera).GetRotationMatrix() * this.WorldCamera.Projection;
                //effect.Parameters["depthOutMode"].SetValue(outputDepth && (!FSOEnvironment.UseMRT));
            }

            if (Sprites.Count == 0) return;
            int i = 0;
            foreach (var sprites in Sprites)
            {
                if (cache != null) {
                    if (i >= cache.Count) cache.Add(new _2DDrawBuffer());
                    EndDrawSprites(sprites, cache[i].Groups, outputDepth);
                }
                else
                {
                    var temp = new _2DDrawBuffer();
                    EndDrawSprites(sprites, temp.Groups, OutputDepth);
                    PPXDepthEngine.RenderPPXDepth(effect, false, (depth) =>
                    {
                        RenderCache(new List<_2DDrawBuffer> { temp });
                        //EndDrawSprites(sprites, null, OutputDepth);
                    });
                    temp.Dispose();
                }
                i++;
            }

        }

        public void EndDrawSprites(_2DSpriteGroup sprites, List<_2DDrawGroup> cache, bool outputDepth)
        {
            var effect = Effect;
            // draw all spritelists one by one. 
            if (outputDepth)
            {
                var spritesWithNoDepth = sprites.Sprites[_2DBatchRenderMode.NO_DEPTH];
                
                RenderSpriteList(spritesWithNoDepth, effect, WorldBatchTechniques.drawSimple, cache);

                var spritesWithDepth = sprites.Sprites[_2DBatchRenderMode.Z_BUFFER];
                RenderSpriteList(spritesWithDepth, effect, WorldBatchTechniques.drawZSpriteDepthChannel, cache);

                var floors = sprites.Sprites[_2DBatchRenderMode.FLOOR];
                RenderSpriteList(floors, effect, WorldBatchTechniques.drawZSpriteDepthChannel, cache);

                var walls = sprites.Sprites[_2DBatchRenderMode.WALL];
                RenderSpriteList(walls, effect, WorldBatchTechniques.drawZWallDepthChannel, cache);

                var spritesWithRestoreDepth = sprites.Sprites[_2DBatchRenderMode.RESTORE_DEPTH];
                RenderSpriteList(spritesWithRestoreDepth, effect, WorldBatchTechniques.drawSimpleRestoreDepth, cache);
            }
            else
            {
                /**
                 * Render the no depth items first
                 */
                var spritesWithNoDepth = sprites.Sprites[_2DBatchRenderMode.NO_DEPTH];
                RenderSpriteList(spritesWithNoDepth, effect, (OBJIDMode) ? WorldBatchTechniques.drawSimpleID : WorldBatchTechniques.drawSimple, cache); //todo: no depth sprites have fixed depth relative to their position
                //the flies object and sim balloons/skill gauges/relationship plusses use this mode

                var spritesWithDepth = sprites.Sprites[_2DBatchRenderMode.Z_BUFFER];
                RenderSpriteList(spritesWithDepth, effect, (OBJIDMode) ? WorldBatchTechniques.drawZSpriteOBJID : WorldBatchTechniques.drawZSprite, cache);

                var floors = sprites.Sprites[_2DBatchRenderMode.FLOOR];
                RenderSpriteList(floors, effect, WorldBatchTechniques.drawZSpriteDepthChannel, cache);

                var walls = sprites.Sprites[_2DBatchRenderMode.WALL];
                RenderSpriteList(walls, effect, (OBJIDMode) ? WorldBatchTechniques.drawZSpriteOBJID : WorldBatchTechniques.drawZWall, cache);

                var spritesWithRestoreDepth = sprites.Sprites[_2DBatchRenderMode.RESTORE_DEPTH];
                RenderSpriteList(spritesWithRestoreDepth, effect, WorldBatchTechniques.drawSimpleRestoreDepth, cache);
            }
        }

        public void RenderCache(List<_2DDrawBuffer> cache)
        {
            var effect = this.Effect;
            Device.BlendState = BlendState.AlphaBlend;
            //  set the only parameter this effect takes.
            var frontDir = FrontDirForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation);
            effect.dirToFront = frontDir;
            effect.offToBack = BackOffForRot(((FSO.LotView.Utils.WorldCamera)WorldCamera).Rotation);
            //frontDir /= 3;
            //WorldContent._2DWorldBatchEffect.Parameters["LightOffset"].SetValue(new Vector2(frontDir.X / (6 * 75), frontDir.Z / (6 * 75)));
            var mat = this.WorldCamera.View * this.WorldCamera.Projection;
            effect.worldViewProjection = mat;
            effect.iWVP = Matrix.Invert(mat);
            //effect.Parameters["depthOutMode"].SetValue(OutputDepth && (!FSOEnvironment.UseMRT));

            foreach (var buffer in cache)
            {
                PPXDepthEngine.RenderPPXDepth(effect, false, (depth) =>
                {
                    foreach (var group in buffer.Groups)
                        RenderDrawGroup(group);
                });
                
            }
        }

        private List<_2DSpriteTextureGroup> GroupByTexture(List<_2DSprite> sprites)
        {
            var result = new List<_2DSpriteTextureGroup>();
            var map = new Dictionary<PixelMaskTuple, _2DSpriteTextureGroup>();

            foreach (var sprite in sprites)
            {
                var tuple = new PixelMaskTuple(sprite.Pixel, sprite.Mask);
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
            const float bias = 0.15f; //all 2d graphics are slightly offset forwards
            switch (rot)
            {
                case WorldRotation.TopLeft:
                    return new Vector4(bias, 0, bias, 0);
                case WorldRotation.TopRight:
                    return new Vector4(bias, 0, 3-bias, 0);
                case WorldRotation.BottomRight:
                    return new Vector4(3-bias, 0, 3-bias, 0);
                case WorldRotation.BottomLeft:
                    return new Vector4(3-bias, 0, bias, 0);
            }
            return new Vector4(0, 0, 0, 0);
        }

        public Rectangle GetSpriteListBounds()
        {
            List<_2DSprite> all = new List<_2DSprite>();
            for (var i=0; i<Sprites.Count; i++) {
                for (int j = 0; j < Sprites[i].Sprites.Count; j++)
                {
                    List<_2DSprite> list = Sprites[i].Sprites.Values.ElementAt(j);
                    all.AddRange(list);
                }
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
            effect.pixelTexture = group.Pixel;
            if (group.Depth != null) effect.depthTexture = group.Depth;
            if (group.Mask != null) effect.maskTexture = group.Mask;

            effect.SetTechnique(group.Technique);
            EffectPassCollection passes = effect.CurrentTechnique.Passes;
            
            EffectPass pass = passes[Math.Min(passes.Count - 1, WorldConfig.Current.DirPassOffset)];
            pass.Apply();
            if (group.VertBuf != null)
            {
                Device.SetVertexBuffer(group.VertBuf);
                Device.Indices = group.IndexBuf;
                Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, group.Primitives);
            }
            else
            {
                Device.DrawUserIndexedPrimitives<_2DSpriteVertex>(
                    PrimitiveType.TriangleList, group.Vertices, 0, group.Vertices.Length,
                    group.Indices, 0, group.Indices.Length / 3);
            }
        }

        private void RenderSpriteList(List<_2DSprite> sprites, WorldBatchEffect effect, WorldBatchTechniques technique, List<_2DDrawGroup> cache){
            if (sprites.Count == 0) { return; }
            bool floors = sprites.First().RenderMode == _2DBatchRenderMode.FLOOR;
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
                        , GetUV(texture, left, top), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID, sprite.Room, sprite.Floor);
                    vertices[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Right, dstRectangle.Top, 0)
                        , GetUV(texture, right, top), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID, sprite.Room, sprite.Floor);
                    vertices[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Right, dstRectangle.Bottom, 0)
                        , GetUV(texture, right, bot), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID, sprite.Room, sprite.Floor);
                    vertices[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Left, dstRectangle.Bottom, 0)
                        , GetUV(texture, left, bot), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID, sprite.Room, sprite.Floor);
                }

                VertexBuffer vb = null;
                IndexBuffer ib = null;

                var count = indices.Length / 3;
                if (count > 50) //completely arbitrary number, but seems to keep things fast. dont gen if it isn't "worth it".
                {
                    vb = new VertexBuffer(Device, typeof(_2DSpriteVertex), vertices.Length, BufferUsage.WriteOnly);
                    vb.SetData(vertices);
                    ib = new IndexBuffer(Device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
                    ib.SetData(indices);
                    vertices = null;
                    indices = null;
                }

                var dg = new _2DDrawGroup()
                {
                    Pixel = group.Pixel,
                    Depth = group.Depth,
                    Mask = group.Mask,

                    VertBuf = vb,
                    IndexBuf = ib,
                    Vertices = vertices,
                    Indices = indices,
                    Primitives = count,
                    Technique = technique,
                    Floors = floors
                };

                if (cache != null) cache.Add(dg);
                else
                {
                    RenderDrawGroup(dg);
                    dg.Dispose();
                }
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
            height = (int)(height/PreciseZoom);
            width = (int)(width/PreciseZoom);

            transX += (int)(ScreenWidth - (ScreenWidth / PreciseZoom)) / 2;
            transY -= (int)(ScreenHeight - (ScreenHeight / PreciseZoom)) / 2;
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

            ViewProjection = View * Projection;
            Effect.viewProjection = this.View * this.Projection;
        }

        private Dictionary<ITextureProvider, Texture2D> _TextureCache = new Dictionary<ITextureProvider, Texture2D>();

        /// <summary>
        /// Gets a texture from this 2DWorldBatch's texture cache.
        /// </summary>
        /// <param name="item">An ITextureProvider instance.</param>
        /// <returns>A Texture2D instance.</returns>
        public Texture2D GetTexture(ITextureProvider item)
        {
            if (item == null) return null;
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

        public void ClearTextureCache()
        {
            lock (_TextureCache)
            {
                _TextureCache.Clear();
            }
        }

        private Dictionary<IWorldTextureProvider, WorldTexture> _WorldTextureCache = new Dictionary<IWorldTextureProvider, WorldTexture>();
        public WorldTexture GetWorldTexture(IWorldTextureProvider item)
        {
            return item.GetWorldTexture(this.Device);
        }

        public void Dispose()
        {
            SpriteIndices.Dispose();
            foreach (var buf in Buffers)
            {
                buf.Dispose();
            }
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
                PPXDepthEngine.SetPPXTarget(Target, DepthTarget, true);

                Batch.OutputDepth = true; //depth surface always uses depth techniques
                Batch.Resume();

                Pass++;
                return true;
            }
            return false;
            /*else if (Pass == 1)
            {
                if (FSOEnvironment.UseMRT) return false;
                else
                {
                    Batch.Pause();
                    GD.SetRenderTarget(DepthTarget);
                    Batch.OutputDepth = true;
                    Batch.Resume();
                    Pass++;
                    return true;
                }
            }
            return false;*/
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
            PPXDepthEngine.SetPPXTarget(null, null, false); //need to unbind both before we can extract their textures.
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
                PPXDepthEngine.SetPPXTarget(Target, null, true);
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
            PPXDepthEngine.SetPPXTarget(null, null, false);
            ExtractPixelTexture();
            Batch.Resume();
        }

        #endregion
    }



    public enum _2DBatchRenderMode {
        NO_DEPTH,
        Z_BUFFER,
        RESTORE_DEPTH,
        WALL,
        FLOOR
    }

    public class _2DSpriteTextureGroup
    {
        public Texture2D Pixel;
        public Texture2D Depth;
        public Texture2D Mask;
        public List<_2DSprite> Sprites = new List<_2DSprite>();
    }
}
