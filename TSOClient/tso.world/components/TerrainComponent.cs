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
using FSO.Content.Model;
using FSO.Common;
using FSO.Common.Utils;

namespace FSO.LotView.Components
{
    public class TerrainComponent : WorldComponent
    {
        private Rectangle Size;

        private int GeomLength;
        private byte[] GrassState; //0 = green, 255 = brown. to start with, should be randomly distriuted in range 0-128.
        private int NumPrimitives;
        private int BladePrimitives;
        private int GridPrimitives;
        private int TGridPrimitives;
        private IndexBuffer IndexBuffer;
        private IndexBuffer BladeIndexBuffer;
        private IndexBuffer GridIndexBuffer;
        private IndexBuffer TGridIndexBuffer;
        private VertexBuffer VertexBuffer;

        private TerrainType LightType = TerrainType.GRASS;
        private TerrainType DarkType = TerrainType.GRASS;

        private Color LightGreen = new Color(80, 116, 59);
        private Color LightBrown = new Color(157, 117, 65);
        private Color DarkGreen = new Color(8, 52, 8);
        private Color DarkBrown = new Color(81, 60, 18);
        private int GrassHeight;
        private float GrassDensityScale = 1f;
        public bool DepthMode;

        private Effect Effect;
        public bool DrawGrid = false;
        public bool TerrainDirty = true;
        private Blueprint Bp;

        public TerrainComponent(Rectangle size, Blueprint blueprint){
            this.Size = size;
            this.Effect = WorldContent.GrassEffect;
            this.Bp = blueprint;

            UpdateLotType();
        }

        public void UpdateTerrain(TerrainType light, TerrainType dark, sbyte[] heights, byte[] grass)
        {
            LightType = light;
            DarkType = dark;
            GrassState = grass;
            UpdateLotType();
            TerrainDirty = true;
        }

        public void UpdateLotType()
        {
            int index = (int)LightType;
            LightGreen = LotTypeGrassInfo.LightGreen[index];
            DarkGreen = LotTypeGrassInfo.DarkGreen[index];
            if (LightType != DarkType)
            {
                var dindex = (int)DarkType;
                LightBrown = LotTypeGrassInfo.LightGreen[dindex];
                DarkBrown = LotTypeGrassInfo.DarkGreen[dindex];
            }
            else
            {
                LightBrown = LotTypeGrassInfo.LightBrown[index];
                DarkBrown = LotTypeGrassInfo.DarkBrown[index];
            }
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
            if (GrassState == null)
            {
                TerrainDirty = true; //yikes! try again to see if we have it next frame
                return;
            }
            TerrainDirty = false;
            if (VertexBuffer != null)
            {
                IndexBuffer.Dispose();
                BladeIndexBuffer.Dispose();
                VertexBuffer.Dispose();
                GridIndexBuffer?.Dispose();
                TGridIndexBuffer?.Dispose();
            }

            /** Convert rectangle to world units **/
            var quads = Size.Width;

            var quadWidth = WorldSpace.GetWorldFromTile((float)Size.Width / (float)quads);
            var quadHeight = WorldSpace.GetWorldFromTile((float)Size.Height / (float)quads);
            var numQuads = quads * quads;
            var archSize = quads + 2;

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

                    Color tlCol = Color.Lerp(LightGreen, LightBrown, GrassState[y * archSize + x]/255f);
                    Color trCol = Color.Lerp(LightGreen, LightBrown, GrassState[y * archSize + ((x + 1) % archSize)] / 255f);
                    Color blCol = Color.Lerp(LightGreen, LightBrown, GrassState[((y + 1) % archSize) * archSize + x] / 255f);
                    Color brCol = Color.Lerp(LightGreen, LightBrown, GrassState[((y + 1) % archSize) * archSize + ((x + 1) % archSize)] / 255f);

                    Geom[geomOffset++] = new TerrainVertex(tl, tlCol.ToVector4(), new Vector2(x * 64, y * 64), GrassState[y * archSize + x] / 255f);
                    Geom[geomOffset++] = new TerrainVertex(tr, trCol.ToVector4(), new Vector2((x + 1) * 64, y * 64), GrassState[y * archSize + ((x + 1) % archSize)] / 255f);
                    Geom[geomOffset++] = new TerrainVertex(br, brCol.ToVector4(), new Vector2((x + 1) * 64, (y + 1) * 64), GrassState[((y + 1) % archSize) * archSize + ((x + 1) % archSize)] / 255f);
                    Geom[geomOffset++] = new TerrainVertex(bl, blCol.ToVector4(), new Vector2(x * 64, (y + 1) * 64), GrassState[((y + 1) % archSize) * archSize + x] / 255f);
                }
            }

