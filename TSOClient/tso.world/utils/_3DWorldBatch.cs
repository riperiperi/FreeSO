/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
    public class _3DWorldBatch {

        private WorldState State;
        private GraphicsDevice Device;
        private BasicEffect Effect;

        private List<_3DSprite> Sprites = new List<_3DSprite>();

        public _3DWorldBatch(WorldState state){
            this.State = state; 
            this.Effect = new BasicEffect(state.Device, null);
        }


        public void Begin(GraphicsDevice device)
        {
            this.Sprites.Clear();
            this.Device = device;
        }


        /// <summary
        /// </summary>
        /// <param name="group"></param>
        public void DrawMesh(Matrix world, List<AvatarBindingInstance> group)
        {
            foreach (var item in group){
                DrawMesh(world, item);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="group"></param>
        public void DrawMesh(Matrix world, AvatarBindingInstance binding){
            this.Sprites.Add(new _3DSprite {
                Effect = _3DSpriteEffect.CHARACTER,
                Geometry = binding.Mesh,
                Texture = binding.Texture,
                World = world
            });
        }

        public void End()
        {
            Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            var character = Sprites.Where(x => x.Effect == _3DSpriteEffect.CHARACTER).ToList();
            RenderSpriteList(character, Effect, Effect.CurrentTechnique);

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

        private void RenderSpriteList(List<_3DSprite> sprites, BasicEffect effect, EffectTechnique technique){
            ApplyCamera(effect);
            effect.TextureEnabled = true;
            Device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            Device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            
            var byTexture = sprites.GroupBy(x => x.Texture);
            foreach (var group in byTexture){
                effect.Texture = group.Key;
                effect.CommitChanges();

                effect.Begin();
                foreach (var pass in technique.Passes)
                {
                    pass.Begin();
                    foreach (var geom in group){
                        effect.World = geom.World;
                        effect.CommitChanges();

                        geom.Geometry.DrawGeometry(this.Device);
                    }
                    pass.End();
                }
                effect.End();
            }
        }

        public void ApplyCamera(BasicEffect effect){
            effect.View = State.Camera.View;
            effect.Projection = State.Camera.Projection;
        }
        public void ApplyCamera(BasicEffect effect, WorldComponent component)
        {
            effect.World = component.World;
            effect.View = State.Camera.View;
            effect.Projection = State.Camera.Projection;
        }
    }
}
