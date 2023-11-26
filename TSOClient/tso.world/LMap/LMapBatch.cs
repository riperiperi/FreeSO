using FSO.Common;
using FSO.Common.Utils;
using FSO.LotView.Components;
using FSO.LotView.Effects;
using FSO.LotView.Model;
using FSO.LotView.RC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.LotView.LMap
{
    public class LMapBatch : IDisposable
    {
        private struct DirtyRoom
        {
            public ushort RoomID;
            public int Priority;
        }

        public int targetResPerTile = 16;
        int resPerTile = 16;
        int borderSize = 1;

        private ShadowGeometry ShadowGeo = new ShadowGeometry();

        public RenderTarget2D ShadowTarg;
        public RenderTarget2D ObjShadowTarg;

        public int ShadowTargQualityDivider = 1;
        public int LastShadowTargQualityDivider = 1;
        public RenderTarget2D OutsideShadowTarg;
        public RenderTarget2D OutsideShadowTargPost;
        private SpriteBatch ShadowTargBlit;
        private int OutShadowFloor = -1;

        public RenderTarget2D LightMap;
        public RenderTarget2D LightMapDirection;
        public WallComponentRC WallComp
        {
            get
            {
                return Blueprint.WCRC;
            }
        }

        GraphicsDevice GD;
        private GradEffect GradEffect;
        private LightMap2DEffect LightEffect;
        Matrix Projection;

        public Blueprint Blueprint;
        private LightData OutdoorsLight;
        public Vector3 SunVector;
        public bool Night;
        public Vector3 LightVec = new Vector3(0, 1f, 0);
        public Vector2 MapLayout; //width * height floors. Ordered by width first. x = floor%width, y = (int)floot/width.
        public Vector2 InvMapLayout;

        private List<DirtyRoom> DirtyRooms = new List<DirtyRoom>();
        public sbyte RedrawFloor;
        public Color LastOutsideColor;

        private Point ScissorBase;

        // -- Our rendering process for each room --
        // 1. Activate Scissor rectangle for Room bounds
        // 2. Fill room black 
        // 3. For each light:
        //   3a. Scissor reapplies to room intersect light bounds
        //   3b. Generate wall shadow map for light. 
        //   3c. Generate object shadow map for light.
        //   3d. Additive blend light onto room lightmap texture, using room map limit shader
        // 4. If outdoors, apply outdoors light:
        //   4a. Scissor reapplies to room bounds again
        //   4b. Generate wall shadow map for light. (with gradient falloff, fixed direction)
        //   4c. Generate object shadow map for light. (fixed shadow direction and height)
        //   4d. Additive blend light onto room lightmap texture, using room map limit shader


        // Room map textures currently:
        // (second optional, requires floating point surface format)
        // RGBA: RGB, ShadIntensity
        // HalfVec2: DirX, DirY

        // ShadIntensity is the average of RGB modified by object floor shadows.
        // Factor is reapplied:
        // - On floors
        // - On the bottom of walls

        //alternate
        // RGBA: LightIntensity, OutdoorsIntensity, LightIntensityShad, OutdoorsIntensityShad
        // HalfVec4: DirX, DirY, DirXShad, DirYShad (directional component enabled)

        public LMapBatch(GraphicsDevice device, int res)
        {
            GD = device;
            targetResPerTile = res;

            this.GradEffect = WorldContent.Grad2DEffect;
            this.LightEffect = WorldContent.Light2DEffect;

            InitBasicData();
        }

        public void SetMapLayout(int width, int height)
        {
            var w = Blueprint.Width;
            var h = Blueprint.Height;
            MapLayout = new Vector2(width, height);
            var invMapLayout = new Vector3(1f / width, 1, 1f / height);
            InvMapLayout = new Vector2(invMapLayout.X, invMapLayout.Z);
            var factor = new Vector3(1f / (3 * (w - borderSize)), 1 / (3 * 2.95f), 1f / (3 * (h - borderSize)));
            factor *= invMapLayout;

            foreach (var effect in WorldContent.LightEffects)
            {
                effect.WorldToLightFactor = factor;
                effect.MapLayout = MapLayout;
            }
            LightEffect.MapLayout = MapLayout;
        }

        public void SetFloor(sbyte floor, WorldState state)
        {
            //the goal is to center 0-(75*16)
            var width = (int)MapLayout.X;
            var height = (int)MapLayout.Y;
            var x = floor % width;
            var y = floor / width;
            //var floorWidth = 75 * 16;
            //var floorHeight = 75 * 16;

            LightEffect.UVBase = new Vector2(x, y);
            LightEffect.roomMap = state.Rooms.RoomMaps[floor];

            var res = (Blueprint.Width - borderSize) * resPerTile;
            ScissorBase = new Point(res * x, res * y);
        }

        public void Init(Blueprint blueprint)
        {
            this.Blueprint = blueprint;

            var w = blueprint.Width;
            var h = blueprint.Height;
            var ultra = WorldConfig.Current.UltraLighting;
            var directional = WorldConfig.Current.Directional;

            if (w > 64 && FSOEnvironment.SoftwareDepth) ultra = false;

            resPerTile = ultra ? targetResPerTile : targetResPerTile/2;

            var wl = resPerTile * (w - borderSize);
            var wh = resPerTile * (h - borderSize);

            Dispose();

            ShadowTarg = new RenderTarget2D(GD, wl, wh, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            ObjShadowTarg = new RenderTarget2D(GD, (ultra)?(wl*2):wl, (ultra)?(wh*2):wh, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            LightMap = new RenderTarget2D(GD, (wl * 3), (wh * 2), false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents); //just ground floor for now.
            ShadowTargBlit = new SpriteBatch(GD);
            Projection = Matrix.CreateOrthographicOffCenter(new Rectangle(0, 0, (w - borderSize) * 16, (h - borderSize) * 16), -10, 10);
            if (directional) LightMapDirection = new RenderTarget2D(GD, (w - borderSize)*3*4, (h - borderSize)*2*4, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            //initialize lighteffect with default params
            LightEffect.shadowMap = ShadowTarg;
            LightEffect.floorShadowMap = ObjShadowTarg;
            LightEffect.ShadowPowers = new Vector2(1f, 1f);
            LightEffect.RoomUVOff = new Vector2(-1f / w, -1f / h);
            LightEffect.TileSize = new Vector2(1f / w, 1f / h);

            LightEffect.Projection = Matrix.CreateOrthographicOffCenter(new Rectangle(0, 0, 1, 1), -10, 10);
            LightEffect.LightPower = 2.2f;
            LightEffect.RoomUVRescale = new Vector2((w - borderSize) / (float)w, (h - borderSize) / (float)h);
            LightEffect.RoomUVOff = new Vector2(0, 0);
            LightEffect.SSAASize = new Vector2(1f / 600);

            SetMapLayout(3, 2);
        }

        private VertexPosition[] Dat;
        private VertexBuffer LightBuf;
        public void InitBasicData()
        {
            //PrimitiveType.TriangleStrip
            Dat = new VertexPosition[]
            {
                new VertexPosition(new Vector3(0, 0, 0)),
                new VertexPosition(new Vector3(1, 0, 0)),
                new VertexPosition(new Vector3(0, 1, 0)),
                new VertexPosition(new Vector3(1, 1, 0))
            };
            LightBuf = new VertexBuffer(GD, typeof(VertexPosition), 4, BufferUsage.None);
            LightBuf.SetData(Dat);
        }

        public void ResetDraw()
        {
            GD.SetRenderTarget(null);
        }

        private int ColorDiff(Color col1, Color col2)
        {
            return Math.Abs(col1.R - col2.R) + Math.Abs(col1.G - col2.G) + Math.Abs(col1.B - col2.B);
        }

        private void AddDirtyRoom(ushort id, bool important)
        {
            for (int i = 0; i < DirtyRooms.Count; i++)
            {
                DirtyRoom room = DirtyRooms[i];

                if (room.RoomID == id)
                {
                    room.Priority = (important || room.Priority == int.MaxValue)
                        ? int.MaxValue : (room.Priority * 2);

                    DirtyRooms[i] = room;

                    return;
                }
            }

            DirtyRooms.Add(new DirtyRoom { RoomID = id, Priority = important ? int.MaxValue : 1 });
        }

        public void InvalidateOutdoors()
        {
            //if the outside color is too different from the last, we need to invalidate all instead.
            if (ColorDiff(Blueprint.OutsideColor, LastOutsideColor) > 20 || 
                (Blueprint.OutsideColor == Color.White && LastOutsideColor != Color.White))
            {
                InvalidateAll();
                return;
            }

            OutShadowFloor = -1;
            var rooms = Blueprint.Rooms;
            var lightRooms = Blueprint.Light;
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if ((!room.IsOutside && (WallComp == null || !lightRooms[i].Lights.Any(x => x.OutdoorsColor))) || room.WallLines == null) continue;
                AddDirtyRoom((ushort)i, false);
            }
        }

        public void InvalidateRoom(ushort room, bool important)
        {
            var rooms = Blueprint.Rooms;

            if (room >= rooms.Count) return;
            var rep = rooms[room];
            if (rep.Floor > RedrawFloor) return;

            AddDirtyRoom(room, important);
        }

        public void ParseInvalidated(sbyte floorLimit, WorldState state)
        {
            GD.BlendState = BlendState.AlphaBlend;
            SetMapLayout(3, 2);

            if (floorLimit > RedrawFloor)
            {
                RedrawAll(state, 6);
                RedrawFloor = 6;
            }

            if (DirtyRooms.Count == 0) return;

            //initialize lighteffect with default params
            sbyte floor = 0;
            SetFloor(floor, state);

            var rooms = Blueprint.Rooms;
            var lightRooms = Blueprint.Light;

            var ordered = DirtyRooms.OrderBy(x => rooms[x.RoomID].Floor);

            int unimportantRoomsProcessed = 0;

            foreach (var rm in ordered)
            {
                if (unimportantRoomsProcessed >= rm.Priority)
                {
                    continue;
                }

                var room = rooms[rm.RoomID];
                if (room.WallLines == null || room.Floor > floorLimit)
                {
                    DirtyRooms.Remove(rm);
                    continue;
                }
                if (room.Floor != floor)
                {
                    floor = room.Floor;
                    SetFloor(floor, state);
                }
                if (rm.RoomID >= lightRooms.Length) break;
                var light = lightRooms[rm.RoomID];
                DrawRoom(room, light, true);
                DirtyRooms.RemoveAll(x => x.RoomID == rm.RoomID);

                if (rm.Priority != int.MaxValue)
                {
                    unimportantRoomsProcessed++;
                }
            }

            GD.SetRenderTarget(null);
        }

        public void InvalidateAll()
        {
            LastOutsideColor = Blueprint.OutsideColor;
            RedrawFloor = 0;
        }

        public void RedrawAll(WorldState state, int floorLimit)
        {
            OutShadowFloor = -1;
            DirtyRooms.Clear();
            sbyte floor = 0;
            SetFloor(floor, state);
            var rooms = Blueprint.Rooms;
            var lightRooms = Blueprint.Light;

            GD.SetRenderTarget(LightMap);
            GD.Clear(Color.White * (Blueprint.MinOut.A / 255f));

            if (LightMapDirection != null)
            {
                GD.SetRenderTarget(LightMapDirection);
                GD.Clear(Color.TransparentBlack);
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room.WallLines == null || room.Floor > floorLimit) continue;
                if (room.Floor != floor)
                {
                    floor = room.Floor;
                    SetFloor(floor, state);
                }
                if (i >= lightRooms.Length) break;
                var light = lightRooms[i];
                DrawRoom(room, light, false);
            }

            GD.SetRenderTarget(null);
        }

        private Rectangle DrawRect;

        private float DayOffset = 0.25f;
        private float DayDuration = 0.60f;

        public LightData BuildOutdoorsLight(double tod)
        {
            DayOffset = 0.25f;
            DayDuration = 0.60f;

            bool night = false;
            double modTime;
            var offStart = 1 - (DayOffset + DayDuration);
            if (tod < DayOffset)
            {
                modTime = (offStart + tod) * 0.5 / (1 - DayDuration);
                night = true;
            }
            else if (tod > DayOffset + DayDuration)
            {
                modTime = (tod - (1 - offStart)) * 0.5 / (1 - DayDuration);
                night = true;
            }
            else
            {
                modTime = (tod - DayOffset) * 0.5 / DayDuration;
            }

            var light = new LightData()
            {
                //LightPos = tilePos * 16,
                LightPos = new Vector2(-1000, -1000),
                LightType = LightType.OUTDOORS
            };

            Matrix Transform = Matrix.Identity;

            Transform *= Matrix.CreateRotationY((float)((modTime + 0.5) * Math.PI * 2.0)); //Controls the rotation of the sun/moon around the city. 
            Transform *= Matrix.CreateRotationZ((float)(Math.PI * (45.0 / 180.0))); //Sun is at an angle of 45 degrees to horizon at it's peak. idk why, it's winter maybe? looks nice either way
            Transform *= Matrix.CreateRotationY((float)(Math.PI * 0.3)); //Offset from front-back a little. This might need some adjusting for the nicest sunset/sunrise locations.

            var lightPos = new Vector3(0, 0, -3000);
            lightPos = Vector3.Transform(lightPos, Transform);
            var z = lightPos.Z;
            if (lightPos.Y < 0) lightPos.Y *= -1;

            SunVector = lightPos;
            SunVector.Normalize();
            Night = night;

            light.LightPos = new Vector2(lightPos.Z, -lightPos.X);
            light.LightDir = -light.LightPos;
            light.LightDir.Normalize();
            lightPos.Normalize();

            LightVec = new Vector3(lightPos.Z, 1f, -lightPos.X);
            Blueprint.Terrain.LightVec = LightVec;

            light.FalloffMultiplier = (float)Math.Sqrt(lightPos.X * lightPos.X + lightPos.Z * lightPos.Z) / lightPos.Y;

            if (modTime > 0.25) modTime = 0.5 - modTime;

            if (Math.Abs(modTime) < 0.05) //Near the horizon, shadows should gracefully fade out into the opposite shadows (moonlight/sunlight)
                light.ShadowMultiplier = (float)((Math.Abs(modTime) * 20)) * 1f;
            else
                light.ShadowMultiplier = 1; //Shadow strength. Remember to change the above if you alter this.

            if (night) light.ShadowMultiplier *= 1.33f;

            OutdoorsLight = light;
            Blueprint.OutdoorsLight = light;

            return light;
        }

        public RasterizerState Scissor = new RasterizerState() { ScissorTestEnable = true, CullMode = CullMode.None };

        public void DrawRoom(Room room, RoomLighting lighting, bool clear)
        {
            var size = Blueprint.Width - borderSize;
            //TODO: set floor shadow map here to stop surrounding light issues
            LightEffect.floorShadowMap = ObjShadowTarg;
            LightEffect.TargetRoom = (float)room.RoomID; 
            var bigBounds = new Rectangle(lighting.Bounds.X * resPerTile, lighting.Bounds.Y * resPerTile, lighting.Bounds.Width * resPerTile, lighting.Bounds.Height * resPerTile);
            bigBounds = Rectangle.Intersect(bigBounds, new Rectangle(0, 0, size * resPerTile, size * resPerTile));
            GD.RasterizerState = Scissor;
            if (clear)
            {
                GD.SetRenderTarget(LightMap);
                DrawRect = bigBounds;
                DrawRect.Offset(ScissorBase);
                GD.ScissorRectangle = DrawRect;

                var effect = LightEffect;
                effect.CurrentTechnique = effect.Techniques[0];
                EffectPassCollection passes = effect.Techniques[0].Passes;
                effect.LightColor = Vector4.One * (Blueprint.MinOut.A / 255f);
                GD.BlendState = BlendState.Opaque;
                passes[2].Apply();

                GD.SetVertexBuffer(LightBuf);
                GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

                if (LightMapDirection != null)
                {
                    GD.SetRenderTarget(LightMapDirection);
                    DrawRect = ScaleDirectionScissor(DrawRect);
                    GD.ScissorRectangle = DrawRect;
                    effect.CurrentTechnique = effect.Techniques[1];
                    passes = effect.Techniques[1].Passes;
                    effect.LightColor = Vector4.Zero;
                    GD.BlendState = BlendState.Opaque;
                    passes[2].Apply();
                    GD.SetVertexBuffer(LightBuf);
                    GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                }
            }
            var outFactor = Vector4.One - Blueprint.MinOut.ToVector4();

            var factor = 16f / resPerTile;

            var colorTech = 0; //
            var dirTech = 1; //

            if (room.IsOutside || WallComp != null)
            {
                var res = (Blueprint.Width - borderSize) * resPerTile;
                if (!room.IsOutside)
                {
                    DrawRect = new Rectangle(0, 0, res, res);
                }
                else
                {
                    DrawRect = bigBounds;
                }
                if (OutdoorsLight == null) BuildOutdoorsLight(Blueprint.OutsideTime);
                var light = OutdoorsLight;
                //generate shadows
                light.Level = (sbyte)(room.Floor);
                DrawWallShadows(room.WallLines, light);
                if (room.IsOutside && !WorldConfig.Current.UltraLighting)
                {
                    DrawObjShadows(lighting.ObjectFootprints, light);
                }

                //draw the light onto the lightmap
                GD.SetRenderTarget(LightMap);
                DrawRect.Offset(ScissorBase);
                GD.ScissorRectangle = DrawRect;
                LightEffect.LightColor = Color.White.ToVector4() * outFactor.W;
                LightEffect.ShadowPowers = new Vector2(0.75f, 0.6f) * light.ShadowMultiplier;
                LightEffect.LightHeight = 1f/(float)Blueprint.Width;

                LightEffect.LightPosition = light.LightPos / (size * 16f); //in position space (0,1)
                LightEffect.LightDirection = new Vector3(-SunVector.Z, SunVector.Y*-1, SunVector.X);
                LightEffect.LightSize = float.MaxValue; //in position space (0,1)
                LightEffect.IsOutdoors = true;

                var effect = LightEffect;
                effect.CurrentTechnique = effect.Techniques[colorTech];
                EffectPassCollection passes = effect.Techniques[colorTech].Passes;
                passes[room.IsOutside ? ((WallComp == null) ? 1 : 4) : 3].Apply();

                GD.SetVertexBuffer(LightBuf);
                GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

                if (LightMapDirection != null)
                {
                    GD.SetRenderTarget(LightMapDirection);
                    effect.CurrentTechnique = effect.Techniques[dirTech];
                    passes = effect.Techniques[dirTech].Passes;
                    DrawRect = ScaleDirectionScissor(DrawRect);
                    GD.ScissorRectangle = DrawRect;

                    passes[room.IsOutside ? ((WallComp == null) ? 1 : 4) : 3].Apply();

                    GD.SetVertexBuffer(LightBuf);
                    GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                }

                LightEffect.shadowMap = ShadowTarg;
            }

            var order = lighting.Lights.OrderBy(x => x.OutdoorsColor ? 0 : 1);
            var hasMulOutside = false;
            foreach (var light in order)
            {
                if (!light.OutdoorsColor && !hasMulOutside)
                {
                    MultiplyOutdoors(bigBounds);
                    hasMulOutside = true;
                }
                if (light.WindowRoom != -1)
                {
                    var wroom = Blueprint.Light[Blueprint.Rooms[light.WindowRoom].Base];
                    light.LightIntensity = wroom.AmbientLight / 150f;
                }
                if (light.LightIntensity < 0.2f) continue;

                DrawRect = new Rectangle((int)(light.LightBounds.X / factor), (int)(light.LightBounds.Y / factor), (int)(light.LightBounds.Width / factor), (int)(light.LightBounds.Height / factor));
                DrawRect = Rectangle.Intersect(DrawRect, bigBounds);

                //generate shadows

                DrawWallShadows(room.WallLines, light);
                DrawObjShadows(lighting.ObjectFootprints, light);

                //draw the light onto the lightmap
                GD.SetRenderTarget(LightMap);
                DrawRect.Offset(ScissorBase);
                GD.ScissorRectangle = DrawRect;
                LightEffect.ShadowPowers = new Vector2(1f, 1f);

                LightEffect.LightPosition = light.LightPos / (size * 16f); //in position space (0,1)
                LightEffect.LightSize = light.LightSize / (size * 16f); //in position space (0,1)
                var l = light.LightColor.ToVector4();
                l.W = (l.X + l.Y + l.Z) / 3;
                
                if (light.OutdoorsColor) l = Vector4.Multiply(l, outFactor);
                else l *= 0.70f;
                LightEffect.LightColor = l;
                LightEffect.IsOutdoors = light.OutdoorsColor;
                LightEffect.LightIntensity = light.LightIntensity;

                var effect = LightEffect;
                effect.CurrentTechnique = effect.Techniques[colorTech];
                EffectPassCollection passes = effect.Techniques[colorTech].Passes;

                if (WorldConfig.Current.UltraLighting)
                {
                    LightEffect.BlurMin = (light.OutdoorsColor)?(1 / (Blueprint.Width*9f)):0;
                    LightEffect.BlurMax = (1 / (Blueprint.Width * 5f));
                    passes[5].Apply();
                } else
                    passes[0].Apply();

                GD.SetVertexBuffer(LightBuf);
                GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

                if (LightMapDirection != null)
                {
                    GD.SetRenderTarget(LightMapDirection);
                    effect.CurrentTechnique = effect.Techniques[dirTech];
                    passes = effect.Techniques[dirTech].Passes;
                    DrawRect = ScaleDirectionScissor(DrawRect);

                    passes[0].Apply();

                    GD.SetVertexBuffer(LightBuf);
                    GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                }
            }
            if (!hasMulOutside)
            {
                MultiplyOutdoors(bigBounds);
            }
        }

        public void MultiplyOutdoors(Rectangle bigBounds)
        {
            DrawRect = bigBounds;
            DrawRect.Offset(ScissorBase);
            for (int i = 0; i < (LightMapDirection != null ? 4 : 1); i++)
            {
                GD.SetRenderTarget((i==0)?LightMap:LightMapDirection);
                if (i == 1) DrawRect = ScaleDirectionScissor(DrawRect);
                GD.ScissorRectangle = DrawRect;

                var effect = LightEffect;
                var tech = (i == 1) ? 1 : 0;
                effect.CurrentTechnique = effect.Techniques[tech];
                EffectPassCollection passes = effect.Techniques[tech].Passes;
                var l = Blueprint.OutsideColor.ToVector4();
                l.W = (l.X + l.Y + l.Z) / 3;
                if (i >= 2) effect.LightColor = new Vector4(new Vector3(Math.Abs(SunVector.Z), Math.Abs(SunVector.Y), Math.Abs(SunVector.X)) * l.W * ((i==3)?-1:1), l.W);
                else effect.LightColor = l;
                GD.BlendState = MulBlend;
                passes[2].Apply();

                if (i == 2) GD.BlendState = MinBlend;
                else if (i == 3) GD.BlendState = MaxBlend;

                GD.SetVertexBuffer(LightBuf);
                GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }
        }

        private Rectangle ScaleDirectionScissor(Rectangle src)
        {
            var factor = LightMap.Width / (float)LightMapDirection.Width;
            return new Rectangle((int)(src.X / factor), (int)(src.Y / factor), (int)(src.Width / factor), (int)(src.Height / factor));
        }

        internal void DrawShadows(GradMesh geom, int pass, LightData light)
        {
            var pointLight = light.LightPos;
            var effect = this.GradEffect;
            effect.Projection = Projection;
            GD.ScissorRectangle = DrawRect;
            GD.Clear(Color.Black);

            effect.CurrentTechnique = effect.Techniques[0];
            EffectPassCollection passes = effect.Techniques[0].Passes;
            passes[pass].Apply();

            if (geom.VertexCount > 0) GD.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, geom.Vertices, 0, geom.VertexCount, geom.Indices, 0, geom.IndexCount / 3);
        }

        public Matrix GetSunlightMat(LightData pointLight)
        {
            var mat = Matrix.Identity;
            //we have to build our own matrix here, which is weird
            //the y axis has to contribute to the other two axis, using the light direction.

            mat.M11 = 1; mat.M12 = 0; mat.M31 = pointLight.LightDir.X;//; //x axis. 
            mat.M21 = 0; mat.M22 = 1; mat.M32 = pointLight.LightDir.Y;//light.LightDir.Y; //y axis.
            mat.M33 = 0;

            mat = Matrix.CreateScale(16, 16, 32 * pointLight.FalloffMultiplier) * mat;
            return mat;
        }

        private Matrix ProjFromTan(float tan, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
        {
            var result = new Matrix();
            float num = 1f / tan;
            float num9 = num / aspectRatio;
            result.M11 = num9;
            result.M12 = result.M13 = result.M14 = 0;
            result.M22 = num;
            result.M21 = result.M23 = result.M24 = 0;
            result.M31 = result.M32 = 0f;
            result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M34 = -1;
            result.M41 = result.M42 = result.M44 = 0;
            result.M43 = (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance);
            return result;
        }

        public Matrix GetLightMat(LightData pointLight)
        {
            //point light projection onto a floor surface.
            //this can get a bit weird!
            //we need to create a frustrum that starts at the light's position, and with all edges on the edge of the lightmap

            var height = pointLight.Height; //lights are assumed to be in the middle

            var tan = ((Blueprint.Width- borderSize) /2f) / height;
            var fov = (float)Math.Atan(tan);
            var lpos = new Vector2(pointLight.LightPos.X / 16f, pointLight.LightPos.Y / 16f);

            //return Matrix.CreateTranslation(-lpos.X, -lpos.Y, height) * Matrix.CreatePerspectiveFieldOfView(fov, 1, 0.01f, 3f) * Matrix.CreateTranslation(lpos.X, lpos.Y, 0);
            var mat = Matrix.CreateTranslation(-(lpos.X), -(lpos.Y), -height) * ProjFromTan(tan, 1, 0.01f, height) * Matrix.CreateScale(1, -1f, 1) * Matrix.CreateTranslation(lpos.X / (Blueprint.Width - borderSize) *2 - 1f, -(lpos.Y / ((Blueprint.Height - borderSize) /2f) - 1f), 0);
            return mat;
        }

        public BlendState MaxBlend = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Max,
            ColorDestinationBlend = Blend.One,
        };
        
        public BlendState MinBlend = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Min,
            ColorBlendFunction = BlendFunction.Min,
            ColorDestinationBlend = Blend.One,
        };

        public BlendState AddBlendRed = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Add,
            ColorBlendFunction = BlendFunction.Add,
            ColorDestinationBlend = Blend.One,
            ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Alpha
        };

        public BlendState MaxBlendRed = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Max,
            ColorDestinationBlend = Blend.One,
            ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Alpha
        };

        public BlendState MaxBlendGreen = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Max,
            ColorDestinationBlend = Blend.One,
            ColorWriteChannels = ColorWriteChannels.Green | ColorWriteChannels.Alpha
        };

        public BlendState MulBlend = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Add,
            ColorBlendFunction = BlendFunction.Add,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaDestinationBlend = Blend.SourceAlpha,
            ColorSourceBlend = Blend.Zero,
            AlphaSourceBlend = Blend.Zero
        };


        public void CreateOutsideIfMissing()
        {
            if (LastShadowTargQualityDivider != ShadowTargQualityDivider)
            {
                OutsideShadowTarg.Dispose();
                OutsideShadowTargPost.Dispose();
            }
            if (OutsideShadowTarg == null || OutsideShadowTarg.IsDisposed)
            {
                var div = ShadowTargQualityDivider;
                OutsideShadowTarg = new RenderTarget2D(GD, (ShadowTarg.Width*2)/div, (ShadowTarg.Height*2) / div, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                OutsideShadowTargPost = new RenderTarget2D(GD, (ShadowTarg.Width*2) / div, (ShadowTarg.Height*2) / div, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                LightEffect.SSAASize = new Vector2(1f / OutsideShadowTarg.Width, 1f / OutsideShadowTarg.Height);
                LastShadowTargQualityDivider = ShadowTargQualityDivider;
            }
        }

        public static BlendState OpaqueBA = new BlendState()
        {
            ColorBlendFunction = BlendFunction.Add,
            AlphaBlendFunction = BlendFunction.Add,

            ColorDestinationBlend = Blend.Zero,
            ColorSourceBlend = Blend.One,

            AlphaDestinationBlend = Blend.Zero,
            AlphaSourceBlend = Blend.One,

            ColorWriteChannels = ColorWriteChannels.Blue | ColorWriteChannels.Alpha
        };

        public void DrawWallShadows(List<Vector2[]> walls, LightData pointLight)
        {
            if (pointLight.LightType == LightType.OUTDOORS && WallComp != null)
            {
                CreateOutsideIfMissing();
                LightEffect.shadowMap = OutsideShadowTarg;

                if (OutShadowFloor == pointLight.Level) return;
                OutShadowFloor = pointLight.Level;
                GD.SetRenderTarget(OutsideShadowTarg);
                var rect = new Rectangle(DrawRect.X * 2, DrawRect.Y * 2, DrawRect.Width*2, DrawRect.Height*2);
                GD.ScissorRectangle = rect;
                GD.Clear(Color.Black);
                var effect = this.GradEffect;

                effect.Projection = Projection;

                var mat = GetSunlightMat(pointLight);

                GD.BlendState = MaxBlendRed;

                WallComp.DrawLMap(GD, pointLight, Projection, mat);

                GD.BlendState = MaxBlendRed;
                Blueprint.Terrain.DrawLMap(GD, pointLight, Projection, mat);
                Blueprint.RoofComp.DrawLMap(GD, pointLight, Projection, mat);

                effect.CurrentTechnique = effect.Techniques[0];
                EffectPassCollection passes = effect.Techniques[0].Passes;
                passes[2].Apply();

                if (WorldConfig.Current.UltraLighting) Draw3DObjShadows(pointLight, false);

                //blit outside shadows onto post target (for blur)

                if (OutsideShadowTargPost != null)
                {
                    var blend = GD.BlendState;
                    var rast = GD.RasterizerState;
                    var seffect = WorldContent.SpriteEffect;

                    //seffect.blurAmount = 0.7f / Blueprint.Width);
                    //seffect.blurAmount = 0.4f / Blueprint.Width);
                    //seffect.heightMultiplier = pointLight.FalloffMultiplier);

                    var blur = (0.2f / Blueprint.Width) * (float)Math.Pow(pointLight.FalloffMultiplier, 0.8f);
                    var height = Math.Max(pointLight.FalloffMultiplier / 1.5f, 1);
                    var harden = 0.03f * (float)Math.Sqrt(pointLight.FalloffMultiplier);

                    //lower shadow target quality as blur size increases (to save on memory bandwidth)
                    if (pointLight.FalloffMultiplier > 6)
                    {
                        ShadowTargQualityDivider = 4;
                    }
                    if (pointLight.FalloffMultiplier > 3)
                    {
                        ShadowTargQualityDivider = 2;
                    }
                    else
                    {
                        ShadowTargQualityDivider = 1;
                    }

                    seffect.blurAmount = new Vector2(blur, blur * 2 / 5f);
                    seffect.heightMultiplier = new Vector2(height, height * 5 / 2f);
                    seffect.hardenBias = new Vector2(harden, harden * 0.5f);
                    seffect.noiseTexture = TextureGenerator.GetUniformNoise(GD);

                    for (int i=0; i<4; i++)
                    {
                        seffect.SetTechnique((int)SpriteEffectTechniques.ShadowSeparableBlit1 + i);
                        RenderTarget2D tex;
                        if (i%2 == 0)
                        {
                            GD.SetRenderTarget(OutsideShadowTargPost);
                            tex = OutsideShadowTarg;
                        } else
                        {
                            GD.SetRenderTarget(OutsideShadowTarg);
                            tex = OutsideShadowTargPost;
                        }

                        ShadowTargBlit.Begin(blendState: (i == 1)? OpaqueBA : BlendState.Opaque, effect: seffect, samplerState: SamplerState.PointClamp);
                        ShadowTargBlit.Draw(tex, new Rectangle(0, 0, tex.Width, tex.Height), Color.White);
                        ShadowTargBlit.End();
                    }
                    /*
                    ShadowTargBlit.Begin(blendState: BlendState.Opaque, effect: seffect);
                    seffect.CurrentTechnique = seffect.Techniques["ShadowBlurBlit"];
                    seffect.blurAmount = 0.7f / Blueprint.Width);
                    seffect.heightMultiplier = pointLight.FalloffMultiplier);
                    seffect.noiseTexture"]?.SetValue(TextureGenerator.GetUniformNoise(GD));
                    ShadowTargBlit.Draw(OutsideShadowTarg, new Rectangle(0, 0, OutsideShadowTarg.Width, OutsideShadowTarg.Height), Color.White);
                    ShadowTargBlit.End();
                    */

                    GD.SetRenderTarget(OutsideShadowTarg);
                    GD.RasterizerState = rast;
                    GD.BlendState = blend;
                }
            }
            else
            {
                GD.SetRenderTarget(ShadowTarg);
                var geom = ShadowGeo.GenerateWallShadows(walls, pointLight);
                GD.BlendState = AddBlendRed;
                DrawShadows(geom, (pointLight.LightType == LightType.OUTDOORS) ? 2 : 0, pointLight);
            }
        }

        public void DrawObjShadows(List<Rectangle> objects, LightData pointLight)
        {
            GD.SetRenderTarget(ObjShadowTarg);
            if (WorldConfig.Current.UltraLighting)
            {
                Draw3DObjShadows(pointLight, true);
            }
            else
            {
                GradMesh geom;
                if (pointLight.LightType == LightType.ROOM)
                {
                    geom = ShadowGeo.GenerateObjShadows(objects.Where(x => x.Intersects(pointLight.LightBounds)).ToList(), pointLight);
                }
                else
                {
                    geom = ShadowGeo.GenerateObjShadows(objects, pointLight);
                }

                GD.BlendState = MaxBlendGreen;
                DrawShadows(geom, 1, pointLight);
            }
        }

        public void Draw3DObjShadows(LightData pointLight, bool clear)
        {
            var effect = WorldContent.RCObject;

            //we doubled the shadow resolution, so this is different.
            var dr = DrawRect;
            GD.ScissorRectangle = new Rectangle(dr.X*2, dr.Y*2, dr.Width*2, dr.Height*2);
            GD.BlendState = MaxBlendGreen;
            if (clear) GD.Clear(Color.Black);

            effect.SetTechnique(RCObjectTechniques.LMapDraw);
            EffectPassCollection passes = effect.Techniques[0].Passes;
            passes[0].Apply();

            var outside = pointLight.LightType == LightType.OUTDOORS;

            if (outside)
                effect.ViewProjection = Matrix.CreateScale(1 / 3f, -1/9f, 1 / 3f) * Matrix.CreateRotationX((float)Math.PI/-2) * GetSunlightMat(pointLight) * Projection;
            else
                effect.ViewProjection = Matrix.CreateScale(1 / 3f, -1 / 3f, 1 / 3f) * Matrix.CreateRotationX((float)Math.PI / -2) * GetLightMat(pointLight);

            var lp16 = pointLight.LightPos / 16f;
            var li16 = pointLight.LightSize / 16f;

            List<ObjectComponent> objs;

            if (outside) {
                objs = new List<ObjectComponent>();
                for (int i = 0; i < Blueprint.Rooms.Count; i++)
                {
                    var room = Blueprint.Rooms[i];
                    if (room.IsOutside && room.Base == i)
                    {
                        //add components from this room to obj list
                        objs.AddRange(Blueprint.Light[i].Components);
                    }
                }
            } else
            {
                objs = Blueprint.Light[pointLight.Room].Components;
            }

            foreach (var obj in objs)
            {
                if ((outside && obj.Level > pointLight.Level) || (Math.Abs(obj.Position.X - lp16.X) + Math.Abs(obj.Position.Y - lp16.Y) < li16))
                {
                    obj.DrawLMap(GD, pointLight.Level);
                }
            }
        }

        public RenderTarget2D DebugLightMap()
        {
            RedrawAll(null, 6);
            return LightMap;
        }

        public RenderTarget2D DebugShadows(List<Vector2[]> walls, LightData pointLight)
        {
            DrawWallShadows(walls, pointLight);
            return ShadowTarg;
        }


        public RenderTarget2D DebugShadows(List<Rectangle> objects, LightData pointLight)
        {
            DrawObjShadows(objects, pointLight);
            return ShadowTarg;
        }

        public void Dispose()
        {
            ShadowTarg?.Dispose();
            ObjShadowTarg?.Dispose();
            OutsideShadowTarg?.Dispose();
            OutsideShadowTargPost?.Dispose();
            LightMap?.Dispose();
            LightMapDirection?.Dispose();
        }
    }
}