            var GridIndices = GetGridIndicesForArea(Bp.BuildableArea, quads);
            var TGridIndices = GetGridIndicesForArea(Bp.TargetBuildableArea, quads);

            VertexBuffer = new VertexBuffer(device, typeof(TerrainVertex), Geom.Length, BufferUsage.None);
            VertexBuffer.SetData(Geom);

            IndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * Indexes.Length, BufferUsage.None);
            IndexBuffer.SetData(Indexes);

            BladePrimitives = (bindexOffset / 3);

            BladeIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * Indexes.Length, BufferUsage.None);
            BladeIndexBuffer.SetData(BladeIndexes);
            GeomLength = Geom.Length;

            if (GridIndices.Length > 0)
            {
                GridIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * GridIndices.Length, BufferUsage.None);
                GridIndexBuffer.SetData(GridIndices);
                GridPrimitives = GridIndices.Length / 2;
            }

            if (TGridIndices.Length > 0)
            {
                TGridIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * TGridIndices.Length, BufferUsage.None);
                TGridIndexBuffer.SetData(TGridIndices);
                TGridPrimitives = TGridIndices.Length / 2;
            }
        }

        private int[] GetGridIndicesForArea(Rectangle area, int quads)
        {
            var gridIndexOff = 0;
            var w = area.Width;
            var h = area.Height;
            var ox = area.X - 1;
            var oy = area.Y - 1;
            int[] GridIndices = new int[(w * h) * 4 + (w + h) * 4]; //top left top right for all tiles, then the lines at the ends on bottom sides. Note that we need line start and endpoints.

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var tileOff = ((y + oy) * quads + x + ox) * 4;
                    GridIndices[gridIndexOff++] = tileOff;
                    GridIndices[gridIndexOff++] = tileOff + 1;
                    GridIndices[gridIndexOff++] = tileOff;
                    GridIndices[gridIndexOff++] = tileOff + 3;

                    if (x == w - 1)
                    {
                        GridIndices[gridIndexOff++] = tileOff + 1; //+x
                        GridIndices[gridIndexOff++] = tileOff + 2; //+x+y
                    }
                    if (y == h - 1)
                    {
                        GridIndices[gridIndexOff++] = tileOff + 3; //+y
                        GridIndices[gridIndexOff++] = tileOff + 2; //+x+y
                    }
                }
            }
            return GridIndices;
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

        /// <summary>
        /// Render the terrain
        /// </summary>
        /// <param name="device"></param>
        /// <param name="world"></param>
        public override void Draw(GraphicsDevice device, WorldState world){
            if (TerrainDirty) RegenTerrain(device, world, Bp);
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
                var translation = ((world.Zoom == WorldZoom.Far) ? -7 : ((world.Zoom == WorldZoom.Medium) ? -5 : -3)) * (20 / 522f);
                if (world.PreciseZoom < 1) translation /= world.PreciseZoom;
                else translation *= world.PreciseZoom;
                var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, translation, 0);
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

                if (GridPrimitives > 0 && world.BuildMode > 0)
                {
                    RenderTargetBinding[] rts = null;
                    if (FSOEnvironment.UseMRT)
                    {
                        rts = device.GetRenderTargets();
                        if (rts.Length > 1)
                        {
                            device.SetRenderTarget((RenderTarget2D)rts[0].RenderTarget);
                        }
                    }
                    
                    var depth = device.DepthStencilState;
                    device.DepthStencilState = DepthStencilState.None;
                    Effect.CurrentTechnique = Effect.Techniques["DrawGrid"];
                    Effect.Parameters["World"].SetValue(Matrix.Identity * Matrix.CreateTranslation(0, ((int)(world.Zoom)-1) * (18 / 522f) * grassScale, 0));

                    if (TGridPrimitives > 0 && !TGridIndexBuffer.IsDisposed)
                    {
                        //draw target size in red, below old size
                        device.Indices = TGridIndexBuffer;
                        Effect.Parameters["DiffuseColor"].SetValue(new Vector4(0.5f, 1f, 0.5f, 1.0f));
                        foreach (var pass in Effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, TGridPrimitives);
                        }
                    }

                    Effect.Parameters["DiffuseColor"].SetValue(new Vector4(0, 0, 0, 1.0f));
                    device.Indices = GridIndexBuffer;
                    foreach (var pass in Effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, GridPrimitives);
                    }


                    device.DepthStencilState = depth;

                    if (FSOEnvironment.UseMRT)
                    {
                        device.SetRenderTargets(rts);
                    }
                }
            });
        }
    }
}
