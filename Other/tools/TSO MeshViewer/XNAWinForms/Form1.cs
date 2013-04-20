using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using XNA = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Win32;
using LogThis;

namespace Dressup
{
    /// <summary>
    /// Windows form that inherits from Dressup and adds the rendering of a simple rotating triangle
    /// 
    /// Author: Iñaki Ayucar (http://graphicdna.blogspot.com)
    /// Date: 14/11/2007
    /// 
    /// This software is distributed "for free" for any non-commercial usage. The software is provided “as-is.” 
    /// You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
    /// </summary>
    public partial class Form1 : XNAWinForm
    {
        private Outfit m_CurrentOutfit;
        private Appearance m_CurrentAppearance;

        private float m_CurrentFrame = 0.0f;
        private Anim m_CurrentAnim;

        private Sim m_RenderSim;
        private bool m_LoadBodyComplete = false, m_LoadHeadComplete = false;
        //Which type of mesh of mesh is currently selected?
        private bool m_BodySelected = false, m_HeadSelected = false;

        private float m_RotationX = 0.0f, m_RotationY = 0.8f, m_RotationZ = 0.0f;
        private float m_Scale = 0.3f;
        private Matrix mViewMat, mWorldMat, mProjectionMat;
        private BasicEffect m_BodyEffect, m_HeadEffect, m_SkeletonEffect;

        private bool m_RenderSkeleton;

        public Form1()
        {
            InitializeComponent();

            this.DeviceResetting += new XNAWinForm.EmptyEventHandler(mWinForm_DeviceResetting);
            this.DeviceReset += new XNAWinForm.GraphicsDeviceDelegate(mWinForm_DeviceReset);
            this.OnFrameRender += new XNAWinForm.GraphicsDeviceDelegate(mWinForm_OnFrameRender);
            this.OnFrameMove += new GraphicsDeviceDelegate(Form1_OnFrameMove);

            Log.UseSensibleDefaults("Log.txt", "", eloglevel.info);

            mViewMat = mWorldMat = mProjectionMat = Matrix.Identity;

            //Check for the existence of TSO on the user's machine, and get the correct installation-path.
            RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (Array.Exists(softwareKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Maxis") == 0; }))
            {
                RegistryKey maxisKey = softwareKey.OpenSubKey("Maxis");
                if (Array.Exists(maxisKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("The Sims Online") == 0; }))
                {
                    RegistryKey tsoKey = maxisKey.OpenSubKey("The Sims Online");
                    string installDir = (string)tsoKey.GetValue("InstallDir");
                    installDir += "TSOClient\\";
                    GlobalSettings.Default.StartupPath = installDir;
                }
                else
                {
                    MessageBox.Show("Error TSO was not found on your system.");
                    Application.Exit();
                }
            }
            else
            {
                MessageBox.Show("Error: No Maxis products were found on your system.");
                Application.Exit();
            }

            foreach (KeyValuePair<ulong, string> Pair in ContentManager.Resources)
            {
                if (Pair.Value.Contains("bodies\\purchasables"))
                    LstBodies.Items.Add("Body: 0x" + String.Format("{0:X}", Pair.Key));
                else if (Pair.Value.Contains("heads\\purchasables"))
                    LstHeads.Items.Add("Head: 0x" + String.Format("{0:X}", Pair.Key));
                else if (Pair.Value.Contains("hands\\groups"))
                    LstHeads.Items.Add("Hand: 0x" + String.Format("{0:X}", Pair.Key));
                else if(Pair.Value.Contains("animations\\"))
                    LstAnimations.Items.Add("Animation: 0x" + string.Format("{0:X}", Pair.Key));
            }

            LstHeads.SelectedIndexChanged += new EventHandler(LstHeads_SelectedIndexChanged);
            LstAppearances.SelectedIndexChanged += new EventHandler(LstAppearances_SelectedIndexChanged);

            m_RenderSim = new Sim(mWorldMat);
            m_CurrentAnim = new Anim(ContentManager.GetResourceFromLongID(0xd200000007));
        }

        /// <summary>
        /// User clicked on an item in the list containing available appearances.
        /// </summary>
        private void LstAppearances_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_CurrentAppearance = new Appearance(ContentManager.GetResourceFromLongID(
                (ulong)LstAppearances.SelectedItem));

            List<Binding> Bindings = new List<Binding>();

            foreach (ulong BindingID in m_CurrentAppearance.BindingIDs)
                Bindings.Add(new Binding(ContentManager.GetResourceFromLongID(BindingID)));

            if (m_BodySelected)
                m_RenderSim.AddBodyTexture(this.Device, Bindings[0].TextureAssetID);
            else if (m_HeadSelected)
                m_RenderSim.AddHeadTexture(this.Device, Bindings[0].TextureAssetID);
        }

