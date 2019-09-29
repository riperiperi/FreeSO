using FSO.Common.Utils;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Platform
{
    public class WorldPlatform2D : IWorldPlatform
    {
        public Blueprint bp;
        private List<_2DDrawBuffer> StaticWallCache = new List<_2DDrawBuffer>();

        public WorldPlatform2D(Blueprint bp)
        {
            this.bp = bp;
        }

        public void Dispose()
        {
            
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
            //wCam.ViewDimensions = new Vector2(-1, -1);
            var oldPreciseZoom = state.PreciseZoom;
            state.ForceCamera(Utils.Camera.CameraControllerType._2D);

            //full invalidation because we must recalculate all object sprites. slow but necessary!
            state.RenderingThumbnail = true;
            state.Zoom = WorldZoom.Far;
            state.Rotation = WorldRotation.TopLeft;
            state.Level = bp.Stories;
            state.PreciseZoom = 1/4f;
            state._2D.PreciseZoom = state.PreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();

            var oldCenter = state.CenterTile;
            state.CenterTile = bp.GetThumbCenterTile(state);
            state.CenterTile -= state.WorldSpace.GetTileFromScreen(new Vector2((576 - state.WorldSpace.WorldPxWidth)*4, (576 - state.WorldSpace.WorldPxHeight)*4) / 2);
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            state.TempDraw = true;
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
                    bp.FloorGeom.SliceReset(gd, new Rectangle(6, 6, bp.Width - 13, bp.Height - 13));
                    //Blueprint.SetLightColor(WorldContent.GrassEffect, Color.White, Color.White);
                    bp.Terrain.Draw(gd, state);
                    bp.Terrain.DrawMask(gd, state, state.View, state.Projection);
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
                    rooflessCallback?.Invoke(bufferTexture.Get());
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
            state.TempDraw = false;
            state.CenterTile = oldCenter;

            state.Zoom = oldZoom;
            state.Rotation = oldRotation;
            state.Level = oldLevel;
            state.RenderingThumbnail = false;
            bp.Cutaway = oldCutaway;

            var tex = bufferTexture.Get();
            return tex; //TextureUtils.Clip(gd, tex, bounds);
        }

        public short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd, WorldState state)
        {
            /** Draw all objects to a texture as their IDs **/
            var oldCenter = state.CenterTile;
            var tileOff = state.WorldSpace.GetTileFromScreen(new Vector2(x, y));
            state.CenterTile += tileOff;
            var pxOffset = state.WorldSpace.GetScreenOffset();
            var _2d = state._2D;
            Promise<Texture2D> bufferTexture = null;

            state.WorldRectangle = new Rectangle((-pxOffset).ToPoint(), new Point(1, 1));

            state.TempDraw = true;
            state.ObjectIDMode = true;
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
                        if (obj.Level != state.Level || !obj.DoDraw(state)) continue;
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
                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(avatar.Position));
                        _2d.OffsetTile(avatar.Position);
                        avatar.Draw(gd, state);
                    }
                    //state._3D.End();
                }

            }
            state.ObjectIDMode = false;
            state._2D.OBJIDMode = false;
            state.TempDraw = false;
            state.CenterTile = oldCenter;

            var tex = bufferTexture.Get();
            Color[] data = new Color[1];
            tex.GetData<Color>(data);
            var f = Vector3.Dot(new Vector3(data[0].R / 255.0f, data[0].G / 255.0f, data[0].B / 255.0f), new Vector3(1.0f, 1 / 255.0f, 1 / 65025.0f));
            return (short)Math.Round(f * 65535f);
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
            state.TempDraw = true;
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
                        _2d.SetShaderOffsets(pxOffset + thumbOffset, tileOff); //offset object into rotated position
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
            state.TempDraw = false;
            state.RenderingThumbnail = false;
            gd.DepthStencilState = oldDS;

            var tex = bufferTexture.Get();
            return TextureUtils.Clip(gd, tex, b);
        }

        public void RecacheWalls(GraphicsDevice gd, WorldState state, bool cutawayOnly)
        {
            //in 2d, if we have 3d wall shadows enabled we also have to update the 3d wall geometry
            bp.WCRC?.Generate(gd, state, cutawayOnly);

            var _2d = state._2D;
            _2d.Pause();
            _2d.Resume(); //clear the sprite buffer before we begin drawing what we're going to cache
            bp.WallComp.Draw(gd, state);
            ClearDrawBuffer(bp.WallCache2D);
            state.PrepareLighting();
            _2d.End(bp.WallCache2D, true);
        }

        public void ClearDrawBuffer(List<_2DDrawBuffer> buf)
        {
            foreach (var b in buf) b.Dispose();
            buf.Clear();
        }
    }
}
