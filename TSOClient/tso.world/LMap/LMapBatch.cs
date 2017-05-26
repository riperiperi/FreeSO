using FSO.LotView.Model;
using FSO.Vitaboy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.LMap
{
    public class LMapBatch : IDisposable
    {
        int resPerTile = 8;

        public RenderTarget2D ShadowTarg;
        public RenderTarget2D ObjShadowTarg;

        public RenderTarget2D LightMap;

        public List<Texture2D> RoomMaps;

        GraphicsDevice GD;
        private Effect GradEffect;
        private Effect LightEffect;
        Matrix Projection;

        private Blueprint Blueprint;
        private LightData OutdoorsLight;
        public Vector2 MapLayout; //width * height floors. Ordered by width first. x = floor%width, y = (int)floot/width.
        public Vector2 InvMapLayout;

        public HashSet<ushort> DirtyRooms = new HashSet<ushort>();
        public sbyte RedrawFloor;

        bool cleared = false;

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
            var factor = new Vector3(1f / (3 * (w-2)), 1 / (3 * 2.95f), 1f / (3 * (h-2)));
            factor *= invMapLayout;

            WorldContent.GrassEffect.Parameters["WorldToLightFactor"].SetValue(factor);
            WorldContent._2DWorldBatchEffect.Parameters["WorldToLightFactor"].SetValue(factor);
            Avatar.Effect.Parameters["WorldToLightFactor"].SetValue(factor);

            WorldContent.GrassEffect.Parameters["MapLayout"].SetValue(MapLayout);
            WorldContent._2DWorldBatchEffect.Parameters["MapLayout"].SetValue(MapLayout);
            LightEffect.Parameters["MapLayout"].SetValue(MapLayout);
            Avatar.Effect.Parameters["MapLayout"].SetValue(MapLayout);
        }

        public void SetFloor(sbyte floor)
        {
            //the goal is to center 0-(75*16)
            var width = (int)MapLayout.X;
            var height = (int)MapLayout.Y;
            var x = floor % width;
            var y = floor / width;
            //var floorWidth = 75 * 16;
            //var floorHeight = 75 * 16;

            LightEffect.Parameters["UVBase"].SetValue(new Vector2(x, y));
            LightEffect.Parameters["roomMap"].SetValue(RoomMaps[floor]);

            var res = (Blueprint.Width - 2) * 8;
            ScissorBase = new Point(res * x, res * y);
        }

        public void Init(Blueprint blueprint)
        {
            this.Blueprint = blueprint;

            var w = blueprint.Width;
            var h = blueprint.Height;

            var wl = 8 * (w-2);
            var wh = 8 * (h-2);

            Dispose();

            ShadowTarg = new RenderTarget2D(GD, wl, wh, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            ObjShadowTarg = new RenderTarget2D(GD, wl, wh, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            LightMap = new RenderTarget2D(GD, wl * 3, wh * 2, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents); //just ground floor for now.
            RoomMaps = new List<Texture2D>();
            for (int i = 0; i < 5; i++) RoomMaps.Add(new Texture2D(GD, w, h, false, SurfaceFormat.Color));
            Projection = Matrix.CreateOrthographicOffCenter(new Rectangle(0, 0, (w-2) * 16, (h-2) * 16), -10, 10);

            //initialize lighteffect with default params
            LightEffect.Parameters["shadowMap"].SetValue(ShadowTarg);
            LightEffect.Parameters["floorShadowMap"].SetValue(ObjShadowTarg);
            LightEffect.Parameters["ShadowPowers"].SetValue(new Vector2(1f, 1f));
            LightEffect.Parameters["RoomUVOff"].SetValue(new Vector2(-1f / w, -1f / h));
            LightEffect.Parameters["TileSize"].SetValue(new Vector2(resPerTile / (w * 8f), resPerTile / (h * 8f)));

            LightEffect.Parameters["Projection"].SetValue(Matrix.CreateOrthographicOffCenter(new Rectangle(0, 0, 1, 1), -10, 10));
            LightEffect.Parameters["LightPower"].SetValue(2.0f);
            LightEffect.Parameters["RoomUVRescale"].SetValue(new Vector2((w - 2) / (float)w, (h - 2) / (float)h));
            LightEffect.Parameters["RoomUVOff"].SetValue(new Vector2(0, 0));

            SetMapLayout(3, 2);
        }

        public void SetRoomMap(sbyte floor, uint[] map)
        {
            RoomMaps[floor].SetData(map);
            var test = new Color[RoomMaps[floor].Width * RoomMaps[floor].Height];
            RoomMaps[floor].GetData<Color>(test);
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
            cleared = false;
        }

        public void InvalidateOutdoors()
        {
            sbyte floor = 0;
            SetFloor(floor);

            var rooms = Blueprint.Rooms;
            var lightRooms = Blueprint.Light;
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (!room.IsOutside || room.WallLines == null) continue;
                DirtyRooms.Add((ushort)i);
                /*
                if (room.Floor != floor)
                {
                    floor = room.Floor;
                    SetFloor(floor);
                }
                var light = lightRooms[i];
                DrawRoom(room, light, true);
                */
            }
        }

        public void InvalidateRoom(ushort room)
        {
            var rooms = Blueprint.Rooms;
            var lightRooms = Blueprint.Light;

            var rep = rooms[room];
            if (rep.Floor > RedrawFloor) return;

            DirtyRooms.Add(room);
        }

        public void ParseInvalidated(sbyte floorLimit)
        {
            SetMapLayout(3, 2);
            if (floorLimit > RedrawFloor)
            {
                RedrawAll();
                RedrawFloor = 6;
            }

            //initialize lighteffect with default params
            sbyte floor = 0;
            SetFloor(floor);

            var rooms = Blueprint.Rooms;
            var lightRooms = Blueprint.Light;

            var dirty = new List<ushort>(DirtyRooms);
            foreach (var rm in dirty)
            {
                var room = rooms[rm];
                if (room.WallLines == null || room.Floor > floorLimit) continue;
                if (room.Floor != floor)
                {
                    floor = room.Floor;
                    SetFloor(floor);
                }
                var light = lightRooms[rm];
                DrawRoom(room, light, true);
                DirtyRooms.Remove(rm);
            }
            GD.SetRenderTarget(null);
        }

        public void InvalidateAll()
        {
            RedrawFloor = 0;
        }

        public void RedrawAll()
        {
            DirtyRooms.Clear();
            sbyte floor = 0;
            SetFloor(floor);
            var rooms = Blueprint.Rooms;
            var lightRooms = Blueprint.Light;

            GD.SetRenderTarget(LightMap);
            GD.Clear(Color.TransparentBlack);

            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room.WallLines == null) continue;
                if (room.Floor != floor)
                {
                    floor = room.Floor;
                    SetFloor(floor);
                }
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

            Matrix Transform = Microsoft.Xna.Framework.Matrix.Identity;

            Transform *= Microsoft.Xna.Framework.Matrix.CreateRotationY((float)((modTime + 0.5) * Math.PI * 2.0)); //Controls the rotation of the sun/moon around the city. 
            Transform *= Microsoft.Xna.Framework.Matrix.CreateRotationZ((float)(Math.PI * (45.0 / 180.0))); //Sun is at an angle of 45 degrees to horizon at it's peak. idk why, it's winter maybe? looks nice either way
            Transform *= Microsoft.Xna.Framework.Matrix.CreateRotationY((float)(Math.PI * 0.3)); //Offset from front-back a little. This might need some adjusting for the nicest sunset/sunrise locations.

            var lightPos = new Vector3(0, 0, -3000);
            lightPos = Vector3.Transform(lightPos, Transform);
            var z = lightPos.Z;
            if (lightPos.Y < 0) lightPos.Y *= -1;
            light.LightPos = new Vector2(lightPos.Z, -lightPos.X);
            light.LightDir = -light.LightPos;
            light.LightDir.Normalize();
            lightPos.Normalize();
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

        public void DrawRoom(Room room, RoomLighting lighting, bool clear)
        {
            var size = Blueprint.Width - 2;
            LightEffect.Parameters["TargetRoom"].SetValue((float)room.RoomID);
            var bigBounds = new Rectangle(lighting.Bounds.X * resPerTile, lighting.Bounds.Y * resPerTile, lighting.Bounds.Width * resPerTile, lighting.Bounds.Height * resPerTile);
            bigBounds = Rectangle.Intersect(bigBounds, new Rectangle(0, 0, size*8, size*8));
            GD.RasterizerState = new RasterizerState() { ScissorTestEnable = true, CullMode = CullMode.None };
            if (clear)
            {
                GD.SetRenderTarget(LightMap);
                DrawRect = bigBounds;
                DrawRect.Offset(ScissorBase);
                GD.ScissorRectangle = DrawRect;

                var effect = LightEffect;
                effect.CurrentTechnique = effect.Techniques[0];
                EffectPassCollection passes = effect.Techniques[0].Passes;
                passes[2].Apply();

                GD.SetVertexBuffer(LightBuf);
                GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

            var factor = 16f / resPerTile;
            foreach (var light in lighting.Lights)
            {
                if (light.WindowRoom != -1)
                {
                    var wroom = Blueprint.Light[Blueprint.Rooms[light.WindowRoom].Base];
                    light.LightIntensity = wroom.AmbientLight / 150f;
                }
                if (light.LightIntensity < 0.2f) continue;

                DrawRect = new Rectangle((int)(light.LightBounds.X / factor), (int)(light.LightBounds.Y / factor), (int)(light.LightBounds.Width / factor), (int)(light.LightBounds.Height / factor));
                DrawRect = Rectangle.Intersect(DrawRect, bigBounds);

                //generate shadows
                DrawObjShadows(lighting.ObjectFootprints, light);
                DrawWallShadows(room.WallLines, light);

                //draw the light onto the lightmap
                GD.SetRenderTarget(LightMap);
                DrawRect.Offset(ScissorBase);
                GD.ScissorRectangle = DrawRect;
                LightEffect.Parameters["ShadowPowers"].SetValue(new Vector2(1f, 1f));

                LightEffect.Parameters["LightPosition"].SetValue(light.LightPos / (size*16f)); //in position space (0,1)
                LightEffect.Parameters["LightSize"].SetValue(light.LightSize / (size*16f)); //in position space (0,1)
                LightEffect.Parameters["IsOutdoors"].SetValue(light.OutdoorsColor);
                LightEffect.Parameters["LightIntensity"].SetValue(light.LightIntensity);

                var effect = LightEffect;
                effect.CurrentTechnique = effect.Techniques[0];
                EffectPassCollection passes = effect.Techniques[0].Passes;
                passes[0].Apply();

                GD.SetVertexBuffer(LightBuf);
                GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }


            if (room.IsOutside)
            {
                var res = (Blueprint.Width-2) * 8;
                DrawRect = new Rectangle(0, 0, res, res);
                if (OutdoorsLight == null) BuildOutdoorsLight(Blueprint.OutsideTime);
                var light = OutdoorsLight;
                //generate shadows
                DrawObjShadows(lighting.ObjectFootprints, light);
                DrawWallShadows(room.WallLines, light);

                //draw the light onto the lightmap
                GD.SetRenderTarget(LightMap);
                DrawRect.Offset(ScissorBase);
                GD.ScissorRectangle = DrawRect;
                LightEffect.Parameters["ShadowPowers"].SetValue(new Vector2(0.75f, 0.6f) * light.ShadowMultiplier);

                LightEffect.Parameters["LightPosition"].SetValue(light.LightPos / (size * 16f)); //in position space (0,1)
                LightEffect.Parameters["LightSize"].SetValue(float.MaxValue); //in position space (0,1)
                LightEffect.Parameters["IsOutdoors"].SetValue(true);

                var effect = LightEffect;
                effect.CurrentTechnique = effect.Techniques[0];
                EffectPassCollection passes = effect.Techniques[0].Passes;
                passes[1].Apply();

                GD.SetVertexBuffer(LightBuf);
                GD.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }
        }

        public void DrawShadows(Tuple<GradVertex[], int[]> geom, int pass, LightData light)
        {
            var pointLight = light.LightPos;
            var effect = this.GradEffect;
            effect.Parameters["Projection"].SetValue(Projection);
            GD.ScissorRectangle = DrawRect;
            GD.Clear(Color.Black);
            /*GD.SetRenderTarget(ShadowTarg);
            if (!cleared)
            {
                cleared = true;
                //GD.RasterizerState = new RasterizerState() { ScissorTestEnable = true, CullMode = CullMode.None };
                //GD.ScissorRectangle = new Rectangle((int)pointLight.X / 2 - 100, (int)pointLight.Y / 2 - 100, 200, 200);
                GD.Clear(Color.Black);
            }*/
            effect.CurrentTechnique = effect.Techniques[0];
            EffectPassCollection passes = effect.Techniques[0].Passes;
            passes[pass].Apply();

            if (geom.Item1.Length > 0) GD.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, geom.Item1, 0, geom.Item1.Length, geom.Item2, 0, geom.Item2.Length / 3);
            //GD.SetRenderTarget(null);
        }

        public void DrawWallShadows(List<Vector2[]> walls, LightData pointLight)
        {
            GD.SetRenderTarget(ShadowTarg);
            var geom = ShadowGeometry.GenerateWallShadows(walls, pointLight);
            DrawShadows(geom, (pointLight.LightType == LightType.OUTDOORS)?2:0, pointLight);
        }

        public void DrawObjShadows(List<Rectangle> objects, LightData pointLight)
        {
            GD.SetRenderTarget(ObjShadowTarg);
            Tuple<GradVertex[], int[]> geom;
            if (pointLight.LightType == LightType.ROOM)
            {
                geom = ShadowGeometry.GenerateObjShadows(objects.Where(x => x.Intersects(pointLight.LightBounds)).ToList(), pointLight);
            } else
            {
                geom = ShadowGeometry.GenerateObjShadows(objects, pointLight);
            }
            DrawShadows(geom, 1, pointLight);
        }


        public RenderTarget2D DebugLightMap()
        {
            RedrawAll();
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
            LightMap?.Dispose();
            if (RoomMaps != null)
            {
                foreach (var map in RoomMaps)
                {
                    map?.Dispose();
                }
            }
        }
    }
}
