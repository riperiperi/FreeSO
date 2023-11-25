using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;

namespace FSO.LotView
{
    public abstract class WorldComponent {
        /** Instance ID **/
        public long ID;

        public float DrawOrder;

        public virtual void Initialize(GraphicsDevice device, WorldState world) {
        }

        public abstract void Draw(GraphicsDevice device, WorldState world);
        public virtual void Update(GraphicsDevice device, WorldState world) { }

        public virtual void OnRotationChanged(WorldState world) {
        }

        public virtual void OnZoomChanged(WorldState world) {
        }

        public virtual void OnPositionChanged() { }


        public short TileX = -2;
        public short TileY = -2;
        protected sbyte _Level = -2;
        public virtual sbyte Level
        {
            get { return _Level; }
            set { _Level = value; }
        }

        /// <summary>
        /// Position of the object in tile units
        /// </summary>
        protected Vector3 _Position = new Vector3(0.0f, 0.0f, 0.0f);
        public virtual Vector3 Position {
            get {
                return _Position;
            }
            set {
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
        
        protected virtual bool _WorldDirty { get; set; }
        protected Matrix _World;
        public virtual Matrix World
        {
            get
            {
                if (_WorldDirty)
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
