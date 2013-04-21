using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.ThreeD;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using SimsLib.ThreeD;

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

            var world = World;

            foreach (var effect in m_Effects)
            {
                effect.World = world;

                effect.View = scene.Camera.View;
                effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)device.PresentationParameters.BackBufferWidth /
                    (float)device.PresentationParameters.BackBufferHeight,
                    1.0f, 100.0f);

                effect.Projection = scene.Camera.Projection;

                effect.Texture = m_Sim.HeadTexture;
                effect.TextureEnabled = true;
                effect.CommitChanges();
                effect.Begin();

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Begin();

                    foreach (Face Fce in m_Sim.HeadMesh.FaceData)
                    {
                        if (m_Sim.HeadMesh.VertexTexNormalPositions != null)
                        {
                            VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                            Vertex[0] = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.VertexA];
                            Vertex[1] = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.VertexB];
                            Vertex[2] = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.VertexC];

                            Vertex[0].TextureCoordinate = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.VertexA].TextureCoordinate;
                            Vertex[1].TextureCoordinate = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.VertexB].TextureCoordinate;
                            Vertex[2].TextureCoordinate = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.VertexC].TextureCoordinate;

                            GameFacade.GraphicsDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, Vertex, 0, 1);
                        }
                    }

                    pass.End();
                }

                effect.End();
            }
        }

        public override void Update(GameTime Time)
        {
            m_Rotation += 0.001f;
            GameFacade.Scenes.WorldMatrix = Matrix.CreateRotationX(m_Rotation);
        }
    }
}
