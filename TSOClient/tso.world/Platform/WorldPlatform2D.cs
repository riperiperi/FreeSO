using FSO.Common.Utils;
using FSO.LotView.Components;
using FSO.LotView.Effects;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace FSO.LotView.Platform
{
    public class WorldPlatform2D : IWorldPlatform
    {
        public Blueprint bp;
        private List<_2DDrawBuffer> StaticWallCache = new List<_2DDrawBuffer>();
        private RenderTarget2D AvatarThumbTarget;

        public WorldPlatform2D(Blueprint bp)
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
            //if (!(state.Camera is WorldCamera)) return new Texture2D(gd, 8, 8);
            var oldZoom = state.Zoom;
            var oldRotation = state.Rotation;
            var oldLevel = state.Level;
            var oldCutaway = bp.Cutaway;
            var wCam = state.Camera2D;
            var oldViewDimensions = wCam.ViewDimensions;
            var oldPreciseZoom = state.PreciseZoom;
            var oldCenter = state.CenterTile;
            state.ForceCamera(Utils.Camera.CameraControllerType._2D);

            //full invalidation because we must recalculate all object sprites. slow but necessary!
            state.RenderingThumbnail = true;
            state.Zoom = WorldZoom.Far;
            state.Rotation = WorldRotation.TopLeft;
            state.Level = bp.Stories;
            var ts1 = Content.Content.Get().TS1;
            state.PreciseZoom = ts1 ? (1 / 2f) : (1 / 4f);
            var size = ts1 ? (bp.Width * 16) : (576);
            state._2D.PreciseZoom = state.PreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();

            state._2D.ResizeBuffer(_2DWorldBatch.BUFFER_LOTTHUMB, size, size);

            state.CenterTile = bp.GetThumbCenterTile(state);
            state.CenterTile -= state.WorldSpace.GetTileFromScreen(new Vector2((size - state.WorldSpace.WorldPxWidth) / state.PreciseZoom, (size - state.WorldSpace.WorldPxHeight) / state.PreciseZoom) / 2);
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            bp.Cutaway = new bool[bp.Cutaway.Length];

            var _2d = state._2D;
            state.ClearLighting(false);
            Promise<Texture2D> bufferTexture = null;
            var lastLight = state.OutsideColor;
            state.OutsideColor = Color.White;
            state._2D.OBJIDMode = false;
            state.PrepareCamera();
            using (var buffer = state._2D.WithBuffer(_2DWorldBatch.BUFFER_LOTTHUMB, ref bufferTexture))
            {
                _2d.SetScroll(pxOffset);
                while (buffer.NextPass())
                {
                    _2d.Pause();
                    _2d.Resume();
                    if (bp.FineArea != null) bp.FloorGeom.BuildableReset(gd, bp.FineArea);
                    else bp.FloorGeom.SliceReset(gd, new Rectangle(6, 6, bp.Width - 13, bp.Height - 13));
                    //Blueprint.SetLightColor(WorldContent.GrassEffect, Color.White, Color.White);
                    var build = state.SilentBuildMode;
                    state.SilentBuildMode = 0;
                    bp.Terrain.Draw(gd, state);
                    bp.Terrain.DrawMask(gd, state, state.View, state.Projection);
                    state.SilentBuildMode = build;
                    bp.WallComp.Draw(gd, state);
                    _2d.Pause();
                    _2d.Resume();
                    _2d.PrepareImmediate(Effects.WorldBatchTechniques.drawZSpriteDepthChannel);
                    foreach (var obj in bp.Objects)
                    {
                        var tilePosition = obj.Position;
                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition));
                        _2d.OffsetTile(tilePosition);
                        obj.Draw(gd, state);
                    }
                    _2d.Pause();
                    _2d.Resume();
                    rooflessCallback?.Invoke(gd.GetRenderTargets()[0].RenderTarget as RenderTarget2D);
                    bp.RoofComp.Draw(gd, state);
                }

            }

            bp.Changes.SetFlag(BlueprintGlobalChanges.LIGHTING_CHANGED);
            bp.Changes.SetFlag(BlueprintGlobalChanges.FLOOR_CHANGED);
            //return things to normal
            //state.PrepareLighting();
            state.OutsideColor = lastLight;
            state.PreciseZoom = oldPreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();
            wCam.ViewDimensions = oldViewDimensions;
            
            state.Zoom = oldZoom;
            state.Rotation = oldRotation;
            state.Level = oldLevel;
            state.CenterTile = oldCenter; //must be restored after rotation.
            state.RenderingThumbnail = false;
            bp.Cutaway = oldCutaway;

            var tex = bufferTexture.Get();
            return tex;
        }

        public short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd, WorldState state)
        {
            var ray = state.CameraRayAtScreenPos(new Vector2(x, y));

            /** Draw all objects to a texture as their IDs **/
            var oldCenter = state.CenterTile;
            var tileOff = state.WorldSpace.GetTileFromScreen(new Vector2(x, y));
            state.CenterTile += tileOff;
            var pxOffset = state.WorldSpace.GetScreenOffset();
            var _2d = state._2D;
            Promise<Texture2D> bufferTexture = null;

            var oldDS = gd.DepthStencilState;
            gd.DepthStencilState = DepthStencilState.Default;

            state.WorldRectangle = new Rectangle((-pxOffset).ToPoint(), new Point(1, 1));

            short specialResult = 0;
            
            state._2D.OBJIDMode = true;
            using (var buffer = _2d.WithBuffer(_2DWorldBatch.BUFFER_OBJID, ref bufferTexture))
            {
                _2d.SetScroll(-pxOffset);

                while (buffer.NextPass())
                {
                    _2d.PrepareImmediate(Effects.WorldBatchTechniques.drawZSpriteOBJID);
                    foreach (var obj in bp.Objects)
                    {
                        var tilePosition = obj.Position;
                        if (obj.Level > state.Level || !obj.DoDraw(state)) continue;
                        obj.Draw(gd, state);
                    }
                    _2d.EndImmediate();

                    //state._3D.Begin(gd);
                    var effect = WorldContent.AvatarEffect;
                    effect.CurrentTechnique = WorldContent.AvatarEffect.Techniques[1];
                    effect.Parameters["View"].SetValue(state.View);
                    effect.Parameters["Projection"].SetValue(state.Projection);

                    foreach (var avatar in bp.Avatars)
                    {
                        if (avatar.Level > state.Level) continue;
                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(avatar.Position));
                        _2d.OffsetTile(avatar.Position);
                        avatar.Draw(gd, state);

                        if (avatar.Visible && avatar.MyMario != null)
                        {
                            var pos = avatar.GetPelvisPosition() * 3;
                            pos = new Vector3(pos.X, pos.Z, pos.Y);
                            var box = new BoundingBox(pos - new Vector3(0.5f, 2, 0.5f), pos + new Vector3(0.5f, 2, 0.5f));
                            var intr = box.Intersects(ray);
                            if (intr != null)
                            {
                                specialResult = avatar.ObjectID;
                            }
                        }
                        //state._3D.End();
                    }
                }

            }
            state._2D.OBJIDMode = false;
            state.CenterTile = oldCenter;

            var tex = bufferTexture.Get();
            Color[] data = new Color[1];
            tex.GetData<Color>(data);
            var f = Vector3.Dot(new Vector3(data[0].R / 255.0f, data[0].G / 255.0f, data[0].B / 255.0f), new Vector3(1.0f, 1 / 255.0f, 1 / 65025.0f));

            gd.DepthStencilState = oldDS;

            return specialResult != 0 ? specialResult : (short)Math.Round(f * 65535f);
        }

        public Texture2D GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd, WorldState state)
        {
            var oldZoom = state.Zoom;
            var oldRotation = state.Rotation;
            var oldPreciseZoom = state.PreciseZoom;
            /** Center average position **/
            Vector3 average = new Vector3();
            for (int i = 0; i < positions.Length; i++)
            {
                average += positions[i];
            }
            average /= positions.Length;

            state.ForceCamera(Utils.Camera.CameraControllerType._2D);
            state.RenderingThumbnail = true;
            state.SilentZoom = WorldZoom.Near;
            state.SilentRotation = WorldRotation.BottomRight;
            state.SilentPreciseZoom = 1;
            state._2D.PreciseZoom = state.PreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();
            state.DrawOOB = true;
            var pxOffset = new Vector2(442, 275) - state.WorldSpace.GetScreenFromTile(average);

            var _2d = state._2D;
            Promise<Texture2D> bufferTexture = null;
            Promise<Texture2D> depthTexture = null;
            state._2D.OBJIDMode = false;
            Rectangle? bounds = null;
            state.ClearLighting(false);

            //Blueprint.SetLightColor(WorldContent._2DWorldBatchEffect, Color.White, Color.White);
            //Blueprint.SetLightColor(WorldContent.GrassEffect, Color.White, Color.White);
            //Blueprint.SetLightColor(Vitaboy.Avatar.Effect, Color.White, Color.White);
            var oldDS = gd.DepthStencilState;
            gd.DepthStencilState = DepthStencilState.Default;
            state.PrepareCamera();

            using (var buffer = state._2D.WithBuffer(_2DWorldBatch.BUFFER_THUMB, ref bufferTexture, _2DWorldBatch.BUFFER_THUMB_DEPTH, ref depthTexture))
            {
                _2d.SetScroll(new Vector2());
                while (buffer.NextPass())
                {
                    _2d.PrepareImmediate(Effects.WorldBatchTechniques.drawZSpriteDepthChannel);
                    for (int i = 0; i < objects.Length; i++)
                    {
                        var obj = objects[i];
                        var tilePosition = positions[i];

                        var tileOff = tilePosition - obj.Position;

                        //we need to trick the object into believing it is in a set world state.
                        var oldObjRot = obj.Direction;
                        var oldRoom = obj.Room;

                        obj.Direction = Direction.NORTH;
                        obj.Room = 65535;
                        state.SilentZoom = WorldZoom.Near;
                        state.SilentRotation = WorldRotation.BottomRight;
                        var thumbOffset = state.WorldSpace.GetScreenFromTile(tileOff);
                        _2d.SetShaderOffsets(pxOffset + thumbOffset, WorldSpace.GetWorldFromTile(tileOff)); //offset object into rotated position
                        obj.OnRotationChanged(state);
                        obj.OnZoomChanged(state);

                        var oPx = state.WorldSpace.GetScreenFromTile(tilePosition) + pxOffset;
                        obj.ValidateSprite(state);
                        var offBound = obj.Bounding;
                        if (offBound.Width != 0)
                        {
                            offBound.Offset(pxOffset + thumbOffset);

                            if (offBound.Location.X != int.MaxValue)
                            {
                                if (bounds == null) bounds = offBound;
                                else bounds = Rectangle.Union(offBound, bounds.Value);
                            }
                        }

                        obj.Draw(gd, state);

                        //return everything to normal
                        obj.Direction = oldObjRot;
                        obj.Room = oldRoom;
                        state.SilentZoom = oldZoom;
                        state.SilentRotation = oldRotation;
                        obj.OnRotationChanged(state);
                        obj.OnZoomChanged(state);
                    }
                    _2d.EndImmediate();
                }
            }

            var b = bounds ?? new Rectangle();
            b.Inflate(1, 1);
            //bounds = new Rectangle(0, 0, 1024, 1024);
            b.X = Math.Max(0, Math.Min(1023, b.X));
            b.Y = Math.Max(0, Math.Min(1023, b.Y));
            if (b.Width + b.X > 1024) b.Width = 1024 - b.X;
            if (b.Height + b.Y > 1024) b.Height = 1024 - b.Y;

            //return things to normal
            state.DrawOOB = false;
            state.SilentPreciseZoom = oldPreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();
            state.RenderingThumbnail = false;
            gd.DepthStencilState = oldDS;

            var tex = bufferTexture.Get();
            return TextureUtils.Clip(gd, tex, b);
        }

        public void RecacheWalls(GraphicsDevice gd, WorldState state, bool cutawayOnly)
        {
            //in 2d, if we have 3d wall shadows enabled we also have to update the 3d wall geometry
            if (bp.SM64 != null && !cutawayOnly)
            {
                bp.WCRC?.Generate(gd, state, cutawayOnly, false);
            }
            bp.WCRC?.Generate(gd, state, cutawayOnly);

            var _2d = state._2D;
            _2d.Pause();
            _2d.Resume(); //clear the sprite buffer before we begin drawing what we're going to cache
            bp.WallComp.Draw(gd, state);
            ClearDrawBuffer(bp.WallCache2D);
            state.PrepareLighting();
            _2d.End(bp.WallCache2D, true);
        }

        public Texture2D GetAvatarThumb(AvatarComponent avatarComp, GraphicsDevice gd)
        {
            // Thread-safety contract: must be called on the game thread.
            //
            // We hold lock(Avatar.Bindings) inside DrawGeometry while iterating bindings,
            // and each binding.Texture.Get(device) takes lock(textureRef). On the game
            // thread, AbstractTextureRef.Get does direct Process(); off the game thread
            // it does GameThread.NextUpdate(...).Result — which blocks waiting for the
            // game thread to drain its callback queue. If the game thread is parked
            // waiting for a different lock we hold, that's a hard deadlock (broken only
            // by NextUpdate's 5s timeout). Fail fast on misuse so the bug is obvious.
            if (!GameThread.IsInGameThread())
                throw new InvalidOperationException(
                    "WorldPlatform2D.GetAvatarThumb must be called on the game thread; "
                    + "marshal via GameThread.NextUpdate first.");

            // The avatar mesh is 3D Vitaboy geometry drawn with AvatarEffect regardless of
            // whether the world platform is 2D or 3D. Render two views into a single
            // 400×1000 target: isometric body in the top 400×600 region, front-facing head
            // in the bottom 400×400 region. The server splits the resulting PNG into
            // body.png and head.png. Done as one render target so we only do one SaveAsPng.
            if (avatarComp?.Avatar == null || avatarComp.Avatar.Skeleton == null) return null;

            const int bodyW = 400, bodyH = 600, headSquare = 400;
            const int totalH = bodyH + headSquare; // 1000
            if (AvatarThumbTarget == null)
                AvatarThumbTarget = new RenderTarget2D(gd, bodyW, totalH, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

            var headBone = avatarComp.Avatar.Skeleton.GetBone("HEAD");
            float headY = headBone != null ? headBone.AbsolutePosition.Y : 3f;

            var oldBlend = gd.BlendState;
            var oldDepth = gd.DepthStencilState;
            var oldRaster = gd.RasterizerState;

            gd.SetRenderTarget(AvatarThumbTarget);
            gd.Clear(Color.Transparent);

            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.Default;
            gd.RasterizerState = RasterizerState.CullCounterClockwise;

            var effect = WorldContent.AvatarEffect;
            effect.CurrentTechnique = effect.Techniques[0];
            effect.Parameters["ObjectID"].SetValue(0f);
            effect.Parameters["Level"].SetValue(1f);
            effect.Parameters["AmbientLight"].SetValue(Vector4.One);
            effect.Parameters["World"].SetValue(Matrix.Identity);
            avatarComp.Avatar.LightPositions = null;

            // ---- Body pass: isometric 45° azimuth + 30° elevation, top 400×600 ----
            gd.Viewport = new Viewport(0, 0, bodyW, bodyH);
            {
                float az = MathHelper.PiOver4;
                float el = MathHelper.ToRadians(30f);
                float dist = headY * 2.0f;
                var camTarget = new Vector3(0f, headY * 0.5f, 0f);
                var camPos = camTarget + new Vector3(
                    dist * (float)Math.Cos(el) * (float)Math.Sin(az),
                    dist * (float)Math.Sin(el),
                    dist * (float)Math.Cos(el) * (float)Math.Cos(az)
                );
                float orthoH = headY * 1.25f;
                float orthoW = orthoH * bodyW / bodyH;
                effect.Parameters["View"].SetValue(Matrix.CreateLookAt(camPos, camTarget, Vector3.Up));
                effect.Parameters["Projection"].SetValue(Matrix.CreateOrthographic(orthoW, orthoH, 0.1f, 100f));
                avatarComp.Avatar.DrawGeometry(gd, effect);
            }

            // ---- Head pass: front-facing, framed on the head + shoulders, bottom 400×400 ----
            gd.Viewport = new Viewport(0, bodyH, headSquare, headSquare);
            {
                float dist = headY * 2.0f;
                // Center on the head bone, frame top-of-head down to mid-chest.
                var camTarget = new Vector3(0f, headY * 0.92f, 0f);
                var camPos = camTarget + new Vector3(0f, 0f, dist);
                // Ortho height ~half the avatar height — head + neck + shoulders.
                float orthoH = headY * 0.55f;
                float orthoW = orthoH; // square head pane
                effect.Parameters["View"].SetValue(Matrix.CreateLookAt(camPos, camTarget, Vector3.Up));
                effect.Parameters["Projection"].SetValue(Matrix.CreateOrthographic(orthoW, orthoH, 0.1f, 100f));
                avatarComp.Avatar.DrawGeometry(gd, effect);
            }

            // Unbind to the back buffer rather than restoring previously-bound RTs.
            // Round-tripping via GetRenderTargets/SetRenderTargets has been observed to
            // poison MonoGame's internal resolve-framebuffer dictionary and crash the
            // next frame's lightmap pass with KeyNotFoundException. The frame that draws
            // after us will rebind whatever it needs anyway.
            gd.SetRenderTarget(null);
            gd.BlendState = oldBlend;
            gd.DepthStencilState = oldDepth;
            gd.RasterizerState = oldRaster;

            return AvatarThumbTarget;
        }

        public void ClearDrawBuffer(List<_2DDrawBuffer> buf)
        {
            foreach (var b in buf) b.Dispose();
            buf.Clear();
        }
    }
}
