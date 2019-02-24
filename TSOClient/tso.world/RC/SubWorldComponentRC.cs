using FSO.LotView.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using FSO.Content;
using FSO.LotView.Model;

namespace FSO.LotView.RC
{
    public class SubWorldComponentRC : SubWorldComponent
    {
        public SubWorldComponentRC(GraphicsDevice Device) : base(Device)
        {
        }

        /// <summary>
        /// Prep work before screen is painted
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="state"></param>
        public override void PreDraw(GraphicsDevice gd, WorldState state)
        {
            if (Blueprint == null) return;
            Blueprint.Terrain.SubworldOff = GlobalPosition * 3;
            var damage = Blueprint.Damage;
            var oldLevel = state.Level;
            var oldBuild = state.BuildMode;
            state.SilentLevel = State.Level;
            state.SilentBuildMode = 0;

            /**
             * This is a little bit different from a normal 2d world. All objects are part of the static 
             * buffer, and they are redrawn into the parent world's scroll buffers.
             */

            var lightChangeType = 0;

            var recacheWalls = false;
            var recacheObjects = false;
            var updateColor = false;

            Blueprint.OutsideTime = state.Light?.Blueprint?.OutsideTime ?? 0.5f;

            foreach (var item in damage)
            {
                switch (item.Type)
                {
                    case BlueprintDamageType.ROTATE:
                    case BlueprintDamageType.ZOOM:
                    case BlueprintDamageType.LEVEL_CHANGED:
                        recacheObjects = true;
                        recacheWalls = true;
                        break;
                    case BlueprintDamageType.SCROLL:
                        break;
                    case BlueprintDamageType.OBJECT_MOVE:
                    case BlueprintDamageType.OBJECT_GRAPHIC_CHANGE:
                    case BlueprintDamageType.OBJECT_RETURN_TO_STATIC:
                        recacheObjects = true;
                        break;
                    case BlueprintDamageType.WALL_CUT_CHANGED:
                    case BlueprintDamageType.FLOOR_CHANGED:
                    case BlueprintDamageType.WALL_CHANGED:
                        recacheWalls = true;
                        break;

                    case BlueprintDamageType.LIGHTING_CHANGED:
                        if (lightChangeType >= 2) break;
                        var room = (ushort)item.TileX;

                        State.Light?.InvalidateRoom(room);
                        updateColor = true;
                        //TicksSinceLight = 0;
                        break;
                    case BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED:
                        if (lightChangeType >= 1) break;
                        lightChangeType = 1;

                        updateColor = true;
                        State.Light?.BuildOutdoorsLight(Blueprint.OutsideTime);
                        State.Light?.InvalidateOutdoors();

                        //TicksSinceLight = 0;
                        break;
                    case BlueprintDamageType.ROOM_CHANGED:
                        for (sbyte i = 0; i < Blueprint.RoomMap.Length; i++)
                        {
                            State.Rooms.SetRoomMap(i, Blueprint.RoomMap[i]);
                        }
                        if (State.Light != null)
                        {
                            if (lightChangeType < 2)
                            {
                                lightChangeType = 2;
                                State.Light?.BuildOutdoorsLight(Blueprint.OutsideTime);
                                updateColor = true;
                                State.Light.InvalidateAll();
                            }
                        }
                        Blueprint.Indoors = null;
                        Blueprint.RoofComp.ShapeDirty = true;
                        break;
                }
            }
            damage.Clear();

            if (updateColor)
            {
                State.OutsideColor = state.OutsideColor;
                Blueprint.OutsideColor = state.OutsideColor;
            }
            State.LightingAdjust = state.OutsideColor.ToVector3() / State.OutsideColor.ToVector3();
            State.Light?.ParseInvalidated(FloorsUsed, State);

            var is2d = state.Camera is WorldCamera;
            if (is2d)
            {
                state._2D.End();
                state._2D.Begin(state.Camera);
            }
            if (recacheWalls)
            {
                //clear the sprite buffer before we begin drawing what we're going to cache
                Blueprint.Terrain.RegenTerrain(gd, Blueprint);
                Blueprint.FloorGeom.FullReset(gd, false);
                Blueprint.WCRC.Generate(gd, state, false);
            }

            state.SilentBuildMode = oldBuild;
            state.SilentLevel = oldLevel;
        }

        public override void DrawArch(GraphicsDevice gd, WorldState parentState)
        {
            var parentScroll = parentState.CenterTile;
            if (!(parentState.Camera is WorldCamera))
                parentState.Camera.Translation = new Vector3(GlobalPosition.X * 3, 0, GlobalPosition.Y * 3);
            else parentState.CenterTile += GlobalPosition; //TODO: vertical offset

            if (State.Light != null)
            {
                State.PrepareLighting();
            }
            else
            {
                parentState.ClearLighting(true);
            }
            
            var level = parentState.SilentLevel;
            var build = parentState.SilentBuildMode;
            parentState.SilentLevel = 5;
            parentState.SilentBuildMode = 0;
            Blueprint.Terrain._3D = true;
            Blueprint.Terrain.Draw(gd, parentState);
            parentState.SilentBuildMode = build;
            var effect = WorldContent.RCObject;
            gd.BlendState = BlendState.NonPremultiplied;
            parentState.DrawOOB = false;
            var view = parentState.Camera.View;
            var vp = view * parentState.Camera.Projection;
            effect.Parameters["ViewProjection"].SetValue(vp);
            Blueprint.WCRC.Draw(gd, parentState);
            Blueprint.RoofComp.Draw(gd, parentState);
            parentState.SilentLevel = level;
            effect.CurrentTechnique = effect.Techniques["Draw"];

            var frustrum = new BoundingFrustum(vp);
            var objs = Blueprint.Objects.Where(x => frustrum.Intersects(((ObjectComponentRC)x).GetBounds()))
                .OrderBy(x => ((ObjectComponentRC)x).SortDepth(view));
            foreach (var obj in objs)
            {
                obj.Draw(gd, parentState);
            }

            parentState.CenterTile = parentScroll;
            if (!(parentState.Camera is WorldCamera))
                parentState.Camera.Translation = Vector3.Zero;
            parentState.PrepareLighting();
        }

        public override ObjectComponent MakeObjectComponent(GameObject obj)
        {
            return new ObjectComponentRC(obj);
        }

        public BoundingBox Bounds;

        public void UpdateBounds()
        {
            float minAlt = 0;
            foreach (var height in Blueprint.Altitude)
            {
                var alt = height * Blueprint.TerrainFactor - Blueprint.BaseAlt;
                if (alt < minAlt)
                {
                    minAlt = alt;
                }
            }
            Bounds = new BoundingBox(new Vector3(GlobalPosition.X * -3, minAlt, GlobalPosition.Y * -3), new Vector3(GlobalPosition.X * -3 + Blueprint.Width*3, 1000, GlobalPosition.Y * -3 + Blueprint.Height*3));
        }
    }
}
