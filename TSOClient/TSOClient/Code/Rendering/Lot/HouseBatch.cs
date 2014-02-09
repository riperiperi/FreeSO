using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.Code.Rendering.Lot.Framework;

namespace TSOClient.Code.Rendering.Lot
{
    /// <summary>
    /// Similar to SpriteBatch but more fit for purpose
    /// RE z-buffers
    /// </summary>
    public class HouseBatch
    {
        //private VertexPositionColorTexture[] Vertices;
        //private short[] Indices;
        //private int VertexCount = 0;
        //private int IndexCount = 0;
        //private VertexDeclaration Declaration;
        private GraphicsDevice Device;
        //private Texture2D Texture;
        //private Texture2D ZTexture;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;
        public Effect Effect;

        public HouseBatch(GraphicsDevice gd)
        {
            this.Device = gd;

            //this.Vertices = new VertexPositionColorTexture[256];
            //this.Indices = new short[Vertices.Length * 3 / 2];

            ResetMatrices(GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
            Effect = GameFacade.Game.Content.Load<Effect>("Effects/HouseBatch");
        }





        private int DrawOrder;
        private List<HouseBatchSprite> Sprites = new List<HouseBatchSprite>();

        public void Begin()
        {
            DrawOrder = 0;
            Sprites.Clear();
        }

        public void Draw(HouseBatchSprite sprite){
            sprite.DrawOrder = DrawOrder++;
            Sprites.Add(sprite);
        }


        public void End()
        {
            var color = Color.White;

            var declaration = new VertexDeclaration(Device, VertexPositionColorTexture.VertexElements);
            Device.VertexDeclaration = declaration;

            var effect = this.Effect;

            //  set the only parameter this effect takes.
            effect.Parameters["viewProjection"].SetValue(this.View * this.Projection);

            /**
             * Flush the sprites to the screen
             */
            Sprites.Sort(new HouseBatchSorter<HouseBatchSprite>());

            /** Group by texture **/
            var groupByTexture = Sprites.GroupBy(x => new { Pixel = x.Pixel, Depth = x.Depth, Mode = x.RenderMode });
            foreach (var group in groupByTexture){
                var texture = group.Key.Pixel;
                var depth = group.Key.Depth;
                var mode = group.Key.Mode;
                var numSprites = group.Count();

                effect.Parameters["diffuseTexture"].SetValue(texture);
                if (depth != null){
                    effect.Parameters["depthTexture"].SetValue(depth);
                }

                EffectTechnique technique = null;
                switch (mode)
                {
                    case HouseBatchRenderMode.NO_DEPTH:
                        technique = effect.Techniques["drawSimple"];
                        break;

                    case HouseBatchRenderMode.Z_BUFFER:
                        technique = effect.Techniques["drawWithDepth"];
                        break;
                }

                /** Build vertex data **/
                var verticies = new VertexPositionColorTexture[4 * numSprites];
                var indices = new short[6 * numSprites];
                var indexCount = 0;
                var vertexCount = 0;

                foreach (var sprite in group){

                    var srcRectangle = sprite.SrcRect;
                    var dstRectangle = sprite.DestRect;

                    indices[indexCount++] = (short)(vertexCount + 0);
                    indices[indexCount++] = (short)(vertexCount + 1);
                    indices[indexCount++] = (short)(vertexCount + 3);
                    indices[indexCount++] = (short)(vertexCount + 1);
                    indices[indexCount++] = (short)(vertexCount + 2);
                    indices[indexCount++] = (short)(vertexCount + 3);
                    // add the new vertices

                    verticies[vertexCount++] = new VertexPositionColorTexture(
                        new Vector3(dstRectangle.Left, dstRectangle.Top, 0)
                        , color, GetUV(texture, srcRectangle.Left, srcRectangle.Top));
                    verticies[vertexCount++] = new VertexPositionColorTexture(
                        new Vector3(dstRectangle.Right, dstRectangle.Top, 0)
                        , color, GetUV(texture, srcRectangle.Right, srcRectangle.Top));
                    verticies[vertexCount++] = new VertexPositionColorTexture(
                        new Vector3(dstRectangle.Right, dstRectangle.Bottom, 0)
                        , color, GetUV(texture, srcRectangle.Right, srcRectangle.Bottom));
                    verticies[vertexCount++] = new VertexPositionColorTexture(
                        new Vector3(dstRectangle.Left, dstRectangle.Bottom, 0)
                        , color, GetUV(texture, srcRectangle.Left, srcRectangle.Bottom));
                }

                effect.CurrentTechnique = technique;
                effect.Begin();
                EffectPassCollection passes = technique.Passes;
                for (int i = 0; i < passes.Count; i++)
                {
                    EffectPass pass = passes[i];
                    pass.Begin();
                    Device.DrawUserIndexedPrimitives<VertexPositionColorTexture>(
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


        //public void DrawZ(Texture2D texture, Texture2D zbuffer, Rectangle dstRectangle, Color color)
        //{
        //    DrawZ(texture, zbuffer, new Rectangle(0, 0, texture.Width, texture.Height), dstRectangle, color);
        //}

        //public void DrawZ(Texture2D texture, Texture2D zbuffer, Rectangle srcRectangle, Rectangle dstRectangle, Color color)
        //{
        //    //  if the texture changes, we flush all queued sprites.
        //    if ((this.Texture != null && this.Texture != texture) ||
        //        (this.ZTexture != null && this.ZTexture != zbuffer))
        //        this.Flush();
        //    this.Texture = texture;
        //    this.ZTexture = zbuffer;

        //    //  ensure space for my vertices and indices.
        //    this.EnsureSpace(6, 4);

        //    //  add the new indices
        //    Indices[IndexCount++] = (short)(VertexCount + 0);
        //    Indices[IndexCount++] = (short)(VertexCount + 1);
        //    Indices[IndexCount++] = (short)(VertexCount + 3);
        //    Indices[IndexCount++] = (short)(VertexCount + 1);
        //    Indices[IndexCount++] = (short)(VertexCount + 2);
        //    Indices[IndexCount++] = (short)(VertexCount + 3);

        //    // add the new vertices
        //    Vertices[VertexCount++] = new VertexPositionColorTexture(
        //        new Vector3(dstRectangle.Left, dstRectangle.Top, 0)
        //        , color, GetUV(srcRectangle.Left, srcRectangle.Top));
        //    Vertices[VertexCount++] = new VertexPositionColorTexture(
        //        new Vector3(dstRectangle.Right, dstRectangle.Top, 0)
        //        , color, GetUV(srcRectangle.Right, srcRectangle.Top));
        //    Vertices[VertexCount++] = new VertexPositionColorTexture(
        //        new Vector3(dstRectangle.Right, dstRectangle.Bottom, 0)
        //        , color, GetUV(srcRectangle.Right, srcRectangle.Bottom));
        //    Vertices[VertexCount++] = new VertexPositionColorTexture(
        //        new Vector3(dstRectangle.Left, dstRectangle.Bottom, 0)
        //        , color, GetUV(srcRectangle.Left, srcRectangle.Bottom));

        //    //  we premultiply all vertices times the world matrix.
        //    //  the world matrix changes alot and we don't want to have to flush
        //    //  every time it changes.
        //    Matrix world = this.World;
        //    for (int i = VertexCount - 4; i < VertexCount; i++)
        //        Vector3.Transform(ref Vertices[i].Position, ref world, out Vertices[i].Position);
        //}










        //public void Draw(Texture2D texture, Rectangle dstRectangle, Color color)
        //{
        //    Draw(texture, new Rectangle(0, 0, texture.Width, texture.Height), dstRectangle, color);
        //}

        //public void Draw(Texture2D texture, Rectangle srcRectangle, Rectangle dstRectangle, Color color)
        //{
        //    //  if the texture changes, we flush all queued sprites.
        //    if (this.Texture != null && this.Texture != texture)
        //        this.Flush();
        //    this.Texture = texture;
        //    this.ZTexture = null;

        //    //  ensure space for my vertices and indices.
        //    this.EnsureSpace(6, 4);

        //    //  add the new indices
        //    Indices[IndexCount++] = (short)(VertexCount + 0);
        //    Indices[IndexCount++] = (short)(VertexCount + 1);
        //    Indices[IndexCount++] = (short)(VertexCount + 3);
        //    Indices[IndexCount++] = (short)(VertexCount + 1);
        //    Indices[IndexCount++] = (short)(VertexCount + 2);
        //    Indices[IndexCount++] = (short)(VertexCount + 3);

        //    // add the new vertices
        //    Vertices[VertexCount++] = new VertexPositionColorTexture(
        //        new Vector3(dstRectangle.Left, dstRectangle.Top, 0)
        //        , color, GetUV(srcRectangle.Left, srcRectangle.Top));
        //    Vertices[VertexCount++] = new VertexPositionColorTexture(
        //        new Vector3(dstRectangle.Right, dstRectangle.Top, 0)
        //        , color, GetUV(srcRectangle.Right, srcRectangle.Top));
        //    Vertices[VertexCount++] = new VertexPositionColorTexture(
        //        new Vector3(dstRectangle.Right, dstRectangle.Bottom, 0)
        //        , color, GetUV(srcRectangle.Right, srcRectangle.Bottom));
        //    Vertices[VertexCount++] = new VertexPositionColorTexture(
        //        new Vector3(dstRectangle.Left, dstRectangle.Bottom, 0)
        //        , color, GetUV(srcRectangle.Left, srcRectangle.Bottom));

        //    //  we premultiply all vertices times the world matrix.
        //    //  the world matrix changes alot and we don't want to have to flush
        //    //  every time it changes.
        //    Matrix world = this.World;
        //    for (int i = VertexCount - 4; i < VertexCount; i++)
        //        Vector3.Transform(ref Vertices[i].Position, ref world, out Vertices[i].Position);
        //}

        //private Vector2 GetUV(float x, float y)
        //{
        //    return new Vector2(x / (float)Texture.Width, y / (float)Texture.Height);
        //}

        //private void EnsureSpace(int indexSpace, int vertexSpace)
        //{
        //    if (IndexCount + indexSpace >= Indices.Length)
        //        Array.Resize(ref Indices, Math.Max(IndexCount + indexSpace, Indices.Length * 2));
        //    if (VertexCount + vertexSpace >= Vertices.Length)
        //        Array.Resize(ref Vertices, Math.Max(VertexCount + vertexSpace, Vertices.Length * 2));
        //}


        //public void Flush()
        //{
        //    if (ZTexture != null) { this.FlushZ(); return; }

        //    if (this.VertexCount > 0)
        //    {
        //        if (this.Declaration == null || this.Declaration.IsDisposed)
        //            this.Declaration = new VertexDeclaration(Device, VertexPositionColorTexture.VertexElements);

        //        Device.VertexDeclaration = this.Declaration;

        //        Effect effect = this.Effect;
        //        //  set the only parameter this effect takes.
        //        effect.Parameters["viewProjection"].SetValue(this.View * this.Projection);
        //        effect.Parameters["diffuseTexture"].SetValue(this.Texture);

        //        EffectTechnique technique = effect.CurrentTechnique;
        //        effect.Begin();
        //        EffectPassCollection passes = technique.Passes;
        //        for (int i = 0; i < passes.Count; i++)
        //        {
        //            EffectPass pass = passes[i];
        //            pass.Begin();

        //            Device.DrawUserIndexedPrimitives<VertexPositionColorTexture>(
        //                PrimitiveType.TriangleList, this.Vertices, 0, this.VertexCount,
        //                this.Indices, 0, this.IndexCount / 3);

        //            pass.End();
        //        }
        //        effect.End();

        //        this.VertexCount = 0;
        //        this.IndexCount = 0;
        //    }
        //}



        //public void FlushZ()
        //{
        //    if (this.VertexCount > 0)
        //    {
        //        if (this.Declaration == null || this.Declaration.IsDisposed)
        //            this.Declaration = new VertexDeclaration(Device, VertexPositionColorTexture.VertexElements);

        //        Device.VertexDeclaration = this.Declaration;

        //        Effect effect = this.Effect;
        //        //  set the only parameter this effect takes.
        //        effect.Parameters["viewProjection"].SetValue(this.View * this.Projection);
        //        effect.Parameters["diffuseTexture"].SetValue(this.Texture);
        //        effect.Parameters["depthTexture"].SetValue(this.ZTexture);

        //        EffectTechnique technique = effect.Techniques["PaintDepth"];
        //        effect.Begin();
        //        EffectPassCollection passes = technique.Passes;
        //        for (int i = 0; i < passes.Count; i++)
        //        {
        //            EffectPass pass = passes[i];
        //            pass.Begin();

        //            Device.DrawUserIndexedPrimitives<VertexPositionColorTexture>(
        //                PrimitiveType.TriangleList, this.Vertices, 0, this.VertexCount,
        //                this.Indices, 0, this.IndexCount / 3);

        //            pass.End();
        //        }
        //        effect.End();

        //        this.VertexCount = 0;
        //        this.IndexCount = 0;
        //    }
        //}

    }

    public enum ZBlitMode
    {
        /** Writes the z value but does not do a comparason, useful for baloons etc **/
        WriteOnly,
        /** Does a Z <= compare **/
        ReadWrite
    }
}
