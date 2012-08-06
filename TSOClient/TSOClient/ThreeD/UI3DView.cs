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
                    m_CurrentSims[m_CurrentSims.Count - 1].HeadMesh = 
                        new Mesh(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID), false);
                    m_CurrentSims[m_CurrentSims.Count - 1].HeadMesh.ProcessMesh();

                    m_CurrentSims[m_CurrentSims.Count - 1].HeadTexture = Texture2D.FromFile(m_Scene.SceneMgr.Device,
                        new MemoryStream(ContentManager.GetResourceFromLongID(Bnd.TextureAssetID)));
                }
                else
                {
                    m_Effects[0] = new BasicEffect(m_Scene.SceneMgr.Device, null);
                    m_CurrentSims[0].HeadMesh = new Mesh(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID), false);
                    m_CurrentSims[0].HeadMesh.ProcessMesh();

                    m_CurrentSims[0].HeadTexture = Texture2D.FromFile(m_Scene.SceneMgr.Device,
                        new MemoryStream(ContentManager.GetResourceFromLongID(Bnd.TextureAssetID)));
                }
            }
            else
            {
                m_Effects.Add(new BasicEffect(m_Scene.SceneMgr.Device, null));
                m_CurrentSims.Add(Character);
                m_CurrentSims[0].HeadMesh = new Mesh(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID), false);
                m_CurrentSims[0].HeadMesh.ProcessMesh();

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
                                    foreach (Face Fce in Character.HeadMesh.Faces)
                                    {
                                        if (Character.HeadMesh.VertexTexNormalPositions != null)
                                        {
                                            VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                                            Vertex[0] = Character.HeadMesh.VertexTexNormalPositions[Fce.AVertexIndex];
                                            Vertex[1] = Character.HeadMesh.VertexTexNormalPositions[Fce.BVertexIndex];
                                            Vertex[2] = Character.HeadMesh.VertexTexNormalPositions[Fce.CVertexIndex];

                                            Vertex[0].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.AVertexIndex].TextureCoordinate;
                                            Vertex[1].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.BVertexIndex].TextureCoordinate;
                                            Vertex[2].TextureCoordinate = Character.HeadMesh.VertexTexNormalPositions[Fce.CVertexIndex].TextureCoordinate;

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

        private RenderTarget2D CreateRenderTarget(GraphicsDevice device, int numberLevels, SurfaceFormat surface)
        {
            MultiSampleType type = device.PresentationParameters.MultiSampleType;

            // If the card can't use the surface format
            if (!GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(
                DeviceType.Hardware,
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format,
                TextureUsage.None,
                QueryUsages.None,
                ResourceType.RenderTarget,
                surface))
            {
                // Fall back to current display format
                surface = device.DisplayMode.Format;
            }
            // Or it can't accept that surface format 
            // with the current AA settings
            else if (!GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(
                DeviceType.Hardware, surface,
                device.PresentationParameters.IsFullScreen, type))
            {
                // Fall back to no antialiasing
                type = MultiSampleType.None;
            }

            /*int width, height;

            // See if we can use our buffer size as our texture
            CheckTextureSize(device.PresentationParameters.BackBufferWidth,
                device.PresentationParameters.BackBufferHeight,
                out width, out height);*/

            // Create our render target
            return new RenderTarget2D(device,
                80, 210, numberLevels, surface,
                type, 0);
        }

        private bool CheckTextureSize(int width, int height, out int newwidth, out int newheight)
        {
            bool retval = false;

            GraphicsDeviceCapabilities Caps;
            Caps = GraphicsAdapter.DefaultAdapter.GetCapabilities(
                DeviceType.Hardware);

            // Check if Device requires Power2 textures 
            if (Caps.TextureCapabilities.RequiresPower2)
            {
                retval = true;  // Return true to indicate the numbers changed 

                // Find the nearest base two log of the current width,  
                // and go up to the next integer                 
                double exp = Math.Ceiling(Math.Log(width) / Math.Log(2));
                // and use that as the exponent of the new width 
                width = (int)Math.Pow(2, exp);
                // Repeat the process for height 
                exp = Math.Ceiling(Math.Log(height) / Math.Log(2));
                height = (int)Math.Pow(2, exp);
            }

            if (Caps.TextureCapabilities.RequiresSquareOnly)
            {
                retval = true;  // Return true to indicate numbers changed 
                width = Math.Max(width, height);
                height = width;
            }

            newwidth = Math.Min(Caps.MaxTextureWidth, width);
            newheight = Math.Min(Caps.MaxTextureHeight, height);
            return retval;
        }

        private DepthStencilBuffer CreateDepthStencil(RenderTarget2D target)
        {
            return new DepthStencilBuffer(target.GraphicsDevice, target.Width,
                target.Height, target.GraphicsDevice.DepthStencilBuffer.Format,
                target.MultiSampleType, target.MultiSampleQuality);
        }

        private DepthStencilBuffer CreateDepthStencil(RenderTarget2D target, DepthFormat depth)
        {
            if (GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch(
                DeviceType.Hardware,
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format,
                target.Format,
                depth))
            {
                return new DepthStencilBuffer(target.GraphicsDevice,
                    target.Width, target.Height, depth,
                    target.MultiSampleType, target.MultiSampleQuality);
            }
            else
                return CreateDepthStencil(target);
        }
    }
}