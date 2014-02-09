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
using Microsoft.Xna.Framework;
using SimsLib.ThreeD;
using TSOClient.VM;

namespace TSOClient.Code.Rendering.Sim
{
    public class SimRenderer : ThreeDElement
    {
        private List<BasicEffect> m_Effects;
        private float m_Rotation;
        private SpriteBatch m_SBatch;

        private bool m_IsInvalidated = false;

        public SimRenderer()
        {
            m_Effects = new List<BasicEffect>();
            m_SBatch = new SpriteBatch(GameFacade.GraphicsDevice);
            m_Effects.Add(new BasicEffect(GameFacade.GraphicsDevice, null));
        }

        public override void DeviceReset(GraphicsDevice Device)
        {
            m_IsInvalidated = true;

            Device.VertexDeclaration = new VertexDeclaration(Device, VertexPositionNormalTexture.VertexElements);
            Device.RenderState.CullMode = CullMode.None;

            m_Sim.SimSkeleton = new Skeleton();
            m_Sim.SimSkeleton.Read(ContentManager.GetResourceFromLongID(0x100000005));

            for (int i = 0; i < m_Sim.HeadBindings.Count; i++)
                m_Sim.HeadBindings[i] = new SimModelBinding(m_Sim.HeadBindings[i].BindingID);

            for (int i = 0; i < m_Sim.BodyBindings.Count; i++)
                m_Sim.BodyBindings[i] = new SimModelBinding(m_Sim.BodyBindings[i].BindingID);

            //Hands... (data abstraction = PITA!)
            for (int i = 0; i < m_Sim.LeftHandBindings.FistBindings.Count; i++)
                m_Sim.LeftHandBindings.FistBindings[i] = new SimModelBinding(m_Sim.LeftHandBindings.FistBindings[i].BindingID);
            for (int i = 0; i < m_Sim.LeftHandBindings.IdleBindings.Count; i++)
                m_Sim.LeftHandBindings.IdleBindings[i] = new SimModelBinding(m_Sim.LeftHandBindings.IdleBindings[i].BindingID);
            for (int i = 0; i < m_Sim.LeftHandBindings.PointingBindings.Count; i++)
                m_Sim.LeftHandBindings.PointingBindings[i] = new SimModelBinding(m_Sim.LeftHandBindings.PointingBindings[i].BindingID);

            for (int i = 0; i < m_Sim.RightHandBindings.FistBindings.Count; i++)
                m_Sim.RightHandBindings.FistBindings[i] = new SimModelBinding(m_Sim.RightHandBindings.FistBindings[i].BindingID);
            for (int i = 0; i < m_Sim.RightHandBindings.IdleBindings.Count; i++)
                m_Sim.RightHandBindings.IdleBindings[i] = new SimModelBinding(m_Sim.RightHandBindings.IdleBindings[i].BindingID);
            for (int i = 0; i < m_Sim.RightHandBindings.PointingBindings.Count; i++)
                m_Sim.RightHandBindings.PointingBindings[i] = new SimModelBinding(m_Sim.RightHandBindings.PointingBindings[i].BindingID);

            //This can be rewritten - I've no idea why the rotation seems to be reset...
            RotationZ = 262.32f;

            m_IsInvalidated = false;
        }

        /// <summary>
        /// Information about the sim we are rendering
        /// </summary>
        private TSOClient.VM.Sim m_Sim;
        public TSOClient.VM.Sim Sim
        {
            get { return m_Sim; }
            set
            {
                m_Sim = value;
            }
        }

        public override void Draw(GraphicsDevice device, ThreeDScene scene)
        {
            if (m_Sim == null) { return; }

            if(!m_IsInvalidated)
            {
                device.VertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);
                device.RenderState.CullMode = CullMode.None;

                var world = World;

                foreach (var effect in m_Effects)
                {
                    effect.World = world;
                    effect.View = scene.Camera.View;
                    effect.Projection = scene.Camera.Projection;

                    /** Head **/
                    foreach (var binding in m_Sim.HeadBindings)
                    {
                        effect.Texture = binding.Texture;
                        effect.TextureEnabled = true;
                        effect.CommitChanges();
                        effect.Begin();

                        foreach (var pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Begin();
                            binding.Mesh.Draw(device);
                            pass.End();
                        }

                        effect.End();
                    }

                    foreach (var binding in m_Sim.BodyBindings)
                    {
                        effect.Texture = binding.Texture;
                        effect.TextureEnabled = true;
                        effect.CommitChanges();
                        effect.Begin();

                        foreach (var pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Begin();
                            binding.Mesh.Draw(device);
                            pass.End();
                        }

                        effect.End();
                    }

                    //Only draw idle bindings for now...
                    foreach (var binding in m_Sim.LeftHandBindings.IdleBindings)
                    {
                        effect.Texture = binding.Texture;
                        effect.TextureEnabled = true;
                        effect.CommitChanges();
                        effect.Begin();

                        foreach (var pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Begin();
                            binding.Mesh.Draw(device);
                            pass.End();
                        }

                        effect.End();
                    }

                    foreach (var binding in m_Sim.RightHandBindings.IdleBindings)
                    {
                        effect.Texture = binding.Texture;
                        effect.TextureEnabled = true;
                        effect.CommitChanges();
                        effect.Begin();

                        foreach (var pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Begin();
                            binding.Mesh.Draw(device);
                            pass.End();
                        }

                        effect.End();
                    }
                }
            }
        }

        public override void Update(GameTime Time)
        {
            m_Rotation += 0.001f;
            GameFacade.Scenes.WorldMatrix = Matrix.CreateRotationX(m_Rotation);
        }
    }
}
