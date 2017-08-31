using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using FSO.LotView.Components;
using FSO.LotView.Utils;
using FSO.Common.Utils;

namespace FSO.LotView.RC
{
    /// <summary>
    /// An alternate implenentation of World2D that renders the game with a 3D camera.
    /// While the world is technically no longer "2D", this fills the purpose of handling
    /// what the isometric renderer typically renders to the static 2D buffers (objects, architecture, terrain)
    /// 
    /// RC stands for reconstruction, the primary method used to render game objects in 3D.
    /// </summary>
    public class World2DRC : World2D
    {
        public IRCSurroundings Surroundings;
        private bool Drawn;
        public RenderTarget2D LotThumbTarget;
        public RenderTarget2D ObjThumbTarget;

        public override void PreDraw(GraphicsDevice gd, WorldState state)
        {
            //var oht = state.BaseHeight;
            //state.BaseHeight = 0;
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            //state.BaseHeight = oht;
            var damage = Blueprint.Damage;
            var _2d = state._2D;

            /**
             * Tasks:
             *  If zoom or rotation has changed, redraw all static layers
             *  If scroll has changed, redraw static layer if the scroll is outwith the buffered region
             *  If architecture has changed, redraw appropriate static layer
             *  If there is a new object in the static layer, redraw the static layer
             *  If an objects in the static layer has changed, redraw the static layer and move the object to the dynamic layer
             *  If wall visibility has changed, redraw wall layer (should think about how this works with breakthrough wall mode
             */

            var im = state.ThisFrameImmediate;

            var recacheWalls = false;
            var recacheCutaway = false;
            var recacheFloors = false;
            var recacheTerrain = false;
            var recacheObjects = false;
            var drawImmediate = false;

            var lightChangeType = 0;

            if (TicksSinceLight++ > 60 * 4) damage.Add(new BlueprintDamage(BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED));

            WorldObjectRenderInfo info = null;

            foreach (var item in damage)
            {
                switch (item.Type)
                {
                    case BlueprintDamageType.ROTATE:
                    case BlueprintDamageType.ZOOM:
                    case BlueprintDamageType.LEVEL_CHANGED:
                        recacheObjects = true;
                        recacheWalls = true;
                        recacheFloors = true;
                        state.Light?.InvalidateOutdoors();
                        //recacheTerrain = true;
                        break;
                    case BlueprintDamageType.SCROLL:
                        break;
                    case BlueprintDamageType.PRECISE_ZOOM:
                        drawImmediate = true;
                        break;
                    case BlueprintDamageType.LIGHTING_CHANGED:
                        if (lightChangeType >= 2) break;
                        var room = (ushort)item.TileX;

                        state.Light?.InvalidateRoom(room);
                        Blueprint.GenerateRoomLights();
                        state.OutsideColor = Blueprint.RoomColors[1];
                        state._3D.RoomLights = Blueprint.RoomColors;

                        if (state.AmbientLight != null)
                        {
                            state.AmbientLight.SetData(Blueprint.RoomColors);
                        }
                        TicksSinceLight = 0;
                        break;
                    case BlueprintDamageType.OUTDOORS_LIGHTING_CHANGED:
                        if (lightChangeType >= 1) break;
                        lightChangeType = 1;

                        state.Light?.InvalidateOutdoors();
                        Blueprint.GenerateRoomLights();
                        state.OutsideColor = Blueprint.RoomColors[1];
                        state._3D.RoomLights = Blueprint.RoomColors;

                        if (state.AmbientLight != null)
                        {
                            state.AmbientLight.SetData(Blueprint.RoomColors);
                        }
                        TicksSinceLight = 0;
                        break;
                    case BlueprintDamageType.OBJECT_MOVE:
                        /** Redraw if its in static layer **/
                        /*
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.STATIC)
                        {
                            recacheObjects = true;
                            info.Layer = WorldObjectRenderLayer.DYNAMIC;
                        }
                        if (item.Component is ObjectComponent) ((ObjectComponent)item.Component).DynamicCounter = 0;*/
                        break;
                    case BlueprintDamageType.OBJECT_GRAPHIC_CHANGE:
                        /** Redraw if its in static layer **/
                                                /*
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.STATIC)
                        {
                            recacheObjects = true;
                            info.Layer = WorldObjectRenderLayer.DYNAMIC;
                        }
                        if (item.Component is ObjectComponent) ((ObjectComponent)item.Component).DynamicCounter = 0;*/
                        break;
                    case BlueprintDamageType.OBJECT_RETURN_TO_STATIC:
                        /*
                        info = GetRenderInfo(item.Component);
                        if (info.Layer == WorldObjectRenderLayer.DYNAMIC)
                        {
                            recacheObjects = true;
                            info.Layer = WorldObjectRenderLayer.STATIC;
                        }*/
                        break;
                    case BlueprintDamageType.WALL_CUT_CHANGED:
                        recacheCutaway = true;
                        break;
                    case BlueprintDamageType.ROOF_STYLE_CHANGED:
                        Blueprint.RoofComp.StyleDirty = true;
                        break;
                    case BlueprintDamageType.ROOM_CHANGED:
                        for (sbyte i = 0; i < Blueprint.RoomMap.Length; i++)
                        {
                            state.Rooms.SetRoomMap(i, Blueprint.RoomMap[i]);
                        }
                        if (state.Light != null)
                        {
                            if (lightChangeType < 2)
                            {
                                lightChangeType = 2;
                                state.Light.InvalidateAll();
                            }
                        }
                        Blueprint.RoofComp.ShapeDirty = true;
                        break;
                    case BlueprintDamageType.FLOOR_CHANGED:
                    case BlueprintDamageType.WALL_CHANGED:
                        recacheFloors = true;
                        recacheWalls = true;
                        Blueprint.RoofComp.ShapeDirty = true;
                        break;
                }
            }
            damage.Clear();

            if (recacheTerrain)
                Blueprint.Terrain.RegenTerrain(gd, state, Blueprint);

            if (recacheWalls)
                Blueprint.WCRC?.Generate(gd, state, false);
            else if (recacheCutaway)
                Blueprint.WCRC?.Generate(gd, state, true);

            if (recacheFloors)
            {
                Blueprint.FloorGeom.FullReset(gd, state.BuildMode > 1);
            }

            if (Drawn)
                state.Light?.ParseInvalidated((sbyte)(state.Level + ((state.DrawRoofs) ? 1 : 0)), state);

            if (recacheObjects)
            {
                /* objects no longer statically cached?
                _2d.Pause();
                _2d.Resume();

                foreach (var obj in Blueprint.Objects)
                {
                    if (obj.Level > state.Level) continue;
                    var renderInfo = GetRenderInfo(obj);
                    if (renderInfo.Layer == WorldObjectRenderLayer.STATIC)
                    {
                        var tilePosition = obj.Position;
                        _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(tilePosition));
                        _2d.OffsetTile(tilePosition);
                        _2d.SetObjID(obj.ObjectID);
                        obj.Draw(gd, state);
                    }
                }
                ClearDrawBuffer(StaticObjectsCache);
                _2d.End(StaticObjectsCache, true);
                */
            }

            state.ThisFrameImmediate = drawImmediate;
        }

