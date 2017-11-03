using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;

namespace FSO.LotView.Components
{
    public abstract class EntityComponent : WorldComponent
    {
        public Texture2D Headline;
        public Blueprint blueprint;
        public Vector3 MTOffset;

        public short ObjectID; //set this any time it changes so that hit test works.

        public abstract Vector2 GetScreenPos(WorldState world);

        public abstract ushort Room { get; set; }

        public virtual Vector3 GetSLOTPosition(int slot, bool avatar)
        {
            return new Vector3(0, 0, 0);
        }

        public EntityComponent Container;
        public int ContainerSlot;

        protected bool _Visible = true;
        public bool Visible { get { return _Visible; } set { _Visible = value; } }

        /// <summary>
        /// Position of the object in tile units
        /// </summary>
        protected Vector3 _Position = new Vector3(0.0f, 0.0f, 0.0f);
        public override Vector3 Position
        {
            get
            {
                if (Container == null) return _Position;
                else return Container.GetSLOTPosition(ContainerSlot, false);
            }
            set
            {
                _Position = value;
                if (blueprint != null) _Position.Z += blueprint.InterpAltitude(new Vector3(0.5f, 0.5f, 0) + _Position - MTOffset/16);
                OnPositionChanged();
                _WorldDirty = true;
            }
        }

        public Vector3 UnmoddedPosition
        {
            get
            {
                return _Position;
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
            get
            {
                return 0;
            }
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            
        }

        public override Matrix World
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
    }
}
