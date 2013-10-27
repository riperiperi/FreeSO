using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.ThreeD;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using SimsLib.ThreeD;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.Rendering.Sim
{
    public class SimRenderer : ThreeDElement
    {
        private List<BasicEffect> m_Effects;
        private float m_Rotation;
        private int m_Width, m_Height;
        private SpriteBatch m_SBatch;

        public SimRenderer()
        {
            m_Effects = new List<BasicEffect>();
            m_SBatch = new SpriteBatch(GameFacade.GraphicsDevice);
            m_Effects.Add(new BasicEffect(GameFacade.GraphicsDevice, null));
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
            }
        }

        public override void Update(UpdateState GState)
        {
            m_Rotation += 0.001f;
            GameFacade.Scenes.WorldMatrix = Matrix.CreateRotationX(m_Rotation);
        }
    }
}
