using FSO.LotView.Components;
using FSO.LotView.Model;
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
        public TimedDynamicStaticLayers Layers = new TimedDynamicStaticLayers(5);

        public HashSet<ObjectComponent> DynamicObjects => Layers.DynamicObjects;
        public HashSet<ObjectComponent> StaticObjects => Layers.StaticObjects;

        public HashSet<ObjectComponent> ObjectMoved;

        public object FloorChanges;
        public object WallChanges;
        public BlueprintGlobalChanges Dirty;

        public ScrollBuffer StaticSurface; //copied in from parent - reference is for checking pxOffset

        public bool StaticSurfaceDirty;
        public bool StaticObjectDirty;

        public bool DrawImmediate;

        public void PreDraw(GraphicsDevice gd, WorldState state)
        {
            if ((Dirty & BlueprintGlobalChanges.ALL) > 0)
            {
                if ((Dirty & BlueprintGlobalChanges.VIEW_CHANGE_2D) > 0)
                {
                    StaticSurfaceDirty = true;
                    //invalidate walls
                    //invalidate floors
                    //invalidate outdoors?
                }
                if ((Dirty & BlueprintGlobalChanges.SCROLL) > 0)
                {
                    //invalidate scroll buffers if our new scroll view is outside their ranges
                    var pxOffset = -state.WorldSpace.GetScreenOffset();
                    if (StaticObjects == null || StaticSurface.PxOffset != StaticSurface.GetScrollIncrement(pxOffset, state))
                    {
                        StaticSurfaceDirty = true;
                    }
                }
                if ((Dirty & BlueprintGlobalChanges.PRECISE_ZOOM) > 0)
                {
                    DrawImmediate = true; 
                }
                if ((Dirty & BlueprintGlobalChanges.LIGHTING_CHANGED) > 0)
                {
                    //process invalid list

                }
                if ((Dirty & BlueprintGlobalChanges.OUTDOORS_LIGHTING_CHANGED) > 0)
                {
                    //should probably be partially folded into the above

                }

                var wallFlags = (Dirty & (BlueprintGlobalChanges.WALL_CHANGED | BlueprintGlobalChanges.WALL_CUT_CHANGED));
                if (wallFlags > 0)
                {
                    //process wall changes
                    Blueprint.WCRC?.Generate(gd, state, wallFlags == BlueprintGlobalChanges.WALL_CUT_CHANGED);
                    StaticSurfaceDirty = true;
                }

                if ((Dirty & (BlueprintGlobalChanges.FLOOR_CHANGED)) > 0)
                {
                    //process floor changes
                    StaticSurfaceDirty = true;
                }

                if ((Dirty & (BlueprintGlobalChanges.OPENGL_SECOND_DRAW)) > 0)
                {
                    //must redraw to avoid issues with floors
                    StaticSurfaceDirty = true;
                }
            }

            if (recacheWalls || recacheCutaway)
            {
                _2d.Pause();
                _2d.Resume(); //clear the sprite buffer before we begin drawing what we're going to cache
                Blueprint.WallComp.Draw(gd, state);
                ClearDrawBuffer(StaticWallCache);
                state.PrepareLighting();
                _2d.End(StaticWallCache, true);
            }

            if (recacheFloors)
            {
                _2d.Pause();
                _2d.Resume(); //clear the sprite buffer before we begin drawing what we're going to cache
                //Blueprint.FloorComp.Draw(gd, state);
                Blueprint.FloorGeom.FullReset(gd, state.BuildMode > 1);
                ClearDrawBuffer(StaticFloorCache);
                _2d.End(StaticFloorCache, true);
            }

            //for moved objects, regenerate depth and move them to dynamic if they aren't already there.
            foreach (var obj in ObjectMoved)
            {
                obj.UpdateDrawOrder(state);
                Layers.EnsureDynamic(obj);
                if (obj.RenderInfo.Layer == WorldObjectRenderLayer.STATIC)
                {
                    StaticObjects.Remove(obj);
                    DynamicObjects.Add(obj);
                }
            }
            StaticObjectDirty = ObjectMoved.Count > 0;
            if (StaticObjectDirty) StaticSurfaceDirty = true;
            ObjectMoved.Clear();

            if (Layers.Update())
            {
                //static buffer is dirty.
                StaticSurfaceDirty = true;
            }
        }

        public void RegisterObjectChange(ObjectComponent item)
        {
            ObjectMoved.Add(item);
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
        OPENGL_SECOND_DRAW = 1 << 8,
        FLOOR_CHANGED = 1 << 9, //global invalidation of scroll buffer, but more specific invalidation for texture-geometry groups
        WALL_CHANGED = 1 << 10, //global invalidation of scroll buffer, but more specific invalidation for regions
        WALL_CUT_CHANGED = 1 << 11,  //global invalidation of scroll buffer, but more specific invalidation for regions

        VIEW_CHANGE_2D = ROTATE | ZOOM | LEVEL_CHANGED; //all of these invalidate the scroll buffers.
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
