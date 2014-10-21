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
using tso.world.model;

namespace tso.world
{
    public abstract class WorldComponent {
        /** Instance ID **/
        public long ID;

        public virtual Vector3 GetSLOTPosition(int slot) {
            return new Vector3(0, 0, 0);   
        }

        public abstract float PreferredDrawOrder { get; }

        public virtual void Initialize(GraphicsDevice device, WorldState world){
            OnWorldChanged(world);
        }

        public abstract void Draw(GraphicsDevice device, WorldState world);

        public virtual void OnRotationChanged(WorldState world){
            OnWorldChanged(world);
        }

        public virtual void OnZoomChanged(WorldState world){
            OnWorldChanged(world);
        }

        public virtual void OnScrollChanged(WorldState world){
            OnWorldChanged(world);
        }

        public virtual void OnWorldChanged(WorldState world){
        }

        public virtual void OnPositionChanged() { }


        public short TileX = -2;
        public short TileY = -2;
        public sbyte Level = -2;

        public WorldComponent Container;
        public int ContainerSlot;

        /// <summary>
        /// Position of the object in tile units
        /// </summary>
        protected Vector3 _Position = new Vector3(0.0f, 0.0f, 0.0f);
        public virtual Vector3 Position {
            get{
                if (Container == null) return _Position;
                else return Container.GetSLOTPosition(ContainerSlot);
            }
            set{
                _Position = value;
                OnPositionChanged();
                _WorldDirty = true;
            }
        }

        public virtual Direction Direction { get; set; }

        public Vector3 TilePosition
        {
            get
            {
                return new Vector3(TileX, TileY, Level);
            }
            set
            {
                TileX = (short)Math.Round(value.X);
                TileY = (short)Math.Round(value.Y);
                Level = (sbyte)Math.Round(value.Z);
            }
        }

        protected bool _WorldDirty = true;
        protected Matrix _World;
        public Matrix World
        {
            get
            {
                if (_WorldDirty || (Container != null))
                {
                    var worldPosition = WorldSpace.GetWorldFromTile(Position);
                    _World = Matrix.CreateTranslation(worldPosition);
                    _WorldDirty = false;
                }
                return _World;
            }
        }

        //
        //var worldPosition = State.WorldSpace.GetWorldFromTile(position);

    }
}
