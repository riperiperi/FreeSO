using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using tso.content;
using tso.world.utils;
using tso.files.formats.iff.chunks;
using tso.world.model;

namespace tso.world.components
{
    public class ObjectComponent : WorldComponent
    {
        private GameObject Obj;
        private DGRP DrawGroup;
        private DGRPRenderer dgrp;
        public Blueprint blueprint;

        public ObjectComponent(GameObject obj){
            this.Obj = obj;
            if (obj.OBJ.BaseGraphicID > 0)
            {
                var gid = obj.OBJ.BaseGraphicID;
                //if (obj.OBJ.GUID == 0x98E0F8BD)
                //{
                //    var dgroups = obj.Resource.List<DGRP>();
                //    gid += 10;
                //    gid = 125;
                //}
                this.DrawGroup = obj.Resource.Get<DGRP>(gid);
                dgrp = new DGRPRenderer(this.DrawGroup);
                dgrp.DynamicSpriteBaseID = obj.OBJ.DynamicSpriteBaseId;
                dgrp.NumDynamicSprites = obj.OBJ.NumDynamicSprites;
            }
        }

        public DGRP DGRP
        {
            get
            {
                return DrawGroup;
            }
            set
            {
                DrawGroup = value;
                dgrp.DGRP = value;
                blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.SCROLL, TileX, TileY, Level));
            }
        }

        private ushort _DynamicSpriteFlags = 0x0000;
        public ushort DynamicSpriteFlags
        {
            get{
                return _DynamicSpriteFlags;
            }set{
                _DynamicSpriteFlags = value;
                if (dgrp != null){
                    dgrp.DynamicSpriteFlags = value;
                }
            }
        }

        public override float PreferredDrawOrder
        {
            get {
                return 2000.0f + (this.Position.X + this.Position.Y);
            }
        }

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
                if (dgrp != null){
                    dgrp.Direction = value;
                }
            }
        }

        public override void OnRotationChanged(WorldState world)
        {
            base.OnRotationChanged(world);
            if (dgrp != null){
                dgrp.InvalidateRotation();
            }
        }

        public override void OnZoomChanged(WorldState world)
        {
            base.OnZoomChanged(world);
            if (dgrp != null){
                dgrp.InvalidateZoom();
            }
        }

        public override void OnScrollChanged(WorldState world)
        {
            base.OnScrollChanged(world);
            if (dgrp != null){
                dgrp.InvalidateScroll();
            }
        }

        //public override void OnPositionChanged(){
        //    base.OnPositionChanged();
        //    if (dgrp != null){
        //        dgrp.Position = this.Position;
        //    }
        //}

        public override void Draw(GraphicsDevice device, WorldState world){
            if (this.DrawGroup == null) { return; }
            //world._2D.Draw(this.DrawGroup);
            dgrp.Draw(world);
        }
    }
}
