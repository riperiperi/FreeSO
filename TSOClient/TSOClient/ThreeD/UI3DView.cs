/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TSOClient.VM;
using SimsLib.ThreeD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LogThis;

namespace TSOClient.ThreeD
{
    /// <summary>
    /// Represents a surface for rendering 3D elements (sims) on top of UI elements.
    /// </summary>
    public class UI3DView : ThreeDElement
    {
        private List<BasicEffect> m_Effects;

        private float m_Rotation;

        private List<Sim> m_CurrentSims;

        private int m_Width, m_Height;
        private bool m_SingleRenderer = true;

        private SpriteBatch m_SBatch;

        /// <summary>
        /// Constructs a new UI3DView instance. 
        /// </summary>
        /// <param name="Width">The width of this UI3DView surface.</param>
        /// <param name="Height">The height of this UI3DView surface.</param>
        /// <param name="SingleRenderer">Will this surface be used to render a single, or multiple sims?</param>
        /// <param name="Screen">The ThreeDScene instance with which to create this UI3DView instance.</param>
        /// <param name="StrID">The string ID for this UI3DView instance.</param>
        public UI3DView(int Width, int Height, bool SingleRenderer, ThreeDScene Screen, string StrID)
            : base(Screen)
        {
            m_Effects = new List<BasicEffect>();
            m_Width = Width;
            m_Height = Height;
            m_SingleRenderer = SingleRenderer;

            m_CurrentSims = new List<Sim>();

            m_SBatch = new SpriteBatch(m_Scene.SceneMgr.Device);
            m_Scene.SceneMgr.Device.DeviceReset += new EventHandler(GraphicsDevice_DeviceReset);
        }

        /// <summary>
        /// Occurs when the graphicsdevice was reset, meaning all 3D resources 
        /// have to be recreated.
        /// </summary>
        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            for (int i = 0; i < m_Effects.Count; i++)
                m_Effects[i] = new BasicEffect(m_Scene.SceneMgr.Device, null);

            m_Scene.SceneMgr.Device.VertexDeclaration = new VertexDeclaration(m_Scene.SceneMgr.Device,
                VertexPositionNormalTexture.VertexElements);
            m_Scene.SceneMgr.Device.RenderState.CullMode = CullMode.None;

