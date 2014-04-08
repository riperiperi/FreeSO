using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSO.Vitaboy;
using tso.world.model;

namespace tso.world.components
{
    public class AvatarComponent : WorldComponent
    {
        public Avatar Avatar;

        public override Vector3 GetSLOTPosition(int slot)
        {
            var handpos = Avatar.Skeleton.GetBone("R_FINGER0").AbsolutePosition / 3;
            return Vector3.Transform(new Vector3(handpos.X, handpos.Z, handpos.Y), Matrix.CreateRotationZ((float)RadianDirection)) + this.Position - new Vector3(0.5f, 0.5f, 0f); //todo, rotate relative to avatar
        }

        private double RadianDirection;

        private Direction _Direction;
        public Direction Direction
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
                        RadianDirection = Math.PI;
                        break;
                    case Direction.EAST:
                        RadianDirection = Math.PI*1.5;
                        break;
                    case Direction.SOUTH:
                        RadianDirection = 0;
                        break;
                    case Direction.WEST:
                        RadianDirection = Math.PI*0;
                        break;
                }
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

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            if (Avatar != null){
                world._3D.DrawMesh(Matrix.CreateRotationY((float)RadianDirection)*this.World, Avatar.Bindings);
            }
        }
    }
}
