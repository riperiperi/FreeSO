/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Vitaboy;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;
using FSO.Common;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// Used for drawing 3D elements in world.
    /// </summary>
    public class _3DWorldBatch 
    {
        private WorldState State;
        private GraphicsDevice Device;
        public bool OBJIDMode = false;
        //private BasicEffect Effect;

        private List<_3DSprite> Sprites = new List<_3DSprite>();

        public Color[] RoomLights { get; internal set; }

        public _3DWorldBatch(WorldState state)
        {
            this.State = state; 
            //this.Effect = new BasicEffect(state.Device);
        }

        /// <summary>
        /// Begins rendering, should always be called before DrawMesh()!
        /// </summary>
        /// <param name="device">GraphicsDevice instance.</param>
        public void Begin(GraphicsDevice device)
        {
            this.Sprites.Clear();
            this.Device = device;
        }

        public void DrawMesh(Matrix world, Avatar binding, short objID, ushort room, Color color, int level)
        {
            this.Sprites.Add(new _3DSprite {
                Effect = _3DSpriteEffect.CHARACTER,
                Geometry = binding,
                World = world,
                ObjectID = objID,
                Room = room,
                Color = color,
                Level = level
            });
        }

        /// <summary>
        /// Ends rendering, should always be called after DrawMesh()!
        /// </summary>
        public void End()
        {
            if (Sprites.Count == 0) return;
            //Device.RasterizerState.CullMode = CullMode.CullCounterClockwiseFace;

            var character = Sprites.Where(x => x.Effect == _3DSpriteEffect.CHARACTER).ToList();
            var pass = WorldConfig.Current.PassOffset*2;

            PPXDepthEngine.RenderPPXDepth(Avatar.Effect, true, (depth) =>
            {
                RenderSpriteList(character, Avatar.Effect, Avatar.Effect.Techniques[OBJIDMode ? 1 : pass]);
            });

            /*
            ApplyCamera(Effect);
            Effect.World = world;
            Effect.TextureEnabled = true;
            Effect.Texture = binding.Texture;
            Effect.CommitChanges();

            Effect.Begin();
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                binding.Mesh.Draw(Device);
                pass.End();
            }
            Effect.End();*/
        }

        private void RenderSpriteList(List<_3DSprite> sprites, Effect effect, EffectTechnique technique){
            //TODO: multiple types of 3dsprite. This was originally here to group meshes by texture, 
            //but since passing a texture uniform is less expensive than passing >16 matrices we now group
            //by avatar anyways. Other 3d sprites might include the roof, terrain, 3d versions of objects??
            //(when we come to 3d reconstruction from depth map)

            effect.CurrentTechnique = technique;
            ApplyCamera(effect);
            effect.Parameters["SoftwareDepth"].SetValue(FSOEnvironment.SoftwareDepth);
            foreach (var pass in technique.Passes)
            {
                foreach (var geom in sprites)
                {
                    if (OBJIDMode) effect.Parameters["ObjectID"].SetValue(geom.ObjectID / 65535f);
                    effect.Parameters["Level"].SetValue((float)geom.Level);
                    if (RoomLights != null)
                    {
                        var col = RoomLights[geom.Room].ToVector4() * geom.Color.ToVector4();
                        effect.Parameters["AmbientLight"].SetValue(col);
                    }
                    /*if (geom.Geometry is Avatar)
                    {
                        Avatar mG = (Avatar)geom.Geometry;
                        if (mG.BoneMatrices != null) effect.Parameters["SkelBindings"].SetValue(mG.BoneMatrices);
                    }*/
                    effect.Parameters["World"].SetValue(geom.World);
                    pass.Apply();

                    geom.Geometry.DrawGeometry(this.Device, effect);
                }
            }
            effect.Parameters["SoftwareDepth"].SetValue(false); //reset this for non-world purposes
        }

        public void ApplyCamera(Effect effect){
            effect.Parameters["View"].SetValue(State.Camera.View);
            effect.Parameters["Projection"].SetValue(State.Camera.Projection);
        }
        public void ApplyCamera(BasicEffect effect, WorldComponent component)
        {
            effect.View = State.Camera.View;
            effect.Projection = State.Camera.Projection;
            effect.World = component.World;
        }
    }
}