        /// <summary>
        /// User clicked on an item in the list containing available heads.
        /// </summary>
        private void LstHeads_SelectedIndexChanged(object sender, EventArgs e)
        {
            string SelectedStr = (string)LstHeads.SelectedItem;
            string Type = SelectedStr.Split(":".ToCharArray())[0];
            SelectedStr = SelectedStr.Split(":".ToCharArray())[1].Replace(" ", "");

            foreach(KeyValuePair<ulong, string> Pair in ContentManager.Resources)
            {
                //HAndGroup files are used to group together different hand meshes and textures.
                if (Pair.Key == Convert.ToUInt64(SelectedStr, 16) && Type == "Hand")
                {
                    Hag HandGroup = new Hag(ContentManager.GetResourceFromLongID(Pair.Key));

                    m_CurrentAppearance = new Appearance(ContentManager.GetResourceFromLongID(
                        HandGroup.Appearances[0]));

                    LstAppearances.Items.Clear();

                    foreach (ulong AppearanceID in HandGroup.Appearances)
                        LstAppearances.Items.Add(AppearanceID);

                    List<Binding> Bindings = new List<Binding>();

                    foreach (ulong BindingID in m_CurrentAppearance.BindingIDs)
                        Bindings.Add(new Binding(ContentManager.GetResourceFromLongID(BindingID)));

                    m_RenderSim.AddLHandTexture(this.Device, Bindings[0].TextureAssetID);

                    m_RenderSim.AddLHandMesh(Bindings[0].MeshAssetID);
                    break;
                }
                else
                {
                    m_HeadSelected = true;
                    m_BodySelected = false;

                    //Check if the selected hexstring equals a ulong ID in ContentManager.
                    if (Pair.Key == Convert.ToUInt64(SelectedStr, 16))
                    {
                        PurchasableObject PO = new PurchasableObject(ContentManager.GetResourceFromLongID(Pair.Key));

                        m_CurrentOutfit = new Outfit(ContentManager.GetResourceFromLongID(PO.OutfitID));
                        m_CurrentAppearance = new Appearance(
                            ContentManager.GetResourceFromLongID(m_CurrentOutfit.LightAppearanceID));

                        LstAppearances.Items.Clear();
                        LstAppearances.Items.Add(m_CurrentOutfit.LightAppearanceID);
                        LstAppearances.Items.Add(m_CurrentOutfit.MediumAppearanceID);
                        LstAppearances.Items.Add(m_CurrentOutfit.DarkAppearanceID);

                        List<Binding> Bindings = new List<Binding>();

                        foreach (ulong BindingID in m_CurrentAppearance.BindingIDs)
                            Bindings.Add(new Binding(ContentManager.GetResourceFromLongID(BindingID)));

                        m_RenderSim.AddHeadTexture(this.Device, Bindings[0].TextureAssetID);

                        m_RenderSim.AddHeadMesh(Bindings[0].MeshAssetID);
                        break;
                    }
                }
            }

            m_LoadHeadComplete = true;
        }