            // Create camera and projection matrix
            m_Scene.SceneMgr.WorldMatrix = Matrix.Identity;
            m_Scene.SceneMgr.ViewMatrix = Matrix.CreateLookAt(Vector3.Right * 5, Vector3.Zero, Vector3.Down);
            m_Scene.SceneMgr.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)m_Scene.SceneMgr.Device.PresentationParameters.BackBufferWidth /
                    (float)m_Scene.SceneMgr.Device.PresentationParameters.BackBufferHeight, 1.0f, 100.0f);
        }

        /// <summary>
        /// Loads a head mesh.
        /// </summary>
        /// <param name="MeshID">The ID of the mesh to load.</param>
        /// <param name="TexID">The ID of the texture to load.</param>
        public void LoadHeadMesh(Sim Character, Outfit Outf, int SkinColor)
        {
            Appearance Apr;

            switch (SkinColor)
            {
                case 0:
                    Apr = new Appearance(ContentManager.GetResourceFromLongID(Outf.LightAppearanceID));
                    break;
                case 1:
                    Apr = new Appearance(ContentManager.GetResourceFromLongID(Outf.MediumAppearanceID));
                    break;
                case 2:
                    Apr = new Appearance(ContentManager.GetResourceFromLongID(Outf.DarkAppearanceID));
                    break;
                default:
                    Apr = new Appearance(ContentManager.GetResourceFromLongID(Outf.LightAppearanceID));
                    break;
            }

            Binding Bnd = new Binding(ContentManager.GetResourceFromLongID(Apr.BindingIDs[0]));

            if (m_CurrentSims.Count > 0)
            {
                if (!m_SingleRenderer)
                {
                    m_Effects.Add(new BasicEffect(m_Scene.SceneMgr.Device, null));
                    m_CurrentSims.Add(Character);
                    //Skeleton must always be loaded before the mesh.
                    m_CurrentSims[m_CurrentSims.Count - 1].Skel = new Skeleton();
                    m_CurrentSims[m_CurrentSims.Count - 1].Skel.Read(ContentManager.GetResourceFromLongID(0x100000005));
                    m_CurrentSims[m_CurrentSims.Count - 1].HeadMesh = new Mesh();
                    m_CurrentSims[m_CurrentSims.Count - 1].HeadMesh.Read(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID));
                    m_CurrentSims[m_CurrentSims.Count - 1].HeadMesh.ProcessMesh(m_CurrentSims[m_CurrentSims.Count - 1].Skel);

                    m_CurrentSims[m_CurrentSims.Count - 1].HeadTexture = Texture2D.FromFile(m_Scene.SceneMgr.Device,
                        new MemoryStream(ContentManager.GetResourceFromLongID(Bnd.TextureAssetID)));
                }
                else
                {
                    m_Effects[0] = new BasicEffect(m_Scene.SceneMgr.Device, null);
                    //Skeleton must always be loaded before the mesh.
                    m_CurrentSims[0].Skel = new Skeleton();
                    m_CurrentSims[0].Skel.Read(ContentManager.GetResourceFromLongID(0x100000005));
                    m_CurrentSims[0].HeadMesh = new Mesh();
                    m_CurrentSims[0].HeadMesh.Read(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID));
                    m_CurrentSims[0].HeadMesh.ProcessMesh(m_CurrentSims[0].Skel);

                    m_CurrentSims[0].HeadTexture = Texture2D.FromFile(m_Scene.SceneMgr.Device,
                        new MemoryStream(ContentManager.GetResourceFromLongID(Bnd.TextureAssetID)));
                }
            }
            else
            {
                m_Effects.Add(new BasicEffect(m_Scene.SceneMgr.Device, null));
                m_CurrentSims.Add(Character);
                //Skeleton must always be loaded before the mesh.
                m_CurrentSims[0].Skel = new Skeleton();
                m_CurrentSims[0].Skel.Read(ContentManager.GetResourceFromLongID(0x100000005));
                m_CurrentSims[0].HeadMesh = new Mesh();
                m_CurrentSims[0].HeadMesh.Read(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID));
                m_CurrentSims[0].HeadMesh.ProcessMesh(m_CurrentSims[0].Skel);

                m_CurrentSims[0].HeadTexture = Texture2D.FromFile(m_Scene.SceneMgr.Device,
                    new MemoryStream(ContentManager.GetResourceFromLongID(Bnd.TextureAssetID)));
            }
        }

        public override void Update(GameTime GTime)
        {
            m_Rotation += 0.01f;
            m_Scene.SceneMgr.WorldMatrix = Matrix.CreateRotationX(m_Rotation);

            base.Update(GTime);
        }

        public override void Draw()
        {
            base.Draw();

            for(int i = 0; i < m_Effects.Count; i++)
            {
                for(int j = 0; j < m_CurrentSims.Count; j++)
                {
                    if (m_CurrentSims[j].HeadTexture != null)
                    {
                        m_Effects[i].World = m_Scene.SceneMgr.WorldMatrix * 
                            Matrix.CreateTranslation(new Vector3(m_CurrentSims[j].HeadXPos, m_CurrentSims[j].HeadYPos, 0.0f));
                        m_Effects[i].View = Matrix.CreateLookAt(Vector3.Backward * 17, Vector3.Zero, Vector3.Right);
                        m_Effects[i].Projection = m_Scene.SceneMgr.ProjectionMatrix;

                        m_Effects[i].Texture = m_CurrentSims[j].HeadTexture;
                        m_Effects[i].TextureEnabled = true;

                        m_Effects[i].EnableDefaultLighting();

                        m_Effects[i].CommitChanges();

                        // Draw
                        m_Effects[i].Begin();

                        for(int k = 0; k < m_Effects[i].Techniques.Count; k++)
                        {
                            foreach (EffectPass Pass in m_Effects[i].Techniques[k].Passes)
                            {
                                Pass.Begin();

                                foreach (Sim Character in m_CurrentSims)
                                {
                                    foreach (Face Fce in Character.HeadMesh.FaceData)
                                    {
                                        if (Character.HeadMesh.VertexTexNormalPositions != null)
                                        {
                                            VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                                            Vertex[0] = Character.HeadMesh.VertexTexNormalPositions[Fce.VertexA];
                                            Vertex[1] = Character.HeadMesh.VertexTexNormalPositions[Fce.VertexB];
                                            Vertex[2] = Character.HeadMesh.VertexTexNormalPositions[Fce.VertexC];

                                            Vertex[0].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.VertexA].TextureCoordinate;
                                            Vertex[1].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.VertexB].TextureCoordinate;
                                            Vertex[2].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.VertexC].TextureCoordinate;

                                            m_Scene.SceneMgr.Device.DrawUserPrimitives<VertexPositionNormalTexture>(
                                                PrimitiveType.TriangleList, Vertex, 0, 1);
                                        }
                                    }
                                }

                                Pass.End();
                                m_Effects[i].End();
                            }
                        }
                    }
                }
            }
        }
    }
}