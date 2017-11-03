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
    public class AvatarComponent : EntityComponent
    {
        public Avatar Avatar;
        public bool IsPet;
        public float Scale = 1;
        public int Level = 0;

        private static Vector2[] PosCenterOffsets = new Vector2[]{
            new Vector2(2+16, 79+8),
            new Vector2(3+32, 158+16),
            new Vector2(5+64, 316+32)
        };

        public override Vector3 GetSLOTPosition(int slot)
        {
            var handpos = Avatar.Skeleton.GetBone("R_FINGER0").AbsolutePosition / 3.0f;
            return Vector3.Transform(new Vector3(handpos.X, handpos.Z, handpos.Y), Matrix.CreateRotationZ((float)(RadianDirection+Math.PI))) + this.Position - new Vector3(0.5f, 0.5f, 0f);
        }

        public Vector3 GetPelvisPosition()
        {
            var pelvis = Avatar.Skeleton.GetBone("PELVIS").AbsolutePosition / 3.0f;
            return Vector3.Transform(new Vector3(pelvis.X, pelvis.Z, pelvis.Y), Matrix.CreateRotationZ((float)(RadianDirection + Math.PI))) + this.Position - new Vector3(0.5f, 0.5f, 0f);
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
                    var pos = Container.GetSLOTPosition(ContainerSlot);
                    pos.Z = (float)Math.Round(pos.Z / 2.95f) * 2.95f;
                    return pos + new Vector3(0.5f, 0.5f, 0); //pos + new Vector3(0.5f, 0.5f, (IsPet ? 0 : -1.4f)); //apply offset to snap character into slot
                }
            }
            set
            {
                _Position = value;
                Level = (int)(value.Z / 2.94f);
                if (blueprint != null) AltitudeOff = new Vector3(0, 0, blueprint.InterpAltitude(_Position));
                OnPositionChanged();
                _WorldDirty = true;
            }
        }

        public Vector3 StoredPosition
        {
            get { return _Position; }
        }

        public override float PreferredDrawOrder
        {
            get { return 5000.0f;  }
        }

        public override void Initialize(GraphicsDevice device, WorldState world)
        {
            base.Initialize(device, world);
            Avatar.StoreOnGPU(device);
        }

        public override Vector2 GetScreenPos(WorldState world)
        {
            var headpos = Avatar.Skeleton.GetBone("HEAD").AbsolutePosition;
            var projected = Vector4.Transform(new Vector4(headpos, 1), Matrix.CreateRotationY((float)(Math.PI - RadianDirection)) * this.World * world.Camera.View * world.Camera.Projection);
            if (world.Camera is WorldCamera) projected.Z = 1;
            var res1 = new Vector2(projected.X / projected.Z, -projected.Y / projected.Z);
            //res1.X /= PPXDepthEngine.SSAA;
            //res1.Y /= PPXDepthEngine.SSAA;
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

        public void DrawHeadline3D(GraphicsDevice device, WorldState world)
        {
            if (Headline == null || Headline.IsDisposed) return;
            var gd = world.Device;
            var effect = WorldContent.GetBE(gd);

            effect.TextureEnabled = true;
            effect.VertexColorEnabled = false;

            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;
            world.Camera.View.Decompose(out scale, out rotation, out translation);
            var headpos = Avatar.Skeleton.GetBone("HEAD").AbsolutePosition / 3.0f;
            var tHead1 = Vector3.Transform(new Vector3(headpos.X, headpos.Z, headpos.Y), Matrix.CreateRotationZ((float)(RadianDirection + Math.PI)));
            var newWorld = Matrix.CreateScale(Headline.Width / 64f, Headline.Height / -64f, 1) * Matrix.Invert(Matrix.CreateFromQuaternion(rotation)) * Matrix.CreateTranslation(new Vector3(tHead1.X * 3, 1.6f + tHead1.Z * 3, tHead1.Y * 3)) * this.World;

            effect.DiffuseColor = Color.White.ToVector3();
            effect.World = newWorld;
            effect.Texture = Headline;
            effect.View = world.Camera.View;
            effect.Projection = world.Camera.Projection;
            effect.CurrentTechnique.Passes[0].Apply();

            gd.SetVertexBuffer(WorldContent.GetTextureVerts(gd));
            gd.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            Avatar.Position = WorldSpace.GetWorldFromTile(Position);
            var headpos = Avatar.Skeleton.GetBone("HEAD").AbsolutePosition / 3.0f;
            var tHead1 = Vector3.Transform(new Vector3(headpos.X, headpos.Z, headpos.Y), Matrix.CreateRotationZ((float)(RadianDirection + Math.PI)));
            var transhead = tHead1 + this.Position - new Vector3(0.5f, 0.5f, 0f);

            if (!Visible) return;

            if (Avatar != null){

                Color col = Color.White;
                if ((DisplayFlags & AvatarDisplayFlags.ShowAsGhost) > 0) col = new Color(32, 255, 96) * 0.66f;
                else if (IsDead) col = new Color(255, 255, 255, 64);

                Avatar.LightPositions = (WorldConfig.Current.AdvancedLighting)?CloseLightPositions(Position):null;
                var newWorld = Matrix.CreateRotationY((float)(Math.PI - RadianDirection)) * this.World;
                if (Scale != 1f) newWorld = Matrix.CreateScale(Scale) * newWorld;
                world._3D.DrawMesh(newWorld, Avatar, (short)ObjectID, (Room>65532 || Room == 0)?Room:blueprint.Rooms[Room].Base, col, Level); 
            }

            if (Headline != null && !Headline.IsDisposed)
            {
                var headOff = (transhead-Position) + new Vector3(0,0,0.66f);
                if (world is WorldStateRC)
                {
                    //this is done in world2DRC, after everything else.
                }
                else
                {
                    var headPx = world.WorldSpace.GetScreenFromTile(headOff);

                    var item = world._2D.NewSprite(_2DBatchRenderMode.Z_BUFFER);
                    item.Pixel = Headline;
                    item.Depth = TextureGenerator.GetWallZBuffer(device)[30];

                    item.SrcRect = new Rectangle(0, 0, Headline.Width, Headline.Height);
                    item.WorldPosition = headOff;
                    var off = PosCenterOffsets[(int)world.Zoom - 1];
                    item.DestRect = new Rectangle(
                        ((int)headPx.X - Headline.Width / 2) + (int)off.X,
                        ((int)headPx.Y - Headline.Height / 2) + (int)off.Y, Headline.Width, Headline.Height);
                    item.Room = Room;
                    world._2D.Draw(item);
                }
            }
        }
    }
}
