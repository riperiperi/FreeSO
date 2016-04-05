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
using FSO.Content;
using FSO.LotView.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;

namespace FSO.LotView.Components
{
    public class ObjectComponent : EntityComponent
    {
        private static Vector2[] PosCenterOffsets = new Vector2[]{
            new Vector2(2+16, 79+8),
            new Vector2(3+32, 158+16),
            new Vector2(5+64, 316+32)
        };

        public GameObject Obj;

        private DGRP DrawGroup;
        private DGRPRenderer dgrp;
        public WorldObjectRenderInfo renderInfo;
        public Blueprint blueprint;
        private int DynamicCounter; //how long this sprite has been dynamic without changing sprite
        public List<SLOTItem> ContainerSlots;
        public new bool Visible {
            get { return _Visible; }
            set {
                if (_Visible != value)
                {
                    _Visible = value;
                    if (blueprint != null) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                }
            }
        }

        public Rectangle Bounding { get { return dgrp.Bounding; } }

        public override ushort Room
        {
            get
            {
                return dgrp.Room;
            }
            set
            {
                dgrp.Room = value;
            }
        }

        public override Vector3 GetSLOTPosition(int slot)
        {
            var item = (ContainerSlots != null && ContainerSlots.Count > slot)?ContainerSlots[slot]:null;
            if (item != null)
            {
                var off = item.Offset;
                var centerRelative = new Vector3(off.X * (1 / 16.0f), off.Y * (1 / 16.0f), ((item.Height != 5) ? SLOT.HeightOffsets[item.Height-1] : off.Z) * (1 / 5.0f));
                centerRelative = Vector3.Transform(centerRelative, Matrix.CreateRotationZ(RadianDirection));

                return this.Position + centerRelative;
            } else return this.Position;
        }

        public ObjectComponent(GameObject obj){
            this.Obj = obj;
            renderInfo = new WorldObjectRenderInfo();
            if (obj.OBJ.BaseGraphicID > 0)
            {
                var gid = obj.OBJ.BaseGraphicID;
                this.DrawGroup = obj.Resource.Get<DGRP>(gid);  
            }
            dgrp = new DGRPRenderer(this.DrawGroup);
            dgrp.DynamicSpriteBaseID = obj.OBJ.DynamicSpriteBaseId;
            dgrp.NumDynamicSprites = obj.OBJ.NumDynamicSprites;
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
                if (blueprint != null) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                DynamicCounter = 0;
            }
        }

        private bool _ForceDynamic;

        public bool ForceDynamic
        {
            get
            {
                return _ForceDynamic;
            }
            set
            {
                if (blueprint != null && _ForceDynamic != value)
                {
                    if (value) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                    else blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_RETURN_TO_STATIC, TileX, TileY, Level, this));
                }
                _ForceDynamic = value;
            }

        }

        private uint _DynamicSpriteFlags = 0x00000000;
        public uint DynamicSpriteFlags
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

        private float RadianDirection
        {
            get
            {
                switch (_Direction)
                {
                    case Direction.NORTH:
                        return 0;
                    case Direction.EAST:
                        return (float)Math.PI/2;
                    case Direction.SOUTH:
                        return (float)Math.PI;
                    case Direction.WEST:
                        return (float)Math.PI*1.5f;
                    default:
                        return 0;
                }
            }
        }

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
                if (dgrp != null){
                    dgrp.Direction = value;
                    dgrp.InvalidateRotation();
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

        public void ValidateSprite(WorldState world)
        {
            dgrp.ValidateSprite(world);
        }

        public override void Draw(GraphicsDevice device, WorldState world){
            if (this.DrawGroup == null) { return; }
            if (!world.TempDraw)
            {
                LastScreenPos = world.WorldSpace.GetScreenFromTile(Position) + world.WorldSpace.GetScreenOffset() + PosCenterOffsets[(int)world.Zoom-1];
                LastZoomLevel = (int)world.Zoom;
            }
            if (!Visible) return;
            dgrp.Draw(world);

            if (Headline != null)
            {
                var headOff = new Vector3(0, 0, 0.66f);
                var headPx = world.WorldSpace.GetScreenFromTile(headOff);

                var item = new _2DSprite();
                item.Pixel = Headline;
                item.Depth = TextureGenerator.GetWallZBuffer(device)[30];
                item.RenderMode = _2DBatchRenderMode.Z_BUFFER;

                item.SrcRect = new Rectangle(0, 0, Headline.Width, Headline.Height);
                item.WorldPosition = headOff;
                var off = PosCenterOffsets[(int)world.Zoom - 1];
                item.DestRect = new Rectangle(
                    ((int)headPx.X-Headline.Width/2) + (int)off.X, 
                    ((int)headPx.Y-Headline.Height/2)+ (int)off.Y, Headline.Width, Headline.Height);
                world._2D.Draw(item);
            }

            bool forceDynamic = ForceDynamic;
            if (Container != null && Container is ObjectComponent) {
                forceDynamic = ((ObjectComponent)Container).ForceDynamic;
                if (forceDynamic && renderInfo.Layer == WorldObjectRenderLayer.STATIC) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
            }
            if (renderInfo.Layer == WorldObjectRenderLayer.DYNAMIC && !forceDynamic && DynamicCounter++ > 120 && blueprint != null) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_RETURN_TO_STATIC, TileX, TileY, Level, this));
        }
    }
}
