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
using FSO.LotView.LMap;

namespace FSO.LotView.Components
{
    public class TerrainComponent : WorldComponent, IDisposable
    {
        private Rectangle Size;

        private int GeomLength;
        private byte[] GrassState; //0 = green, 255 = brown. to start with, should be randomly distriuted in range 0-128.
        private short[] GroundHeight;
        private int NumPrimitives;
        private int BladePrimitives;
        private int GridPrimitives;
        private int TGridPrimitives;
        private IndexBuffer IndexBuffer;
        private IndexBuffer BladeIndexBuffer;
        private IndexBuffer GridIndexBuffer;
        private IndexBuffer TGridIndexBuffer;
        private VertexBuffer VertexBuffer;
        public float Alpha = 1f;

        private TerrainType LightType = TerrainType.GRASS;
        private TerrainType DarkType = TerrainType.GRASS;
        public Vector3 LightVec = new Vector3(0, 1, 0);

        private Color LightGreen = new Color(80, 116, 59);
        private Color LightBrown = new Color(157, 117, 65);
        private Color DarkGreen = new Color(8, 52, 8);
        private Color DarkBrown = new Color(81, 60, 18);
        private int GrassHeight;
        private float GrassDensityScale = 1f;
        public bool DepthMode;

        public Vector2 SubworldOff = Vector2.Zero;

        private Effect Effect;
        public bool DrawGrid = false;
        public bool TerrainDirty = true;
        private Blueprint Bp;
        public bool _3D = false;

        public TerrainComponent(Rectangle size, Blueprint blueprint) {
            this.Size = size;
            this.Effect = WorldContent.GrassEffect;
            this.Bp = blueprint;

            UpdateLotType();
        }

        public void UpdateTerrain(TerrainType light, TerrainType dark, short[] heights, byte[] grass)
        {
            //DECEMBER TEMP: snow replace
            //TODO: tie to tuning, or serverside weather system.
            //if (light == TerrainType.GRASS || light == TerrainType.SAND) light = TerrainType.SNOW;
            //if (dark == TerrainType.SAND) dark = TerrainType.SNOW;
            LightType = light;
            DarkType = dark;
            GrassState = grass;
            GroundHeight = heights;
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
            if (GrassHeight == 0) GrassHeight = 1;
            GrassDensityScale = LotTypeGrassInfo.GrassDensity[index];
        }


        private Vector3 GetNormalAt(int x, int y)
        {
            var sum = new Vector3();
            var rotToNormalXY = Matrix.CreateRotationZ((float)(Math.PI / 2));
            var rotToNormalZY = Matrix.CreateRotationX(-(float)(Math.PI / 2));
            var limit = (Size.Width - 1);

            if (x < limit)
            {
                var vec = new Vector3();
                vec.X = 1;
                vec.Y = GetElevationPoint(x + 1, y) - GetElevationPoint(x, y);
                vec = Vector3.Transform(vec, rotToNormalXY);
                sum += vec;
            }

            if (x > 1)
            {
                var vec = new Vector3();
                vec.X = 1;
                vec.Y = GetElevationPoint(x, y) - GetElevationPoint(x - 1, y);
                vec = Vector3.Transform(vec, rotToNormalXY);
                sum += vec;
            }

            if (y < limit)
            {
                var vec = new Vector3();
                vec.Z = 1;
                vec.Y = GetElevationPoint(x, y + 1) - GetElevationPoint(x, y);
                vec = Vector3.Transform(vec, rotToNormalZY);
                sum += vec;
            }

            if (y > 1)
            {
                var vec = new Vector3();
                vec.Z = 1;
                vec.Y = GetElevationPoint(x, y) - GetElevationPoint(x, y - 1);
                vec = Vector3.Transform(vec, rotToNormalZY);
                sum += vec;
            }
            if (sum != Vector3.Zero) sum.Normalize();
            return sum;
        }

