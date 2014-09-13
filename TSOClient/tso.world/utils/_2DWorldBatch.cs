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
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSO.Common.utils;
using TSO.Files.utils;
using TSO.Common.rendering.framework.camera;

namespace tso.world.utils
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

        public void OffsetPixel(Vector2 pxOffset){
            this.PxOffset = pxOffset;
        }
        public void OffsetTile(Vector3 tileOffset){
            this.TileOffset = tileOffset;
            this.WorldOffset = WorldSpace.GetWorldFromTile(tileOffset);
        }
        public void SetObjID(short obj)
        {
            this.ObjectID = obj;
        }

        public _2DWorldBatch(GraphicsDevice device, int numBuffers, SurfaceFormat[] surfaceFormats)
        {
            this.Device = device;
            this.Effect = WorldContent._2DWorldBatchEffect;
            //TODO: World size
            Sprites.Add(_2DBatchRenderMode.NO_DEPTH, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.RESTORE_DEPTH, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.WALL, new List<_2DSprite>());
            Sprites.Add(_2DBatchRenderMode.Z_BUFFER, new List<_2DSprite>());

            ResetMatrices(device.Viewport.Width, device.Viewport.Height);

            for (var i = 0; i < numBuffers; i++)
            {
                Buffers.Add(
                    RenderUtils.CreateRenderTarget(device, 1, surfaceFormats[i], (i == 4) ? 1 : device.Viewport.Width, (i == 4) ? 1 : device.Viewport.Height) //buffer 4 (objid) is 1x1
                );
            }
        }

        public void Draw(_2DSprite sprite)
        {
            sprite.AbsoluteDestRect = new Rectangle((int)(sprite.DestRect.X + PxOffset.X), (int)(sprite.DestRect.Y + PxOffset.Y), sprite.DestRect.Width, sprite.DestRect.Height);
            sprite.AbsoluteWorldPosition = new Vector3(sprite.WorldPosition.X + WorldOffset.X, sprite.WorldPosition.Y + WorldOffset.Y, sprite.WorldPosition.Z + WorldOffset.Z);
            sprite.AbsoluteTilePosition = new Vector3(sprite.TilePosition.X + TileOffset.X, sprite.TilePosition.Y + TileOffset.Y, sprite.TilePosition.Z + TileOffset.Z);
            sprite.ObjectID = ObjectID;
            sprite.DrawOrder = DrawOrder;
            Sprites[sprite.RenderMode].Add(sprite);
            DrawOrder++;
        }

        public void DrawBasic(Texture2D texture, Vector2 position)
        {
            this.Draw(new _2DSprite {
                RenderMode = _2DBatchRenderMode.NO_DEPTH,
                Pixel = texture,
                SrcRect = new Rectangle(0, 0, texture.Width, texture.Height),
                DestRect = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height)
            });
        }

        public void DrawBasicRestoreDepth(Texture2D texture, Texture2D depth, Vector2 position)
        {
            this.Draw(new _2DSprite
            {
                RenderMode = _2DBatchRenderMode.RESTORE_DEPTH,
                Pixel = texture,
                Depth = depth,
                SrcRect = new Rectangle(0, 0, texture.Width, texture.Height),
                DestRect = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height)
            });
        }


        /// <summary>
        /// Reset for a draw loop
        /// </summary>
        public void Begin(ICamera worldCamera){
            this.WorldCamera = worldCamera;

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

        public void Resume(){
            this.Begin(this.WorldCamera);
        }

        public _2DWorldRenderPlane WithBuffer(int bufferIndex, ref Promise<Texture2D> output, int depthBufferIndex, ref Promise<Texture2D> depthOutput)
        {
            var promise = new Promise<Texture2D>(x => null);
            output = promise;

            depthOutput = new Promise<Texture2D>(x => null);

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

            return new _2DWorldRenderPlane(
                this,
                promise,
                Buffers[bufferIndex]
            );
        }

        private List<RenderTarget2D> Buffers = new List<RenderTarget2D>();

        /// <summary>
        /// Processes the acculimated draw commands and paints the screen
        /// </summary>
        public void End(){

            var color = Color.White;
            var declaration = new VertexDeclaration(Device, _2DSpriteVertex.VertexElements);
            Device.VertexDeclaration = declaration;
            
            var effect = this.Effect;
            
            //  set the only parameter this effect takes.
            effect.Parameters["dirToFront"].SetValue(FrontDirForRot(((tso.world.utils.WorldCamera)WorldCamera).Rotation));
            effect.Parameters["offToBack"].SetValue(BackOffForRot(((tso.world.utils.WorldCamera)WorldCamera).Rotation));
            effect.Parameters["viewProjection"].SetValue(this.View * this.Projection);
            effect.Parameters["worldViewProjection"].SetValue(this.WorldCamera.View * this.WorldCamera.Projection);
            effect.CommitChanges();

            if (OutputDepth)
            {
                var spritesWithNoDepth = Sprites[_2DBatchRenderMode.NO_DEPTH];
                RenderSpriteList(spritesWithNoDepth, effect, effect.Techniques["drawSimple"]);

                var spritesWithDepth = Sprites[_2DBatchRenderMode.Z_BUFFER];
                RenderSpriteList(spritesWithDepth, effect, effect.Techniques["drawZSpriteDepthChannel"]);

                var walls = Sprites[_2DBatchRenderMode.WALL];
                RenderSpriteList(walls, effect, effect.Techniques["drawZWallDepthChannel"]);
            }
            else
            {
                /**
                 * Render the no depth items first
                 */
                var spritesWithNoDepth = Sprites[_2DBatchRenderMode.NO_DEPTH];
                RenderSpriteList(spritesWithNoDepth, effect, effect.Techniques[(OBJIDMode)?"drawSimpleID":"drawSimple"]); //todo: no depth sprites have fixed depth relative to their position
                //the flies object and sim balloons/skill gauges/relationship plusses use this mode

                var spritesWithDepth = Sprites[_2DBatchRenderMode.Z_BUFFER];
                RenderSpriteList(spritesWithDepth, effect, effect.Techniques[(OBJIDMode) ? "drawZSpriteOBJID" : "drawZSprite"]);

                var walls = Sprites[_2DBatchRenderMode.WALL];
                RenderSpriteList(walls, effect, effect.Techniques[(OBJIDMode) ? "drawZSpriteOBJID" : "drawZWall"]);

                var spritesWithRestoreDepth = Sprites[_2DBatchRenderMode.RESTORE_DEPTH];
                RenderSpriteList(spritesWithRestoreDepth, effect, effect.Techniques["drawSimpleRestoreDepth"]);
            }

            /*
            EffectTechnique technique = null;
            switch (mode)
            {
                case _2DBatchRenderMode.NO_DEPTH:
                    technique = effect.Techniques["drawSimple"];
                    break;
                case _2DBatchRenderMode.Z_BUFFER:
                    technique = effect.Techniques["drawWithDepth"];
                    break;
            }*/
        }

        private List<_2DSpriteTextureGroup> GroupByTexture(List<_2DSprite> sprites)
        {
            var result = new List<_2DSpriteTextureGroup>();
            var map = new Dictionary<Tuple<Texture2D, Texture2D>, _2DSpriteTextureGroup>();

            foreach (var sprite in sprites){
                var tuple = new Tuple<Texture2D, Texture2D>(sprite.Pixel, sprite.Mask);
                if (!map.ContainsKey(tuple))
                {
                    var grouping = new _2DSpriteTextureGroup
                    {
                        Pixel = sprite.Pixel,
                        Depth = sprite.Depth,
                        Mask = sprite.Mask
                    };
                    grouping.Sprites.Add(sprite);
                    map.Add(tuple, grouping);
                    result.Add(grouping);
                }
                else
                {
                    map[tuple].Sprites.Add(sprite);
                }
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

        private void RenderSpriteList(List<_2DSprite> sprites, Effect effect, EffectTechnique technique){
            if (sprites.Count == 0) { return; }

            /** Group by texture **/
            var groupByTexture = GroupByTexture(sprites);
            foreach (var group in groupByTexture)
            {
                var texture = group.Pixel;
                var depth = group.Depth;
                var numSprites = group.Sprites.Count;

                effect.Parameters["pixelTexture"].SetValue(texture);
                if (depth != null)
                {
                    effect.Parameters["depthTexture"].SetValue(depth);
                }
                if (group.Mask != null)
                {
                    effect.Parameters["maskTexture"].SetValue(group.Mask);
                }

                /** Build vertex data **/
                var verticies = new _2DSpriteVertex[4 * numSprites];
                var indices = new short[6 * numSprites];
                var indexCount = 0;
                var vertexCount = 0;

                foreach (var sprite in group.Sprites)
                {
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

                    verticies[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Left + 0.5f, dstRectangle.Top + 0.5f, 0)
                        , GetUV(texture, left, top), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                    verticies[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Right + 0.5f, dstRectangle.Top + 0.5f, 0)
                        , GetUV(texture, right, top), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                    verticies[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Right + 0.5f, dstRectangle.Bottom + 0.5f, 0)
                        , GetUV(texture, right, bot), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                    verticies[vertexCount++] = new _2DSpriteVertex(
                        new Vector3(dstRectangle.Left + 0.5f, dstRectangle.Bottom + 0.5f, 0)
                        , GetUV(texture, left, bot), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                }

                effect.CurrentTechnique = technique;
                effect.Begin();
                EffectPassCollection passes = technique.Passes;
                for (int i = 0; i < passes.Count; i++)
                {
                    EffectPass pass = passes[i];
                    pass.Begin();
                    Device.DrawUserIndexedPrimitives<_2DSpriteVertex>(
                        PrimitiveType.TriangleList, verticies, 0, verticies.Length,
                        indices, 0, indices.Length / 3);
                    pass.End();
                }
                effect.End();
            }
        }

        private Vector2 GetUV(Texture2D Texture, float x, float y)
        {
            return new Vector2(x / (float)Texture.Width, y / (float)Texture.Height);
        }

        public void ResetMatrices(int width, int height)
        {
            this.World = Matrix.Identity;
            this.View = new Matrix(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, -1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            this.Projection = Matrix.CreateOrthographicOffCenter(
                0, width, -height, 0, 0, 1);
        }

        


        private Dictionary<ITextureProvider, Texture2D> _TextureCache = new Dictionary<ITextureProvider, Texture2D>();
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
            lock (_WorldTextureCache){
                if (_WorldTextureCache.ContainsKey(item))
                {
                    return _WorldTextureCache[item];
                }
                var texture = item.GetWorldTexture(this.Device);
                _WorldTextureCache.Add(item, texture);
                return texture;
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
                GD.SetRenderTarget(0, Target);
                GD.SetRenderTarget(1, DepthTarget); //render to multiple targets, 0 is color, 1 is depth!
                GD.Clear(Color.TransparentBlack);
                Batch.OutputDepth = true;
                Batch.Resume();

                Pass++;
                return true;
            }
            return false ;
        }

        protected void ExtractDepthTexture()
        {
            var texture = DepthTarget.GetTexture();
            DepthTexture.SetValue(texture);
        }

        public override void Dispose()
        {
            Batch.Pause();
            Batch.OutputDepth = false;
            GD.SetRenderTarget(0, null);
            GD.SetRenderTarget(1, null); //need to unbind both before we can extract their textures.
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
                GD.SetRenderTarget(0, Target);
                GD.Clear(Color.TransparentBlack);
                Batch.Resume();

                Pass++;
                return true;
            }
            return false;
        }

        protected void ExtractPixelTexture()
        {
            var texture = Target.GetTexture();
            Texture.SetValue(texture);
        }

        #region IDisposable Members

        public virtual void Dispose(){
            Batch.Pause();
            GD.SetRenderTarget(0, null);
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
