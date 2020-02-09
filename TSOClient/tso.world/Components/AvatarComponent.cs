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
using Microsoft.Xna.Framework;
using FSO.Vitaboy;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using FSO.Common.Utils;
using FSO.LotView.LMap;
using FSO.LotView.RC;

namespace FSO.LotView.Components
{
    public class AvatarComponent : EntityComponent, IDisposable
    {
        public Avatar Avatar;
        public bool IsPet;
        public float Scale = 1;
        public int ALevel = 0;
        public _2DStandaloneSprite HeadlineSprite;

        private static Vector2[] PosCenterOffsets = new Vector2[]{
            new Vector2(2+16, 79+8),
            new Vector2(3+32, 158+16),
            new Vector2(5+64, 316+32)
        };

        private static string[] SlotBones = new string[]
        {
            "R_FINGER0",
            "HEAD",
            "PELVIS"
        };

        public override Vector3 GetSLOTPosition(int slot, bool avatar)
        {
            var handpos = Avatar.Skeleton.GetBone(SlotBones[slot]).AbsolutePosition / 3.0f * Scale;
            return Vector3.Transform(new Vector3(handpos.X, handpos.Z, handpos.Y), Matrix.CreateRotationZ((float)(RadianDirection+Math.PI))) + this.Position - new Vector3(0.5f, 0.5f, 0f);
        }

        public Vector3 GetPelvisPosition()
        {
            var pelvis = Avatar.Skeleton.GetBone("PELVIS").AbsolutePosition / 3.0f;
            return Vector3.Transform(new Vector3(pelvis.X, pelvis.Z, pelvis.Y), Matrix.CreateRotationZ((float)(RadianDirection + Math.PI))) + this.Position;// - new Vector3(0.5f, 0.5f, 0f);
        }

        public double RadianDirection;
        public override ushort Room { get; set; }
        public AvatarDisplayFlags DisplayFlags;
        public bool IsDead;

        private Direction _Direction;
        public override Direction Direction
        {
            get
            {
                return _Direction;
            }
            set
            {
                _Direction = value;
                switch (value)
                {
                    case Direction.NORTH:
                        RadianDirection = 0;
                        break;
                    case Direction.EAST:
                        RadianDirection = Math.PI*0.5;
                        break;
                    case Direction.SOUTH:
                        RadianDirection = Math.PI;
                        break;
                    case Direction.WEST:
                        RadianDirection = Math.PI*1.5;
                        break;
                }
            }
        }

        private Vector3 AltitudeOff;

        public override Vector3 Position
        {
            get
            {
                if (Container == null) return _Position + AltitudeOff;
                else
                {
                    var pos = Container.GetSLOTPosition(ContainerSlot, true);
                    return pos + new Vector3(0.5f, 0.5f, (1-Scale)/2); //pos + new Vector3(0.5f, 0.5f, (IsPet ? 0 : -1.4f)); //apply offset to snap character into slot
                }
            }
            set
            {
                _Position = value;
                ALevel = (int)(value.Z / 2.94f);
                if (blueprint != null) AltitudeOff = new Vector3(0, 0, blueprint.InterpAltitude(_Position));
                OnPositionChanged();
                _WorldDirty = true;
            }
        }

        public Vector3 StoredPosition
        {
            get { return _Position; }
        }

        public override void Initialize(GraphicsDevice device, WorldState world)
        {
            base.Initialize(device, world);
            Avatar.StoreOnGPU(device);
        }

        public override Vector2 GetScreenPos(WorldState world)
        {
            var headpos = Avatar.Skeleton.GetBone("HEAD").AbsolutePosition;
            var projected = Vector4.Transform(new Vector4(headpos, 1), Matrix.CreateRotationY((float)(Math.PI - RadianDirection)) * this.World * world.View * world.Projection);
            if (world.CameraMode < CameraRenderMode._3D) projected.Z = 1;
            var res1 = new Vector2(projected.X / projected.Z, -projected.Y / projected.Z);
            var size = PPXDepthEngine.GetWidthHeight();
            return new Vector2((size.X / PPXDepthEngine.SSAA) * 0.5f * (res1.X + 1f), (size.Y / PPXDepthEngine.SSAA) * 0.5f * (res1.Y + 1f)); //world.WorldSpace.GetScreenFromTile(transhead) + world.WorldSpace.GetScreenOffset() + PosCenterOffsets[(int)world.Zoom - 1];
        }

        private List<Vector2> CloseLightPositions(Vector3 Position)
        {
            if (blueprint == null || Room >= blueprint.Rooms.Count || Room == 0) return null;
            var room = blueprint.Rooms[Room].Base;
            var lights = (room >= blueprint.Light.Length)?new List<LightData>():blueprint.Light[room].Lights;
            var xy = new Vector2(Position.X, Position.Y);
            var result = new List<System.Tuple<float, Vector2>>();

            foreach (var light in lights)
            {
                var dist = (xy * 16 - light.LightPos).Length();
                if (light.LightIntensity > 0.2f && dist < light.LightSize)
                {
                    result.Add(new System.Tuple<float, Vector2>(dist, (light.LightPos / 16f) * 3f));
                }
            }

            if (blueprint.Rooms[room].IsOutside && blueprint.OutdoorsLight != null)
            {
                result.Add(new System.Tuple<float, Vector2>(0, new Vector2(Position.X*3, Position.Y*3)+(-blueprint.OutdoorsLight.LightDir*3f * blueprint.OutdoorsLight.FalloffMultiplier)));
            }
            return result.OrderBy(x => x.Item1).Select(x => x.Item2).Take(4).ToList();
        }

