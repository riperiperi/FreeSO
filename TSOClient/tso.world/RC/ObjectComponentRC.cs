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

        public override sbyte Level
        {
            get { return _Level; }
            set { _Level = value; dgrp.Level = value; }
        }

        private bool _BoundsDirty = true;
        private BoundingBox _Bounds;
        public override Matrix World
        {
            get
            {
                if (_WorldDirty || (Container != null))
                {
                    _BoundsDirty = true;
                    var worldPosition = WorldSpace.GetWorldFromTile(Position);
                    _World = Matrix.CreateTranslation(worldPosition);
                    _World = Matrix.CreateScale(3f) * Matrix.CreateRotationY(-RadianDirection) * Matrix.CreateTranslation(new Vector3(1.5f, 0.1f, 1.5f)) * _World;
                    _WorldDirty = false;
                }
                return _World;
            }
        }

        public override DGRP DGRP
        {
            get
            {
                return DrawGroup;
            }
            set
            {
                _BoundsDirty = true;
                DrawGroup = value;
                if (blueprint != null && dgrp.DGRP != value)
                {
                    blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                    DynamicCounter = 0;
                }
                dgrp.DGRP = value;
            }
        }

        public BoundingBox GetBounds()
        {
            if (_BoundsDirty || _WorldDirty)
            {
                var bounds = ((DGRPRendererRC)dgrp).GetBounds();
                if (bounds == null) return new BoundingBox(); //don't cache
                _Bounds = BoundingBox.CreateFromPoints(bounds.Value.GetCorners().Select(x => Vector3.Transform(x, World)));
                _BoundsDirty = false;
            }
            return _Bounds;
        }

        public float? IntersectsBounds(Ray ray)
        {
            return GetBounds().Intersects(ray);
        }

        public float SortDepth (Matrix vp)
        {
            if (!Visible) return 0;
            var w = World;
            var t = w.Translation;
            return Vector3.Dot(t, vp.Forward);
        }

        public override Vector2 GetScreenPos(WorldState world)
        {
            var projected = Vector4.Transform(new Vector4(0, 0, 0, 1f), this.World * world.Camera.View * world.Camera.Projection);
            if (world.Camera is WorldCamera) projected.Z = 1;
            var res1 = new Vector2(projected.X / projected.Z, -projected.Y / projected.Z);
            var size = PPXDepthEngine.GetWidthHeight();
            return new Vector2((size.X / PPXDepthEngine.SSAA) * 0.5f * (res1.X + 1f), (size.Y / PPXDepthEngine.SSAA) * 0.5f * (res1.Y + 1f)); //world.WorldSpace.GetScreenFromTile(transhead) + world.WorldSpace.GetScreenOffset() + PosCenterOffsets[(int)world.Zoom - 1];
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            //#if !DEBUG 
            if (!Visible || (!world.DrawOOB && (Position.X < -2043 && Position.Y < -2043)) || Level < 1) return;
            if (CutawayHidden) return;
            //#endif
            ((DGRPRendererRC)dgrp).World = World;
            if (this.DrawGroup != null) dgrp.Draw(world);
        }

        public override void DrawLMap(GraphicsDevice device, sbyte level)
        {
            //#if !DEBUG 
            if (!Visible || (Position.X < -2043 && Position.Y < -2043) || Level < 1) return;
            //#endif
            ((DGRPRendererRC)dgrp).World = World;
            if (this.DrawGroup != null) ((DGRPRendererRC)dgrp).DrawLMap(device, level);
        }
    }
}
