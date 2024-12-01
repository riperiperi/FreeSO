using System;
using System.Collections.Generic;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Platform
{
    public class WorldPlatformNull : IWorldPlatform
    {
        private Blueprint bp;
        private List<_2DDrawBuffer> StaticWallCache = new List<_2DDrawBuffer>();
        public WorldPlatformNull(Blueprint bp)
        {
            this.bp = bp;
        }

        public void Dispose()
        {
        }

        public void SwapBlueprint(Blueprint bp)
        {
            this.bp = bp;
        }

        public Texture2D GetLotThumb(GraphicsDevice gd, WorldState state, Action<Texture2D> rooflessCallback)
        {
            return null;
        }

        public short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd, WorldState state)
        {
            return 0;
        }

        public Texture2D GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd, WorldState state)
        {
            return null;
        }

        public void ClearDrawBuffer(List<_2DDrawBuffer> buf)
        {
            foreach (var b in buf) b.Dispose();
            buf.Clear();
        }

        public void RecacheWalls(GraphicsDevice gd, WorldState state, bool cutawayOnly)
        {
            //in 2d, if we have 3d wall shadows enabled we also have to update the 3d wall geometry
            bp.WCRC?.Generate(gd, state, cutawayOnly);

            var _2d = state._2D;
            _2d.Pause();
            _2d.Begin(state.Camera2D); //clear the sprite buffer before we begin drawing what we're going to cache
            bp.WallComp.Draw(gd, state);
            ClearDrawBuffer(bp.WallCache2D);
            state.PrepareLighting();
            _2d.End(bp.WallCache2D, true);
        }
    }
}
