using FSO.Common;
using FSO.Common.Utils;
using FSO.LotView.LMap;
using FSO.LotView.Model;
using FSO.LotView.Platform;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

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
        public bool UseFade = true;

        //should this be used now? may be faster for surround - but then again culling may be faster than both
        private List<_2DDrawBuffer> StaticObjectsCache = new List<_2DDrawBuffer>();
        //private List<_2DDrawBuffer> StaticArchCache = new List<_2DDrawBuffer>();
        protected sbyte FloorsUsed = 1;

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
            State.SetCameraType(this, LotView.Utils.Camera.CameraControllerType._2D);
            GameThread.InUpdate(() =>
            {
                State.AmbientLight = new Texture2D(device, 256, 256);
                State.OutsidePx = new Texture2D(device, 1, 1);

                ChangedWorldConfig(device);
            });

            HasInitGPU = true;
            HasInit = HasInitGPU & HasInitBlueprint;
        }

        public override void InitBlueprint(Blueprint blueprint)
        {
            this.Blueprint = blueprint;
            HasInitBlueprint = true;
            HasInit = HasInitGPU & HasInitBlueprint;

            Light?.Init(Blueprint);
            State.Rooms.Init(blueprint);
            blueprint.Changes.SetFlag(BlueprintGlobalChanges.ROOM_CHANGED);
            blueprint.Changes.SetFlag(BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED);
            Architecture = new WorldArchitecture(blueprint);
            Entities = new WorldEntities(blueprint);
            Platform = new WorldPlatformNull(blueprint);
            State.Platform = Platform;
            State.Changes = blueprint.Changes;
            blueprint.Changes.Subworld = true;
        }

        public override void InitDefaultGraphicsMode()
        {
        }

        public void CalculateFloorsUsed()
        {
            FloorsUsed = (sbyte)(Blueprint.GetFloorsUsed() + 1);
        }

        /// <summary>
        /// Prep work before screen is painted
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="state"></param>
        public virtual void PreDraw(GraphicsDevice gd, WorldState state)
        {

            if (Blueprint == null) return;
            Blueprint.Terrain.SubworldOff = GlobalPosition * 3;
            Blueprint.Terrain.FadeDistance = UseFade ? 77 * 3f : 1000f;
            Blueprint.OutsideTime = state.Light?.Blueprint?.OutsideTime ?? 0.5f;

            var oldLevel = state.Level;
            var oldBuild = state.BuildMode;

            state.SilentLevel = State.Level;
            state.SilentBuildMode = 0;
            State._2D = state._2D;
            Blueprint.Changes.PreDraw(gd, State);

            state.SilentBuildMode = oldBuild;
            state.SilentLevel = oldLevel;

            /**
             * This is a little bit different from a normal 2d world. All objects are part of the static 
             * buffer, and they are redrawn into the parent world's scroll buffers.
             * We use the same BlueprintChanges for simplicity, though after load it won't really change. (and static/dynamic distinction is ignored)
             */

            if (Blueprint.Changes.UpdateColor)
            {
                State.OutsideColor = state.OutsideColor;
                Blueprint.OutsideColor = state.OutsideColor;
            }
            State.LightingAdjust = state.OutsideColor.ToVector3() / State.OutsideColor.ToVector3();

            /*
            if (Blueprint == null) return;
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            var damage = Blueprint.Damage;
            var _2d = state._2D;
            var oldLevel = state.Level;
            var oldBuild = state.BuildMode;
            state.SilentLevel = State.Level;
            state.SilentBuildMode = 0;

            int lightChangeType = 0;

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
            if (is2d) {
                state._2D.End();
                state._2D.Begin(state.Camera);
            }
            if (recacheWalls)
            {
                //clear the sprite buffer before we begin drawing what we're going to cache
                Blueprint.Terrain.RegenTerrain(gd, Blueprint);
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
            */
        }

        public void SubDraw(GraphicsDevice gd, WorldState parentState, Action<Vector2> action)
        {
            var parentScroll = parentState.CenterTile;
            if (parentState.CameraMode > CameraRenderMode._2D)
                parentState.Cameras.ModelTranslation = new Vector3(GlobalPosition.X * 3, 0, GlobalPosition.Y * 3);
            else parentState.CenterTile += GlobalPosition; //TODO: vertical offset
            var pxOffset = -parentState.WorldSpace.GetScreenOffset();

            if (State.Light != null)
            {
                State.PrepareLighting(parentState);
            }
            else
            {
                parentState.ClearLighting(true);
            }
            var oldLevel = parentState.Level;
            parentState.SilentLevel = State.Level;

            action(pxOffset);
            
            parentState.SilentLevel = oldLevel;


            if (parentState.CameraMode > CameraRenderMode._2D)
                parentState.Cameras.ModelTranslation = Vector3.Zero;
            else
                parentState.CenterTile = parentScroll;
            parentState.PrepareLighting();
        }

        public bool DoDraw(WorldState state)
        {
            return state.Frustum.Intersects(Bounds);
        }

        public BoundingBox Bounds;

        public void UpdateBounds()
        {
            float minAlt = 0;
            float maxAlt = 0;
            foreach (var height in Blueprint.Altitude)
            {
                var alt = height * Blueprint.TerrainFactor - Blueprint.BaseAlt;
                if (alt < minAlt)
                {
                    minAlt = alt;
                }
                if (alt > maxAlt)
                {
                    maxAlt = alt;
                }
            }
            //calculate the maximum floor. shave 1 off to account for buildable area being smaller, but minimum 1 floor as trees are likely on floor 1
            maxAlt += Math.Max(1, FloorsUsed - 1) * 2.95f * 3;
            Bounds = new BoundingBox(new Vector3(GlobalPosition.X * -3, minAlt, GlobalPosition.Y * -3), new Vector3(GlobalPosition.X * -3 + Blueprint.Width * 3, maxAlt, GlobalPosition.Y * -3 + Blueprint.Height * 3));
        }

        // unused

        public virtual void DrawArch(GraphicsDevice gd, WorldState parentState)
        {
            var parentScroll = parentState.CenterTile;
            if (parentState.CameraMode > CameraRenderMode._2D)
                parentState.Cameras.ModelTranslation = new Vector3(GlobalPosition.X*3, 0, GlobalPosition.Y*3);
            else parentState.CenterTile += GlobalPosition; //TODO: vertical offset

            var pxOffset = -parentState.WorldSpace.GetScreenOffset();

            if (State.Light != null)
            {
                State.PrepareLighting(parentState);
            }
            else
            {
                parentState.ClearLighting(true);
            }

            parentState._2D.SetScroll(pxOffset);
            var level = parentState.SilentLevel;
            parentState.SilentLevel = 5;
            Blueprint.Terrain.Draw(gd, parentState);
            if (parentState.CameraMode < CameraRenderMode._3D)
            {
                //parentState._2D.RenderCache(StaticArchCache);
                parentState._2D.Pause();
            }
            Blueprint.RoofComp.Draw(gd, parentState);
            parentState.SilentLevel = level;

            parentState.CenterTile = parentScroll;
            if (parentState.CameraMode > CameraRenderMode._2D)
                parentState.Cameras.ModelTranslation = Vector3.Zero;
            parentState.PrepareLighting();
        }

        public virtual void DrawObjects(GraphicsDevice gd, WorldState parentState)
        {
            var parentScroll = parentState.CenterTile;
            parentState.CenterTile += GlobalPosition; //TODO: vertical offset

            if (State.Light != null)
            {
                State.PrepareLighting(parentState);
            }
            else
            {
                parentState.ClearLighting(true);
            }
            var pxOffset = -parentState.WorldSpace.GetScreenOffset();

            parentState._2D.SetScroll(pxOffset);
            parentState._2D.RenderCache(StaticObjectsCache);

            parentState.CenterTile = parentScroll;
            parentState.PrepareLighting();
        }

        public void RefreshLighting()
        {
            Blueprint.Changes.SetFlag(BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED);
        }

        public override void ChangedWorldConfig(GraphicsDevice gd)
        {
            //destroy any features that are no longer enabled.

            var config = WorldConfig.Current;
            if (config.AdvancedLighting && config.Complex)
            {
                State.AmbientLight?.Dispose();
                State.AmbientLight = null;
                Light?.Dispose();
                Light = null;
                if (Light == null)
                {
                    Light = new LMapBatch(gd, 6);
                    if (Blueprint != null)
                    {
                        Light?.Init(Blueprint);
                        Blueprint.Changes.SetFlag(BlueprintGlobalChanges.ROOM_CHANGED);
                        Blueprint.Changes.SetFlag(BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED);
                    }
                    State.Light = Light;
                }
            }
            else
            {
                Light?.Dispose();
                Light = null;
                State.Light = null;
            }

            if (Blueprint != null && !FSOEnvironment.Enable3D)
            {
                var shad3D = (Blueprint.WCRC != null);
                if (config.Shadow3D != shad3D)
                {
                    if (Light != null && config.Shadow3D)
                    {
                        Blueprint.WCRC = new RC.WallComponentRC();
                        Blueprint.WCRC.blueprint = Blueprint;
                        Blueprint.WCRC.Generate(gd, State, false);
                    }
                    else
                    {
                        Blueprint.WCRC?.Dispose();
                        Blueprint.WCRC = null;
                    }
                    Blueprint.Changes.SetFlag(BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
