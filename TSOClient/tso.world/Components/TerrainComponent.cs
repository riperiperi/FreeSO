using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;
using FSO.LotView.Utils;
using FSO.LotView.Model;
using FSO.Content.Model;
using FSO.Common;
using FSO.Common.Utils;
using FSO.LotView.LMap;
using FSO.LotView.Effects;
using FSO.Common.Model;

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
        public VertexBuffer VertexBuffer;
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
        public float FadeDistance = 77 * 3f;

        private GrassEffect Effect;
        public bool DrawGrid = false;
        public bool TerrainDirty = true;
        private Blueprint Bp;
        public bool _3D = false;

        private bool GridAsTexture;
        private Texture2D GridTex;

        public TerrainComponent(Rectangle size, Blueprint blueprint) {
            this.Size = size;
            this.Effect = WorldContent.GrassEffect;
            this.Bp = blueprint;
            GridAsTexture = FSOEnvironment.Enable3D;

            UpdateLotType();
        }

        public void UpdateTerrain(TerrainType light, TerrainType dark, short[] heights, byte[] grass)
        {
            //DECEMBER TEMP: snow replace
            //TODO: tie to tuning, or serverside weather system.
            LightType = light;
            DarkType = dark;

            //special tuning from server
            var forceSnow = DynamicTuning.Global?.GetTuning("city", 0, 0);
            if (forceSnow != null) ForceSnow(forceSnow.Value);

            GrassState = grass;
            GroundHeight = heights;
            UpdateLotType();
            TerrainDirty = true;

            Bp.SM64?.UpdateTerrain();
        }

        public void ForceSnow(float type)
        {
            switch (type)
            {
                case 1f: // Summer
                    if (LightType == TerrainType.SNOW)
                    {
                        LightType = TerrainType.GRASS;
                    }
                    if (DarkType == TerrainType.SNOW)
                    {
                        DarkType = TerrainType.GRASS;
                    }
                    break;
                case 2f: // Autumn
                    if (LightType == TerrainType.GRASS)
                    {
                        LightType = TerrainType.TS1AutumnGrass;
                    }
                    if (DarkType == TerrainType.GRASS)
                    {
                        DarkType = TerrainType.TS1AutumnGrass;
                    }
                    break;
                default: // Winter
                    if (LightType == TerrainType.GRASS || LightType == TerrainType.SAND) LightType = TerrainType.SNOW;
                    if (DarkType == TerrainType.SAND) DarkType = TerrainType.SNOW;
                    break;
            }
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

        public float GetElevationPoint(int x, int y)
        {
            if (x >= Size.Width || y >= Size.Height) return 0;
            return GroundHeight[((y) * (Size.Width) + (x))] * Bp.TerrainFactor * 3;
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

            TerrainParallaxVertex[] Geom = new TerrainParallaxVertex[numQuads * 4];
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

                    Geom[geomOffset++] = new TerrainParallaxVertex(tl, tlCol.ToVector4(), new Vector2(((x - y) + 1) * 0.5f, (x + y) * 0.5f), GetGrassState(x, y), GetNormalAt(x, y));
                    Geom[geomOffset++] = new TerrainParallaxVertex(tr, trCol.ToVector4(), new Vector2(((x - y) + 2) * 0.5f, (x + 1 + y) * 0.5f), GetGrassState(x + 1, y), GetNormalAt(x + 1, y));
                    Geom[geomOffset++] = new TerrainParallaxVertex(br, brCol.ToVector4(), new Vector2(((x - y) + 1) * 0.5f, (x + y + 2) * 0.5f), GetGrassState(x + 1, y + 1), GetNormalAt(x + 1, y + 1));
                    Geom[geomOffset++] = new TerrainParallaxVertex(bl, blCol.ToVector4(), new Vector2((x - y) * 0.5f, (x + y + 1) * 0.5f), GetGrassState(x, y + 1), GetNormalAt(x, y + 1));
                }
            }

            var GridIndices = (Bp.FineArea != null) ? GetGridIndicesForFine(Bp.FineArea, quads) : GetGridIndicesForArea(Bp.BuildableArea, quads);
            var TGridIndices = GetGridIndicesForArea(Bp.TargetBuildableArea, quads);

            VertexBuffer = new VertexBuffer(device, typeof(TerrainParallaxVertex), Geom.Length, BufferUsage.None);
            VertexBuffer.SetData(Geom);

            IndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * Indexes.Length, BufferUsage.None);
            IndexBuffer.SetData(Indexes);

            BladePrimitives = (bindexOffset / 3);

            BladeIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * Indexes.Length, BufferUsage.None);
            BladeIndexBuffer.SetData(BladeIndexes);
            GeomLength = Geom.Length;

            var primLength = (GridAsTexture) ? 3 : 2;

            if (GridIndices.Length > 0)
            {
                GridIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * GridIndices.Length, BufferUsage.None);
                GridIndexBuffer.SetData(GridIndices);
                GridPrimitives = GridIndices.Length / primLength;
            }

            if (TGridIndices.Length > 0)
            {
                TGridIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * TGridIndices.Length, BufferUsage.None);
                TGridIndexBuffer.SetData(TGridIndices);
                TGridPrimitives = TGridIndices.Length / primLength;
            }

            if (GridTex == null)
            {
                using (var strm = File.Open($"Content/Textures/lot/tile_dashed.png", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    GridTex = GenMips(device, Texture2D.FromStream(device, strm));
                }
            }
        }

        private Texture2D GenMips(GraphicsDevice device, Texture2D texture)
        {
            var data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            texture.Dispose();
            texture = new Texture2D(device, texture.Width, texture.Height, true, SurfaceFormat.Color);
            TextureUtils.UploadWithAvgMips(texture, device, data);
            return texture;
        }

        public TerrainParallaxVertex[] GetVertices(GraphicsDevice gd)
        {
            if (VertexBuffer == null) RegenTerrain(gd, Bp);
            var dat = new TerrainParallaxVertex[VertexBuffer.VertexCount];
            VertexBuffer.GetData<TerrainParallaxVertex>(dat);
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
            area = Rectangle.Intersect(area, new Rectangle(0, 0, quads, quads));
            var fine = new bool[quads * quads];
            var ox = area.X;
            var oy = area.Y;
            var w = area.Width;
            var h = area.Height;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    fine[x + ox + (y + oy) * quads] = true;
                }
            }

            return GetGridIndicesForFine(fine, quads);
        }

        private int[] GetGridTrisForFine(bool[] area, int quads)
        {
            List<int> GridIndices = new List<int>();
            var i = quads + 1;
            for (var y = 1; y < quads - 1; y++)
            {
                for (var x = 1; x < quads - 1; x++)
                {
                    var tile = area[i];

                    if (tile)
                    {
                        var tileOff = i * 4;
                        GridIndices.Add(tileOff);
                        GridIndices.Add(tileOff + 1);
                        GridIndices.Add(tileOff + 2); //+x+y

                        GridIndices.Add(tileOff);
                        GridIndices.Add(tileOff + 2); //+x+y
                        GridIndices.Add(tileOff + 3);
                    }
                    i++;
                }
                i += 2;
            }
            return GridIndices.ToArray();
        }

        private int[] GetGridIndicesForFine(bool[] area, int quads)
        {
            if (GridAsTexture)
            {
                return GetGridTrisForFine(area, quads);
            }
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
            var _3d = world.CameraMode == CameraRenderMode._3D;
            var nonIso = world.CameraMode != CameraRenderMode._2D;
            if (TerrainDirty || VertexBuffer == null || GridAsTexture != _3d)
            {
                GridAsTexture = _3d;
                RegenTerrain(device, Bp);
            }
            if (VertexBuffer == null) return;
            if (world.Light != null) LightVec = world.Light.LightVec;
            var transitionIntensity = (world.Camera as WorldCamera3D)?.FromIntensity ?? 0f;
            Alpha = 1 - (float)Math.Pow(transitionIntensity, 150f);

            device.DepthStencilState = DepthStencilState.Default;
            device.BlendState = BlendState.NonPremultiplied;
            device.RasterizerState = RasterizerState.CullNone;
            PPXDepthEngine.RenderPPXDepth(Effect, true, (depthMode) =>
            {
            Effect.LightGreen = LightGreen.ToVector4();
            Effect.DarkGreen = DarkGreen.ToVector4();
            Effect.DarkBrown = DarkBrown.ToVector4();
            Effect.LightBrown = LightBrown.ToVector4();
                var light = new Vector3(0.3f, 1, -0.3f);

            Effect.LightVec = LightVec;
            Effect.UseTexture = false;
            Effect.ScreenSize = new Vector2(device.Viewport.Width, device.Viewport.Height) / world.PreciseZoom;
            Effect.TerrainNoise = TextureGenerator.GetTerrainNoise(device);
            Effect.TerrainNoiseMip = TextureGenerator.GetTerrainNoise(device);
            Effect.GrassFadeMul = (float)Math.Sqrt(device.Viewport.Width/1920f);

            Effect.FadeRectangle = new Vector4(FadeDistance / 2f + SubworldOff.X, FadeDistance / 2f + SubworldOff.Y, FadeDistance, FadeDistance);
            Effect.FadeWidth = 35f*3;

            Effect.TileSize = new Vector2(1f / Bp.Width, 1f / Bp.Height);
            Effect.RoomMap = world.Rooms.RoomMaps[0];
            Effect.RoomLight = world.AmbientLight;
            Effect.Alpha = Alpha;

            var offset = -world.WorldSpace.GetScreenOffset();
            var cam2d = world.Cameras.Camera2D.Camera;
            var rot =( world.Cameras.Camera2D.Camera.RotateOff / 180) * Math.PI;
            var smat = new Vector4((float)Math.Cos(rot), (float)Math.Sin(rot) * 0.5f, -(float)Math.Sin(rot) / 0.5f, (float)Math.Cos(rot));
            var sr = Math.Abs(smat.Y);
            Effect.ScreenMatrix = smat;
            var anchor = cam2d.RotationAnchor;
            var ctr = new Vector2();
            if (anchor != null)
            {
                ctr = world.WorldSpace.GetScreenFromTile(new Vector2(anchor.Value.X, anchor.Value.Y));
                ctr -= world.WorldSpace.GetScreenFromTile(new Vector2(cam2d.CenterTile.X, cam2d.CenterTile.Y));
            }
            ctr += world.WorldSpace.WorldPx / 2;
            Effect.ScreenRotCenter = ctr;

            Effect.Projection = world.Projection;
            var view = world.View;
            //if (!_3d) view = view * Matrix.CreateTranslation(0, 0, -0.25f);
            Effect.View = view;
            //world._3D.ApplyCamera(Effect);
            var translation = ((world.Zoom == WorldZoom.Far) ? -7 : ((world.Zoom == WorldZoom.Medium) ? -5 : -3)) * (20 / 522f);
            if (world.PreciseZoom < 1) translation /= world.PreciseZoom;
            else translation *= world.PreciseZoom;
            var altOff = Bp.BaseAlt * Bp.TerrainFactor * 3;
            var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, translation - altOff, 0);
            Effect.World = worldmat;
            if (_3d) Effect.CamPos = world.Camera.Position + (world.Cameras.ModelTranslation ?? new Vector3());
            else
            {
                var flat = view;
                flat.Translation = new Vector3(0, 0, 0);
                var pos = Vector3.Transform(new Vector3(0, 0, 20000), Matrix.Invert(flat));
                Effect.CamPos = pos;
            }
            Effect.DiffuseColor = world.OutsideColor.ToVector4() * Color.Lerp(LightGreen, Color.White, 0.25f).ToVector4();

            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;

            Effect.UseTexture = true;
            Effect.IgnoreColor = true;
            Effect.SetTechnique(GrassTechniques.DrawBase);

            var floors = new HashSet<sbyte>();
            for (sbyte f = 0; f < world.Level; f++) floors.Add(f);
            var pass = Effect.CurrentTechnique.Passes[(_3d) ? 2 : WorldConfig.Current.PassOffset];
            Bp.FloorGeom.DrawFloor(device, Effect, world.Zoom, world.Rotation, world.Rooms.RoomMaps, floors, pass, state: world, screenAlignUV: !nonIso, withCeilings: true);
            Effect.GrassShininess = 0.02f;// (float)0.25);

            pass.Apply();

            float grassScale;
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
            var primitives = Bp.FloorGeom.SetGrassIndices(device, Effect, world);

            var parallax = false;

            Effect.TexMatrix = new Vector4(1f, 1f, -1f, 1f);
            Effect.TexOffset = new Vector2(0.5f, 0.5f);

            if (primitives > 0)
            {
                Effect.Alpha = (Alpha-0.75f) * 4;
                Effect.Level = (float)0.0001f;
                Effect.RoomMap = world.Rooms.RoomMaps[0];
                Effect.SetTechnique(GrassTechniques.DrawBlades);
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
                    
                if (parallax) { 
                    grassScale *= grassNum;
                    grassNum = 1;
                    }
                for (int i = 1; i <= grassNum; i++)
                {
                    Effect.World = Matrix.Identity * Matrix.CreateTranslation(0, i * (20 / 522f) * grassScale - altOff, 0);

                    if (!parallax)
                        Effect.GrassProb = grassDensity * ((grassNum - (i / (2f * grassNum))) / (float)grassNum);
                    else
                        Effect.GrassProb = grassDensity * ((4 - (2 / (2f * 4))) / (float)4);
                    Effect.ParallaxHeight = grassScale * (20 / 522f) * (100/512f) / 4;
                    offset += new Vector2(smat.Z, smat.W);
                        
                    var off2 = new Vector2(world.WorldSpace.WorldPxWidth, world.WorldSpace.WorldPxHeight);
                    off2 = (off2 / world.PreciseZoom - off2) / 2;

                        Effect.ScreenOffset = offset - off2;

                        pass = Effect.CurrentTechnique.Passes[(_3d)?((parallax)?3:2):WorldConfig.Current.PassOffset];
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
                    Effect.SetTechnique(GrassTechniques.DrawGrid);
                    Effect.BaseTex = GridTex;
                    Effect.World = Matrix.Identity * Matrix.CreateTranslation(0, (18 / 522f) * grassScale - altOff, 0);
                    pass = Effect.CurrentTechnique.Passes[(GridAsTexture)?2:0];

                    if (GridAsTexture)
                    {
                        if (TGridPrimitives > 0 && !TGridIndexBuffer.IsDisposed)
                        {
                            //draw target size in red, below old size
                            device.Indices = TGridIndexBuffer;
                            Effect.DiffuseColor = new Vector4(0.5f, 1f, 0.5f, 1.0f);
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, TGridPrimitives);
                        }


                        Effect.DiffuseColor = 
                            Content.Content.Get().TS1 ?
                            new Vector4(1.0f, 1.0f, 1.0f, 0.8f) :
                            new Vector4(0.0f, 0.0f, 0.0f, 0.8f);
                        device.Indices = GridIndexBuffer;
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, GridPrimitives);
                    }
                    else
                    {
                        if (TGridPrimitives > 0 && !TGridIndexBuffer.IsDisposed)
                        {
                            //draw target size in red, below old size
                            device.Indices = TGridIndexBuffer;
                            Effect.DiffuseColor = new Vector4(0.5f, 1f, 0.5f, 1.0f);
                            pass = Effect.CurrentTechnique.Passes[(_3d) ? 1 : 0];
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, TGridPrimitives);
                        }

                        Effect.DiffuseColor = new Vector4(0, 0, 0, 1.0f);
                        device.Indices = GridIndexBuffer;
                        pass = Effect.CurrentTechnique.Passes[(_3d) ? 1 : 0];
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

        public void DrawLMap(GraphicsDevice gd, LightData light, Matrix projection, Matrix lightTransform)
        {
            if (TerrainDirty || VertexBuffer == null) RegenTerrain(gd, Bp);
            if (VertexBuffer == null) return;
            //light.Normalize();
            Effect.UseTexture = false;
            Effect.Projection = projection;
            var view = Matrix.Identity;
            Effect.View = view;

            var s = Matrix.Identity;
            s.M22 = 0;
            s.M33 = 0;
            s.M23 = 1;
            s.M32 = 1;

            var worldmat = Matrix.CreateScale(1 / 3f, 1f, 1 / 3f) * s * lightTransform;
            Effect.World = worldmat;

            gd.SetVertexBuffer(VertexBuffer);
            gd.Indices = IndexBuffer;

            Effect.UseTexture = true;
            Effect.IgnoreColor = true;
            Effect.DiffuseColor = new Vector4(1, 1, 1, 1);
            Effect.SetTechnique(GrassTechniques.DrawLMap);

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
                Effect.UseTexture = false;
                Effect.Projection = projection;
                Effect.Level = (float)0.0001f;
                Effect.RoomMap = world.Rooms.RoomMaps[0];

                var _3d = _3D;
                Effect.View = view;
                //world._3D.ApplyCamera(Effect);
                var translation = (0 * (20 / 522f));
                if (world.PreciseZoom < 1) translation /= world.PreciseZoom;
                else translation *= world.PreciseZoom;
                var altOff = Bp.BaseAlt * Bp.TerrainFactor * 3;
                var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, translation - altOff, 0);
                Effect.World = worldmat;

                gd.SetVertexBuffer(VertexBuffer);
                gd.Indices = IndexBuffer;

                Effect.UseTexture = false;
                Effect.IgnoreColor = false;
                Effect.SetTechnique(GrassTechniques.DrawMask);

                Effect.LightVec = LightVec;
                Effect.MulRange = 3f;
                Effect.MulBase = 0.12f;
                Effect.BlurBounds = new Vector4(6, 6, 68, 68);
                Effect.DiffuseColor = world.OutsideColor.ToVector4();

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

            Effect.LightGreen = LightGreen.ToVector4();
            Effect.DarkGreen = DarkGreen.ToVector4();
            Effect.DarkBrown = DarkBrown.ToVector4();
            Effect.LightBrown = LightBrown.ToVector4();
            var light = new Vector3(0.3f, 1, -0.3f);

            Effect.LightVec = LightVec;
            Effect.UseTexture = false;
            Effect.ScreenSize = new Vector2(device.Viewport.Width, device.Viewport.Height) / world.PreciseZoom;
            Effect.TerrainNoise = TextureGenerator.GetTerrainNoise(device);
            Effect.TerrainNoiseMip = TextureGenerator.GetTerrainNoise(device);
            Effect.GrassFadeMul = (float)Math.Sqrt(device.Viewport.Width / 1920f);

            Effect.FadeRectangle = new Vector4(FadeDistance / 2f + SubworldOff.X, FadeDistance / 2f + SubworldOff.Y, FadeDistance, FadeDistance);
            Effect.FadeWidth = 35f * 3;

            Effect.TileSize = new Vector2(1f / Bp.Width, 1f / Bp.Height);
            Effect.RoomMap = world.Rooms.RoomMaps[0];
            Effect.RoomLight = world.AmbientLight;
            Effect.Alpha = 1f;

            var offset = -world.WorldSpace.GetScreenOffset();

            Effect.Projection = projection;
            var _3d = _3D;
            Effect.View = view;

            var translation = ((world.Zoom == WorldZoom.Far) ? -7 : ((world.Zoom == WorldZoom.Medium) ? -5 : -3)) * (20 / 522f);
            if (world.PreciseZoom < 1) translation /= world.PreciseZoom;
            else translation *= world.PreciseZoom;
            var altOff = Bp.BaseAlt * Bp.TerrainFactor * 3;
            var worldmat = Matrix.Identity * Matrix.CreateTranslation(0, translation - altOff, 0);
            Effect.World = worldmat;
            if (world.CameraMode == CameraRenderMode._3D) Effect.CamPos = world.Camera.Position + (world.Cameras.ModelTranslation ?? new Vector3());
            else Effect.CamPos = new Vector3(0, 9999, 0);
            Effect.GrassShininess = (float)0.0;
            Effect.DiffuseColor = world.OutsideColor.ToVector4() * Color.Lerp(LightGreen, Color.White, 0.25f).ToVector4();

            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;

            Effect.UseTexture = true;
            Effect.IgnoreColor = true;
            Effect.SetTechnique(GrassTechniques.DrawBase);

            var pass = Effect.CurrentTechnique.Passes[(_3d) ? 2 : WorldConfig.Current.PassOffset];
            Bp.FloorGeom.DrawFloor(device, Effect, world.Zoom, world.Rotation, world.Rooms.RoomMaps, floors, pass, state: world);

            pass.Apply();

            int grassScale = 1;
            float grassDensity = 0.43f;

            grassDensity *= GrassDensityScale;
            var primitives = Bp.FloorGeom.SetGrassIndices(device, Effect, world);

            if (floors.Contains(0) && primitives > 0 && _3D == _3d)
            {
                Effect.Level = (float)0.0001f;
                Effect.RoomMap = world.Rooms.RoomMaps[0];
                Effect.SetTechnique(GrassTechniques.DrawBlades);
                int grassNum = grassDepth;

                var depth = device.DepthStencilState;
                device.DepthStencilState = DepthStencilState.DepthRead;
                for (int i = 0; i < grassNum; i++)
                {
                    Effect.World = Matrix.Identity * Matrix.CreateTranslation(0, i * (20 / 522f) * grassScale - altOff, 0);
                    Effect.GrassProb = grassDensity * ((grassNum - (i / (2f * grassNum))) / (float)grassNum);
                    offset += new Vector2(0, 1);

                    var off2 = new Vector2(world.WorldSpace.WorldPxWidth, world.WorldSpace.WorldPxHeight);
                    off2 = (off2 / world.PreciseZoom - off2) / 2;

                    Effect.ScreenOffset = offset - off2;

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
                GridTex?.Dispose();
            }
        }
    }
}
