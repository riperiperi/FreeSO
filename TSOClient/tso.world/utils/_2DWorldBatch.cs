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
        
        protected List<_2DSprite> Sprites = new List<_2DSprite>();
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
            Sprites.Add(sprite);
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
            this.Sprites.Clear();
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
        //public RenderTarget2D GetBuffer()
        //{
        //    if (Buffers.Count > 0)
        //    {
        //        var item = Buffers[0];
        //        Buffers.RemoveAt(0);
        //        return item;
        //    }
        //    return null;
        //}

        //public void FreeBuffer(RenderTarget2D buffer)
        //{
        //    Buffers.Add(buffer);
        //}

        /// <summary>
        /// Processes the acculimated draw commands and paints the screen
        /// </summary>
        public void End(){
            var color = Color.White;
            var declaration = new VertexDeclaration(Device, _2DSpriteVertex.VertexElements);
            Device.VertexDeclaration = declaration;
            
            var effect = this.Effect;
            
            //  set the only parameter this effect takes.
            effect.Parameters["viewProjection"].SetValue(this.View * this.Projection);
            effect.Parameters["worldViewProjection"].SetValue(this.WorldCamera.View * this.WorldCamera.Projection);
            effect.CommitChanges();

            if (OutputDepth)
            {
                var spritesWithDepth = Sprites.Where(x => x.RenderMode == _2DBatchRenderMode.Z_BUFFER).ToList();
                RenderSpriteList(spritesWithDepth, effect, effect.Techniques["drawZSpriteDepthChannel"]);
            }
            else
            {
                /**
                 * Render the no depth items first
                 */
                string test;
                if (OBJIDMode) test = "why";
                var spritesWithNoDepth = Sprites.Where(x => x.RenderMode == _2DBatchRenderMode.NO_DEPTH).ToList();
                RenderSpriteList(spritesWithNoDepth, effect, effect.Techniques[(OBJIDMode)?"drawSimpleID":"drawSimple"]);

                var spritesWithDepth = Sprites.Where(x => x.RenderMode == _2DBatchRenderMode.Z_BUFFER).ToList();
                RenderSpriteList(spritesWithDepth, effect, effect.Techniques[(OBJIDMode) ? "drawZSpriteOBJID" : "drawZSprite"]);

                var spritesWithRestoreDepth = Sprites.Where(x => x.RenderMode == _2DBatchRenderMode.RESTORE_DEPTH).ToList();
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
            var map = new Dictionary<Texture2D, _2DSpriteTextureGroup>();

            foreach (var sprite in sprites){
                if (!map.ContainsKey(sprite.Pixel))
                {
                    var grouping = new _2DSpriteTextureGroup
                    {
                        Pixel = sprite.Pixel,
                        Depth = sprite.Depth
                    };
                    grouping.Sprites.Add(sprite);
                    map.Add(sprite.Pixel, grouping);
                    result.Add(grouping);
                }
                else
                {
                    map[sprite.Pixel].Sprites.Add(sprite);
                }
            }
            return result;
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
                    //effect.Parameters["pixelTexture"].SetValue(depth);
                    effect.Parameters["depthTexture"].SetValue(depth);
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

                    if (sprite.FlipHorizontally)
                    {
                        verticies[vertexCount++] = new _2DSpriteVertex(
                            new Vector3(dstRectangle.Left, dstRectangle.Top, 0)
                            , GetUV(texture, srcRectangle.Right, srcRectangle.Top), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                        verticies[vertexCount++] = new _2DSpriteVertex(
                            new Vector3(dstRectangle.Right, dstRectangle.Top, 0)
                            , GetUV(texture, srcRectangle.Left, srcRectangle.Top), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                        verticies[vertexCount++] = new _2DSpriteVertex(
                            new Vector3(dstRectangle.Right, dstRectangle.Bottom, 0)
                            , GetUV(texture, srcRectangle.Left, srcRectangle.Bottom), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                        verticies[vertexCount++] = new _2DSpriteVertex(
                            new Vector3(dstRectangle.Left, dstRectangle.Bottom, 0)
                            , GetUV(texture, srcRectangle.Right, srcRectangle.Bottom), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                    }
                    else
                    {
                        verticies[vertexCount++] = new _2DSpriteVertex(
                            new Vector3(dstRectangle.Left, dstRectangle.Top, 0)
                            , GetUV(texture, srcRectangle.Left, srcRectangle.Top + 0.5f), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                        verticies[vertexCount++] = new _2DSpriteVertex(
                            new Vector3(dstRectangle.Right, dstRectangle.Top, 0)
                            , GetUV(texture, srcRectangle.Right, srcRectangle.Top + 0.5f), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                        verticies[vertexCount++] = new _2DSpriteVertex(
                            new Vector3(dstRectangle.Right, dstRectangle.Bottom, 0)
                            , GetUV(texture, srcRectangle.Right, srcRectangle.Bottom), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                        verticies[vertexCount++] = new _2DSpriteVertex(
                            new Vector3(dstRectangle.Left, dstRectangle.Bottom, 0)
                            , GetUV(texture, srcRectangle.Left, srcRectangle.Bottom), sprite.AbsoluteWorldPosition, (Single)sprite.ObjectID);
                    }
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
                return base.NextPass();
            }else if (Pass == 1){
                Pass++;
                Batch.Pause();
                GD.SetRenderTarget(0, DepthTarget);
                GD.Clear(Color.TransparentBlack);
                Batch.Resume();
                Batch.OutputDepth = true;
                return true;
            }
            return base.NextPass();
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
        RESTORE_DEPTH
    }

    public class _2DSpriteTextureGroup
    {
        public Texture2D Pixel;
        public Texture2D Depth;
        public List<_2DSprite> Sprites = new List<_2DSprite>();
    }
}