        private float GetElevationPoint(int x, int y)
        {
            if (x >= Size.Width || y >= Size.Height) return 0;
            return GroundHeight[((y) * (Size.Width) + (x))] * Bp.TerrainFactor * 3;
        }

        public override float PreferredDrawOrder
        {
            get { return 0.0f; }
        }

        public void RegenTerrain(GraphicsDevice device, Blueprint blueprint)
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
            var archSize = quads;

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

                    tl.Y = GetElevationPoint(x, y);
                    tr.Y = GetElevationPoint(x + 1, y);
                    bl.Y = GetElevationPoint(x, y + 1);
                    br.Y = GetElevationPoint(x + 1, y + 1);

                    Indexes[indexOffset++] = geomOffset;
                    Indexes[indexOffset++] = (geomOffset + 1);
                    Indexes[indexOffset++] = (geomOffset + 2);

                    Indexes[indexOffset++] = (geomOffset + 2);
                    Indexes[indexOffset++] = (geomOffset + 3);
                    Indexes[indexOffset++] = geomOffset;

                    short tx = (short)x, ty = (short)y;

                    if (blueprint.GetFloor(tx, ty, 1).Pattern == 0 &&
                        (blueprint.GetWall(tx, ty, 1).Segments & (WallSegments.HorizontalDiag | WallSegments.VerticalDiag)) == 0)
                    {
                        BladeIndexes[bindexOffset++] = geomOffset;
                        BladeIndexes[bindexOffset++] = (geomOffset + 1);
                        BladeIndexes[bindexOffset++] = (geomOffset + 2);

                        BladeIndexes[bindexOffset++] = (geomOffset + 2);
                        BladeIndexes[bindexOffset++] = (geomOffset + 3);
                        BladeIndexes[bindexOffset++] = geomOffset;
                    }

                    Color tlCol = Color.Lerp(LightGreen, LightBrown, GetGrassState(x, y));
                    Color trCol = Color.Lerp(LightGreen, LightBrown, GetGrassState(x + 1, y));
                    Color blCol = Color.Lerp(LightGreen, LightBrown, GetGrassState(x, y + 1));
                    Color brCol = Color.Lerp(LightGreen, LightBrown, GetGrassState(x + 1, y + 1));