        public override short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd, WorldState state)
        {
            //var sPos = new Vector3(((float)x / state.WorldSpace.WorldPxWidth) * 2 - 1, 1 - ((float)y / state.WorldSpace.WorldPxHeight) * 2, 0);
            var sPos = new Vector3(x, y, 0);
            var p1 = gd.Viewport.Unproject(sPos, state.Camera.Projection, state.Camera.View, Matrix.Identity);
            sPos.Z = 1;
            var p2 = gd.Viewport.Unproject(sPos, state.Camera.Projection, state.Camera.View, Matrix.Identity);
            var dir = p2 - p1;
            dir.Normalize();
            var ray = new Ray(p1, p2 - p1);
            ray.Direction.Normalize();
            short bestObj = 0;
            float bestDistance = float.MaxValue;
            foreach (var obj in Blueprint.Objects)
            {
                if (obj.Level != state.Level || !obj.Visible || obj.CutawayHidden) continue;
                var objR = (ObjectComponentRC)obj;
                var intr = objR.IntersectsBounds(ray);
                if (obj.Container != null && intr != null) intr = intr.Value - 1.5f;
                if (intr != null && intr.Value < bestDistance)
                {
                    bestObj = obj.ObjectID;
                    bestDistance = intr.Value;
                }
            }

            foreach (var sim in Blueprint.Avatars)
            {
                if (!sim.Visible) continue;
                var pos = sim.GetPelvisPosition()*3;
                pos = new Vector3(pos.X, pos.Z, pos.Y) + new Vector3(1.5f, 0, 1.5f);
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

        /// <summary>
        /// Gets an object group's thumbnail provided an array of objects.
        /// </summary>
        /// <param name="objects">The object components to draw.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        public override Texture2D GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd, WorldState state)
        {

            var cam = (WorldCamera3D)state.Camera;
            var oldCamOrg = cam.ProjectionOrigin;
            var oldCamPos = cam.Position;
            var oldCamTarg = cam.Target;

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
            state.TempDraw = true;

            var _2d = state._2D;

            if (ObjThumbTarget == null)
                ObjThumbTarget = new RenderTarget2D(gd, 1024, 1024, true, SurfaceFormat.Color, DepthFormat.Depth24);

            gd.SetRenderTarget(ObjThumbTarget);
            var cpoints = new List<Vector3>();
            var vp = state.Camera.View * state.Camera.Projection;
            gd.BlendState = BlendState.NonPremultiplied;
            gd.RasterizerState = RasterizerState.CullNone;
            gd.DepthStencilState = DepthStencilState.Default;
            var effect = WorldContent.RCObject;
            effect.Parameters["ViewProjection"].SetValue(vp);
            effect.CurrentTechnique = effect.Techniques["Draw"];
            state.ClearLighting(true);
            Blueprint.SetLightColor(WorldContent.RCObject, Color.White, Color.White);

            var objs = objects.OrderBy(x => ((ObjectComponentRC)x).SortDepth(vp)).ToList();

            gd.Clear(Color.Transparent);
            for (int i = 0; i < objs.Count; i++)
            {
                var obj = objs[i];
                var robj = (ObjectComponentRC)obj;
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
                cpoints.AddRange(robj.GetBounds().GetCorners().Select(x =>
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
            var bounds3d = BoundingBox.CreateFromPoints(cpoints);
            var bounds = new Rectangle((int)bounds3d.Min.X, (int)bounds3d.Min.Y, (int)(bounds3d.Max.X - bounds3d.Min.X), (int)(bounds3d.Max.Y - bounds3d.Min.Y));

            bounds.Inflate(1, 1);
            bounds.X = Math.Max(0, Math.Min(1023, bounds.X));
            bounds.Y = Math.Max(0, Math.Min(1023, bounds.Y));
            if (bounds.Width + bounds.X > 1024) bounds.Width = 1024 - bounds.X;
            if (bounds.Height + bounds.Y > 1024) bounds.Height = 1024 - bounds.Y;

            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.LIGHTING_CHANGED));

            //return things to normal
            state.DrawOOB = false;
            state.TempDraw = false;

            cam.ProjectionOrigin = oldCamOrg;
            cam.Target = oldCamTarg;
            cam.Position = oldCamPos;

            gd.DepthStencilState = DepthStencilState.None;
            var clip = TextureUtils.Clip(gd, ObjThumbTarget, bounds);
            var dec = TextureUtils.Decimate(clip, gd, 3, true);
            return dec;
        }

        /// <summary>
        /// Gets the current lot's thumbnail.
        /// </summary>
        /// <param name="objects">The object components to draw.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <param name="state">WorldState instance.</param>
        /// <returns>Object's ID if the object was found at the given position.</returns>
        public override Texture2D GetLotThumb(GraphicsDevice gd, WorldState state)
        {
            var oldZoom = state.Zoom;
            var oldRotation = state.Rotation;
            var oldLevel = state.Level;
            var oldCutaway = Blueprint.Cutaway;
            ((WorldStateRC)state).Use2DCam = true;
            var wCam = (WorldCamera)state.Camera;
            var oldViewDimensions = wCam.ViewDimensions;
            //wCam.ViewDimensions = new Vector2(-1, -1);
            var oldPreciseZoom = state.PreciseZoom;

            //full invalidation because we must recalculate all object sprites. slow but necessary!
            state.Zoom = WorldZoom.Far;
            state.Rotation = WorldRotation.TopLeft;
            state.Level = Blueprint.Stories;
            state.PreciseZoom = 1 / 4f;
            state._2D.PreciseZoom = state.PreciseZoom;
            state.WorldSpace.Invalidate();
            state.InvalidateCamera();

            var oldCenter = state.CenterTile;
            state.CenterTile = new Vector2(Blueprint.Width / 2, Blueprint.Height / 2);
            state.CenterTile -= state.WorldSpace.GetTileFromScreen(new Vector2((576 - state.WorldSpace.WorldPxWidth) * 4, (576 - state.WorldSpace.WorldPxHeight) * 4) / 2);
            var pxOffset = -state.WorldSpace.GetScreenOffset();
            state.TempDraw = true;
            Blueprint.Cutaway = new bool[Blueprint.Cutaway.Length];
            

            state.ClearLighting(false);
            if (LotThumbTarget == null)
                LotThumbTarget = new RenderTarget2D(gd, 576, 576);
            var lastLight = state.OutsideColor;
            state.OutsideColor = Color.White;
            state._2D.OBJIDMode = false;

            gd.SetRenderTarget(LotThumbTarget);
            gd.Clear(Color.Transparent);

            state._2D.ResetMatrices(576, 576);

            Blueprint.FloorGeom.SliceReset(gd, new Rectangle(6, 6, Blueprint.Width - 13, Blueprint.Height - 13));
            Blueprint.SetLightColor(WorldContent.GrassEffect, Color.White, Color.White);
            Blueprint.SetLightColor(WorldContent.RCObject, Color.White, Color.White);
            Blueprint.Terrain.Draw(gd, state);

            var effect = WorldContent.RCObject;
            gd.BlendState = BlendState.NonPremultiplied;
            var vp = state.Camera.View * state.Camera.Projection;
            effect.Parameters["ViewProjection"].SetValue(vp);

            var cuts = Blueprint.Cutaway;
            Blueprint.Cutaway = new bool[cuts.Length];
            Blueprint.WCRC?.Generate(gd, state, false);
            Blueprint.WCRC?.Draw(gd, state);
            Blueprint.Cutaway = cuts;
            Blueprint.WCRC?.Generate(gd, state, false);

            gd.BlendState = BlendState.NonPremultiplied;
            gd.RasterizerState = RasterizerState.CullNone;

            effect.CurrentTechnique = effect.Techniques["Draw"];
            var frustrum = new BoundingFrustum(vp);
            var objs = Blueprint.Objects.OrderBy(x => ((ObjectComponentRC)x).SortDepth(vp));
            foreach (var obj in objs)
            {
                obj.Draw(gd, state);
            }
            Blueprint.RoofComp.Draw(gd, state);


            gd.SetRenderTarget(null);

            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.LIGHTING_CHANGED));
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.FLOOR_CHANGED));
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
            Blueprint.Cutaway = oldCutaway;