        /// <summary>
        /// User clicked on an item in the list containing available heads.
        /// </summary>
        private void LstBodies_SelectedIndexChanged(object sender, EventArgs e)
        {
            string SelectedStr = (string)LstBodies.SelectedItem;
            SelectedStr = SelectedStr.Split(":".ToCharArray())[1].Replace(" ", "");

            m_BodySelected = true;
            m_HeadSelected = false;

            foreach (KeyValuePair<ulong, string> Pair in ContentManager.Resources)
            {
                //Check if the selected hexstring equals a ulong ID in ContentManager.
                if (Pair.Key == Convert.ToUInt64(SelectedStr, 16))
                {
                    PurchasableObject PO = new PurchasableObject(ContentManager.GetResourceFromLongID(Pair.Key));

                    m_CurrentOutfit = new Outfit(ContentManager.GetResourceFromLongID(PO.OutfitID));
                    m_CurrentAppearance = new Appearance(
                        ContentManager.GetResourceFromLongID(m_CurrentOutfit.LightAppearanceID));

                    LstAppearances.Items.Clear();
                    LstAppearances.Items.Add(m_CurrentOutfit.LightAppearanceID);
                    LstAppearances.Items.Add(m_CurrentOutfit.MediumAppearanceID);
                    LstAppearances.Items.Add(m_CurrentOutfit.DarkAppearanceID);

                    List<Binding> Bindings = new List<Binding>();

                    foreach (ulong BindingID in m_CurrentAppearance.BindingIDs)
                        Bindings.Add(new Binding(ContentManager.GetResourceFromLongID(BindingID)));

                    m_RenderSim.AddBodyTexture(this.Device, Bindings[0].TextureAssetID);

                    //The file selected was most likely a body-mesh, so apply the adult skeleton to it.
                    if (Pair.Value.Contains("bodies"))
                    {
                        m_RenderSim.AddBodyMesh(Bindings[0].MeshAssetID);
                        break;
                    }
                }
            }

            m_LoadBodyComplete = true;
        }

        /// <summary>
        /// User clicked on an item in the list containing available animations.
        /// </summary>
        private void LstAnimations_SelectedIndexChanged(object sender, EventArgs e)
        {
            string SelectedStr = (string)LstAnimations.SelectedItem;
            SelectedStr = SelectedStr.Split(":".ToCharArray())[1].Replace(" ", "");

            foreach (KeyValuePair<ulong, string> Pair in ContentManager.Resources)
            {
                //Check if the selected hexstring equals a ulong ID in ContentManager.
                if (Pair.Key == Convert.ToUInt64(SelectedStr, 16))
                    m_CurrentAnim = new Anim(ContentManager.GetResourceFromLongID(Pair.Key));
            }
        }

        /// <summary>
        /// Updates the scene.
        /// </summary>
        private void Form1_OnFrameMove(Microsoft.Xna.Framework.Graphics.GraphicsDevice pDevice)
        {
            mWorldMat = Matrix.Identity * Matrix.CreateScale(m_Scale) * Matrix.CreateRotationX(m_RotationX) * Matrix.CreateRotationY(m_RotationY) * Matrix.CreateRotationZ(m_RotationZ);

            if (m_RenderSim.GetBodyMesh() != null && m_RenderSim.GetHeadMesh() != null)
            {
                m_RenderSim.GetBodyMesh().AdvanceFrame(ref m_RenderSim.SimSkeleton, m_CurrentAnim,
                    ref m_CurrentFrame, .02f);
                m_RenderSim.SimSkeleton.ComputeBonePositions(m_RenderSim.SimSkeleton.RootBone, mWorldMat);
            }
        }

