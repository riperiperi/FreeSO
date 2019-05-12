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

namespace FSO.LotView
{
    /// <summary>
    /// Draws entities in the world. Drawn after all architecture, before semitransparent architecture and particles.
    /// 
    /// - Objects
    ///   - STATIC: a list of objects that have not changed in a while. drawn to
    ///   - DYNAMIC: a list of objects that have changed recently. Drawn on top of the static buffer
    ///   - 3D: There are no dynamic and static layers in this mode - everything on camera is drawn every frame.
    /// - Avatars
    ///   - 2D: Drawn before all objects. Semitransparent avatars (ghosts) are drawn after all objects.
    ///   - 3D: Z-sorted with objects.
    /// - Particles:
    ///   - 2D: Drawn with the object in dynamic layer.
    ///   - 3D: Drawn somehow
    /// 
    /// (part of lotview 2.0)
    /// </summary>
    public class WorldEntities
    {
        public bool UseStaticBuffer;
        public Blueprint Blueprint;

        //2d rendering mode
        private List<_2DDrawBuffer> StaticObjectsCache = new List<_2DDrawBuffer>();

        private void ClearDrawBuffer(List<_2DDrawBuffer> buf)
        {
            foreach (var b in buf) b.Dispose();
            buf.Clear();
        }

        public void Predraw2D(GraphicsDevice gd, WorldState state)
        {
            var changes = Blueprint.Changes;
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            var _2d = state._2D;

            //scroll buffer loads in increments of SCROLL_BUFFER
            var newOff = changes.StaticSurface.GetScrollIncrement(pxOffset, state);
            var oldCenter = state.CenterTile;
            state.CenterTile += state.WorldSpace.GetTileFromScreen(newOff - pxOffset); //offset the scroll to the position of the scroll buffer.
            var tileOffset = state.CenterTile;

            pxOffset = newOff;

            if (!changes.DrawImmediate)
            {
                state.PrepareLighting();

                if (changes.StaticSurfaceDirty)
                {
                    /** Draw static objects to a texture **/
                    Promise<Texture2D> bufferTexture = null;
                    Promise<Texture2D> depthTexture = null;
                    using (var buffer = state._2D.WithBuffer(BUFFER_STATIC, ref bufferTexture, BUFFER_STATIC_DEPTH, ref depthTexture))
                    {

                        while (buffer.NextPass())
                        {
                            //in the old world, floors and walls were drawn onto the same static buffer.
                            //we might want to do the same, but it doesn't play too well with our current behavior.
                            //DrawFloorBuf(gd, state, pxOffset);
                            //DrawWallBuf(gd, state, pxOffset);
                            DrawObjBuf(gd, state, pxOffset);
                        }
                    }
                    Static = new ScrollBuffer(bufferTexture.Get(), depthTexture.Get(), newOff, new Vector3(tileOffset, 0));
                }
            }
            //state._2D.PreciseZoom = state.PreciseZoom;
            state.CenterTile = oldCenter; //revert to our real scroll position

            state.ThisFrameImmediate = drawImmediate;
        }

        public void Draw2D(GraphicsDevice gd, WorldState state)
        {
            var changes = Blueprint.Changes;
            var _2d = state._2D;
            /**
             * Draw static objects
             */
            _2d.OffsetPixel(Vector2.Zero);
            _2d.SetScroll(new Vector2());

            var pxOffset = -state.WorldSpace.GetScreenOffset();
            var tileOffset = state.CenterTile;

            if (state.ThisFrameImmediate)
            {
                _2d.SetScroll(pxOffset);
                _2d.Begin(state.Camera);

                var p2O = pxOffset;
                DrawObjBuf(gd, state, p2O);
            }
            else
            {
                _2d.SetScroll(new Vector2());
                _2d.Begin(state.Camera);
                state._2D.PreciseZoom = 1f;
                if (Static != null)
                {
                    _2d.DrawScrollBuffer(Static, pxOffset, new Vector3(tileOffset, 0), state);
                    _2d.Pause();
                    _2d.Resume();
                }
                state._2D.PreciseZoom = state.PreciseZoom;
            }
            _2d.SetScroll(pxOffset);

            _2d.End();

            /**
             * Draw dynamic objects.
             */

            _2d.SetScroll(pxOffset);

            var size = new Vector2(state.WorldSpace.WorldPxWidth, state.WorldSpace.WorldPxHeight);
            var mainBd = state.WorldSpace.GetScreenFromTile(state.CenterTile);
            var diff = pxOffset - mainBd;
            var worldBounds = new Rectangle((pxOffset).ToPoint(), size.ToPoint());
            
            _2d.OffsetPixel(new Vector2());
            _2d.OffsetTile(new Vector3());
            _2d.PrepareImmediate(Effects.WorldBatchTechniques.drawZSpriteDepthChannel);

            var dyn = changes.DynamicObjects.OrderBy(x => x.DrawOrder);

            foreach (var obj in dyn)
            {
                if (obj.Level > state.Level) continue;
                var tilePosition = obj.Position;
                var oPx = state.WorldSpace.GetScreenFromTile(tilePosition);
                obj.ValidateSprite(state);
                var offBound = new Rectangle(obj.Bounding.Location + oPx.ToPoint(), obj.Bounding.Size);
                if (!offBound.Intersects(worldBounds)) continue;
                obj.DrawImmediate(gd, state);
            }

            _2d.EndImmediate();

            foreach (var op in Blueprint.ObjectParticles)
            {
                if (op.Level <= state.Level && op.Owner.Visible && (op.Owner.Position.X > -2043 || op.Owner.Position.Y > -2043))
                    op.Draw(gd, state);
            }
        }

        private void DrawObjBuf(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            var _2d = state._2D;

            //foreach (var sub in Blueprint.SubWorlds) sub.DrawObjects(gd, state);

            var staticObj = Blueprint.Changes.StaticObjects.OrderBy(x => x.DrawOrder);

            _2d.OffsetPixel(new Vector2());
            _2d.OffsetTile(new Vector3());
            _2d.PrepareImmediate(Effects.WorldBatchTechniques.drawZSpriteDepthChannel);

            foreach (var obj in staticObj)
            {
                if (obj.Level > state.Level) continue;
                var tilePosition = obj.Position;
                var oPx = state.WorldSpace.GetScreenFromTile(tilePosition);
                obj.ValidateSprite(state);
                //var offBound = new Rectangle(obj.Bounding.Location + oPx.ToPoint(), obj.Bounding.Size);
                //if (!offBound.Intersects(worldBounds)) continue;
                obj.DrawImmediate(gd, state);
            }

            _2d.EndImmediate();
        }
    }
}
