/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSO.Vitaboy;
using Microsoft.Xna.Framework;

namespace tso.world.utils
{
    /// <summary>
    /// Used for drawing 3D elements in world.
    /// </summary>
    public class _3DWorldBatch 
    {
        private WorldState State;
        private GraphicsDevice Device;
        //private BasicEffect Effect;

        private List<_3DSprite> Sprites = new List<_3DSprite>();

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

        public void DrawMesh(Matrix world, List<AvatarBindingInstance> group)
        {
            foreach (var item in group)
            {
                DrawMesh(world, item);
            }
        }

        public void DrawMesh(Matrix world, AvatarBindingInstance binding)
        {
            this.Sprites.Add(new _3DSprite {
                Effect = _3DSpriteEffect.CHARACTER,
                Geometry = binding.Mesh,
                Texture = binding.Texture,
                World = world
            });
        }

        /// <summary>
        /// Ends rendering, should always be called after DrawMesh()!
        /// </summary>
        public void End()
        {
            //Device.RasterizerState.CullMode = CullMode.CullCounterClockwiseFace;

            var character = Sprites.Where(x => x.Effect == _3DSpriteEffect.CHARACTER).ToList();
            RenderSpriteList(character, Avatar.Effect, Avatar.Effect.CurrentTechnique);

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
            ApplyCamera(effect);
            //Device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            //Device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            
            var byTexture = sprites.GroupBy(x => x.Texture);
            foreach (var group in byTexture){
                effect.Parameters["MeshTex"].SetValue(group.Key);
                foreach (var pass in technique.Passes)
                {
                    foreach (var geom in group){
                        if (geom.Geometry is Mesh)
                        {
                            Mesh mG = (Mesh)geom.Geometry;
                            if (mG.BoneMatrices != null) effect.Parameters["SkelBindings"].SetValue(mG.BoneMatrices);
                        }
                        effect.Parameters["World"].SetValue(geom.World);
                        pass.Apply();

                        geom.Geometry.DrawGeometry(this.Device);
                    }
                }
            }
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
