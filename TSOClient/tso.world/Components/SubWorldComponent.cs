using FSO.Common.Rendering.Framework;
using FSO.Common.Utils;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Components
{
    public class SubWorldComponent : World
    {
        /// <summary>
        /// Creates a new World instance.
        /// </summary>
        /// <param name="Device">A GraphicsDevice instance.</param>
        public SubWorldComponent(GraphicsDevice Device)
            : base(Device)
        {
        }

        public Vector2 GlobalPosition;

        private List<_2DDrawBuffer> StaticObjectsCache = new List<_2DDrawBuffer>();
        private List<_2DDrawBuffer> StaticArchCache = new List<_2DDrawBuffer>();
        private int TicksSinceLight = 0;

        /// <summary>
        /// Setup anything that needs a GraphicsDevice
        /// </summary>
        /// <param name="layer"></param>
        public void Initialize(GraphicsDevice device)
        {
            /**
             * Setup world state, this object acts as a facade
             * to world objects as well as providing various
             * state settings for the world and helper functions
             */
            State = new WorldState(device, device.Viewport.Width, device.Viewport.Height, this);
            State.AmbientLight = new Texture2D(device, 256, 256);
            State.OutsidePx = new Texture2D(device, 1, 1);

            HasInitGPU = true;
            HasInit = HasInitGPU & HasInitBlueprint;
        }

        public override void InitBlueprint(Blueprint blueprint)
        {
            this.Blueprint = blueprint;
            HasInitBlueprint = true;
            HasInit = HasInitGPU & HasInitBlueprint;
        }

        /// <summary>
        /// Prep work before screen is painted
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="state"></param>
        public virtual void PreDraw(GraphicsDevice gd, WorldState state)
        {
            if (Blueprint == null) return;
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            var damage = Blueprint.Damage;
            var _2d = state._2D;
            var oldLevel = state.Level;
            var oldBuild = state.BuildMode;
            state.SilentLevel = State.Level;
            state.SilentBuildMode = 0;

            /**
             * This is a little bit different from a normal 2d world. All objects are part of the static 
             * buffer, and they are redrawn into the parent world's scroll buffers.
             */

            var recacheWalls = false;
            var recacheObjects = false;

            if (TicksSinceLight++ > 60 * 4) damage.Add(new BlueprintDamage(BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED));

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
                    case BlueprintDamageType.LIGHTING_CHANGED:
                        Blueprint.OutsideColor = state.OutsideColor;
                        Blueprint.GenerateRoomLights();
                        State.OutsideColor = Blueprint.RoomColors[1];
                        State.OutsidePx.SetData(new Color[] { state.OutsideColor });
                        State.AmbientLight.SetData(Blueprint.RoomColors);
                        TicksSinceLight = 0;
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
                }
            }
            damage.Clear();

            var is2d = state.Camera is WorldCamera;
            if (is2d) {
                state._2D.End();
                state._2D.Begin(state.Camera);
            }
            if (recacheWalls)
            {
                //clear the sprite buffer before we begin drawing what we're going to cache
                Blueprint.Terrain.RegenTerrain(gd, state, Blueprint);
                Blueprint.FloorGeom.FullReset(gd, false);
                if (is2d) Blueprint.WallComp.Draw(gd, state);
                StaticArchCache.Clear();
                state._2D.End(StaticArchCache, true);
            }

            if (recacheObjects && is2d)
            {
                state._2D.Pause();
                state._2D.Resume();

                foreach (var obj in Blueprint.Objects)
                {
                    if (obj.Level > state.Level) continue;
                    var tilePosition = obj.Position;
                    state._2D.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition));
                    state._2D.OffsetTile(tilePosition);
                    state._2D.SetObjID(obj.ObjectID);
                    obj.Draw(gd, state);
                }
                StaticObjectsCache.Clear();
                state._2D.End(StaticObjectsCache, true);
            }

            state.SilentBuildMode = oldBuild;
            state.SilentLevel = oldLevel;
        }

        public virtual void DrawArch(GraphicsDevice gd, WorldState parentState)
        {
            var parentScroll = parentState.CenterTile;
            if (!(parentState.Camera is WorldCamera))
                parentState.Camera.Translation = new Vector3(GlobalPosition.X*3, 0, GlobalPosition.Y*3);
            else parentState.CenterTile += GlobalPosition; //TODO: vertical offset

            var pxOffset = -parentState.WorldSpace.GetScreenOffset();

            parentState.ClearLighting(true);

            parentState._2D.SetScroll(pxOffset);
            var level = parentState.SilentLevel;
            parentState.SilentLevel = 5;
            Blueprint.Terrain.Draw(gd, parentState);
            if (parentState.Camera is WorldCamera)
            {
                parentState._2D.RenderCache(StaticArchCache);
                parentState._2D.Pause();
            }
            Blueprint.RoofComp.Draw(gd, parentState);
            parentState.SilentLevel = level;

            parentState.CenterTile = parentScroll;
            if (!(parentState.Camera is WorldCamera))
                parentState.Camera.Translation = Vector3.Zero;
            parentState.PrepareLighting();
        }

        public virtual void DrawObjects(GraphicsDevice gd, WorldState parentState)
        {
            var parentScroll = parentState.CenterTile;
            parentState.CenterTile += GlobalPosition; //TODO: vertical offset

            parentState.ClearLighting(true);
            var pxOffset = -parentState.WorldSpace.GetScreenOffset();

            parentState._2D.SetScroll(pxOffset);
            parentState._2D.RenderCache(StaticObjectsCache);

            parentState.CenterTile = parentScroll;
            parentState.PrepareLighting();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
