using FSO.LotView.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using FSO.Common.Utils;

namespace FSO.LotView.RC
{
    public class ObjectComponentRC : ObjectComponent
    {
        static ObjectComponentRC()
        {
            MakeShadowComponent = (GameObject obj) => new ObjectComponentRC(obj);
        }

        public ObjectComponentRC(GameObject obj) : base(obj)
        {
            dgrp = new DGRPRendererRC(this.DrawGroup, obj.OBJ);
            dgrp.DynamicSpriteBaseID = obj.OBJ.DynamicSpriteBaseId;
            dgrp.NumDynamicSprites = obj.OBJ.NumDynamicSprites;
        }
        
        private BoundingBox _Bounds;
        public override Matrix World
        {
            get
            {
                if (_WorldDirty || (Container != null))
                {
                    _BoundsDirty = true;
                    var worldPosition = WorldSpace.GetWorldFromTile(Position);
                    _World = Matrix.CreateTranslation(new Vector3(1.5f, 0.1f, 1.5f)) * Matrix.CreateTranslation(worldPosition);
                    if (GroundAlign != null) _World = GroundAlign.Value * _World;
                    _World = Matrix.CreateScale(3f) * Matrix.CreateRotationY(-RadianDirection) * _World;
                    _WorldDirty = false;
                }
                return _World;
            }
        }

        public float SortDepth (Matrix vp)
        {
            if (!Visible) return 0;
            var w = World;
            var ctr = w.Translation;
            var forward = vp.Forward;
            forward.Z *= -1;
            if (forward.Z < 0) forward.Y = -forward.Y;
            return Vector3.Dot(ctr, forward);
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            //#if !DEBUG 
            if (!Visible || (!world.DrawOOB && (Position.X < -2043 && Position.Y < -2043)) || Level < 1) return;
            if (CutawayHidden) return;
            //#endif
            var mworld = World3D;
            ((DGRPRendererRC)dgrp).World = mworld;
            if (this.DrawGroup != null) dgrp.Draw(world);

            if ((world.Camera as WorldCamera3D)?.FromIntensity == 0)
            {
                for (int i = 0; i < Particles.Count; i++)
                {
                    var part = Particles[i];
                    if (part.BoundsDirty && part.AutoBounds && dgrp != null)
                    {
                        //this particle needs updated bounds.
                        part.Volume = GetParticleBounds();
                        part.BoundsDirty = false;
                        part.Dispose();
                    }
                    part.Level = Level;
                    part.OwnerWorld = mworld * Matrix.CreateScale(2);
                    part.Draw(device, world);
                    if (part.Dead) Particles.RemoveAt(i--);
                }
            }
        }

        public override BoundingBox GetParticleBounds()
        {
            return ((DGRPRendererRC)dgrp).GetBounds() ?? new BoundingBox();
        }
    }
}
