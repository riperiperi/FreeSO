using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Utils;
using FSO.LotView.Components;
using FSO.LotView.Effects;
using FSO.LotView.Model;
using FSO.LotView.RC;
using FSO.LotView.Utils;
using FSO.LotView.Utils.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Platform
{
    public class WorldPlatform3D : IWorldPlatform
    {
        public Blueprint bp;
        public RenderTarget2D LotThumbTarget;
        public RenderTarget2D ObjThumbTarget;

        public WorldPlatform3D(Blueprint bp)
        {
            this.bp = bp;
        }

        public void Dispose()
        {
            LotThumbTarget?.Dispose();
            ObjThumbTarget?.Dispose();
        }

        public Texture2D GetLotThumb(GraphicsDevice gd, WorldState state, Action<Texture2D> rooflessCallback)
        {
            var oldZoom = state.Zoom;
            var oldRotation = state.Rotation;
            var oldLevel = state.Level;
            var oldCutaway = bp.Cutaway;
            //TODO: switch to 2D cam
            var lastCamera = (state.CameraMode == CameraRenderMode._3D) ? CameraControllerType._3D : CameraControllerType._2D;
            state.ForceCamera(CameraControllerType._2D);
            state.RenderingThumbnail = true;

            var wCam = state.Camera2D;
            var oldViewDimensions = wCam.ViewDimensions;
            //wCam.ViewDimensions = new Vector2(-1, -1);
            var oldPreciseZoom = state.PreciseZoom;

            //full invalidation because we must recalculate all object sprites. slow but necessary!
            state.Zoom = WorldZoom.Far;
            state.Rotation = WorldRotation.TopLeft;
            state.Level = bp.Stories;
            var ts1 = Content.Content.Get().TS1;
            state.PreciseZoom = ts1 ? (1 / 2f) : (1 / 4f);
            var size = ts1 ? (bp.Width * 16) : (576);
            state._2D.PreciseZoom = state.PreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();

            var oldCenter = state.CenterTile;
            state.CenterTile = bp.GetThumbCenterTile(state);
            state.CenterTile -= state.WorldSpace.GetTileFromScreen(new Vector2((size - state.WorldSpace.WorldPxWidth) / state.PreciseZoom, (size - state.WorldSpace.WorldPxHeight) / state.PreciseZoom) / 2);
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            bp.Cutaway = new bool[bp.Cutaway.Length];


            state.ClearLighting(false);
            LotThumbTarget = null;
            if (LotThumbTarget == null)
                LotThumbTarget = new RenderTarget2D(gd, size, size, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            var lastLight = state.OutsideColor;
            state.OutsideColor = Color.White;
            state._2D.OBJIDMode = false;

            gd.SetRenderTarget(LotThumbTarget);
            gd.Clear(Color.Transparent);

            state._2D.ResetMatrices(size, size);
            state.PrepareCamera();

            if (bp.FineArea != null) bp.FloorGeom.BuildableReset(gd, bp.FineArea);
            else bp.FloorGeom.SliceReset(gd, new Rectangle(6, 6, bp.Width - 13, bp.Height - 13));
            Blueprint.SetLightColor(WorldContent.GrassEffect, Color.White, Color.White * 0.75f);
            Blueprint.SetLightColor(WorldContent.RCObject, Color.White, Color.White * 0.75f);
            var build = state.BuildMode;
            state.SilentBuildMode = 0;
            bp.Terrain.Draw(gd, state);
            bp.Terrain.DrawMask(gd, state, state.View, state.Projection);
            state.SilentBuildMode = build;

            var effect = WorldContent.RCObject;
            gd.BlendState = BlendState.NonPremultiplied;
            var view = state.View;
            var vp = view * state.Projection;
            effect.ViewProjection = vp;

            var cuts = bp.Cutaway;
            bp.Cutaway = new bool[cuts.Length];
            bp.WCRC?.Generate(gd, state, false);
            bp.WCRC?.Draw(gd, state);
            bp.Cutaway = cuts;
            bp.WCRC?.Generate(gd, state, false);

            gd.BlendState = BlendState.NonPremultiplied;
            gd.RasterizerState = RasterizerState.CullNone;

            effect.SetTechnique(RCObjectTechniques.Draw);
            var frustrum = new BoundingFrustum(vp);
            /*
            var objs = bp.Objects.OrderBy(x => x.UpdateDrawOrder);
            */
            var fine = bp.FineArea;
            foreach (var obj in bp.Objects)
            {
                if (fine != null && (
                    obj.Position.X < 0 ||
                    obj.Position.X >= bp.Width ||
                    obj.Position.Y < 0 ||
                    obj.Position.Y >= bp.Width || !fine[(int)obj.Position.X + bp.Width * (int)obj.Position.Y])) continue;
                var lastMode = obj.Mode;
                obj.Mode = ComponentRenderMode._3D;
                obj.Draw(gd, state);
                obj.Mode = lastMode;
            }
            rooflessCallback?.Invoke(LotThumbTarget);
            bp.RoofComp.Draw(gd, state);

            gd.SetRenderTarget(null);

            bp.Changes.SetFlag(BlueprintGlobalChanges.LIGHTING_CHANGED);
            bp.Changes.SetFlag(BlueprintGlobalChanges.FLOOR_CHANGED);
            //return things to normal
            //state.PrepareLighting();
            state.OutsideColor = lastLight;
            state.PreciseZoom = oldPreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();
            wCam.ViewDimensions = oldViewDimensions;
            state.CenterTile = oldCenter;

            state.Zoom = oldZoom;
            state.Rotation = oldRotation;
            state.Level = oldLevel;
            bp.Cutaway = oldCutaway;
            state.RenderingThumbnail = false;

            state.ForceCamera(lastCamera);

            var tex = LotThumbTarget;
            return tex; //TextureUtils.Clip(gd, tex, bounds);
        }

        public short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd, WorldState state)
        {
            //var sPos = new Vector3(((float)x / state.WorldSpace.WorldPxWidth) * 2 - 1, 1 - ((float)y / state.WorldSpace.WorldPxHeight) * 2, 0);
            var sPos = new Vector3(x, y, 0);
            var p1 = gd.Viewport.Unproject(sPos, state.Projection, state.View, Matrix.Identity);
            sPos.Z = 1;
            var p2 = gd.Viewport.Unproject(sPos, state.Projection, state.View, Matrix.Identity);
            var dir = p2 - p1;
            dir.Normalize();
            var ray = new Ray(p1, p2 - p1);
            ray.Direction.Normalize();
            short bestObj = 0;
            float bestDistance = float.MaxValue;
            foreach (var obj in bp.Objects)
            {
                if (obj.Level != state.Level || !obj.Visible || obj.CutawayHidden) continue;
                var intr = obj.IntersectsBounds(ray);
                if (obj.Container != null && intr != null) intr = intr.Value - 1.5f;
                if (intr != null && intr.Value < bestDistance)
                {
                    bestObj = obj.ObjectID;
                    bestDistance = intr.Value;
                }
            }

            foreach (var sim in bp.Avatars)
            {
                if (!sim.Visible) continue;
                var pos = sim.GetPelvisPosition() * 3;
                pos = new Vector3(pos.X, pos.Z, pos.Y);
                var box = new BoundingBox(pos - new Vector3(0.5f, 2, 0.5f), pos + new Vector3(0.5f, 2, 0.5f));
                var intr = box.Intersects(ray);
                if (intr != null) intr = intr.Value - 1.5f;
                if (intr != null && intr.Value < bestDistance)
                {
                    bestObj = sim.ObjectID;
                    bestDistance = intr.Value;
                }
            }
            return bestObj;
        }

        public Texture2D GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd, WorldState state)
        {
            var cam = new WorldCamera3D(gd, Vector3.Zero, Vector3.Zero, Vector3.Up);// WorldCamera3D)state.Camera;
            var oldVp = state.ViewProjection;

            /** Center average position **/
            Vector3 average = new Vector3();
            for (int i = 0; i < positions.Length; i++)
            {
                average += positions[i];
            }
            average /= positions.Length;

            cam.ProjectionOrigin = new Vector2(512, 512);
            cam.Target = average + new Vector3(0.5f, 0.5f, 0) * 3f;
            cam.Position = cam.Target + new Vector3(-9, 6, -9);

            state.DrawOOB = true;

            var _2d = state._2D;

            if (ObjThumbTarget == null)
                ObjThumbTarget = new RenderTarget2D(gd, 1024, 1024, true, SurfaceFormat.Color, DepthFormat.Depth24);

            gd.SetRenderTarget(ObjThumbTarget);
            var cpoints = new List<Vector3>();
            var view = state.View;
            var vp = view * state.Projection;
            gd.BlendState = BlendState.NonPremultiplied;
            gd.RasterizerState = RasterizerState.CullNone;
            gd.DepthStencilState = DepthStencilState.Default;
            var effect = WorldContent.RCObject;
            effect.ViewProjection = vp;
            state.ViewProjection = vp;
            effect.SetTechnique(RCObjectTechniques.Draw);
            state.ClearLighting(false);
            Blueprint.SetLightColor(WorldContent.RCObject, Color.White, Color.White);

            var objs = objects.OrderBy(x => { x.UpdateDrawOrder(state); return x.DrawOrder; }).ToList();

            gd.Clear(Color.Transparent);
            for (int i = 0; i < objs.Count; i++)
            {
                var obj = objs[i];
                var tilePosition = positions[Array.IndexOf(objects, obj)];

                //we need to trick the object into believing it is in a set world state.
                var oldObjRot = obj.Direction;
                var oldObjPos = obj.UnmoddedPosition;
                var oldRoom = obj.Room;

                obj.Direction = Direction.NORTH;
                obj.Room = 65535;
                obj.OnRotationChanged(state);
                obj.OnZoomChanged(state);
                obj.Position = tilePosition;
                obj.Draw(gd, state);

                var mat = obj.World * vp;
                cpoints.AddRange(obj.GetBounds().GetCorners().Select(x =>
                {
                    var proj = Vector3.Transform(x, vp);
                    proj.X /= proj.Z;
                    proj.Y /= -proj.Z;
                    proj.X += 1f;
                    proj.X *= 512;
                    proj.Y += 1f;
                    proj.Y *= 512;
                    return proj;
                }));

                //return everything to normal
                obj.Direction = oldObjRot;
                obj.Room = oldRoom;
                obj.UnmoddedPosition = oldObjPos;
                obj.OnRotationChanged(state);
                obj.OnZoomChanged(state);
            }
            gd.SetRenderTarget(null);
            var bounds3d = (cpoints.Count > 0) ? BoundingBox.CreateFromPoints(cpoints) : new BoundingBox();
            var bounds = new Rectangle((int)bounds3d.Min.X, (int)bounds3d.Min.Y, (int)(bounds3d.Max.X - bounds3d.Min.X), (int)(bounds3d.Max.Y - bounds3d.Min.Y));

            bounds.Inflate(1, 1);
            bounds.X = Math.Max(0, Math.Min(1023, bounds.X));
            bounds.Y = Math.Max(0, Math.Min(1023, bounds.Y));
            if (bounds.Width + bounds.X > 1024) bounds.Width = 1024 - bounds.X;
            if (bounds.Height + bounds.Y > 1024) bounds.Height = 1024 - bounds.Y;

            bp.Changes.SetFlag(BlueprintGlobalChanges.LIGHTING_CHANGED);

            //return things to normal
            state.DrawOOB = false;

            state.ViewProjection = oldVp;

            gd.DepthStencilState = DepthStencilState.None;
            var clip = TextureUtils.Clip(gd, ObjThumbTarget, bounds);
            var dec = TextureUtils.Decimate(clip, gd, 3, true);
            return dec;
        }

        public void RecacheWalls(GraphicsDevice gd, WorldState state, bool cutawayOnly)
        {
            bp.WCRC?.Generate(gd, state, cutawayOnly);
        }
    }
}
