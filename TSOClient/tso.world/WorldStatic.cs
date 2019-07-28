using FSO.Common.Utils;
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
    /// Handling for the 2D Static layer.
    /// </summary>
    public class WorldStatic
    {
        public static readonly int NUM_2D_BUFFERS = 6;
        public static readonly int BUFFER_STATIC = 0;
        public static readonly int BUFFER_STATIC_DEPTH = 1;
        public static readonly int BUFFER_OBJID = 2;
        public static readonly int BUFFER_THUMB = 3; //used for drawing thumbnails
        public static readonly int BUFFER_THUMB_DEPTH = 4; //used for drawing thumbnails
        public static readonly int BUFFER_LOTTHUMB = 5;
        
        public static readonly int SCROLL_BUFFER = 512; //resolution to add to render size for scroll reasons

        public ScrollBuffer StaticSurface;
        public World World;

        public WorldStatic(World world)
        {
            World = world;
        }

        private Vector2 GetScrollIncrement(Vector2 pxOffset, WorldState state)
        {
            var scrollSize = SCROLL_BUFFER / state.PreciseZoom;
            return new Vector2((float)Math.Floor(pxOffset.X / scrollSize) * scrollSize, (float)Math.Floor(pxOffset.Y / scrollSize) * scrollSize);
        }

        public void PreDraw(GraphicsDevice gd, WorldState state)
        {
            var changes = state.Changes;
            if (changes.DrawImmediate) return;
            if (changes.StaticSurfaceDirty)
            {
                var pxOffset = -state.WorldSpace.GetScreenOffset();
                var newOff = GetScrollIncrement(pxOffset, state);
                var oldCenter = state.CenterTile;
                state.CenterTile += state.WorldSpace.GetTileFromScreen(newOff - pxOffset); //offset the scroll to the position of the scroll buffer.
                var tileOffset = state.CenterTile;

                /** Draw static objects to a texture **/
                Promise<Texture2D> bufferTexture = null;
                Promise<Texture2D> depthTexture = null;
                using (var buffer = state._2D.WithBuffer(BUFFER_STATIC, ref bufferTexture, BUFFER_STATIC_DEPTH, ref depthTexture))
                {
                    while (buffer.NextPass())
                    {
                        World.Architecture.StaticDraw(gd, state, newOff);
                        World.Entities.StaticDraw(gd, state, newOff);
                    }
                }
                StaticSurface = new ScrollBuffer(bufferTexture.Get(), depthTexture.Get(), newOff, new Vector3(tileOffset, 0));
                changes.StaticSurfaceDirty = false; //static surface has been updated!
                state.CenterTile = oldCenter;
            }
            changes.StaticSurface = StaticSurface; //copy so changes can keep track of when we leave this buffer range
        }

        public void Draw(WorldState state)
        {
            var changes = state.Changes;
            if (changes.DrawImmediate) return;
            var _2d = state._2D;
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            var tileOffset = state.CenterTile;

            _2d.OffsetPixel(Vector2.Zero);
            _2d.SetScroll(new Vector2());
            _2d.Begin(state.Camera);
            state._2D.PreciseZoom = 1f;
            if (StaticSurface != null)
            {
                _2d.DrawScrollBuffer(StaticSurface, pxOffset, new Vector3(tileOffset, 0), state);
                _2d.Pause();
                _2d.Resume();
            }
            state._2D.PreciseZoom = state.PreciseZoom;
        }
    }
}
