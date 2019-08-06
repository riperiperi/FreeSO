using FSO.Common.Utils;
using FSO.LotView.Components;
using FSO.LotView.Effects;
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

        public WorldEntities(Blueprint blueprint)
        {
            Blueprint = blueprint;
        }

        private void ClearDrawBuffer(List<_2DDrawBuffer> buf)
        {
            foreach (var b in buf) b.Dispose();
            buf.Clear();
        }

        public void StaticDraw(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            var changes = Blueprint.Changes;
            //state.PrepareLighting();
            state.PrepareCulling(pxOffset);

            var effect = WorldContent.RCObject;
            gd.BlendState = BlendState.NonPremultiplied;
            effect.ViewProjection = state.ViewProjection;

            effect.SetTechnique(RCObjectTechniques.Draw);

            DrawObjBuf(gd, state, pxOffset);
            //if (false)
            //{
                //foreach (var sub in Blueprint.SubWorlds) sub.SubDraw(gd, state, (pxOffsetSub) => sub.Entities.StaticDraw(gd, state, pxOffsetSub));
            //}
        }

        public void DrawAvatars(GraphicsDevice gd, WorldState state)
        {
            gd.DepthStencilState = DepthStencilState.Default;
            gd.BlendState = BlendState.AlphaBlend;
            gd.RasterizerState = RasterizerState.CullCounterClockwise;
            
            var advDir = (WorldConfig.Current.Directional && WorldConfig.Current.AdvancedLighting);
            var pass = advDir ? 5 : WorldConfig.Current.PassOffset * 2;

            var effect = WorldContent.AvatarEffect;
            effect.CurrentTechnique = WorldContent.AvatarEffect.Techniques[pass];

            effect.Parameters["View"].SetValue(state.View);
            effect.Parameters["Projection"].SetValue(state.Projection);

            var _2d = state._2D;
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            var tileOffset = state.CenterTile;

            _2d.SetScroll(pxOffset);
            _2d.OffsetPixel(new Vector2());
            _2d.OffsetTile(new Vector3());
            _2d.PrepareImmediate(Effects.WorldBatchTechniques.drawZSpriteDepthChannel);

            foreach (var avatar in Blueprint.Avatars)
            {
                if (avatar.Level <= state.Level) avatar.Draw(gd, state);
            }
            
            gd.RasterizerState = RasterizerState.CullNone;
        }

        public void Draw(GraphicsDevice gd, WorldState state)
        {
            var changes = Blueprint.Changes;
            var _2d = state._2D;

            var effect = WorldContent.RCObject;
            gd.BlendState = BlendState.NonPremultiplied;
            effect.ViewProjection = state.ViewProjection;
            gd.RasterizerState = RasterizerState.CullNone;

            effect.SetTechnique(RCObjectTechniques.Draw);

            //Draw dynamic objects.
            
            _2d.OffsetPixel(new Vector2());
            _2d.OffsetTile(new Vector3());
            _2d.PrepareImmediate(Effects.WorldBatchTechniques.drawZSpriteDepthChannel);

            //if we're not using static, draw all the objects here instead
            //TODO: in-place re-order the dynamic objects list to shorten sort time? might not matter for lists this short, and would make it harder to use a hashset
            IEnumerable<ObjectComponent> dyn;
            if (changes.DrawImmediate) dyn = Blueprint.Objects;
            else dyn = changes.DynamicObjects;

            gd.BlendState = BlendState.NonPremultiplied;
            dyn = dyn.Where(x => (x.Level <= state.Level) && x.DoDraw(state));
            if (state.CameraMode == CameraRenderMode._3D) //only use for full 3d - the draw order for 2d rotation is a completely different coordinate space.
            {
                foreach (var obj in dyn) obj.UpdateDrawOrder(state);
            }
            dyn = dyn.OrderBy(x => x.DrawOrder);

            gd.BlendState = BlendState.NonPremultiplied;
            foreach (var obj in dyn)
            {
                obj.Draw(gd, state);
            }

            _2d.EndImmediate();

            //object particles are always dynamic
            foreach (var op in Blueprint.ObjectParticles)
            {
                if (op.Level <= state.Level && op.Owner.Visible && (op.Owner.Position.X > -2043 || op.Owner.Position.Y > -2043))
                    op.Draw(gd, state);
            }

            //foreach (var sub in Blueprint.SubWorlds) sub.SubDraw(gd, state, (pxOffsetSub) => sub.Entities.StaticDraw(gd, state, pxOffsetSub));
        }

        private void DrawObjBuf(GraphicsDevice gd, WorldState state, Vector2 pxOffset)
        {
            var _2d = state._2D;

            //foreach (var sub in Blueprint.SubWorlds) sub.DrawObjects(gd, state);

            _2d.SetScroll(pxOffset);
            _2d.OffsetPixel(new Vector2());
            _2d.OffsetTile(new Vector3());
            _2d.PrepareImmediate(Effects.WorldBatchTechniques.drawZSpriteDepthChannel);
            
            var size = new Vector2(state._2D.LastWidth, state._2D.LastHeight);
            var mainBd = state.WorldSpace.GetScreenFromTile(state.CenterTile);
            var diff = pxOffset - mainBd;
            state.WorldRectangle = new Rectangle((pxOffset).ToPoint(), size.ToPoint());
            state.PrepareCulling(pxOffset);

            IEnumerable<ObjectComponent> staticObj;
            if (Blueprint.Changes.Subworld) staticObj = Blueprint.Objects;
            else staticObj = Blueprint.Changes.StaticObjects;
            staticObj = staticObj.Where(x => (x.Level <= state.Level) && x.DoDraw(state)).OrderBy(x => x.DrawOrder);

            foreach (var obj in staticObj)
            {
                obj.Draw(gd, state);
            }

            _2d.EndImmediate();
        }
    }
}
