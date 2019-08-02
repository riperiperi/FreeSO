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
    /// <summary>
    /// Draws architecture in the world. As these elements cannot be semitransparent, they are drawn before all objects.
    /// 
    /// Terrain:
    /// TerrainComponent (BlendMode.NonPremultiplied)
    /// Draw grass. Shared component, but slightly different draw modes.
    /// 
    /// Floors:
    /// 3DFloorGeometry (BlendMode.Opaque (normal), BlendMode.NonPremultiplied (water))
    /// Drawing is typically handled by TerrainComponent. Geometry updates should be partial - changes will be handled by BlueprintChanges and only update
    /// floor geometry groups that have experienced a change (add/subtract)
    /// 
    /// Walls:
    /// WallComponent, WallComponentRC (BlendMode.Opaque)
    /// In 3D, these are drawn via prebuilt geometry groups. (grouped by texture, split into a grid)
    /// In 2D, these are drawn via prebuilt sprite lists. (grouped by texture, split into a grid)
    /// Both modes use a spatial update system, handled by BlueprintChanges. 
    /// The current floor is split into a grid, while other floors (and surrounds) are still kept whole. 
    /// (closest to divisible by 10, if prime or too far then default to 10. default tso is 77x77, so has 7x7 grid of size 11)
    /// 
    /// Roofs:
    /// RoofComponent
    /// This component is due some refactoring to improve performance of the re-generation step. For now, it is only regenerated when it is seen,
    /// and when any floor is changed beneath it.
    /// Likely the best option would be to use a rectangular dirty map similar to the walls, but also include floor changes. Any roofs touching the
    /// union of the dirty rectangles are also combined with it, then the scan and re-roof are done over that region.
    /// 
    /// === (CO-)DEPENDANCES BETWEEN ARCHITECTURE LAYERS ===
    /// 
    /// Walls depend on Floors: for lighting separation between floors
    /// When a floor is added, the walls around it may start or stop receiving light from the floor above or below.
    /// This one is easy - since walls are dirtied spatially, simply include floor changes in the wall spatial dirty region. (for the relevant floors)
    /// 
    /// Floors depend on walls: Diagonal floors are stored in wall tiles
    /// Needs smarter handling of "dirtying" the wall tile - eg. detect the diag floor tile has changed and send the command to the floor change list rather than the wall one
    /// 
    /// Roofs depend on floors and walls: Roofs only appear on top of indoors rooms, do not appear on floor tiles.
    /// Admittedly hard to solve. We can use some spatial tricks to avoid updating all tiles and rectangles, propagating the dirty region through existing roofs.
    /// 
    /// === OPTIONAL DRAW MODES ===
    /// 
    /// Surround:
    /// Same as above.
    /// 
    /// Potential Future Option for less object/wall z-fighting through upper floors (2D):
    /// Draw only one floor at a time. This could be done with a slight depth bias to make sure we cover walls and objects on the previous floor.
    /// (assumes depth bias can be applied during depth compare, but NOT for depth write. otherwise we'd have to bias each floor.)
    /// 
    /// (part of lotview 2.0)
    /// </summary>
    public class WorldArchitecture
    {
        public Blueprint Blueprint;
        public WorldArchitecture(Blueprint blueprint)
        {
            Blueprint = blueprint;
        }

        public void DrawSub(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            state.PrepareCulling(pxOffset);
            var subs = Blueprint.SubWorlds.Where(sub => sub.DoDraw(state)).ToList();
            foreach (var sub in subs)
            {
                sub.SubDraw(gd, state, (pxOffsetSub) =>
                {
                    sub.Architecture.StaticDraw(gd, state, pxOffsetSub);
                    sub.Entities.StaticDraw(gd, state, pxOffsetSub);
                });
            }
        }

        public void StaticDraw(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            var _2d = state._2D;
            var changes = Blueprint.Changes;
            //draw wall and floors to static buffer
            DrawFloorBuf(gd, state);
            DrawWallBuf(gd, state, pxOffset);
            DrawSub(gd, state, pxOffset);
            //if (false)
            //{
            //foreach (var sub in Blueprint.SubWorlds) sub.SubDraw(gd, state, (pxOffsetSub) => { sub.Architecture.StaticDraw(gd, state, pxOffsetSub); });
            //}
        }

        public void Draw2D(GraphicsDevice gd, WorldState state)
        {
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            var changes = Blueprint.Changes;
            if (changes.DrawImmediate)
            {
                DrawFloorBuf(gd, state);
                DrawWallBuf(gd, state, pxOffset);
                DrawSub(gd, state, pxOffset);
                //foreach (var sub in Blueprint.SubWorlds) sub.SubDraw(gd, state, (pxOffsetSub) => { sub.Architecture.StaticDraw(gd, state, pxOffsetSub); });
            }

            if (state.CameraMode > CameraRenderMode._2D) { 
                var effect = WorldContent.RCObject;
                gd.BlendState = BlendState.NonPremultiplied;
                var view = state.View;
                var vp = view * state.Projection;
                effect.ViewProjection = vp;

                if (Blueprint.WCRC != null) Blueprint.WCRC.Draw(gd, state);
                if (state.DrawRoofs) Blueprint.RoofComp.Draw(gd, state);
            }
        }

        private void DrawFloorBuf(GraphicsDevice gd, WorldState state)
        {
            if (Blueprint.Terrain != null)
            {
                Blueprint.Terrain.DepthMode = state._2D.OutputDepth;
                Blueprint.Terrain.Draw(gd, state);
            }
            //foreach (var sub in Blueprint.SubWorlds) sub.DrawArch(gd, state);
        }

        private void DrawWallBuf(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            if (Blueprint.WallCache2D == null || state.CameraMode > CameraRenderMode._2D) return;
            var _2d = state._2D;
            _2d.SetScroll(pxOffset);
            _2d.RenderCache(Blueprint.WallCache2D);
            _2d.Pause();
            _2d.Resume();
        }
    }
}