        public override Vector3 GetHeadlinePos()
        {
            var headpos = Avatar.Skeleton.GetBone("HEAD").AbsolutePosition / 3.0f;
            return Vector3.Transform(new Vector3(headpos.X, headpos.Z, headpos.Y), Matrix.CreateRotationZ((float)(RadianDirection + Math.PI)));
        }

        public override Vector3 GetLookTarget()
        {
            var headpos = GetHeadlinePos();
            return new Vector3(Position.X + headpos.X, Position.Z + headpos.Z, Position.Y + headpos.Y);
        }

        private Vector4 PowColorVec(Vector4 vec, float pow)
        {
            vec.X = (float)Math.Pow(vec.X, pow);
            vec.Y = (float)Math.Pow(vec.Y, pow);
            vec.Z = (float)Math.Pow(vec.Z, pow);

            return vec;
        }

        public void DrawAvatarMesh(GraphicsDevice device, WorldState state, Matrix world, Color baseCol)
        {
            var effect = WorldContent.AvatarEffect;
            var technique = effect.CurrentTechnique;
            var room = (Room > 65530 || Room == 0) ? Room : blueprint.Rooms[Room].Base;
            foreach (var pass in technique.Passes)
            {
                effect.Parameters["ObjectID"].SetValue(ObjectID / 65535f);
                effect.Parameters["Level"].SetValue(ALevel + 0.0001f);
                var roomLights = blueprint?.RoomColors;
                if (roomLights != null)
                {
                    var col = ((WorldConfig.Current.AdvancedLighting) ? new Vector4(1) : PowColorVec(roomLights[room].ToVector4(), 1 / 2.2f)) * baseCol.ToVector4();
                    effect.Parameters["AmbientLight"].SetValue(col);
                }
                effect.Parameters["World"].SetValue(world);
                pass.Apply();

                Avatar.DrawGeometry(device, effect);
            }
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            var pos = Position;
            Avatar.Position = WorldSpace.GetWorldFromTile(pos);
            if (Avatar.Skeleton == null) return;
            var headpos = Avatar.Skeleton.GetBone("HEAD").AbsolutePosition / 3.0f;
            var tHead1 = Vector3.Transform(new Vector3(headpos.X, headpos.Z, headpos.Y), Matrix.CreateRotationZ((float)(RadianDirection + Math.PI)));
            var transhead = tHead1 + pos - new Vector3(0.5f, 0.5f, 0f);

            if (!Visible) return;

            if (Avatar != null){

                Color col = Color.White;
                if ((DisplayFlags & AvatarDisplayFlags.ShowAsGhost) > 0) col = new Color(32, 255, 96) * 0.66f;
                else if (IsDead) col = new Color(255, 255, 255, 64);

                Avatar.LightPositions = (WorldConfig.Current.AdvancedLighting)?CloseLightPositions(Position):null;
                var newWorld = Matrix.CreateRotationY((float)(Math.PI - RadianDirection)) * this.World;
                if (Scale != 1f) newWorld = Matrix.CreateScale(Scale) * newWorld;
                DrawAvatarMesh(device, world, newWorld, col);
                //world._3D.DrawMesh(newWorld, Avatar, (short)ObjectID, (Room>65530 || Room == 0)?Room:blueprint.Rooms[Room].Base, col, ALevel); 
            }

            if (Headline != null && !Headline.IsDisposed)
            {
                var lastCull = device.RasterizerState;
                var lastBlend = device.BlendState;
                device.RasterizerState = RasterizerState.CullNone;
                device.BlendState = BlendState.NonPremultiplied;
                var headOff = (transhead-Position) + new Vector3(0,0,0.66f);
                if (!world.Cameras.Safe2D)
                {
                    //DrawHeadline3D(device, world);
                }
                else
                {
                    var headPx = world.WorldSpace.GetScreenFromTile(headOff);

                    if (HeadlineSprite == null) HeadlineSprite = new _2DStandaloneSprite();
                    HeadlineSprite.Pixel = Headline;
                    HeadlineSprite.Depth = TextureGenerator.GetWallZBuffer(device)[30];

                    HeadlineSprite.SrcRect = new Rectangle(0, 0, Headline.Width, Headline.Height);
                    HeadlineSprite.WorldPosition = headOff;
                    var off = PosCenterOffsets[(int)world.Zoom - 1];
                    HeadlineSprite.DestRect = new Rectangle(
                        ((int)headPx.X - Headline.Width / 2) + (int)off.X,
                        ((int)headPx.Y - Headline.Height / 2) + (int)off.Y, Headline.Width, Headline.Height);

                    HeadlineSprite.AbsoluteDestRect = HeadlineSprite.DestRect;
                    HeadlineSprite.AbsoluteDestRect.Offset(world.WorldSpace.GetScreenFromTile(pos));
                    HeadlineSprite.AbsoluteWorldPosition = HeadlineSprite.WorldPosition + WorldSpace.GetWorldFromTile(pos);

                    HeadlineSprite.Room = Room;
                    HeadlineSprite.PrepareVertices(device);
                    world._2D.EnsureIndices();
                    world._2D.DrawImmediate(HeadlineSprite);
                }
                device.RasterizerState = lastCull;
                device.BlendState = lastBlend;
            }
        }

        public override void Preload(GraphicsDevice device, WorldState world)
        {
            //nothing important to do here
        }

        public void Dispose()
        {
            HeadlineSprite?.Dispose();
        }
    }
}