                    Geom[geomOffset++] = new TerrainVertex(tl, tlCol.ToVector4(), new Vector2(((x - y) + 1) * 0.5f, (x + y) * 0.5f), GetGrassState(x, y), GetNormalAt(x, y));
                    Geom[geomOffset++] = new TerrainVertex(tr, trCol.ToVector4(), new Vector2(((x - y) + 2) * 0.5f, (x + 1 + y) * 0.5f), GetGrassState(x + 1, y), GetNormalAt(x + 1, y));
                    Geom[geomOffset++] = new TerrainVertex(br, brCol.ToVector4(), new Vector2(((x - y) + 1) * 0.5f, (x + y + 2) * 0.5f), GetGrassState(x + 1, y + 1), GetNormalAt(x + 1, y + 1));
                    Geom[geomOffset++] = new TerrainVertex(bl, blCol.ToVector4(), new Vector2((x - y) * 0.5f, (x + y + 1) * 0.5f), GetGrassState(x, y + 1), GetNormalAt(x, y + 1));
                }
            }

            var GridIndices = (Bp.FineArea != null) ? GetGridIndicesForFine(Bp.FineArea, quads) : GetGridIndicesForArea(Bp.BuildableArea, quads);
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

        public TerrainVertex[] GetVertices(GraphicsDevice gd)
        {
            if (VertexBuffer == null) RegenTerrain(gd, Bp);
            var dat = new TerrainVertex[VertexBuffer.VertexCount];
            VertexBuffer.GetData<TerrainVertex>(dat);
            return dat;
        }

        private float GetGrassState(int x, int y)
        {
            var offset = (y - 1) * Size.Width + x - 1;
            if (offset < 0) return 1;
            return GrassState[offset] / 255f;
        }

        private int[] GetGridIndicesForArea(Rectangle area, int quads)
        {
            var gridIndexOff = 0;
            var w = area.Width;
            var h = area.Height;
            var ox = area.X;
            var oy = area.Y;
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

        private int[] GetGridIndicesForFine(bool[] area, int quads)
        {
            List<int> GridIndices = new List<int>();
            var i = quads+1;
            for (var y = 1; y < quads-1; y++)
            {
                for (var x = 1; x < quads-1; x++)
                {
                    var tile = area[i];

                    if (tile)
                    {
                        var tileOff = i * 4;
                        GridIndices.Add(tileOff);
                        GridIndices.Add(tileOff + 1);
                        GridIndices.Add(tileOff);
                        GridIndices.Add(tileOff + 3);

                        if (x == quads - 1 || !area[i + 1])
                        {
                            GridIndices.Add(tileOff + 1); //+x
                            GridIndices.Add(tileOff + 2); //+x+y
                        }
                        if (y == quads - 1 || !area[i+quads])
                        {
                            GridIndices.Add(tileOff + 3); //+y
                            GridIndices.Add(tileOff + 2); //+x+y
                        }
                    }
                    i++;
                }
                i += 2;
            }
            return GridIndices.ToArray();
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
            if (TerrainDirty || VertexBuffer == null) RegenTerrain(device, Bp);
            if (VertexBuffer == null) return;
            if (world.Light != null) LightVec = world.Light.LightVec;
            var transitionIntensity = (world.Camera as WorldCamera3D)?.FromIntensity ?? 0f;
            Alpha = 1 - (float)Math.Pow(transitionIntensity, 150f);

            device.DepthStencilState = DepthStencilState.Default;
            device.BlendState = BlendState.NonPremultiplied;
            device.RasterizerState = RasterizerState.CullNone;
            PPXDepthEngine.RenderPPXDepth(Effect, true, (depthMode) =>
            {
            Effect.Parameters["LightGreen"].SetValue(LightGreen.ToVector4());
            Effect.Parameters["DarkGreen"].SetValue(DarkGreen.ToVector4());
            Effect.Parameters["DarkBrown"].SetValue(DarkBrown.ToVector4());
            Effect.Parameters["LightBrown"].SetValue(LightBrown.ToVector4());
                var light = new Vector3(0.3f, 1, -0.3f);

            Effect.Parameters["LightVec"]?.SetValue(LightVec);
            Effect.Parameters["UseTexture"].SetValue(false);
            Effect.Parameters["ScreenSize"].SetValue(new Vector2(device.Viewport.Width, device.Viewport.Height) / world.PreciseZoom);
            Effect.Parameters["TerrainNoise"].SetValue(TextureGenerator.GetTerrainNoise(device));
            Effect.Parameters["TerrainNoiseMip"].SetValue(TextureGenerator.GetTerrainNoise(device));
            Effect.Parameters["GrassFadeMul"].SetValue((float)Math.Sqrt(device.Viewport.Width/1920f));

            Effect.Parameters["FadeRectangle"].SetValue(new Vector4(77*3/2f + SubworldOff.X, 77*3/ 2f + SubworldOff.Y, 77*3, 77*3));
            Effect.Parameters["FadeWidth"].SetValue(35f*3);

            Effect.Parameters["TileSize"].SetValue(new Vector2(1f / Bp.Width, 1f / Bp.Height));
            Effect.Parameters["RoomMap"].SetValue(world.Rooms.RoomMaps[0]);
            Effect.Parameters["RoomLight"].SetValue(world.AmbientLight);
            Effect.Parameters["Alpha"].SetValue(Alpha);
            //Effect.Parameters["depthOutMode"].SetValue(DepthMode && (!FSOEnvironment.UseMRT));

            var offset = -world.WorldSpace.GetScreenOffset();

            Effect.Parameters["Projection"].SetValue(world.Camera.Projection);
            var view = world.Camera.View;
            var _3d = _3D;
            if (!_3d) view = view * Matrix.CreateTranslation(0, 0, -0.25f);
            Effect.Parameters["View"].SetValue(view);
            //world._3D.ApplyCamera(Effect);
            var translation = ((world.Zoom == WorldZoom.Far) ? -7 : ((world.Zoom == WorldZoom.Medium) ? -5 : -3)) * (20 / 522f);
            if (world.PreciseZoom < 1) translation /= world.PreciseZoom;
            else translation *= world.PreciseZoom;
            var altOff = Bp.BaseAlt * Bp.TerrainFactor * 3;
            var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, translation - altOff, 0);
            Effect.Parameters["World"].SetValue(worldmat);
            if ((world as RC.WorldStateRC)?.Use2DCam == false) Effect.Parameters["CamPos"]?.SetValue(world.Camera.Position + world.Camera.Translation);
            else Effect.Parameters["CamPos"]?.SetValue(new Vector3(0, 9999, 0));
            Effect.Parameters["DiffuseColor"].SetValue(world.OutsideColor.ToVector4() * Color.Lerp(LightGreen, Color.White, 0.25f).ToVector4());

            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;

            Effect.Parameters["UseTexture"].SetValue(true);
            Effect.Parameters["IgnoreColor"].SetValue(true);
            Effect.CurrentTechnique = Effect.Techniques["DrawBase"];

            var floors = new HashSet<sbyte>();
            for (sbyte f = 0; f < world.Level; f++) floors.Add(f);
            var pass = Effect.CurrentTechnique.Passes[(_3d) ? 2 : WorldConfig.Current.PassOffset];
            Bp.FloorGeom.DrawFloor(device, Effect, world.Zoom, world.Rotation, world.Rooms.RoomMaps, floors, pass, state: world);
            Effect.Parameters["GrassShininess"].SetValue(0.02f);// (float)0.25);

            pass.Apply();
            //device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, NumPrimitives);

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

            //    grassScale = 0;

            grassDensity *= GrassDensityScale;
            var primitives = Bp.FloorGeom.SetGrassIndices(device, Effect, world);

            if (primitives > 0 && _3D == _3d)
            {
                Effect.Parameters["Alpha"].SetValue((Alpha-0.75f) * 4);
                Effect.Parameters["Level"].SetValue((float)0.0001f);
                Effect.Parameters["RoomMap"].SetValue(world.Rooms.RoomMaps[0]);
                Effect.CurrentTechnique = Effect.Techniques["DrawBlades"];
                int grassNum = (int)Math.Ceiling(GrassHeight / (float)grassScale);
                
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
                device.DepthStencilState = DepthStencilState.DepthRead;
                for (int i = 0; i < grassNum; i++)
                {
                    Effect.Parameters["World"].SetValue(Matrix.Identity * Matrix.CreateTranslation(0, i * (20 / 522f) * grassScale - altOff, 0));
                    Effect.Parameters["GrassProb"].SetValue(grassDensity * ((grassNum - (i / (2f * grassNum))) / (float)grassNum));
                    offset += new Vector2(0, 1);
                        
                    var off2 = new Vector2(world.WorldSpace.WorldPxWidth, world.WorldSpace.WorldPxHeight);
                    off2 = (off2 / world.PreciseZoom - off2) / 2;

                        Effect.Parameters["ScreenOffset"].SetValue(offset - off2);

                        pass = Effect.CurrentTechnique.Passes[(_3d)?2:WorldConfig.Current.PassOffset];
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitives);
                    }
                    if (FSOEnvironment.UseMRT)
                    {
                        device.SetRenderTargets(rts);
                    }
                    device.DepthStencilState = depth;
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
                    device.DepthStencilState = DepthStencilState.DepthRead;
                    Effect.CurrentTechnique = Effect.Techniques["DrawGrid"];
                    Effect.Parameters["World"].SetValue(Matrix.Identity * Matrix.CreateTranslation(0, (18 / 522f) * grassScale - altOff, 0));

                    if (TGridPrimitives > 0 && !TGridIndexBuffer.IsDisposed)
                    {
                        //draw target size in red, below old size
                        device.Indices = TGridIndexBuffer;
                        Effect.Parameters["DiffuseColor"].SetValue(new Vector4(0.5f, 1f, 0.5f, 1.0f));
                        pass = Effect.CurrentTechnique.Passes[(_3d)?1:0];
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, TGridPrimitives);
                    }

                    Effect.Parameters["DiffuseColor"].SetValue(new Vector4(0, 0, 0, 1.0f));
                    device.Indices = GridIndexBuffer;
                    pass = Effect.CurrentTechnique.Passes[(_3d) ? 1 : 0];
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, GridPrimitives);


                    device.DepthStencilState = depth;

                    if (FSOEnvironment.UseMRT)
                    {
                        device.SetRenderTargets(rts);
                    }
                }
            });
        }

        public void DrawLMap(GraphicsDevice gd, LightData light, Matrix projection, Matrix lightTransform)
        {
            if (TerrainDirty || VertexBuffer == null) RegenTerrain(gd, Bp);
            if (VertexBuffer == null) return;
            //light.Normalize();
            Effect.Parameters["UseTexture"].SetValue(false);
            Effect.Parameters["Projection"].SetValue(projection);
            var view = Matrix.Identity;
            Effect.Parameters["View"].SetValue(view);

            var s = Matrix.Identity;
            s.M22 = 0;
            s.M33 = 0;
            s.M23 = 1;
            s.M32 = 1;

            var worldmat = Matrix.CreateScale(1 / 3f, 1f, 1 / 3f) * s * lightTransform;
            Effect.Parameters["World"].SetValue(worldmat);

            gd.SetVertexBuffer(VertexBuffer);
            gd.Indices = IndexBuffer;

            Effect.Parameters["UseTexture"].SetValue(true);
            Effect.Parameters["IgnoreColor"].SetValue(true);
            Effect.Parameters["DiffuseColor"].SetValue(new Vector4(1, 1, 1, 1));
            Effect.CurrentTechnique = Effect.Techniques["DrawLMap"];

            var pass = Effect.CurrentTechnique.Passes[0];
            var floors = new HashSet<sbyte>();
            for (sbyte i = (sbyte)(light.Level + 1); i < 5; i++) floors.Add(i);
            Bp.FloorGeom.DrawFloor(gd, Effect, WorldZoom.Near, WorldRotation.TopLeft, null, floors, pass, lightWorld: worldmat, minFloor: light.Level);
        }

        public BlendState Multiply = new BlendState()
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,

            AlphaSourceBlend = Blend.DestinationAlpha,
            AlphaDestinationBlend = Blend.Zero,
        };

        public void DrawMask(GraphicsDevice gd, WorldState world, Matrix view, Matrix projection)
        {
            if (TerrainDirty || VertexBuffer == null) RegenTerrain(gd, Bp);
            if (VertexBuffer == null) return;
            //light.Normalize();
            if (!gd.RasterizerState.ScissorTestEnable) gd.RasterizerState = RasterizerState.CullNone;
            else gd.DepthStencilState = DepthStencilState.None;
            //PPXDepthEngine.RenderPPXDepth(Effect, true, (depthMode) =>
            //{
                Effect.Parameters["UseTexture"].SetValue(false);
                Effect.Parameters["Projection"].SetValue(projection);
                Effect.Parameters["Level"].SetValue((float)0.0001f);
                Effect.Parameters["RoomMap"].SetValue(world.Rooms.RoomMaps[0]);

                var _3d = _3D;
                if (!_3d) view = view * Matrix.CreateTranslation(0, 0, -0.25f);
                Effect.Parameters["View"].SetValue(view);
                //world._3D.ApplyCamera(Effect);
                var translation = (0 * (20 / 522f));
                if (world.PreciseZoom < 1) translation /= world.PreciseZoom;
                else translation *= world.PreciseZoom;
                var altOff = Bp.BaseAlt * Bp.TerrainFactor * 3;
                var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, translation - altOff, 0);
                Effect.Parameters["World"].SetValue(worldmat);

                gd.SetVertexBuffer(VertexBuffer);
                gd.Indices = IndexBuffer;

                Effect.Parameters["UseTexture"].SetValue(false);
                Effect.Parameters["IgnoreColor"].SetValue(false);
                Effect.CurrentTechnique = Effect.Techniques["DrawMask"];

                Effect.Parameters["LightVec"].SetValue(LightVec);
                Effect.Parameters["MulRange"].SetValue(3f);
                Effect.Parameters["MulBase"].SetValue(0.15f);
                Effect.Parameters["BlurBounds"].SetValue(new Vector4(6, 6, 68, 68));


                var pass = Effect.CurrentTechnique.Passes[0];
                pass.Apply();
                var primitives = Bp.FloorGeom.SetGrassIndices(gd, Effect, world);
                var blendstate = gd.BlendState;
                if (primitives > 0)
                {
                    gd.BlendState = Multiply;
                    if (!gd.RasterizerState.ScissorTestEnable) gd.DepthStencilState = DepthStencilState.Default;
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitives);
                    gd.BlendState = blendstate;
                    gd.DepthStencilState = DepthStencilState.Default;
                }
            //});
        }

        /// <summary>
        /// Render the terrain
        /// </summary>
        /// <param name="device"></param>
        /// <param name="world"></param>
        public void DrawCustom(GraphicsDevice device, WorldState world, Matrix view, Matrix projection, int grassDepth, HashSet<sbyte> floors)
        {
            if (TerrainDirty || VertexBuffer == null) RegenTerrain(device, Bp);
            if (VertexBuffer == null) return;
            if (world.Light != null) LightVec = world.Light.LightVec;

            device.DepthStencilState = DepthStencilState.Default;
            device.BlendState = BlendState.NonPremultiplied;
            //device.RasterizerState = RasterizerState.CullNone;

            Effect.Parameters["LightGreen"].SetValue(LightGreen.ToVector4());
            Effect.Parameters["DarkGreen"].SetValue(DarkGreen.ToVector4());
            Effect.Parameters["DarkBrown"].SetValue(DarkBrown.ToVector4());
            Effect.Parameters["LightBrown"].SetValue(LightBrown.ToVector4());
            var light = new Vector3(0.3f, 1, -0.3f);

            Effect.Parameters["LightVec"]?.SetValue(LightVec);
            Effect.Parameters["UseTexture"].SetValue(false);
            Effect.Parameters["ScreenSize"].SetValue(new Vector2(device.Viewport.Width, device.Viewport.Height) / world.PreciseZoom);
            Effect.Parameters["TerrainNoise"].SetValue(TextureGenerator.GetTerrainNoise(device));
            Effect.Parameters["TerrainNoiseMip"].SetValue(TextureGenerator.GetTerrainNoise(device));
            Effect.Parameters["GrassFadeMul"].SetValue((float)Math.Sqrt(device.Viewport.Width / 1920f));

            Effect.Parameters["FadeRectangle"].SetValue(new Vector4(77 * 3 / 2f + SubworldOff.X, 77 * 3 / 2f + SubworldOff.Y, 77 * 3, 77 * 3));
            Effect.Parameters["FadeWidth"].SetValue(35f * 3);

            Effect.Parameters["TileSize"].SetValue(new Vector2(1f / Bp.Width, 1f / Bp.Height));
            Effect.Parameters["RoomMap"].SetValue(world.Rooms.RoomMaps[0]);
            Effect.Parameters["RoomLight"].SetValue(world.AmbientLight);
            Effect.Parameters["Alpha"].SetValue(1f);

            var offset = -world.WorldSpace.GetScreenOffset();

            Effect.Parameters["Projection"].SetValue(projection);
            var _3d = _3D;
            Effect.Parameters["View"].SetValue(view);

            var translation = ((world.Zoom == WorldZoom.Far) ? -7 : ((world.Zoom == WorldZoom.Medium) ? -5 : -3)) * (20 / 522f);
            if (world.PreciseZoom < 1) translation /= world.PreciseZoom;
            else translation *= world.PreciseZoom;
            var altOff = Bp.BaseAlt * Bp.TerrainFactor * 3;
            var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, translation - altOff, 0);
            Effect.Parameters["World"].SetValue(worldmat);
            if ((world as RC.WorldStateRC)?.Use2DCam == false) Effect.Parameters["CamPos"]?.SetValue(world.Camera.Position + world.Camera.Translation);
            else Effect.Parameters["CamPos"]?.SetValue(new Vector3(0, 9999, 0));
            Effect.Parameters["GrassShininess"].SetValue((float)0.0);
            Effect.Parameters["DiffuseColor"].SetValue(world.OutsideColor.ToVector4() * Color.Lerp(LightGreen, Color.White, 0.25f).ToVector4());

            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;

            Effect.Parameters["UseTexture"].SetValue(true);
            Effect.Parameters["IgnoreColor"].SetValue(true);
            Effect.CurrentTechnique = Effect.Techniques["DrawBase"];

            var pass = Effect.CurrentTechnique.Passes[(_3d) ? 2 : WorldConfig.Current.PassOffset];
            Bp.FloorGeom.DrawFloor(device, Effect, world.Zoom, world.Rotation, world.Rooms.RoomMaps, floors, pass, state: world);

            pass.Apply();

            int grassScale = 1;
            float grassDensity = 0.43f;

            grassDensity *= GrassDensityScale;
            var primitives = Bp.FloorGeom.SetGrassIndices(device, Effect, world);

            if (floors.Contains(0) && primitives > 0 && _3D == _3d)
            {
                Effect.Parameters["Level"].SetValue((float)0.0001f);
                Effect.Parameters["RoomMap"].SetValue(world.Rooms.RoomMaps[0]);
                Effect.CurrentTechnique = Effect.Techniques["DrawBlades"];
                int grassNum = grassDepth;

                var depth = device.DepthStencilState;
                device.DepthStencilState = DepthStencilState.DepthRead;
                for (int i = 0; i < grassNum; i++)
                {
                    Effect.Parameters["World"].SetValue(Matrix.Identity * Matrix.CreateTranslation(0, i * (20 / 522f) * grassScale - altOff, 0));
                    Effect.Parameters["GrassProb"].SetValue(grassDensity * ((grassNum - (i / (2f * grassNum))) / (float)grassNum));
                    offset += new Vector2(0, 1);

                    var off2 = new Vector2(world.WorldSpace.WorldPxWidth, world.WorldSpace.WorldPxHeight);
                    off2 = (off2 / world.PreciseZoom - off2) / 2;

                    Effect.Parameters["ScreenOffset"].SetValue(offset - off2);

                    pass = Effect.CurrentTechnique.Passes[(_3d) ? 2 : WorldConfig.Current.PassOffset];
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitives);
                }
                device.DepthStencilState = depth;
            }
        }

        public void Dispose()
        {
            if (VertexBuffer != null)
            {
                IndexBuffer.Dispose();
                BladeIndexBuffer.Dispose();
                VertexBuffer.Dispose();
                GridIndexBuffer?.Dispose();
                TGridIndexBuffer?.Dispose();
            }
        }
    }
}