            ((WorldStateRC)state).Use2DCam = false;

            var tex = LotThumbTarget;
            return tex; //TextureUtils.Clip(gd, tex, bounds);
        }

        SkyDomeComponent Dome;

        public void DrawBg(GraphicsDevice gd, WorldState state, BoundingBox[] skyBounds)
        {
            var frustrum = new BoundingFrustum(state.Camera.View * state.Camera.Projection);

            //frustrum.Contains(skyBounds)
            if ((state.Camera as WorldCamera3D)?.FromIntensity > 0 || skyBounds?.Any(x => x.Intersects(frustrum)) != false)
            {
                if (Dome == null) Dome = new SkyDomeComponent(gd);
                Dome.Draw(gd, state);

                Surroundings?.DrawSurrounding(gd, state.Camera, Dome.FogColor, (Blueprint.SubWorlds.Count>0)?1:0);
            }
            gd.Clear(ClearOptions.DepthBuffer, Color.White, 1, 0);

            foreach (var surround in Blueprint.SubWorlds)
            {
                var bounds = ((SubWorldComponentRC)surround).Bounds;
                if (bounds.Intersects(frustrum))
                    surround.DrawArch(gd, state);
            }

            gd.BlendState = BlendState.NonPremultiplied;
            if (Blueprint.Terrain != null)
            {
                Blueprint.Terrain.DepthMode = state._2D.OutputDepth;
                Blueprint.Terrain._3D = true;
                Blueprint.Terrain.Draw(gd, state);
            }
        }

        public override void Draw(GraphicsDevice gd, WorldState state)
        {
            var effect = WorldContent.RCObject;
            gd.BlendState = BlendState.NonPremultiplied;
            var vp = state.Camera.View * state.Camera.Projection;
            effect.Parameters["ViewProjection"].SetValue(vp);


            Blueprint.WCRC?.Draw(gd, state);

            gd.BlendState = BlendState.NonPremultiplied;
            gd.RasterizerState = RasterizerState.CullNone;

            effect.CurrentTechnique = effect.Techniques["Draw"];
            var frustrum = new BoundingFrustum(vp);
            var objs = Blueprint.Objects.Where(x => x.Level <= state.Level && frustrum.Intersects(((ObjectComponentRC)x).GetBounds()))
                .OrderBy(x => ((ObjectComponentRC)x).SortDepth(vp));
            foreach (var obj in objs)
            {
                obj.Draw(gd, state);
            }

            foreach (var ava in Blueprint.Avatars)
            {
                if (ava.Level < state.Level) ava.DrawHeadline3D(gd, state);
            }
            Drawn = true;
        }

        public override void Dispose()
        {
            base.Dispose();
            LotThumbTarget?.Dispose();
            ObjThumbTarget?.Dispose();
        }
    }
}