        /// <summary>
        /// Draws a skeleton.
        /// </summary>
        /// <param name="Skel">The skeleton to be drawn.</param>
        private void DrawSkeleton(Skeleton Skel)
        {
            m_SkeletonEffect.Begin();
            foreach (var pass in m_SkeletonEffect.Techniques[0].Passes)
            {
                pass.Begin();

                foreach (var bone in Skel.Bones)
                {
                    var color = Microsoft.Xna.Framework.Graphics.Color.Green;

                    if (bone.Name == "ROOT")
                    {
                        color = Microsoft.Xna.Framework.Graphics.Color.Red;
                    }
                    else if (bone.Name == "HEAD")
                    {
                        color = Microsoft.Xna.Framework.Graphics.Color.Yellow;
                    }

                    var vertex = new VertexPositionColor(bone.AbsolutePosition, color);
                    var vertexList = new VertexPositionColor[1] { vertex };
                    this.Device.DrawUserPrimitives(PrimitiveType.PointList, vertexList, 0, 1);
                }

                pass.End();
            }

            m_SkeletonEffect.End();
        }

        /// <summary>
        /// Renders the scene.
        /// </summary>
        private void mWinForm_OnFrameRender(GraphicsDevice pDevice)
        {
            Device.RenderState.DepthBufferEnable = true;
            Device.RenderState.DepthBufferWriteEnable = true;
            Device.RenderState.AlphaBlendEnable = false;
            Device.RenderState.PointSize = 10.0f;

            // Configure effects
            m_HeadEffect.World = this.mWorldMat;
            m_HeadEffect.View = mViewMat;
            m_HeadEffect.Projection = mProjectionMat;

            m_BodyEffect.World = this.mWorldMat;
            m_BodyEffect.View = mViewMat;
            m_BodyEffect.Projection = mProjectionMat;

            m_SkeletonEffect.World = this.mWorldMat;
            m_SkeletonEffect.View = mViewMat;
            m_SkeletonEffect.Projection = mProjectionMat;
            m_SkeletonEffect.EnableDefaultLighting();

            if (m_RenderSim.GetHeadTexture() != null)
            {
                m_HeadEffect.Texture = m_RenderSim.GetHeadTexture();
                m_HeadEffect.TextureEnabled = true;

                m_HeadEffect.EnableDefaultLighting();
            }

            if (m_RenderSim.GetBodyTexture() != null)
            {
                m_BodyEffect.Texture = m_RenderSim.GetBodyTexture();
                m_BodyEffect.TextureEnabled = true;

                m_BodyEffect.EnableDefaultLighting();
            }

            m_HeadEffect.CommitChanges();
            m_BodyEffect.CommitChanges();
            m_SkeletonEffect.CommitChanges();

            if (m_LoadBodyComplete)
            {
                if (!m_RenderSkeleton)
                {
                    foreach (Face Fce in m_RenderSim.GetBodyMesh().FaceData)
                    {
                        // Draw
                        m_BodyEffect.Begin();
                        m_BodyEffect.Techniques[0].Passes[0].Begin();

                        VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                        Vertex[0] = m_RenderSim.GetBodyMesh().VertexTexNormalPositions[Fce.VertexA];
                        Vertex[1] = m_RenderSim.GetBodyMesh().VertexTexNormalPositions[Fce.VertexB];
                        Vertex[2] = m_RenderSim.GetBodyMesh().VertexTexNormalPositions[Fce.VertexC];

                        Vertex[0].TextureCoordinate = m_RenderSim.GetBodyMesh().VertexTexNormalPositions[Fce.VertexA].TextureCoordinate;
                        Vertex[1].TextureCoordinate = m_RenderSim.GetBodyMesh().VertexTexNormalPositions[Fce.VertexB].TextureCoordinate;
                        Vertex[2].TextureCoordinate = m_RenderSim.GetBodyMesh().VertexTexNormalPositions[Fce.VertexC].TextureCoordinate;

                        pDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList,
                            Vertex, 0, 1);

                        m_BodyEffect.Techniques[0].Passes[0].End();
                        m_BodyEffect.End();
                    }

                    m_RenderSim.GetBodyMesh().TransformVertices(m_RenderSim.SimSkeleton.RootBone);
                    m_RenderSim.GetBodyMesh().ProcessMesh(m_RenderSim.SimSkeleton, false);
                }
                else
                {
                    DrawSkeleton(m_RenderSim.SimSkeleton);
                }
            }

