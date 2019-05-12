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
        private Texture2D _Headline;
        public Texture2D Headline
        {
            get
            {
                return _Headline;
            }
            set
            {
                if (_Headline != value)
                {
                    _Headline = value;
                    if (value == null)
                    {
                        blueprint.HeadlineObjects.Remove(this);
                    }
                    else
                    {
                        blueprint.HeadlineObjects.Add(this);
                    }
                }
            }
        }
        public Blueprint blueprint;
        public Vector3 MTOffset;
        public Matrix? GroundAlign; //for realigning objects on sloped terrain (optional, for cars)

        protected short _ObjectID;
        public virtual short ObjectID
        {
            get
            {
                return _ObjectID;
            }
            set
            {
                _ObjectID = value;
            }
        } //set this any time it changes so that hit test works.

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
                if (blueprint != null) _Position.Z += blueprint.InterpAltitude(new Vector3(0.5f, 0.5f, 0) + _Position - MTOffset / 16) + MTOffset.Z / 16f;
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

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            
        }

        public abstract void Preload(GraphicsDevice device, WorldState world);
        public abstract Vector3 GetHeadlinePos();

        public virtual float GetHeadlineScale()
        {
            return 1f;
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
            var tHead1 = GetHeadlinePos();
            var hScale = GetHeadlineScale();
            var newWorld = Matrix.CreateScale(hScale*Headline.Width / 64f, hScale*Headline.Height / -64f, 1) * Matrix.Invert(Matrix.CreateFromQuaternion(rotation)) * Matrix.CreateTranslation(new Vector3(tHead1.X * 3, 1.6f + tHead1.Z * 3, tHead1.Y * 3)) * this.World;

            effect.DiffuseColor = Color.White.ToVector3();
            effect.World = newWorld;
            effect.Texture = Headline;
            effect.View = world.Camera.View;
            effect.Projection = world.Camera.Projection;
            effect.CurrentTechnique.Passes[0].Apply();

            gd.SetVertexBuffer(WorldContent.GetTextureVerts(gd));
            gd.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
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
