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
using FSO.Content;
using Microsoft.Xna.Framework;
using System.IO;
using FSO.LotView.Utils;
using FSO.LotView.Model;
using FSO.Common;
using FSO.Common.Utils;

namespace FSO.LotView.Components
{
    public class TerrainComponent : WorldComponent
    {
        private Rectangle Size;

        private int GeomLength;
        private float[] GrassState; //0 = green, 1 = brown. to start with, should be randomly distriuted in range 0-0.5.
        private int NumPrimitives;
        private int BladePrimitives;
        private IndexBuffer IndexBuffer;
        private IndexBuffer BladeIndexBuffer;
        private VertexBuffer VertexBuffer;

        private LotTypes LotType;
        private Color LightGreen = new Color(80, 116, 59);
        private Color LightBrown = new Color(157, 117, 65);
        private Color DarkGreen = new Color(8, 52, 8);
        private Color DarkBrown = new Color(81, 60, 18);
        private int GrassHeight;
        private float GrassDensityScale = 1f;
        public bool DepthMode;

        private Effect Effect;
        public bool DrawGrid = false;

        public TerrainComponent(Rectangle size){
            this.Size = size;
            this.Effect = WorldContent.GrassEffect;
            LotType = LotTypes.Grass; //(LotTypes)(new Random()).Next(4);

            UpdateLotType();
            GenerateGrassStates();
        }

        public void UpdateLotType()
        {
            int index = (int)LotType;
            LightGreen = LotTypeGrassInfo.LightGreen[index];
            LightBrown = LotTypeGrassInfo.LightBrown[index];
            DarkGreen = LotTypeGrassInfo.DarkGreen[index];
            DarkBrown = LotTypeGrassInfo.DarkBrown[index];
            GrassHeight = LotTypeGrassInfo.Heights[index];
            if (!FSOEnvironment.UseMRT) GrassHeight /= 2;
            GrassDensityScale = LotTypeGrassInfo.GrassDensity[index];
        }

        public override float PreferredDrawOrder
        {
            get { return 0.0f; }
        }

