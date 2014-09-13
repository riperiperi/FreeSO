/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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

        public double RadianDirection;
        public Vector2 LastScreenPos; //todo: move this and slots into an abstract class that contains avatars and objects
        public int LastZoomLevel;

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
                        RadianDirection = Math.PI;
                        break;
                    case Direction.EAST:
                        RadianDirection = Math.PI*1.5;
                        break;
                    case Direction.SOUTH:
                        RadianDirection = 0;
                        break;
                    case Direction.WEST:
                        RadianDirection = Math.PI*0.5;
                        break;
                }
            }
        }

        public override Vector3 Position
        {
            get
            {
                if (Container == null) return _Position;
                else return Container.GetSLOTPosition(ContainerSlot) + new Vector3(0.5f, 0.5f, -1.4f); //apply offset to snap character into slot
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

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            LastScreenPos = world.WorldSpace.GetScreenFromTile(Position) + world.WorldSpace.GetScreenOffset();
            LastZoomLevel = (int)world.Zoom;
            if (Container != null)
            {
                Direction = Container.Direction;
                _WorldDirty = true;
            }
            if (Avatar != null){
                world._3D.DrawMesh(Matrix.CreateRotationY(-(float)RadianDirection)*this.World, Avatar.Bindings); //negated so avatars spin clockwise
            }
        }
    }
}
