using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common;
using FSO.Common.Utils;
using FSO.HIT;
using FSO.LotView.Components.Model;
using FSO.LotView.Components.SM64Geo;
using FSO.LotView.Model;
using FSO.LotView.RC;
using FSO.LotView.Utils.Camera;
using Mario.Controller;
using Mario.Data;
using Mario.Entities;
using Mario.Enum;
using Mario.Geo;
using Mario.Math;
using Mario.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FSO.LotView.Components
{
    /*
     * Design for SM64 geometry:
     * 
     * Surface Groups:
     * 
     * === 0-4 | Floor Levels ===
     *  - Floors are a bit weird due to sharing their base geometry (terrain) but linking it with 3DFloorGeometry later
     *  - Lookup table for ground type ushort -> (SurfaceType, TerrainType), where default is (SurfaceType.SURFACE_DEFAULT, TerrainType.TERRAIN_STONE)
     *  - Can't really make swimmable pools for the deadline (water boxes). Make them fire instead. (SURFACE_BURNING)
     * 
     * === 5 | Walls ===
     *  - All in one. This kind of sucks since it needs to generate all levels, but just do it like shadow RC does.
     *  - Wall surface is SurfaceType.SURFACE_DEFAULT, TerrainType.TERRAIN_STONE
     * 
     * === 6 | Roof ===
     *  - Roof surface is SurfaceType.SURFACE_VERY_SLIPPERY, TerrainType.TERRAIN_STONE
     *  - Built from the roof geometry, updated when the roof is updated. Simple enough.
     * 
     * === Starting at 7 | Object ===
     *  - Objects are a bit weird. Try calculate an object bounding box, then make a basic box for each (tangible) object on the property.
     *  - Maybe make them a bit slippy?
     *  - Stairs/Multitile will be weird. Do we want to replace certain GUIDs with our own bounding box/triangle definitions? (beds?)
     *  - GUID lookup for objects to determine surface type? (burning for fire?)
     *  - Maybe some weird interactions for certain object types. Could be a stretch, since interactions are hard to add.
     */

    // Rules for event:
    // Can't show anything until:
    // - Animation data is present (from server or local Content)
    // - lot is fully loaded
    // Do not spawn a controllable mario or generate collision until:
    // - A controller is plugged (and the component has been shown)

    internal class VisualMario : IDisposable
    {
        public AvatarComponent Avatar;
        private SM64Component Component;

        public Mario.Mario Mario;
        public MarioObject MarioObj;
        public SM64GeometryEmit GeoEmit;

        public Vector3 LastPosition;
        public Vector3 LastRotation;
        public Vector3 LastScale;
        public Matrix[] LastAnimMatrices = new Matrix[15];

        public Vector3? Position;
        public Vector3? Rotation;
        public Vector3? Scale;
        public Matrix[] AnimMatrices = new Matrix[15];
        public float InterpProgress;

        public bool NewFrame;
        public bool Valid = false;

        public Queue<SM64VisualState> QueuedFrames = new Queue<SM64VisualState>();

        public VisualMario(SM64Component component)
        {
            Component = component;

            Mario = new Mario.Mario();
            Mario.Area = new Area();
            MarioObj = new MarioObject();
            GeoEmit = new SM64GeometryEmit();
        }

        public void SubmitMatrices(List<Matrix> matrices)
        {
            var temp = AnimMatrices;
            
            if (LastAnimMatrices.Length != matrices.Count)
            {
                Array.Resize(ref LastAnimMatrices, matrices.Count);
            }

            matrices.CopyTo(LastAnimMatrices);

            AnimMatrices = LastAnimMatrices;
            LastAnimMatrices = temp;
        }

        public void Dispose()
        {
            GeoEmit.Dispose();
        }

        public Vector3 GetMarioPosition()
        {
            //var pos = MarioObj.Header.Gfx.Pos;
            var pos = Vector3.Lerp(LastPosition, Position ?? LastPosition, InterpProgress) / 3;

            return new Vector3(pos.X, pos.Z, pos.Y);// * (1/3f) / 80f;
        }

        private Vector2 GetScreenPos(Vector3 pos, WorldState world)
        {
            var projected = Vector4.Transform(new Vector4(pos, 1f), world.View * world.Projection);
            if (world.CameraMode < CameraRenderMode._3D) projected.Z = 1;
            var res1 = new Vector2(projected.X / projected.Z, -projected.Y / projected.Z);
            var size = PPXDepthEngine.GetWidthHeight();
            return new Vector2((size.X / PPXDepthEngine.SSAA) * 0.5f * (res1.X + 1f), (size.Y / PPXDepthEngine.SSAA) * 0.5f * (res1.Y + 1f));
        }

        public Tuple<float, float> CalculateVolumePan()
        {
            var worldState = Component.State;
            var worldSpace = worldState.WorldSpace;

            var pos = GetMarioPosition() * 3;
            pos = new Vector3(pos.X, pos.Z, pos.Y);
            //worldState.Camera.View * 
            var scrPos = GetScreenPos(pos, worldState);
            scrPos -= new Vector2(worldSpace.WorldPxWidth / 2, worldSpace.WorldPxHeight / 2);

            float pan = Math.Max(-1.0f, Math.Min(1.0f, scrPos.X / worldSpace.WorldPxWidth));
            pan = pan * pan * ((pan > 0) ? 1 : -1);

            float volume = 1f;

            var rcs = worldState.Cameras.ActiveCamera as CameraController3D;
            if (rcs != null)
            {
                var vp = pos;
                var delta = rcs.Camera.Target - new Vector3(vp.X, vp.Y, vp.Z);
                delta.Z /= 3f;
                //volume = 4f / delta.Length();
                volume = 1.5f - delta.Length() / 40f;
                volume *= (10 / ((rcs.Zoom3D * rcs.Zoom3D) + 10));
                volume *= worldState.PreciseZoom;
            }
            else
            {
                volume = 1 - (float)Math.Max(0, Math.Min(1, Math.Sqrt(scrPos.X * scrPos.X + scrPos.Y * scrPos.Y) / worldSpace.WorldPxWidth));
                volume *= worldState.PreciseZoom;

                volume /= 4 - (int)worldState.Zoom;
            }

            volume = Math.Min(1f, Math.Max(0f, volume));

            return new Tuple<float, float>(volume, pan);
        }

        public sbyte DetermineLevel(bool forLight)
        {
            return Component.DetermineLevel(GetMarioPosition(), forLight);
        }
    }

    public class CollisionObject
    {
        public int SlotID;
        public EntityComponent Entity;

        public CollisionObject(int id, EntityComponent entity)
        {
            SlotID = id;
            Entity = entity;
        }
    }

    public class SM64Component : IDisposable
    {
        private const float WorldToSm64 = 80f / 1f;

        private static AnimSource AnimData;

        private Blueprint Bp;
        internal WorldState State;
        private Collision Collision = new Collision();
        private DataSource Data;
        private SM64Scene Scene;
        private Mario.Mario Mario;
        private MarioObject MyMarioObj;
        private bool TerrainUpdated = false;
        private bool DidInitMario = false;
        private bool MarioActiveForMe = false;
        private int DeathFrames = 0;

        private SM64GeometryEmit GeoEmit;
        private MarioGeo Geo;

        private ControllerState LastState = new ControllerState();

        public SM64VisualState MyVisualState;
        public Queue<uint> SoundQueue = new Queue<uint>();
        public short MyID = 0;

        internal Dictionary<AvatarComponent, VisualMario> OtherMarios = new Dictionary<AvatarComponent, VisualMario>();
        internal VisualMario MyMario;

        internal Dictionary<int, CollisionObject> CollisionObjectsById = new Dictionary<int, CollisionObject>();
        internal Dictionary<EntityComponent, CollisionObject> CollisionObjectsByEnt = new Dictionary<EntityComponent, CollisionObject>();
        internal List<CollisionObject> CollisionObjects = new List<CollisionObject>();
        private int ColId;

        private SM64ObjectGeometry ObjectGeometry = new SM64ObjectGeometry();

        /// <summary>
        /// Base surfaces for the terrain. Note that each tile has two triangles.
        /// 1st floor grass tiles reference these base tiles without copying.
        /// Placed floor tiles and upper floors make copies of these base tiles.
        /// </summary>
        private Surface[] TerrainBase;

        private float UpdateRemainder;
        private float UpdateRemainderOther;

        private Light[] TexColors = new Light[]
        {
            new Light(0, 0, 255), // Blue
            new Light(255, 0, 0), // Red
            new Light(254, 193, 121), // Don't know really
            new Light(115, 6, 0), // Brown?
            new Light(255, 255, 255), // White
            new Light(114, 28, 14), // IDK
        };

        private Texture2D Texture;
        public Matrix[] WorkingMatrices = new Matrix[15];

        public SM64Component(Blueprint bp)
        {
            Bp = bp;
            TerrainBase = new Surface[bp.Width * bp.Height * 2];

            try
            {
                Data = new RomSource(new FileStream("Content/sm64.z64", FileMode.Open, FileAccess.Read));
            }
            catch
            {

            }

            Scene = new SM64Scene(this);

            MyMario = new VisualMario(this);

            Mario = MyMario.Mario;
            MyMarioObj = MyMario.MarioObj;
            GeoEmit = MyMario.GeoEmit;
            Geo = new MarioGeo(GeoEmit);

            ColId = bp.Stories + 2;
        }

        public void Init(bool playSound = true)
        {
            Area area = new Area();

            DeathFrames = 0;

            var start = GetBaseMarioPos();

            var pos = start.Item1 * 3 * WorldToSm64;

            var angle = new Vec3s((short)((start.Item2.X / Math.PI) * 32768), (short)((start.Item2.Y / Math.PI) * 32768), (short)((start.Item2.Z / Math.PI) * 32768));

            var spawnInfo = new Mario.SpawnInfo()
            {
                StartPos = new Vec3s((short)pos.X, (short)(pos.Z + 1000), (short)pos.Y),
                StartAngle = angle
            };

            Mario.InitFromSaveFile(spawnInfo);

            Mario.Init(Scene, spawnInfo,
            area,
            MyMarioObj,
            Collision,
            Data);

            Mario.SetMarioAction(MarioAction.ACT_SPAWN_SPIN_AIRBORNE, 0);

            Scene.play_sound(1, new Vec3f());
        }

        public void UpdateOtherMario(AvatarComponent avatar, SM64VisualState state)
        {
            if (!OtherMarios.TryGetValue(avatar, out VisualMario other))
            {
                other = new VisualMario(this);
                other.Avatar = avatar;

                OtherMarios.Add(avatar, other);
            }

            avatar.MyMario = other;

            other.QueuedFrames.Enqueue(state);
        }

        public Tuple<Vector3, Vector3> GetBaseMarioPos()
        {
            // Try find the mailbox
            var mailbox = Bp.Objects.FirstOrDefault(x => x.Obj.OBJ.GUID == 0xEF121974);

            Vector3 pos;
            Vector3 angle = new Vector3();

            if (mailbox != null)
            {
                pos = new Vector3(mailbox.Position.X + 0.5f, mailbox.Position.Y + 0.5f, 0f);

                pos += Vector3.Transform(new Vector3(0, 1, 0), Matrix.CreateRotationZ(-mailbox.RadianDirection));

                angle = new Vector3(0, mailbox.RadianDirection, 0);
            }
            else
            {
                pos = new Vector3(37.5f, 70.5f, 0f);
            }

            float elevation = Bp.InterpAltitude(pos);
            pos.Z = elevation;

            return new Tuple<Vector3, Vector3>(pos, angle);
        }

        public bool TileIndoors(int x, int y, int level)
        {
            if (x < 0 || y < 0 || level < 0 || x >= Bp.Width || y >= Bp.Height || level >= Bp.Stories)
            {
                return false;
            }

            var room = Bp.RoomMap[level - 1][x + y * Bp.Width];
            var room1 = room & 0xFFFF;
            var room2 = (room >> 16) & 0x7FFF;
            if (room1 < Bp.Rooms.Count && !Bp.Rooms[(int)room1].IsOutside) return true;
            if (room2 > 0 && room2 < Bp.Rooms.Count && !Bp.Rooms[(int)room2].IsOutside) return true;
            return false;
        }

        public sbyte DetermineLevel(Vector3 pos, bool forLight)
        {
            float elevation = Bp.InterpAltitude(pos);
            float height = pos.Z - elevation;

            sbyte level = (sbyte)(Math.Max(0, Math.Min(Bp.Stories - 1, Math.Floor((height + 0.5f) / 2.95f))) + 1);

            return forLight || TileIndoors((int)pos.X, (int)pos.Y, level) ? level : (sbyte)(Bp.Stories - 1);
        }

        public void RemoveMario(AvatarComponent avatar)
        {
            // Removes this mario from existence, as its parent avatar is gone.

            if (OtherMarios.TryGetValue(avatar, out VisualMario other))
            {
                other.Dispose();

                OtherMarios.Remove(avatar);
            }
        }

        public void PlaySound(AvatarComponent avatar, uint sound)
        {
            if (OtherMarios.TryGetValue(avatar, out var other))
            {
                Scene.SetSource(other);

                Scene.play_sound(sound, new Vec3f());
            }
        }

        public static void SetAnimData(byte[] data)
        {
            // First, we need to decompress the data.

            try
            {
                using (var mem = new MemoryStream(data))
                {
                    var lengthBytes = new byte[4];
                    mem.Read(lengthBytes, 0, 4);
                    int length = BitConverter.ToInt32(lengthBytes, 0);

                    byte[] resultData = new byte[length];
                    using (var gz = new GZipStream(mem, CompressionMode.Decompress))
                    {
                        gz.Read(resultData, 0, length);
                    }

                    AnimData = new AnimSource(new MemoryStream(resultData));
                }
            }
            catch
            {
                // Didn't work.
            }
        }

        private ControllerState GenerateControllerState()
        {
            // Generate controller state from the first plugged in XNA controller.

            var controllerState = new ControllerState();
            
            var gamepad = GamePad.GetState(0);

            if (gamepad.Buttons.Start == ButtonState.Pressed && gamepad.Buttons.Back == ButtonState.Pressed)
            {
                // Reset mario
                Init(false);
            }

            if (gamepad.Buttons.B == ButtonState.Pressed) controllerState.ButtonDown |= Button.A_BUTTON;
            if (gamepad.Buttons.A == ButtonState.Pressed) controllerState.ButtonDown |= Button.B_BUTTON;
            if (gamepad.Buttons.RightShoulder == ButtonState.Pressed) controllerState.ButtonDown |= Button.Z_TRIG;
            if (gamepad.Buttons.LeftShoulder == ButtonState.Pressed) controllerState.ButtonDown |= Button.Z_TRIG;

            controllerState.StickX = gamepad.ThumbSticks.Left.X * 64;
            controllerState.StickY = gamepad.ThumbSticks.Left.Y * 64;

            controllerState.RawStickX = (short)(gamepad.ThumbSticks.Left.X * 32767f);
            controllerState.RawStickY = (short)(gamepad.ThumbSticks.Left.Y * 32767f);

            controllerState.ButtonPressed = controllerState.ButtonDown & (~LastState.ButtonDown);

            controllerState.StickMag = (float)Math.Sqrt(controllerState.StickX * controllerState.StickX + controllerState.StickY * controllerState.StickY);

            LastState = controllerState;

            return controllerState;
        }

        private void CameraController(WorldState world)
        {
            var refreshMultiplier = 1f / FSOEnvironment.RefreshRate;
            var gamepad = GamePad.GetState(0);

            var camera = world.Cameras.Camera3D;
            camera.RotationX += gamepad.ThumbSticks.Right.X * refreshMultiplier * -3;
            camera.RotationY += gamepad.ThumbSticks.Right.Y * refreshMultiplier * -4;
        }

        private void InitCollision(WorldState world)
        {
            UpdateTerrain();
            Bp.WCRC?.Generate(world.Device, world, false, false);
            UpdateWalls();
            Bp.WCRC?.Generate(world.Device, world, true);
            UpdateRoof();

            foreach (var obj in Bp.Objects)
            {
                UpdateObject(obj);
            }
        }

        public void Update(GraphicsDevice device, WorldState world, bool visible)
        {
            State = world; // bleh

            if (Data == null)
            {
                if (AnimData != null)
                {
                    Data = AnimData;
                }
                else
                {
                    return;
                }
            }

            if (world.ScrollAnchor != null && world.ScrollAnchor.MyMario != null && world.CameraMode == CameraRenderMode._3D)
            {
                CameraController(world);
            }

            if (visible)
            {
                UpdateRemainderOther -= 1f / FSOEnvironment.RefreshRate;
                while (UpdateRemainderOther <= 0)
                {
                    foreach (var other in OtherMarios.Values)
                    {
                        while (other.QueuedFrames.Count > 2) other.QueuedFrames.Dequeue(); // Too far ahead.

                        if (other.QueuedFrames.Count > 0)
                        {
                            var state = other.QueuedFrames.Dequeue();

                            var obj = other.MarioObj;
                            var gfx = obj.Header.Gfx;

                            gfx.Pos = new Vec3f(state.PosX, state.PosY, state.PosZ);
                            gfx.Scale = new Vec3f(state.ScaleX, state.ScaleY, state.ScaleZ);
                            gfx.Angle = new Vec3s(state.AngleX, state.AngleY, state.AngleZ);

                            var animInfo = gfx.AnimInfo;

                            other.Mario.Area.UpdateCounter = state.GlobalAnimTimer;

                            animInfo.AnimID = state.AnimID;
                            animInfo.AnimYTrans = state.AnimYTrans;
                            animInfo.AnimFrame = state.AnimFrame;
                            animInfo.AnimTimer = state.AnimTimer;
                            animInfo.AnimFrameAccelAssist = state.AnimFrameAccelAssist;
                            animInfo.AnimAccel = state.AnimAccel;

                            // Load the animation

                            other.Valid = false;

                            try
                            {
                                Data.LoadMarioAnimation(ref other.Mario.Animation, (MarioAnimation)animInfo.AnimID);
                                animInfo.CurAnim = other.Mario.Animation.TargetAnim;

                                bool drawing = other.GeoEmit.Reset();
                                Geo.SetGeoEmit(other.GeoEmit);
                                Geo.EnableDisplayLists(drawing);
                                Geo.SetAnimationGlobals(other.Mario, other.MarioObj.Header.Gfx.AnimInfo);
                                Geo.mario_geo_body();
                                other.GeoEmit.End(drawing);
                            }
                            catch
                            {
                                // No animation, or didn't work.
                            }

                            other.NewFrame = true;
                            other.Valid = true;
                        }
                    }

                    UpdateRemainderOther += 1 / 30f;
                }
            }

            if (!MarioActiveForMe)
            {
                var gamepad = GamePad.GetState(0);

                if (gamepad.IsConnected && visible)
                {
                    MarioActiveForMe = true;
                    InitCollision(world);
                }

                return;
            }

            // Should only execute at 30 fps.
            if (world != null)
            {
                Scene.SetSource(MyMario);

                if (DidInitMario)
                {
                    if (MyMario.Avatar == null && MyID != 0)
                    {
                        // Look up my avatar, and bind to them.
                        foreach (var avatar in Bp.Avatars)
                        {
                            if (avatar.ObjectID == MyID)
                            {
                                MyMario.Avatar = avatar;
                                avatar.MyMario = MyMario;
                            }
                        }
                    }

                    UpdateRemainder -= 1f / FSOEnvironment.RefreshRate;
                    while (UpdateRemainder <= 0)
                    {
                        if (DeathFrames > 0)
                        {
                            if (--DeathFrames == 0)
                            {
                                Init();
                            }
                        }

                        Mario.State.Controller = GenerateControllerState();

                        Mario.Area.UpdateCounter++;

                        var wallOffset = world.GetFrontDirection();
                        Mario.Area.Camera.Yaw = Angle.Atan2s(wallOffset.Y, wallOffset.X);

                        try
                        {
                            // TODO: update camera yaw
                            // TODO: displacement? (on top of interpolated movement objects? we only really have cars and ducks lol)
                            Mario.MarioUpdate();

                            if (Mario.Floor == null) return;
                            // TODO: update_mario_platform (also displacement related)

                            UpdateVisualState();

                            bool drawing = GeoEmit.Reset();
                            Geo.SetGeoEmit(GeoEmit);
                            Geo.EnableDisplayLists(drawing);
                            Geo.SetAnimationGlobals(Mario, Mario.MarioObj.Header.Gfx.AnimInfo);
                            Geo.mario_geo_body();
                            GeoEmit.End(drawing);

                            MyMario.NewFrame = true;
                        }
                        catch
                        {
                            // don't crash the game if something goes wrong here
                            DidInitMario = false;
                        }
                        //Mario.GeoProcess();

                        UpdateRemainder += 1 / 30f;
                    }
                }
                else
                {
                    Init();

                    DidInitMario = true;
                }
            }
        }

        private void UpdateVisualState()
        {
            ref SM64VisualState state = ref MyVisualState;

            state.Active = true;
            state.GlobalAnimTimer = Mario.Area.UpdateCounter;
            state.PosX = Mario.MarioObj.Header.Gfx.Pos.X;
            state.PosY = Mario.MarioObj.Header.Gfx.Pos.Y;
            state.PosZ = Mario.MarioObj.Header.Gfx.Pos.Z;
            state.ScaleX = Mario.MarioObj.Header.Gfx.Scale.X;
            state.ScaleY = Mario.MarioObj.Header.Gfx.Scale.Y;
            state.ScaleZ = Mario.MarioObj.Header.Gfx.Scale.Z;
            state.AngleX = Mario.MarioObj.Header.Gfx.Angle.X;
            state.AngleY = Mario.MarioObj.Header.Gfx.Angle.Y;
            state.AngleZ = Mario.MarioObj.Header.Gfx.Angle.Z;
            state.AnimID = Mario.MarioObj.Header.Gfx.AnimInfo.AnimID;
            state.AnimYTrans = Mario.MarioObj.Header.Gfx.AnimInfo.AnimYTrans;
            state.AnimFrame = Mario.MarioObj.Header.Gfx.AnimInfo.AnimFrame;
            state.AnimTimer = Mario.MarioObj.Header.Gfx.AnimInfo.AnimTimer;
            state.AnimFrameAccelAssist = Mario.MarioObj.Header.Gfx.AnimInfo.AnimFrameAccelAssist;
            state.AnimAccel = Mario.MarioObj.Header.Gfx.AnimInfo.AnimAccel;
        }

        private Vector3 SmartLerp(Vector3 from, Vector3 to, float fac, float threshold)
        {
            return new Vector3(
                (Math.Abs(to.X - from.X) > threshold) ? to.X : (from.X + (to.X - from.X) * fac),
                (Math.Abs(to.Y - from.Y) > threshold) ? to.Y : (from.Y + (to.Y - from.Y) * fac),
                (Math.Abs(to.Z - from.Z) > threshold) ? to.Z : (from.Z + (to.Z - from.Z) * fac)
                );
        }

        private Matrix SmartLerp(Matrix from, Matrix to, float fac)
        {
            // Check if any transformation axis flip too violently.
            var v11 = Math.Abs(to.M11 - from.M11);
            var v12 = Math.Abs(to.M12 - from.M12);
            var v13 = Math.Abs(to.M13 - from.M13);

            var v21 = Math.Abs(to.M21 - from.M21);
            var v22 = Math.Abs(to.M22 - from.M22);
            var v23 = Math.Abs(to.M23 - from.M23);

            var v31 = Math.Abs(to.M31 - from.M31);
            var v32 = Math.Abs(to.M32 - from.M32);
            var v33 = Math.Abs(to.M33 - from.M33);

            if (v11 > 1 || v12 > 1 || v13 > 1 || v21 > 1 || v22 > 1 || v23 > 1 || v31 > 1 || v32 > 1 || v33 > 1)
            {
                return to;
            }

            return Matrix.Lerp(from, to, fac);
        }

        private void GenerateTexture(GraphicsDevice gd)
        {
            // Generate the colors texture.

            Color[] data = new Color[16 * 16];

            int index = 0;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int baseId = (y > 7) ? 3 : 0;

                    int colorId = baseId + (x / 6);

                    var c = TexColors[colorId];
                    data[index++] = new Color(c.R, c.G, c.B, (byte)255);
                }
            }

            Texture = new Texture2D(gd, 16, 16);
            Texture.SetData(data);
        }

        private void DrawMario(GraphicsDevice gd, WorldState state, VisualMario visual)
        {
            if (Texture == null)
            {
                GenerateTexture(gd);
            }

            MarioObject obj = visual.MarioObj;
            SM64GeometryEmit geoEmit = visual.GeoEmit;

            float baseScale = 1 / (WorldToSm64 * 3);

            var pos = ToVector3(obj.Header.Gfx.Pos) / WorldToSm64;
            var angle = obj.Header.Gfx.Angle;
            Vector3 rotation = new Vector3((float)((angle.X / 32768.0) * Math.PI), (float)((angle.Y / 32768.0) * Math.PI), (float)((angle.Z / 32768.0) * Math.PI));
            var scale = ToVector3(obj.Header.Gfx.Scale) * baseScale;

            var matrices = geoEmit.ExportMatrices();

            if (visual.NewFrame)
            {
                visual.LastPosition = visual.Position ?? pos;
                visual.LastRotation = visual.Rotation ?? rotation;
                visual.LastScale = visual.Scale ?? scale;

                visual.SubmitMatrices(matrices);

                visual.NewFrame = false;
                visual.InterpProgress = 0;
            }

            visual.Position = pos;
            visual.Rotation = rotation;
            visual.Scale = scale;

            pos = Vector3.Lerp(visual.LastPosition, pos, visual.InterpProgress);
            rotation = SmartLerp(visual.LastRotation, rotation, visual.InterpProgress, 0.4f);
            scale = Vector3.Lerp(visual.LastScale, scale, visual.InterpProgress);

            sbyte level = visual.DetermineLevel(true);

            Matrix world = Matrix.CreateScale(scale) *
                Matrix.CreateRotationZ(rotation.Z) *
                Matrix.CreateRotationX(rotation.X) *
                Matrix.CreateRotationY(rotation.Y) *
                Matrix.CreateTranslation(pos);

            //visual.Matrix = world;

            visual.InterpProgress = Math.Min(1, visual.InterpProgress + state.FramePerDraw);

            if (WorkingMatrices.Length != matrices.Count)
            {
                Array.Resize(ref WorkingMatrices, matrices.Count);
            }

            var newMatrices = WorkingMatrices;
            Matrix avatarHead = new Matrix();

            for (int i=0; i<matrices.Count; i++)
            {
                newMatrices[i] = (SmartLerp(visual.LastAnimMatrices[i], visual.AnimMatrices[i], visual.InterpProgress));
                if (i == 2 && visual.Avatar != null)
                {
                    avatarHead = newMatrices[i];
                    newMatrices[i] = new Matrix(); // Remove the head.
                }
                
            }

            //geoEmit.ImportMatrices(newMatrices);

            var geo = geoEmit.Complete(gd);

            if (geo.Item3 == 0) return;

            //var ocolor = state.OutsideColor;
            /*
            var effect = WorldContent.GetBE(gd);

            var color = state.OutsideColor;

            effect.LightingEnabled = false;
            effect.Alpha = 1;
            effect.DiffuseColor = color.ToVector3();
            effect.AmbientLightColor = Vector3.One;
            effect.VertexColorEnabled = false;
            effect.TextureEnabled = false;
            effect.VertexColorEnabled = true;

            effect.View = state.Camera.View;
            effect.Projection = state.Camera.Projection;

            effect.World = world;
            gd.DepthStencilState = DepthStencilState.Default;
            gd.RasterizerState = RasterizerState.CullNone;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.Indices = geo.Item2;//PlaceholderIndices;
                gd.SetVertexBuffer(geo.Item1);//PlaceholderVerts);

                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geo.Item3); //PlaceholderPrimCount);
            }
            */


            var aEffect = WorldContent.AvatarEffect;
            var technique = aEffect.CurrentTechnique;

            aEffect.Parameters["Level"].SetValue((level - 1) + 0.0001f);
            aEffect.Parameters["AmbientLight"].SetValue(WorldConfig.Current.AdvancedLighting ? new Vector4(1) : state.OutsideColor.ToVector4());
            aEffect.Parameters["World"].SetValue(world);
            aEffect.Parameters["SkelBindings"].SetValue(newMatrices);
            aEffect.Parameters["MeshTex"].SetValue(Texture);

            foreach (var pass in aEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.Indices = geo.Item2;//PlaceholderIndices;
                gd.SetVertexBuffer(geo.Item1);//PlaceholderVerts);

                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geo.Item3); //PlaceholderPrimCount);
            }

            if (visual.Avatar != null)
            {
                var avatar = visual.Avatar;
                //var room = (avatar.Room > 65530 || Room == 0) ? Room : blueprint.Rooms[Room].Base;
                foreach (var pass in technique.Passes)
                {
                    aEffect.Parameters["ObjectID"].SetValue(avatar.ObjectID / 65535f);
                    pass.Apply();

                    var rotateHead = Matrix.CreateFromYawPitchRoll((float)(Math.PI / 2), (float)(Math.PI / -2), (float)(Math.PI / 2));

                    avatar.Avatar.DrawHeadOnly(gd, aEffect, rotateHead * Matrix.CreateScale(WorldToSm64 * 3.8f) * Matrix.CreateTranslation(new Vector3(35, 0, 0)) * avatarHead);
                }
            }
        }

        public void Draw(GraphicsDevice gd, WorldState state)
        {
            if (MarioActiveForMe)
            {
                DrawMario(gd, state, MyMario);
            }

            foreach (var other in OtherMarios.Values)
            {
                if (!other.Valid) continue;

                DrawMario(gd, state, other);
            }
        }

        private Vec3i ToVec3i(Vector3 vec)
        {
            return new Vec3i((int)vec.X, (int)vec.Y, (int)vec.Z);
        }

        private Vec3f ToVec3f(Vector3 vec)
        {
            return new Vec3f(vec.X, vec.Y, vec.Z);
        }

        private Vector3 ToVector3(Vec3i vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        private Vector3 ToVector3(Vec3f vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        private Surface CreateSurface(Vector3 v1, Vector3 v2, Vector3 v3, SurfaceType type, TerrainType terrain)
        {
            var normal1 = Vector3.Cross(v2 - v1, v3 - v1);
            normal1.Normalize();

            float minY = Math.Min(v1.Y, Math.Min(v2.Y, v3.Y));
            float maxY = Math.Max(v1.Y, Math.Max(v2.Y, v3.Y));

            return new Surface()
            {
                Type = type,
                Vertex1 = ToVec3i(v1), // libsm64: 32 bit
                Vertex2 = ToVec3i(v2), // libsm64: 32 bit
                Vertex3 = ToVec3i(v3), // libsm64: 32 bit
                Normal = ToVec3f(normal1),
                LowerY = (int)minY + 5,
                UpperY = (int)maxY + 5,
                OriginOffset = -Vector3.Dot(normal1, v1),
                Terrain = terrain
            };
        }

        public void EnsureGroupExists(int groupId)
        {
            var groups = Collision.Groups;
            while (groups.Count <= groupId)
            {
                groups.Add(new CollisionGroup());
            }
        }

        public void UpdateTerrain()
        {
            if (Bp == null || !MarioActiveForMe)
            {
                return;
            }

            TerrainComponent terrain = Bp.Terrain;
            int height = Bp.Height;
            int width = Bp.Width;

            var quadWidth = WorldSpace.GetWorldFromTile(1);
            var quadHeight = WorldSpace.GetWorldFromTile(1);

            SurfaceType defaultSurface = SurfaceType.SURFACE_DEFAULT;
            TerrainType defaultTerrain = TerrainType.TERRAIN_GRASS;

            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var tl = new Vector3(x * quadWidth, 0.0f, y * quadHeight);
                    var tr = new Vector3(tl.X + quadWidth, 0.0f, tl.Z);
                    var bl = new Vector3(tl.X, 0.0f, tl.Z + quadHeight);
                    var br = new Vector3(tl.X + quadWidth, 0.0f, tl.Z + quadHeight);

                    tl.Y = terrain.GetElevationPoint(x, y);
                    tr.Y = terrain.GetElevationPoint(x + 1, y);
                    bl.Y = terrain.GetElevationPoint(x, y + 1);
                    br.Y = terrain.GetElevationPoint(x + 1, y + 1);

                    tl *= WorldToSm64;
                    tr *= WorldToSm64;
                    bl *= WorldToSm64;
                    br *= WorldToSm64;

                    // Generate the triangles for the terrain.

                    // Triangle 1
                    // The tri order is flipped so that the normals point in the proper direction.
                    var triangle1 = CreateSurface(br, tr, tl, defaultSurface, defaultTerrain);

                    // Triangle 2
                    var triangle2 = CreateSurface(tl, bl, br, defaultSurface, defaultTerrain);

                    TerrainBase[index++] = triangle1;
                    TerrainBase[index++] = triangle2;
                }
            }

            TerrainUpdated = true;

            UpdateFloors();
        }

        private Dictionary<ushort, Tuple<SurfaceType, TerrainType>> SpecialFloors = new Dictionary<ushort, Tuple<SurfaceType, TerrainType>>()
        {
            { 65535, new Tuple<SurfaceType, TerrainType>(SurfaceType.SURFACE_BURNING, TerrainType.TERRAIN_WATER) }
        };

        public void UpdateFloors()
        {
            if (Bp == null || !MarioActiveForMe)
            {
                return;
            }

            if (!TerrainUpdated)
            {
                UpdateTerrain();
            }

            var floorGeo = Bp.FloorGeom;

            var floors = floorGeo.Floors;

            var tile = new Vector3[4];

            EnsureGroupExists(floors.Length - 1);

            for (int i = 0; i < floors.Length; i++)
            {
                var floor = floors[i];
                var group = Collision.Groups[i];
                var surfaces = group.Surfaces;

                surfaces.Clear();

                Vector3 heightOffset = new Vector3(0, i * 2.95f * 3 * WorldToSm64, 0);

                foreach (var tuple in floor.GroupForTileType)
                {
                    ushort floorType = tuple.Key;

                    if (floorType == 0 && i > 0)
                    {
                        continue; // Air tiles are not collidable.
                    }

                    var tilegroup = tuple.Value;

                    SurfaceType type = SurfaceType.SURFACE_DEFAULT;
                    TerrainType terrain = TerrainType.TERRAIN_STONE;

                    if (SpecialFloors.TryGetValue(floorType, out var special))
                    {
                        type = special.Item1;
                        terrain = special.Item2;
                    }

                    foreach (var tiletuple in tilegroup.GeomForOffset)
                    {
                        if (floorType == 0)
                        {
                            // Just use the base terrain triangles.
                            // Select a tile by picking the first vertex, dividing by 4.

                            int tileIndex = (tiletuple.Value[0] / 4);

                            // Then select both triangles.

                            surfaces.Add(TerrainBase[tileIndex << 1]);
                            surfaces.Add(TerrainBase[(tileIndex << 1) | 1]);
                        }
                        else
                        {
                            // Generate new triangles using the indices.
                            // They're all going to be on the same tile...
                            // Select it by picking the first vertex, dividing by 4.

                            int tileIndex = (tiletuple.Value[0] / 4);
                            int baseIndex = tileIndex * 4;

                            var tri1 = TerrainBase[tileIndex << 1];
                            var tri2 = TerrainBase[(tileIndex << 1) | 1];

                            tile[0] = ToVector3(tri1.Vertex3);
                            tile[1] = ToVector3(tri1.Vertex2);
                            tile[2] = ToVector3(tri1.Vertex1);
                            tile[3] = ToVector3(tri2.Vertex2);

                            var indices = tiletuple.Value;
                            for (int j = 0; j < indices.Count; j += 3)
                            {
                                var v1 = tile[indices[j + 2] - baseIndex] + heightOffset;
                                var v2 = tile[indices[j + 1] - baseIndex] + heightOffset;
                                var v3 = tile[indices[j] - baseIndex] + heightOffset;

                                // Tri order is flipped so that the normals point in the proper direction.
                                var triangle = CreateSurface(v1,
                                    v2,
                                    v3,
                                    type,
                                    terrain);

                                surfaces.Add(triangle);

                                if (i > 0)
                                {
                                    // Add an underside for the floor
                                    var triangle2 = CreateSurface(v3,
                                        v2,
                                        v1,
                                        type,
                                        terrain);

                                    surfaces.Add(triangle2);
                                }
                            }
                        }
                    }
                }
            }
        }

        private Vector3 WallToVector(WallVertexRC wall, bool useOffset)
        {
            Vector3 vec = wall.Position * WorldToSm64 * 3f;

            if (!useOffset)
            {
                // On thin fences, try half the height.

                float uvY = wall.Info.Y % 1;

                if (uvY > 0.97f && uvY < 0.99f)
                {
                    // This vertex should be lowered.
                    vec.Z -= 2.95f * 1.5f * WorldToSm64;
                }
            }

            return new Vector3(vec.X, vec.Z, vec.Y);
        }

        private void Add(ref Vec3i vec, int x, int y, int z)
        {
            vec.X += x;
            vec.Y += y;
            vec.Z += z;
        }

        private void ToBoundingBox(List<Surface> surfaces, ObjectComponent obj)
        {
            BoundingBox bounds = obj.GetParticleBounds();
            bounds = BoundingBox.CreateFromPoints(bounds.GetCorners().Select(x => Vector3.Transform(x, obj.World3D)));

            //obj.GetRawBounds();

            // Convert from bounding box into a 5 sided cube (do not include the bottom)
            // top/bottom is Z, left/right is X

            Vector3 tr = bounds.Max * WorldToSm64;
            Vector3 bl = bounds.Min * WorldToSm64;
            bl.Y = tr.Y;
            Vector3 tl = new Vector3(bl.X, bl.Y, tr.Z);
            Vector3 br = new Vector3(tr.X, bl.Y, bl.Z);

            ToBoundingBox(surfaces, tl, tr, br, bl, bounds.Min.Y* WorldToSm64);
        }

        private void ToBoundingBox(List<Surface> surfaces, SM64ObjectGeometryObj geo, ObjectComponent obj)
        {
            // Rotate and position the geo based on the object transform.

            var mat = obj.World3D;

            foreach (var box in geo.Boxes)
            {
                ToBoundingBox(surfaces,
                    Vector3.Transform(box.Tl, mat) * WorldToSm64,
                    Vector3.Transform(box.Tr, mat) * WorldToSm64,
                    Vector3.Transform(box.Br, mat) * WorldToSm64,
                    Vector3.Transform(box.Bl, mat) * WorldToSm64,
                    obj.Position.Z * 3 * WorldToSm64);
            }
        }

        private void ToBoundingBox(List<Surface> surfaces, Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl, float floor)
        {
            Vector3 ftr = tr;
            Vector3 fbl = bl;
            Vector3 ftl = tl;
            Vector3 fbr = br;

            ftr.Y = floor;
            fbl.Y = ftr.Y;
            ftl.Y = ftr.Y;
            fbr.Y = ftr.Y;

            SurfaceType type = SurfaceType.SURFACE_DEFAULT;
            TerrainType terrain = TerrainType.TERRAIN_STONE;

            // top

            Surface surf;

            surf = CreateSurface(tl,
                tr,
                br,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            surf = CreateSurface(br,
                bl,
                tl,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            // tr/br side

            surf = CreateSurface(br,
                tr,
                fbr,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            surf = CreateSurface(fbr,
                tr,
                ftr,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            // bl/br side 

            surf = CreateSurface(bl,
                br,
                fbl,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            surf = CreateSurface(fbl,
                br,
                fbr,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            // tr/tl side

            surf = CreateSurface(tr,
                tl,
                ftr,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            surf = CreateSurface(ftr,
                tl,
                ftl,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            // tl/bl side

            surf = CreateSurface(tl,
                bl,
                ftl,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);

            surf = CreateSurface(ftl,
                bl,
                fbl,
                type,
                terrain);
            if (!float.IsNaN(surf.Normal.Y)) surfaces.Add(surf);
        }

        public void UpdateWalls()
        {
            if (!MarioActiveForMe)
            {
                return;
            }

            int wallIndex = Bp.Floors.Length;
            EnsureGroupExists(wallIndex);

            if (Bp.WCRC == null)
            {
                return;
            }

            var wallGroups = Bp.WCRC.GroupsByTexture;

            SurfaceType defaultType = SurfaceType.SURFACE_DEFAULT;
            TerrainType defaultTerrain = TerrainType.TERRAIN_STONE;

            var colgroup = Collision.Groups[wallIndex];
            var surfaces = colgroup.Surfaces;

            var walls = FSO.Content.Content.Get().WorldWalls;

            surfaces.Clear();

            foreach (var levelGroup in wallGroups)
            {
                foreach (var group in levelGroup.Values)
                {
                    if (group.MaskId == 3 || group.MaskId == 15 || walls.GetWallStyle(group.MaskId)?.IsDoor == true)
                    {
                        // If the maskID is one of the default doors (3, 15) or was registered by a door object, make the wall non-solid.
                        continue;
                    }

                    var verts = group.Verts;
                    var indices = group.Indices;
                    bool useOffset = group.UseOffset;
                    for (int j = 0; j < indices.Count; j += 3)
                    {
                        var triangle = CreateSurface(WallToVector(verts[indices[j + 2]], useOffset),
                                    WallToVector(verts[indices[j + 1]], useOffset),
                                    WallToVector(verts[indices[j]], useOffset),
                                    defaultType,
                                    defaultTerrain);

                        if (!useOffset)
                        {
                            int thinWallFactor = 50;
                            int xAdd = (int)(triangle.Normal.X * thinWallFactor);
                            int yAdd = (int)(triangle.Normal.Y * thinWallFactor);
                            int zAdd = (int)(triangle.Normal.Z * thinWallFactor);

                            Add(ref triangle.Vertex1, xAdd, yAdd, zAdd);
                            Add(ref triangle.Vertex2, xAdd, yAdd, zAdd);
                            Add(ref triangle.Vertex3, xAdd, yAdd, zAdd);

                            triangle.OriginOffset = -Vector3.Dot(ToVector3(triangle.Normal), ToVector3(triangle.Vertex1));
                        }

                        surfaces.Add(triangle);
                    }
                }
            }
        }

        public void UpdateRoof()
        {
            if (!MarioActiveForMe)
            {
                return;
            }

            int roofIndex = Bp.Floors.Length + 1;
            EnsureGroupExists(roofIndex);

            SurfaceType defaultType = SurfaceType.SURFACE_HARD_SLIPPERY;
            TerrainType defaultTerrain = TerrainType.TERRAIN_STONE;

            var colgroup = Collision.Groups[roofIndex];
            var surfaces = colgroup.Surfaces;

            surfaces.Clear();

            foreach (var group in Bp.RoofComp.Drawgroups)
            {
                if (group == null) continue;

                var verts = group.Data.Vertices;
                var indices = group.Data.Indices;

                for (int j = 0; j < indices.Length; j += 3)
                {
                    var triangle = CreateSurface(verts[indices[j]].Position * WorldToSm64,
                                verts[indices[j + 1]].Position * WorldToSm64,
                                verts[indices[j + 2]].Position * WorldToSm64,
                                defaultType,
                                defaultTerrain);

                    if (float.IsNaN(triangle.Normal.X)) continue;

                    surfaces.Add(triangle);

                    if (triangle.Normal.Y > 0.1)
                    {
                        // Create a ceiling for the roof too, so they can't pop out from the inside.

                        triangle = CreateSurface(verts[indices[j + 2]].Position * WorldToSm64,
                            verts[indices[j + 1]].Position * WorldToSm64,
                            verts[indices[j]].Position * WorldToSm64,
                            defaultType,
                            defaultTerrain);

                        if (float.IsNaN(triangle.Normal.X)) continue;

                        surfaces.Add(triangle);
                    }
                }
            }
        }

        public void RemoveObject(EntityComponent obj)
        {
            // Find this object on the list, unallocate its id and remove its collision.
            if (CollisionObjectsByEnt.TryGetValue(obj, out var col))
            {
                DeleteFromObjList(CollisionObjects, col);
                CollisionObjectsByEnt.Remove(obj);
                CollisionObjectsById.Remove(col.SlotID);

                Collision.Groups[col.SlotID].Surfaces.Clear();

                if (col.SlotID < ColId) ColId = col.SlotID; //this id is now the smallest free object id.
            }
        }

        public void UpdateObject(EntityComponent obj)
        {
            if (!MarioActiveForMe)
            {
                return;
            }

            if (!(obj is ObjectComponent) || obj.Position.X < 0) return;

            // Does this object exist?
            if (!CollisionObjectsByEnt.TryGetValue(obj, out var col))
            {
                if (!obj.AvatarSolid) return;

                col = AddCol(obj);
            }

            if (!obj.AvatarSolid)
            {
                RemoveObject(obj);

                return;
            }

            // Create this object's surfaces.

            EnsureGroupExists(col.SlotID);

            var surfaces = Collision.Groups[col.SlotID].Surfaces;

            surfaces.Clear();

            var asObj = (ObjectComponent)obj;
            var geo = ObjectGeometry.GetByGUID(asObj.Obj.OBJ.GUID);

            if (geo == null)
            {
                ToBoundingBox(surfaces, asObj);
            }
            else
            {
                ToBoundingBox(surfaces, geo, asObj);
            }
        }

        private int NextColID()
        {
            for (int i = ColId; i > 0; i++)
                if (!CollisionObjectsById.ContainsKey(i)) return i;
            return 0;
        }

        public CollisionObject AddCol(EntityComponent entity)
        {
            var entry = new CollisionObject(ColId, entity);
            CollisionObjectsById.Add(ColId, entry);
            CollisionObjectsByEnt.Add(entity, entry);
            AddToObjList(this.CollisionObjects, entry);
            ColId = NextColID();
            return entry;
        }

        public static void AddToObjList(List<CollisionObject> list, CollisionObject entity)
        {
            if (list.Count == 0) { list.Add(entity); return; }
            int id = entity.SlotID;
            int max = list.Count;
            int min = 0;
            while (max > min)
            {
                int mid = (max + min) / 2;
                int nid = list[mid].SlotID;
                if (id < nid) max = mid;
                else if (id == nid) return; //do not add dupes
                else min = mid + 1;
            }
            list.Insert(min, entity);
            // list.Insert((list[min].ObjectID>id)?min:((list[max].ObjectID > id)?max:max+1), entity);
        }

        public static void DeleteFromObjList(List<CollisionObject> list, CollisionObject entity)
        {
            if (list.Count == 0) { return; }
            int id = entity.SlotID;
            int max = list.Count;
            int min = 0;
            while (max > min)
            {
                int mid = (max + min) / 2;
                int nid = list[mid].SlotID;
                if (id < nid) max = mid;
                else if (id == nid)
                {
                    list.RemoveAt(mid); //found it
                    return;
                }
                else min = mid + 1;
            }
            //list.RemoveAt(min);
        }

        public void Death()
        {
            if (DeathFrames == 0)
            {
                DeathFrames = 60;
            }
        }
        
        private AvatarComponent LocateAvatar(AvatarComponent ava)
        {
            return Bp.Avatars.FirstOrDefault(x => x.ObjectID == ava.ObjectID);
        }

        public void MigrateSM64(WorldState state, Blueprint blueprint)
        {
            Bp = blueprint;

            // Repoint marios to new avatar components.
            if (MyMario.Avatar != null)
            {
                MyMario.Avatar = LocateAvatar(MyMario.Avatar);

                if (MyMario.Avatar != null)
                {
                    MyMario.Avatar.MyMario = MyMario;
                }
            }

            var others = OtherMarios.Values.ToList();
            OtherMarios.Clear();
            foreach (var mario in others)
            {
                mario.Avatar = LocateAvatar(mario.Avatar);

                if (mario.Avatar == null)
                {
                    mario.Dispose();
                }
                else
                {
                    OtherMarios.Add(mario.Avatar, mario);
                }
            }

            Collision.Groups.Clear();
            
            InitCollision(state);
        }

        internal void SoundPlayed(uint sound, VisualMario mario)
        {
            if (mario == MyMario)
            {
                SoundQueue.Enqueue(sound);
            }
        }

        public void Dispose()
        {
            Texture?.Dispose();
            GeoEmit.Dispose();
        }
    }

    class SM64Scene : Mario.Scene
    {
        private SM64Component Component;
        private VisualMario Source;

        // maybe these can be jump sounds, but they're a bit pained
        // bull_fall_med_voxf
        // bull_fall_med_voxm (especially pained)

        // vox_hiccup would be a pretty funny jump sound

        // bull_rider_voxc (yeehaa)

        // vox_salute_herioc
        // voc_scaree are pretty good

        public SM64Scene(SM64Component component)
        {
            Component = component;
        }

        public Dictionary<uint, string> SoundBitsToHitEvt = new Dictionary<uint, string>()
        {
            { Mario.Enum.Sound.SOUND_ACTION_TERRAIN_JUMP, "" },
            { Mario.Enum.Sound.SOUND_ACTION_TERRAIN_LANDING, "bull_falloff_med" }, //might be a bit much (do not use hi lol)

            { Mario.Enum.Sound.SOUND_MARIO_YAH_WAH_HOO, "vox_salute_herioc" },
            { Mario.Enum.Sound.SOUND_MARIO_YAH_WAH_HOO + 1, "vox_salute_herioc" },
            { Mario.Enum.Sound.SOUND_MARIO_YAH_WAH_HOO + 2, "vox_salute_herioc" },
            { Mario.Enum.Sound.SOUND_MARIO_PUNCH_YAH, "bull_fall_med_vox" },
            { Mario.Enum.Sound.SOUND_MARIO_PUNCH_WAH, "bull_fall_med_vox" },
            { Mario.Enum.Sound.SOUND_MARIO_PUNCH_HOO, "bull_fall_med_vox" },
            //{ Mario.Enum.Sound.SOUND_MARIO_HOOHOO, "vox_salute_herioc" },
            { Mario.Enum.Sound.SOUND_MARIO_ON_FIRE, "vox_ouch_big" }, //electrocution_vox
            { Mario.Enum.Sound.SOUND_MARIO_OOOF, "vox_attack_buttplant" }, //"burglar_vox"
            { Mario.Enum.Sound.SOUND_MARIO_HERE_WE_GO, "espresso_buzz_vox" },

            { Mario.Enum.Sound.SOUND_MARIO_YAHOO_WAHA_YIPPEE, "pool_dvboard_dive_vox" },
            { Mario.Enum.Sound.SOUND_MARIO_YAHOO_WAHA_YIPPEE + 1, "pool_dvboard_dive_vox" },
            { Mario.Enum.Sound.SOUND_MARIO_YAHOO_WAHA_YIPPEE + 2, "pool_dvboard_dive_vox" }, //vox_yodel
            { Mario.Enum.Sound.SOUND_MARIO_YAHOO_WAHA_YIPPEE + 3, "pool_dvboard_dive_vox" }, //vox_bull_watch_enthral
            { Mario.Enum.Sound.SOUND_MARIO_YAHOO_WAHA_YIPPEE + 4, "pool_dvboard_dive_vox" }, //vox_arriba

            { Mario.Enum.Sound.SOUND_MARIO_YAHOO, "bull_rider_voxc" },
            { Mario.Enum.Sound.SOUND_MARIO_HOOHOO, "vox_hiccup" },
            { Mario.Enum.Sound.SOUND_MARIO_IMA_TIRED, "vox_yawn_stretch" },
            { Mario.Enum.Sound.SOUND_MARIO_DYING, "vox_drunk" },
            { Mario.Enum.Sound.SOUND_MARIO_DOH, "counter_voxchop" },
            { Mario.Enum.Sound.SOUND_MARIO_UH, "vox_oops" },

            { Mario.Enum.Sound.SOUND_GENERAL_FLAME_OUT, "fireplace_off" },

            { Mario.Enum.Sound.SOUND_ACTION_TERRAIN_BODY_HIT_GROUND, "body_falling" },

            { Mario.Enum.Sound.SOUND_MOVING_LAVA_BURN, "coffee_grind_loop" },

            { 1, "sting_potion_funny" }
        };

        public void SetSource(VisualMario visual)
        {
            Source = visual;
        }

        public override void play_sound(uint soundBits, Vec3f pos)
        {
            // TODO: play from mario location...

            if (SoundBitsToHitEvt.TryGetValue(soundBits, out string evt))
            {
                var hitvm = FSO.HIT.HITVM.Get();

                var sound = hitvm.PlaySoundEvent(evt);

                if (sound == null || soundBits == 1) return;

                int ownerId = (int)(Source.Avatar?.ObjectID ?? 0);

                if (!sound.AlreadyOwns(ownerId)) sound.AddOwner(ownerId);

                var volPan = Source.CalculateVolumePan();

                if (sound.SetVolume(volPan.Item1, volPan.Item2, ownerId) && sound is HITThread && Source.Avatar != null)
                {
                    var thread = (sound as HITThread);
                    thread.ObjectVar = new int[29];
                    thread.ObjectVar[0] = Source.Avatar.Gender;
                }
            }

            Component.SoundPlayed(soundBits, Source);
        }

        public override void level_trigger_warp(WarpOp op)
        {
            Component.Death();
        }
    }
}