        public void RegenTerrain(GraphicsDevice device, WorldState world, Blueprint blueprint)
        {
            if (VertexBuffer != null)
            {
                IndexBuffer.Dispose();
                BladeIndexBuffer.Dispose();
                VertexBuffer.Dispose();
            }

            /** Convert rectangle to world units **/
            var quads = Size.Width;

            var quadWidth = WorldSpace.GetWorldFromTile((float)Size.Width / (float)quads);
            var quadHeight = WorldSpace.GetWorldFromTile((float)Size.Height / (float)quads);
            var numQuads = quads * quads;

            TerrainVertex[] Geom = new TerrainVertex[numQuads * 4];
            int[] Indexes = new int[numQuads * 6];
            int[] BladeIndexes = new int[numQuads * 6];
            NumPrimitives = (numQuads * 2);

            int geomOffset = 0;
            int indexOffset = 0;
            int bindexOffset = 0;

            var offsetX = WorldSpace.GetWorldFromTile(Size.X);
            var offsetY = WorldSpace.GetWorldFromTile(Size.Y);

            for (var y = 0; y < quads; y++)
            {
                for (var x = 0; x < quads; x++)
                {
                    var tl = new Vector3(offsetX + (x * quadWidth), 0.0f, offsetY + (y * quadHeight));
                    var tr = new Vector3(tl.X + quadWidth, 0.0f, tl.Z);
                    var bl = new Vector3(tl.X, 0.0f, tl.Z + quadHeight);
                    var br = new Vector3(tl.X + quadWidth, 0.0f, tl.Z + quadHeight);

                    Indexes[indexOffset++] = geomOffset;
                    Indexes[indexOffset++] = (geomOffset + 1);
                    Indexes[indexOffset++] = (geomOffset + 2);

                    Indexes[indexOffset++] = (geomOffset + 2);
                    Indexes[indexOffset++] = (geomOffset + 3);
                    Indexes[indexOffset++] = geomOffset;

                    short tx = (short)(x + 1), ty = (short)(y + 1);

                    if (blueprint.GetFloor(tx,ty,1).Pattern == 0 && 
                        (blueprint.GetWall(tx,ty, 1).Segments & (WallSegments.HorizontalDiag | WallSegments.VerticalDiag)) == 0)
                    {
                        BladeIndexes[bindexOffset++] = geomOffset;
                        BladeIndexes[bindexOffset++] = (geomOffset + 1);
                        BladeIndexes[bindexOffset++] = (geomOffset + 2);

                        BladeIndexes[bindexOffset++] = (geomOffset + 2);
                        BladeIndexes[bindexOffset++] = (geomOffset + 3);
                        BladeIndexes[bindexOffset++] = geomOffset;
                    }

                    Color tlCol = Color.Lerp(LightGreen, LightBrown, GrassState[y * quads + x]);
                    Color trCol = Color.Lerp(LightGreen, LightBrown, GrassState[y * quads + ((x + 1) % quads)]);
                    Color blCol = Color.Lerp(LightGreen, LightBrown, GrassState[((y + 1) % quads) * quads + x]);
                    Color brCol = Color.Lerp(LightGreen, LightBrown, GrassState[((y + 1) % quads) * quads + ((x + 1) % quads)]);

                    Geom[geomOffset++] = new TerrainVertex(tl, tlCol.ToVector4(), new Vector2(x * 64, y * 64), GrassState[y * quads + x]);
                    Geom[geomOffset++] = new TerrainVertex(tr, trCol.ToVector4(), new Vector2((x + 1) * 64, y * 64), GrassState[y * quads + ((x + 1) % quads)]);
                    Geom[geomOffset++] = new TerrainVertex(br, brCol.ToVector4(), new Vector2((x + 1) * 64, (y + 1) * 64), GrassState[((y + 1) % quads) * quads + ((x + 1) % quads)]);
                    Geom[geomOffset++] = new TerrainVertex(bl, blCol.ToVector4(), new Vector2(x * 64, (y + 1) * 64), GrassState[((y + 1) % quads) * quads + x]);
                }
            }

            var rand = new Random();

            VertexBuffer = new VertexBuffer(device, typeof(TerrainVertex), Geom.Length, BufferUsage.None);
            VertexBuffer.SetData(Geom);

            IndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * Indexes.Length, BufferUsage.None);
            IndexBuffer.SetData(Indexes);

            BladePrimitives = (bindexOffset / 3);

            BladeIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * Indexes.Length, BufferUsage.None);
            BladeIndexBuffer.SetData(BladeIndexes);
            GeomLength = Geom.Length;
        }

        /// <summary>
        /// Setup component to run on graphics device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="world"></param>
        public override void Initialize(GraphicsDevice device, WorldState world)
        {
            base.Initialize(device, world);
        }

        public void GenerateGrassStates() //generates a set of grass states for a lot.
        {
            //right now only works for square lots, but that's all tso has!
            var random = new Random();
            int width = Size.Width;
            float[] result = new float[width * width];
            int initial = width/4; //divide by more for less noisyness!
            float factor = 0.5f/((int)Math.Log(initial, 2));
            while (initial > 0)
            {
                var squared = initial * initial;
                var noise = new float[squared];
                for (int i = 0; i < squared; i++) noise[i] = (float)random.NextDouble()*factor;

                int offset = 0;
                for (int x = 0; x < width; x++)
                {
                    double xInt = (x / (double)(width-1)) * (initial-1);
                    for (int y = 0; y < width; y++)
                    {
                        double yInt = (y / (double)(width - 1)) * (initial - 1);
                        float tl = noise[(int)(Math.Floor(yInt)*initial+Math.Floor(xInt))];
                        float tr = noise[(int)(Math.Floor(yInt) * initial + Math.Ceiling(xInt))];
                        float bl = noise[(int)(Math.Ceiling(yInt) * initial + Math.Floor(xInt))];
                        float br = noise[(int)(Math.Ceiling(yInt) * initial + Math.Ceiling(xInt))];
                        float p = (float)(xInt%1.0);
                        float q = (float)(yInt%1.0);
                        result[offset++] += (tl * (1 - p) + tr * (p)) * (1 - q) + (bl * (1 - p) + br * (p)) * q; //don't you love 2 dimensional linear interpolation?? ;)
                    }
                }
                initial /= 2;
            }
            GrassState = result;
        }

        /// <summary>
        /// Render the terrain
        /// </summary>
        /// <param name="device"></param>
        /// <param name="world"></param>
        public override void Draw(GraphicsDevice device, WorldState world) {

            if (VertexBuffer == null) return;
            PPXDepthEngine.RenderPPXDepth(Effect, true, (depthMode) =>
            {
                Effect.Parameters["LightGreen"].SetValue(LightGreen.ToVector4());
                Effect.Parameters["DarkGreen"].SetValue(DarkGreen.ToVector4());
                Effect.Parameters["DarkBrown"].SetValue(DarkBrown.ToVector4());
                Effect.Parameters["LightBrown"].SetValue(LightBrown.ToVector4());
                Effect.Parameters["ScreenSize"].SetValue(new Vector2(device.Viewport.Width, device.Viewport.Height));
                //Effect.Parameters["depthOutMode"].SetValue(DepthMode && (!FSOEnvironment.UseMRT));

                var offset = -world.WorldSpace.GetScreenOffset();

                world._3D.ApplyCamera(Effect);
                var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, ((world.Zoom == WorldZoom.Far) ? -5 : ((world.Zoom == WorldZoom.Medium) ? -4 : -3)) * (20 / 522f), 0);
                Effect.Parameters["World"].SetValue(worldmat);

                Effect.Parameters["DiffuseColor"].SetValue(new Vector4(world.OutsideColor.R / 255f, world.OutsideColor.G / 255f, world.OutsideColor.B / 255f, 1.0f));

                device.SetVertexBuffer(VertexBuffer);
                device.Indices = IndexBuffer;

                Effect.CurrentTechnique = Effect.Techniques["DrawBase"];
                foreach (var pass in Effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, NumPrimitives);
                }

                int grassScale;
                float grassDensity;
                switch (world.Zoom)
                {
                    case WorldZoom.Far:
                        grassScale = 4;
                        grassDensity = 0.56f;
                        break;
                    case WorldZoom.Medium:
                        grassScale = 2;
                        grassDensity = 0.50f;
                        break;
                    default:
                        grassScale = 1;
                        grassDensity = 0.43f;
                        break;
                }

                grassDensity *= GrassDensityScale;

                if (BladePrimitives > 0)
                {
                    Effect.CurrentTechnique = Effect.Techniques["DrawBlades"];
                    int grassNum = (int)Math.Ceiling(GrassHeight / (float)grassScale);

                    //if (depthMode && (!FSOEnvironment.UseMRT)) return;
                    RenderTargetBinding[] rts = null;
                    if (FSOEnvironment.UseMRT)
                    {
                        rts = device.GetRenderTargets();
                        if (rts.Length > 1)
                        {
                            device.SetRenderTarget((RenderTarget2D)rts[0].RenderTarget);
                        }
                    }
                    device.Indices = BladeIndexBuffer;
                    for (int i = 0; i < grassNum; i++)
                    {
                        Effect.Parameters["World"].SetValue(Matrix.Identity * Matrix.CreateTranslation(0, i * (20 / 522f) * grassScale, 0));
                        Effect.Parameters["GrassProb"].SetValue(grassDensity * ((grassNum - (i / (2f * grassNum))) / (float)grassNum));
                        offset += new Vector2(0, 1);
                        Effect.Parameters["ScreenOffset"].SetValue(offset);

                        foreach (var pass in Effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, BladePrimitives);
                        }
                    }
                    if (FSOEnvironment.UseMRT)
                    {
                        device.SetRenderTargets(rts);
                    }
                }
            });
        }
    }
}
