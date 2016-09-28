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
        public int DynamicCounter; //how long this sprite has been dynamic without changing sprite
        public List<SLOTItem> ContainerSlots;

        public bool HideForCutaway;
        public WallSegments AdjacentWall;

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
            var item = (ContainerSlots != null && ContainerSlots.Count > slot) ? ContainerSlots[slot] : null;
            if (item != null)
            {
                var off = item.Offset;
                var centerRelative = new Vector3(off.X * (1 / 16.0f), off.Y * (1 / 16.0f), ((item.Height != 5) ? SLOT.HeightOffsets[item.Height - 1] : off.Z) * (1 / 5.0f));
                centerRelative = Vector3.Transform(centerRelative, Matrix.CreateRotationZ(RadianDirection));

                return this.Position + centerRelative;
            } else return this.Position;
        }

        public ObjectComponent(GameObject obj) {
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
                if (blueprint != null && dgrp.DGRP != value)
                {
                    blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                    DynamicCounter = 0;
                }
                dgrp.DGRP = value;
            }
        }

        private bool _CutawayHidden;

        public bool CutawayHidden
        {
            get
            {
                return _CutawayHidden;
            }
            set
            {
                if (blueprint != null && _CutawayHidden != value && renderInfo.Layer == WorldObjectRenderLayer.STATIC)
                {
                    blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                    DynamicCounter = 0;
                }
                _CutawayHidden = value;
            }

        }

        private bool _ForceDynamic;

        public bool ForceDynamic
        {
            get
            {
                return (_ForceDynamic || Headline != null);
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

        private ulong _DynamicSpriteFlags = 0x00000000;
        private ulong _DynamicSpriteFlags2 = 0x00000000;
        public ulong DynamicSpriteFlags
        {
            get {
                return _DynamicSpriteFlags;
            } set {

                if (dgrp != null && _DynamicSpriteFlags != value) {
                    dgrp.DynamicSpriteFlags = value;
                    if (blueprint != null) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                    DynamicCounter = 0;
                }
                _DynamicSpriteFlags = value;
            }
        }

        public ulong DynamicSpriteFlags2
        {
            get
            {
                return _DynamicSpriteFlags2;
            }
            set
            {

                if (dgrp != null && _DynamicSpriteFlags2 != value)
                {
                    dgrp.DynamicSpriteFlags2 = value;
                    if (blueprint != null) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                    DynamicCounter = 0;
                }
                _DynamicSpriteFlags2 = value;
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
                        return (float)Math.PI / 2;
                    case Direction.SOUTH:
                        return (float)Math.PI;
                    case Direction.WEST:
                        return (float)Math.PI * 1.5f;
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
                if (dgrp != null) {
                    dgrp.Direction = value;
                    dgrp.InvalidateRotation();
                }
            }
        }

        public override void OnRotationChanged(WorldState world)
        {
            base.OnRotationChanged(world);
            if (dgrp != null) {
                dgrp.InvalidateRotation();
            }
        }

        public override void OnZoomChanged(WorldState world)
        {
            base.OnZoomChanged(world);
            if (dgrp != null) {
                dgrp.InvalidateZoom();
            }
        }

        public override void OnScrollChanged(WorldState world)
        {
            base.OnScrollChanged(world);
            if (dgrp != null) {
                dgrp.InvalidateScroll();
            }
        }

        public void ValidateSprite(WorldState world)
        {
            dgrp.ValidateSprite(world);
        }

        public override Vector2 GetScreenPos(WorldState world)
        {
            return world.WorldSpace.GetScreenFromTile(Position) + world.WorldSpace.GetScreenOffset() + PosCenterOffsets[(int)world.Zoom - 1];
        }

        public static Dictionary<WallSegments, Point> CutawayTests = new Dictionary<WallSegments, Point>
        {
            { WallSegments.BottomLeft, new Point(0,1) },
            { WallSegments.TopLeft, new Point(-1,0) },
            { WallSegments.TopRight, new Point(0,-1)},
            { WallSegments.BottomRight, new Point(1,0) }
        };

        public override void Update(GraphicsDevice device, WorldState world)
        {
            if (Headline != null)
            {
                if (blueprint != null && renderInfo.Layer == WorldObjectRenderLayer.STATIC) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                DynamicCounter = 0; //keep windows and doors on the top floor on the dynamic layer.
            }

            if (HideForCutaway && Level > 0)
            {
                if (!(world.BuildMode > 1) && world.DynamicCutaway && Level == world.Level)
                {
                    if (blueprint != null && renderInfo.Layer == WorldObjectRenderLayer.STATIC) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
                    DynamicCounter = 0; //keep windows and doors on the top floor on the dynamic layer.
                }

                if (Level != world.Level || world.BuildMode > 1) CutawayHidden = false;
                else
                {
                    var tilePos = new Point((int)Math.Round(Position.X), (int)Math.Round(Position.Y));

                    if (tilePos.X >= 0 && tilePos.X < blueprint.Width && tilePos.Y >= 0 && tilePos.Y < blueprint.Height)
                    {
                        var wall = blueprint.Walls[Level - 1][tilePos.Y * blueprint.Width + tilePos.X];
                        var cutTest = new Point();
                        if (!CutawayTests.TryGetValue(AdjacentWall & wall.Segments, out cutTest))
                        {
                            CutawayTests.TryGetValue(wall.OccupiedWalls & wall.Segments, out cutTest);
                        }
                        var positions = new Point[] { tilePos, tilePos + cutTest };

                        var canContinue = true;

                        foreach (var pos in positions)
                        {
                            canContinue = canContinue && (pos.X >= 0 && pos.X < blueprint.Width && pos.Y >= 0 && pos.Y < blueprint.Height
                                && blueprint.Cutaway[pos.Y * blueprint.Width + pos.X]);
                            if (!canContinue) break;
                        }
                        CutawayHidden = canContinue;
                    }
                }
            }

            bool forceDynamic = ForceDynamic;
            if (Container != null && Container is ObjectComponent)
            {
                forceDynamic = ((ObjectComponent)Container).ForceDynamic;
                if (forceDynamic && renderInfo.Layer == WorldObjectRenderLayer.STATIC) blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_GRAPHIC_CHANGE, TileX, TileY, Level, this));
            }
            if (renderInfo.Layer == WorldObjectRenderLayer.DYNAMIC && !forceDynamic && DynamicCounter++ > 120 && blueprint != null)
            {
                blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.OBJECT_RETURN_TO_STATIC, TileX, TileY, Level, this));
            }
        }

        public override void Draw(GraphicsDevice device, WorldState world){
//#if !DEBUG 
            if (!Visible || (Position.X < 0 && Position.Y < 0)) return;
//#endif
            if (CutawayHidden) return;
            if (this.DrawGroup != null) dgrp.Draw(world);

            if (Headline != null && !Headline.IsDisposed)
            {
                var headOff = new Vector3(0, 0, 0.66f);
                var headPx = world.WorldSpace.GetScreenFromTile(headOff);

                var item = world._2D.NewSprite(_2DBatchRenderMode.Z_BUFFER);
                item.Pixel = Headline;
                item.Depth = TextureGenerator.GetWallZBuffer(device)[30];

                item.SrcRect = new Rectangle(0, 0, Headline.Width, Headline.Height);
                item.WorldPosition = headOff;
                var off = PosCenterOffsets[(int)world.Zoom - 1];
                item.DestRect = new Rectangle(
                    ((int)headPx.X-Headline.Width/2) + (int)off.X, 
                    ((int)headPx.Y-Headline.Height/2)+ (int)off.Y, Headline.Width, Headline.Height);
                world._2D.Draw(item);
            }
        }
    }
}
