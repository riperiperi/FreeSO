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

namespace FSO.LotView.Components
{
    public class AvatarComponent : EntityComponent
    {
        public Avatar Avatar;
        public bool IsPet;
        public Blueprint blueprint;

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

        public override Vector3 Position
        {
            get
            {
                if (Container == null) return _Position;
                else return Container.GetSLOTPosition(ContainerSlot) + new Vector3(0.5f, 0.5f, (IsPet?0:-1.4f)); //apply offset to snap character into slot
            }
            set
            {
                _Position = value;
                OnPositionChanged();
                _WorldDirty = true;
            }
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
            var headpos = Avatar.Skeleton.GetBone("HEAD").AbsolutePosition / 3.0f;
            var transhead = Vector3.Transform(new Vector3(headpos.X, headpos.Z, headpos.Y), Matrix.CreateRotationZ((float)(RadianDirection + Math.PI))) + this.Position - new Vector3(0.5f, 0.5f, 0f);
            return world.WorldSpace.GetScreenFromTile(transhead) + world.WorldSpace.GetScreenOffset() + PosCenterOffsets[(int)world.Zoom - 1];
        }

        private List<Vector2> CloseLightPositions(Vector3 Position)
        {
            if (blueprint == null || Room > blueprint.Rooms.Count || Room == 0) return null;
            var room = blueprint.Rooms[Room].Base;
            var lights = blueprint.Light[room].Lights;
            var xy = new Vector2(Position.X, Position.Y);
            var result = new List<Vector2>();

            foreach (var light in lights)
            {
                if (light.LightIntensity > 0.2f && (xy*16 - light.LightPos).Length() < light.LightSize)
                {
                    result.Add((light.LightPos / 16f) * 3f);
                }
            }

            if (blueprint.Rooms[room].IsOutside && blueprint.OutdoorsLight != null)
            {
                result.Add(new Vector2(Position.X*3, Position.Y*3)+(-blueprint.OutdoorsLight.LightDir*3f * blueprint.OutdoorsLight.FalloffMultiplier));
            }
            return result;
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            Avatar.Position = WorldSpace.GetWorldFromTile(Position);
            var headpos = Avatar.Skeleton.GetBone("HEAD").AbsolutePosition / 3.0f;
            var transhead = Vector3.Transform(new Vector3(headpos.X, headpos.Z, headpos.Y), Matrix.CreateRotationZ((float)(RadianDirection + Math.PI))) + this.Position - new Vector3(0.5f, 0.5f, 0f);

            if (!Visible) return;

            if (Avatar != null){

                Color col = Color.White;
                if ((DisplayFlags & AvatarDisplayFlags.ShowAsGhost) > 0) col = new Color(32, 255, 96) * 0.66f;
                else if ((DisplayFlags & AvatarDisplayFlags.TSOGhost) != 0) col = new Color(255, 255, 255, 64);

                Avatar.LightPositions = (WorldConfig.Current.AdvancedLighting)?CloseLightPositions(Position):null;
                world._3D.DrawMesh(Matrix.CreateRotationY((float)(Math.PI-RadianDirection))*this.World, Avatar, (short)ObjectID, (Room>65532 || Room == 0)?Room:blueprint.Rooms[Room].Base, col); 
            }

            if (Headline != null && !Headline.IsDisposed)
            {
                var headOff = (transhead-Position) + new Vector3(0,0,0.66f);
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
