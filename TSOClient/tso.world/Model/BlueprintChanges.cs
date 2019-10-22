using FSO.LotView.Components;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView
{
    public class BlueprintChanges
    {
        public bool Subworld = false;
        public TimedDynamicStaticLayers Layers = new TimedDynamicStaticLayers(5);

        public HashSet<ObjectComponent> DynamicObjects => Layers.DynamicObjects;
        public HashSet<ObjectComponent> StaticObjects => Layers.StaticObjects;

        public HashSet<ObjectComponent> ObjectMoved = new HashSet<ObjectComponent>();
        public HashSet<short> RoomLightInvalid = new HashSet<short>();

        public object FloorChanges;
        public object WallChanges;
        public BlueprintGlobalChanges Dirty;
        public int TicksSinceLight;
        public int LastSubLightUpdate = -1;

        public ScrollBuffer StaticSurface; //copied in from parent - reference is for checking pxOffset

        private bool ObjectSetDirty;
        public bool StaticSurfaceDirty;
        public bool StaticObjectDirty;

        public bool DrawImmediate;
        public bool UpdateColor;

        public bool Arch2D;

        private Blueprint Blueprint;

        public BlueprintChanges(Blueprint blueprint)
        {
            Blueprint = blueprint;
        }

        public double LastTimeOfDay = -99999;

        public void PreDraw(GraphicsDevice gd, WorldState state)
        {
            DrawImmediate = state.ForceImmediate;
            if (state.CameraMode < CameraRenderMode._3D)
            {
                state.CameraMode = (state.Cameras.Safe2D) ? CameraRenderMode._2D : CameraRenderMode._2DRotate;
            }
            if (state.CameraMode > CameraRenderMode._2D) DrawImmediate = true;

            if (Math.Abs(Blueprint.OutsideTime - LastTimeOfDay) > 0.001f)
            {
                Dirty |= BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED;
                LastTimeOfDay = Blueprint.OutsideTime;
            }

            if ((Dirty & BlueprintGlobalChanges.ALL) > 0)
            {
                if ((Dirty & BlueprintGlobalChanges.VIEW_CHANGE_2D) > 0)
                {
                    StaticSurfaceDirty = true;
                    Dirty |= BlueprintGlobalChanges.WALL_CHANGED;
                    Dirty |= BlueprintGlobalChanges.FLOOR_CHANGED;
                    if ((Dirty & BlueprintGlobalChanges.ROTATE) > 0)
                    {
                        foreach (var obj in Blueprint.Objects) obj.UpdateDrawOrder(state);
                    }

                    //invalidate walls
                    //invalidate floors
                    //invalidate outdoors?
                }
                if ((Dirty & BlueprintGlobalChanges.SCROLL) > 0)
                {
                    //invalidate scroll buffers if our new scroll view is outside their ranges
                    var pxOffset = -state.WorldSpace.GetScreenOffset();
                    if (StaticSurface == null || StaticSurface.PxOffset != StaticSurface.GetScrollIncrement(pxOffset, state))
                    {
                        StaticSurfaceDirty = true;
                    }
                }
                if ((Dirty & BlueprintGlobalChanges.PRECISE_ZOOM) > 0)
                {
                    DrawImmediate = true; 
                }

                if ((Dirty & BlueprintGlobalChanges.LIGHTING_ANY) > 0)
                {
                    UpdateColor = true;
                    Blueprint.GenerateRoomLights();
                    state.OutsideColor = Blueprint.RoomColors[1];
                    state.OutsidePx.SetData(new Color[] { new Color(Blueprint.OutsideColor, (Blueprint.OutsideColor.R + Blueprint.OutsideColor.G + Blueprint.OutsideColor.B) / (255 * 3f)) });
                    if (state.AmbientLight != null)
                    {
                        state.AmbientLight.SetData(Blueprint.RoomColors);
                    }
                    if ((Dirty & BlueprintGlobalChanges.ROOM_CHANGED) == 0)
                    {
                        if ((Dirty & BlueprintGlobalChanges.LIGHTING_CHANGED) > 0 && state.Light != null)
                        {
                            //pass invalidated rooms
                            foreach (var room in RoomLightInvalid) {
                                state.Light?.InvalidateRoom((ushort)room);
                            }
                            RoomLightInvalid.Clear();
                        }
                        if ((Dirty & BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED) > 0)
                        {
                            state.Light?.InvalidateOutdoors();

                            if (Blueprint.SubWorlds.Count > 0)
                            {
                                Blueprint.SubWorlds[LastSubLightUpdate].RefreshLighting();
                                LastSubLightUpdate = (LastSubLightUpdate + 1) % Blueprint.SubWorlds.Count;
                            }
                        }
                    }

                    if (LastSubLightUpdate == -1)
                    {
                        foreach (var sub in Blueprint.SubWorlds) sub.RefreshLighting();
                        LastSubLightUpdate = 0;
                    }

                    TicksSinceLight = 0;
                    StaticSurfaceDirty = true;
                }

                var wallFlags = (Dirty & (BlueprintGlobalChanges.WALL_CHANGED | BlueprintGlobalChanges.WALL_CUT_CHANGED));
                if (wallFlags > 0)
                {
                    //process wall changes
                    state.Platform.RecacheWalls(gd, state, wallFlags == BlueprintGlobalChanges.WALL_CUT_CHANGED);
                    StaticSurfaceDirty = true;
                }

                if ((Dirty & BlueprintGlobalChanges.ROOF_STYLE_CHANGED) > 0)
                {
                    Blueprint.RoofComp.StyleDirty = true;
                }

                if ((Dirty & BlueprintGlobalChanges.ARCH_CHANGED) > 0)
                {
                    if ((Dirty & BlueprintGlobalChanges.FLOOR_CHANGED) > 0)
                    {
                        //process floor changes
                        Blueprint.FloorGeom.FullReset(gd, state.BuildMode > 1);
                    }
                    if ((Dirty & BlueprintGlobalChanges.ROOM_CHANGED) > 0)
                    {
                        for (sbyte i = 0; i < Blueprint.RoomMap.Length; i++)
                        {
                            state.Rooms.SetRoomMap(i, Blueprint.RoomMap[i]);
                        }
                        if (state.Light != null)
                        {
                            UpdateColor = true;
                            state.Light.InvalidateAll();
                        }
                        Blueprint.Indoors = null;
                        Blueprint.RoofComp.ShapeDirty = true;
                    }

                    StaticSurfaceDirty = true;
                }

                if ((Dirty & (BlueprintGlobalChanges.OPENGL_SECOND_DRAW)) > 0)
                {
                    //must redraw to avoid issues with floors
                    StaticSurfaceDirty = true;
                }
            }
            Dirty = 0;

            state.Light?.ParseInvalidated((sbyte)(state.Level + ((state.DrawRoofs) ? 1 : 0)), state);

            //for moved objects, regenerate depth and move them to dynamic if they aren't already there.
            foreach (var obj in ObjectMoved)
            {
                if (obj.Dead) continue;
                obj.UpdateDrawOrder(state);
                Layers.EnsureDynamic(obj);
            }
            StaticObjectDirty = ObjectSetDirty;
            ObjectSetDirty = false;
            if (StaticObjectDirty) StaticSurfaceDirty = true;
            ObjectMoved.Clear();

            if (Layers.Update())
            {
                //static buffer is dirty.
                StaticSurfaceDirty = true;
            }
        }

        public void LightChange(short roomID)
        {
            Dirty |= BlueprintGlobalChanges.LIGHTING_CHANGED;
            RoomLightInvalid.Add(roomID);
        }

        public void RegisterObject(ObjectComponent item)
        {
            ObjectSetDirty = true;
            Layers.RegisterObject(item);
        }

        public void UnregisterObject(ObjectComponent item)
        {
            ObjectSetDirty = true;
            Layers.UnregisterObject(item);
        }

        public void RegisterObjectChange(ObjectComponent item)
        {
            ObjectMoved.Add(item);
        }

        public void SetFlag(BlueprintGlobalChanges flag)
        {
            Dirty |= flag;
        }
    }

    [Flags]
    public enum BlueprintGlobalChanges : int
    {
        SCROLL = 1,
        ROTATE = 1 << 1,
        ZOOM = 1 << 2,
        PRECISE_ZOOM = 1 << 3,
        LEVEL_CHANGED = 1 << 4,
        LIGHTING_CHANGED = 1 << 5, //also has per-room component
        OUTDOORS_LIGHTING_CHANGED = 1 << 6,
        ROOF_STYLE_CHANGED = 1 << 7,
        OPENGL_SECOND_DRAW = 1 << 8, //workaround for some stupid texture switch stuff
        FLOOR_CHANGED = 1 << 9, //global invalidation of scroll buffer, but more specific invalidation for texture-geometry groups
        WALL_CHANGED = 1 << 10, //global invalidation of scroll buffer, but more specific invalidation for regions
        WALL_CUT_CHANGED = 1 << 11,  //global invalidation of scroll buffer, but more specific invalidation for regions
        ROOM_CHANGED = 1 << 12,

        VIEW_CHANGE_2D = ROTATE | ZOOM | LEVEL_CHANGED, //all of these invalidate the scroll buffers.
        ARCH_CHANGED = WALL_CHANGED | FLOOR_CHANGED | ROOM_CHANGED,
        LIGHTING_ANY = LIGHTING_CHANGED | OUTDOORS_LIGHTING_CHANGED,
        ALL = 0x0FFF
    }

    /*
    public enum BlueprintDamageType
    {
        OBJECT_GRAPHIC_REFRESH, //also happens when the object moves
        OBJECT_RETURN_TO_STATIC, //handled internally now
        FLOOR_CHANGED,
        WALL_CHANGED,
        WALL_CUT_CHANGED,
        LIGHTING_CHANGED,
        OUTDOORS_LIGHTING_CHANGED,
    }
    */
}