            if (m_LoadHeadComplete)
            {
                foreach (Face Fce in m_RenderSim.GetHeadMesh().FaceData)
                {
                    // Draw
                    m_HeadEffect.Begin();
                    m_HeadEffect.Techniques[0].Passes[0].Begin();

                    VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                    Vertex[0] = m_RenderSim.GetHeadMesh().VertexTexNormalPositions[Fce.VertexA];
                    Vertex[1] = m_RenderSim.GetHeadMesh().VertexTexNormalPositions[Fce.VertexB];
                    Vertex[2] = m_RenderSim.GetHeadMesh().VertexTexNormalPositions[Fce.VertexC];

                    Vertex[0].TextureCoordinate = m_RenderSim.GetHeadMesh().VertexTexNormalPositions[Fce.VertexA].TextureCoordinate;
                    Vertex[1].TextureCoordinate = m_RenderSim.GetHeadMesh().VertexTexNormalPositions[Fce.VertexB].TextureCoordinate;
                    Vertex[2].TextureCoordinate = m_RenderSim.GetHeadMesh().VertexTexNormalPositions[Fce.VertexC].TextureCoordinate;

                    pDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList,
                        Vertex, 0, 1);

                    m_HeadEffect.Techniques[0].Passes[0].End();
                    m_HeadEffect.End();
                }

                m_RenderSim.GetHeadMesh().ProcessMesh(m_RenderSim.SimSkeleton, true);
            }
        }

        /// <summary>
        /// Occurs when the scene is invalidated (between update and rendering).
        /// </summary>
        private void mWinForm_DeviceReset(GraphicsDevice pDevice)
        {
            // Re-Create effect
            m_HeadEffect = new BasicEffect(pDevice, null);
            m_BodyEffect = new BasicEffect(pDevice, null);
            m_SkeletonEffect = new BasicEffect(pDevice, null);

            // Configure device
            pDevice.VertexDeclaration = new VertexDeclaration(pDevice, VertexPositionNormalTexture.VertexElements);
            //Should this be set to another setting?
            pDevice.RenderState.CullMode = CullMode.None;

            // Create camera and projection matrix
            mWorldMat = Matrix.Identity;
            mViewMat = Matrix.CreateLookAt(Vector3.Right * 6.0f, Vector3.Zero, Vector3.Forward);
            mProjectionMat = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)pDevice.PresentationParameters.BackBufferWidth / (float)pDevice.PresentationParameters.BackBufferHeight,
                    1.0f, 100.0f);
        }

        /// <summary>
        /// 
        /// </summary>
        private void mWinForm_DeviceResetting()
        {
            // Dispose all
            if (m_HeadEffect != null)
                m_HeadEffect.Dispose();
            if (m_BodyEffect != null)
                m_BodyEffect.Dispose();
            if (m_SkeletonEffect != null)
                m_SkeletonEffect.Dispose();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutTSODressUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Version: 0.6\r\nCode and design by Mats 'Afr0' Vederhus & ddfzcsm\r\nThanks to Don Hopkins, Eric 'Bobo' Bowman & Andrew D'Addesio", "About TSO DressUp");
        }

        /// <summary>
        /// User clicked the button that enables/disables skeleton rendering.
        /// </summary>
        private void BtnSkeleton_Click(object sender, EventArgs e)
        {
            if (m_RenderSkeleton == true)
                m_RenderSkeleton = false;
            else
                m_RenderSkeleton = true;
        }

        private void BtnAnimation_Click(object sender, EventArgs e)
        {
        }
    }
}