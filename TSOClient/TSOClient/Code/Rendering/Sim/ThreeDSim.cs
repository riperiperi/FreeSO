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
    public class ThreeDSim : ThreeDElement
    {
        private List<BasicEffect> m_Effects;
        private float m_Rotation;
        private int m_Width, m_Height;
        private SpriteBatch m_SBatch;

        public ThreeDSim()
        {
            m_Effects = new List<BasicEffect>();
            m_SBatch = new SpriteBatch(GameFacade.GraphicsDevice);
            m_Effects.Add(new BasicEffect(GameFacade.GraphicsDevice, null));

            //GameFacade.GraphicsDevice.DeviceReset += new EventHandler(GraphicsDevice_DeviceReset);
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

            var world = World;// *Matrix.CreateRotationX(m_Rotation);

            foreach (var effect in m_Effects)
            {
                effect.World = world;//GameFacade.Scenes.WorldMatrix * Matrix.CreateScale(3.0f);// *Microsoft.Xna.Framework.Matrix.CreateTranslation(
                                // new Microsoft.Xna.Framework.Vector3(m_Sim.HeadXPos, m_Sim.HeadYPos, 0.0f));

                effect.View = scene.Camera.View;
                //effect.Projection = GameFacade.Scenes.ProjectionMatrix;
                effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)device.PresentationParameters.BackBufferWidth /
                    (float)device.PresentationParameters.BackBufferHeight,
                    1.0f, 100.0f);

                //var aspect = GameFacade.GraphicsDevice.Viewport.AspectRatio;
                /*var ratioX = 1024.0f / 1024.0f;
                var ratioY = 10.0f / 768.0f;
                var projectionX = 0.0f - (1.0f * ratioX);
                var projectionY = 0.0f - (1.0f * ratioY);
                effect.Projection = Matrix.CreatePerspectiveOffCenter(projectionX, projectionX + 1.0f, (projectionY / aspect), (projectionY+1.0f) / aspect, 1.0f, 100.0f);
                */
                effect.Projection = scene.Camera.Projection;

                effect.Texture = m_Sim.HeadTexture;
                effect.TextureEnabled = true;
                //effect.EnableDefaultLighting();
                
                //foreach (var technique in effect.Techniques)
                //{
                //    effect.CurrentTechnique = technique;
                    effect.CommitChanges();
                    effect.Begin();


                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Begin();

                        foreach (Face Fce in m_Sim.HeadMesh.Faces)
                        {
                            if (m_Sim.HeadMesh.VertexTexNormalPositions != null)
                            {
                                VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                                Vertex[0] = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.AVertexIndex];
                                Vertex[1] = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.BVertexIndex];
                                Vertex[2] = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.CVertexIndex];

                                Vertex[0].TextureCoordinate = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.AVertexIndex].TextureCoordinate;
                                Vertex[1].TextureCoordinate = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.BVertexIndex].TextureCoordinate;
                                Vertex[2].TextureCoordinate = m_Sim.HeadMesh.VertexTexNormalPositions[Fce.CVertexIndex].TextureCoordinate;

                                GameFacade.GraphicsDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, Vertex, 0, 1);
                            }
                        }

                        pass.End();
                    }


                    effect.End();
                }

            }
        //}

        //        /*effect.World = m_Scene.SceneMgr.WorldMatrix * 
        //            Matrix.CreateTranslation(new Vector3(m_CurrentSims[j].HeadXPos, m_CurrentSims[j].HeadYPos, 0.0f));
        //        m_Effects[i].View = Matrix.CreateLookAt(Vector3.Backward * 17, Vector3.Zero, Vector3.Right);
        //        m_Effects[i].Projection = m_Scene.SceneMgr.ProjectionMatrix;

        //        m_Effects[i].Texture = m_CurrentSims[j].HeadTexture;
        //        m_Effects[i].TextureEnabled = true;

        //        m_Effects[i].EnableDefaultLighting();

        //        m_Effects[i].CommitChanges();

        //        // Draw
        //        m_Effects[i].Begin();

        //        for(int k = 0; k < m_Effects[i].Techniques.Count; k++)
        //        {
        //            foreach (EffectPass Pass in m_Effects[i].Techniques[k].Passes)
        //            {
        //                Pass.Begin();

        //                foreach (Sim Character in m_CurrentSims)
        //                {
        //                    foreach (Face Fce in Character.HeadMesh.Faces)
        //                    {
        //                        if (Character.HeadMesh.VertexTexNormalPositions != null)
        //                        {
        //                            VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
        //                            Vertex[0] = Character.HeadMesh.VertexTexNormalPositions[Fce.AVertexIndex];
        //                            Vertex[1] = Character.HeadMesh.VertexTexNormalPositions[Fce.BVertexIndex];
        //                            Vertex[2] = Character.HeadMesh.VertexTexNormalPositions[Fce.CVertexIndex];

        //                            Vertex[0].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.AVertexIndex].TextureCoordinate;
        //                            Vertex[1].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.BVertexIndex].TextureCoordinate;
        //                            Vertex[2].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.CVertexIndex].TextureCoordinate;

        //                            m_Scene.SceneMgr.Device.DrawUserPrimitives<VertexPositionNormalTexture>(
        //                                PrimitiveType.TriangleList, Vertex, 0, 1);
        //                        }
        //                    }
        //                }

        //                Pass.End();
        //                m_Effects[i].End();
        //            }
        //        }
        //    }
        //*/

        //    }



        public override void Update(GameTime Time)
        {

            m_Rotation += 0.001f;
            GameFacade.Scenes.WorldMatrix = Matrix.CreateRotationX(m_Rotation);
        }
    }
}
