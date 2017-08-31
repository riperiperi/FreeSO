using FSO.LotView.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using FSO.Content;
using FSO.LotView.Model;

namespace FSO.LotView.RC
{
    public class SubWorldComponentRC : SubWorldComponent
    {
        public SubWorldComponentRC(GraphicsDevice Device) : base(Device)
        {
        }

        /// <summary>
        /// Prep work before screen is painted
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="state"></param>
        public override void PreDraw(GraphicsDevice gd, WorldState state)
        {
            if (Blueprint == null) return;
            var damage = Blueprint.Damage;
            var oldLevel = state.Level;
            var oldBuild = state.BuildMode;
            state.SilentLevel = State.Level;
            state.SilentBuildMode = 0;

            /**
             * This is a little bit different from a normal 2d world. All objects are part of the static 
             * buffer, and they are redrawn into the parent world's scroll buffers.
             */

            var recacheWalls = false;
            var recacheObjects = false;

            foreach (var item in damage)
            {
                switch (item.Type)
                {
                    case BlueprintDamageType.ROTATE:
                    case BlueprintDamageType.ZOOM:
                    case BlueprintDamageType.LEVEL_CHANGED:
                        recacheObjects = true;
                        recacheWalls = true;
                        break;
                    case BlueprintDamageType.SCROLL:
                        break;
                    case BlueprintDamageType.LIGHTING_CHANGED:
                        break;
                    case BlueprintDamageType.OBJECT_MOVE:
                    case BlueprintDamageType.OBJECT_GRAPHIC_CHANGE:
                    case BlueprintDamageType.OBJECT_RETURN_TO_STATIC:
                        recacheObjects = true;
                        break;
                    case BlueprintDamageType.WALL_CUT_CHANGED:
                    case BlueprintDamageType.FLOOR_CHANGED:
                    case BlueprintDamageType.WALL_CHANGED:
                        recacheWalls = true;
                        break;
                }
            }
            damage.Clear();

            var is2d = state.Camera is WorldCamera;
            if (is2d)
            {
                state._2D.End();
                state._2D.Begin(state.Camera);
            }
            if (recacheWalls)
            {
                //clear the sprite buffer before we begin drawing what we're going to cache
                Blueprint.Terrain.RegenTerrain(gd, state, Blueprint);
                Blueprint.FloorGeom.FullReset(gd, false);
                Blueprint.WCRC.Generate(gd, state, false);
            }

            state.SilentBuildMode = oldBuild;
            state.SilentLevel = oldLevel;
        }

        public override void DrawArch(GraphicsDevice gd, WorldState parentState)
        {
            var parentScroll = parentState.CenterTile;
            if (!(parentState.Camera is WorldCamera))
                parentState.Camera.Translation = new Vector3(GlobalPosition.X * 3, 0, GlobalPosition.Y * 3);
            else parentState.CenterTile += GlobalPosition; //TODO: vertical offset

            State.PrepareLighting();
            
            var level = parentState.SilentLevel;
            parentState.SilentLevel = 5;
            Blueprint.Terrain._3D = true;
            Blueprint.Terrain.Draw(gd, parentState);
            var effect = WorldContent.RCObject;
            gd.BlendState = BlendState.NonPremultiplied;
            parentState.DrawOOB = false;
            var vp = parentState.Camera.View * parentState.Camera.Projection;
            effect.Parameters["ViewProjection"].SetValue(vp);
            Blueprint.WCRC.Draw(gd, parentState);
            Blueprint.RoofComp.Draw(gd, parentState);
            parentState.SilentLevel = level;
            effect.CurrentTechnique = effect.Techniques["Draw"];

            var frustrum = new BoundingFrustum(vp);
            var objs = Blueprint.Objects.Where(x => frustrum.Intersects(((ObjectComponentRC)x).GetBounds()))
                .OrderBy(x => ((ObjectComponentRC)x).SortDepth(vp));
            foreach (var obj in objs)
            {
                obj.Draw(gd, parentState);
            }

            parentState.CenterTile = parentScroll;
            if (!(parentState.Camera is WorldCamera))
                parentState.Camera.Translation = Vector3.Zero;
            parentState.PrepareLighting();
        }

        public override ObjectComponent MakeObjectComponent(GameObject obj)
        {
            return new ObjectComponentRC(obj);
        }

        public BoundingBox Bounds;

        public void UpdateBounds()
        {
            float minAlt = 0;
            foreach (var height in Blueprint.Altitude)
            {
                var alt = height * Blueprint.TerrainFactor - Blueprint.BaseAlt;
                if (alt < minAlt)
                {
                    minAlt = alt;
                }
            }
            Bounds = new BoundingBox(new Vector3(GlobalPosition.X * -3, minAlt, GlobalPosition.Y * -3), new Vector3(GlobalPosition.X * -3 + Blueprint.Width*3, 1000, GlobalPosition.Y * -3 + Blueprint.Height*3));
        }
    }
}
