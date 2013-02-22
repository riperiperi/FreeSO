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

        private Skeleton m_Skeleton; //Defaults to 'adult.skel' unless we are dealing with a dog or cat.
        private Mesh m_CurrentBodyMesh, m_CurrentHeadMesh, m_CurrentHandMesh;
        private Anim m_CurrentAnim;
        private Texture2D m_BodyTex, m_HeadTex, m_HandTex;
        bool m_LoadBodyComplete = false, m_LoadHeadComplete = false;

        private float mRotation = 0f;
        private Matrix mViewMat, mWorldMat, mProjectionMat;
        private BasicEffect m_BodyEffect, m_HeadEffect;

        private SpriteBatch m_SBatch;
        //private Texture2D m_BackgroundTex;

        private bool m_RenderSkeleton;

        private VertexPositionNormalTexture[] m_SkelPoints;

        /// <summary>
        /// 
        /// </summary>
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

                    //TODO: Let user select path manually...
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
            }

            LstHeads.SelectedIndexChanged += new EventHandler(LstHeads_SelectedIndexChanged);
            LstAppearances.SelectedIndexChanged += new EventHandler(LstAppearances_SelectedIndexChanged);

            m_CurrentAnim = new Anim(ContentManager.GetResourceFromLongID(0xd200000007));
        }

        /// <summary>
        /// User clicked on an item in the list containing available appearances.
        /// </summary>
        private void LstAppearances_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_Skeleton == null)
                m_Skeleton = new Skeleton(this.Device, ContentManager.GetResourceFromLongID(0x100000005), ref mWorldMat);

            m_CurrentAppearance = new Appearance(ContentManager.GetResourceFromLongID(
                (ulong)LstAppearances.SelectedItem));

            List<Binding> Bindings = new List<Binding>();

            foreach (ulong BindingID in m_CurrentAppearance.BindingIDs)
                Bindings.Add(new Binding(ContentManager.GetResourceFromLongID(BindingID)));

            m_BodyTex = Texture2D.FromFile(this.Device, new MemoryStream(
                ContentManager.GetResourceFromLongID(Bindings[0].TextureAssetID)));

            string SelectedHeadStr = (string)LstHeads.SelectedItem;
            string SelectedBodyStr = (string)LstBodies.SelectedItem;

            m_CurrentBodyMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), true);
            m_CurrentBodyMesh.TransformVertices2(m_Skeleton.Bones[0], ref mWorldMat);
            m_CurrentBodyMesh.BlendVertices2();
            m_CurrentBodyMesh.ProcessMesh(m_Skeleton);

            m_CurrentHeadMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), false);
            m_CurrentHeadMesh.ProcessMesh(m_Skeleton);
        }

        /// <summary>
        /// User clicked on an item in the list containing available heads.
        /// </summary>
        private void LstHeads_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_Skeleton == null)
                m_Skeleton = new Skeleton(this.Device, ContentManager.GetResourceFromLongID(0x100000005), ref mWorldMat);

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

                    m_HandTex = Texture2D.FromFile(this.Device, new MemoryStream(
                        ContentManager.GetResourceFromLongID(Bindings[0].TextureAssetID)));

                    m_CurrentHandMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), false);
                    m_CurrentHandMesh.ProcessMesh(m_Skeleton);
                }
                else
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

                        m_HeadTex = Texture2D.FromFile(this.Device, new MemoryStream(
                            ContentManager.GetResourceFromLongID(Bindings[0].TextureAssetID)));

                        m_CurrentHeadMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), false);
                        m_CurrentHeadMesh.ProcessMesh(m_Skeleton);
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
            if (m_Skeleton == null)
                m_Skeleton = new Skeleton(this.Device, ContentManager.GetResourceFromLongID(0x100000005), ref mWorldMat);

            string SelectedStr = (string)LstBodies.SelectedItem;
            string Type = SelectedStr.Split(":".ToCharArray())[0];
            SelectedStr = SelectedStr.Split(":".ToCharArray())[1].Replace(" ", "");

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

                    m_BodyTex = Texture2D.FromFile(this.Device, new MemoryStream(
                        ContentManager.GetResourceFromLongID(Bindings[0].TextureAssetID)));

                    //The file selected was most likely a body-mesh, so apply the adult skeleton to it.
                    if (Pair.Value.Contains("bodies"))
                    {
                        m_CurrentBodyMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), true);
                        m_CurrentBodyMesh.TransformVertices2(m_Skeleton.Bones[0], ref mWorldMat);
                        m_CurrentBodyMesh.BlendVertices2();
                        m_CurrentBodyMesh.ProcessMesh(m_Skeleton);
                    }
                }
            }

            m_LoadBodyComplete = true;
        }

        private void Form1_OnFrameMove(Microsoft.Xna.Framework.Graphics.GraphicsDevice pDevice)
        {
            mRotation += 0.05f;
            this.mWorldMat = Matrix.CreateRotationY(mRotation);
            PopulateSkeletonPoints();
        }

        /// <summary>
        /// Populates a list of vertices (points) that are rendered at the location
        /// of each bone in the skeleton.
        /// </summary>
        private void PopulateSkeletonPoints()
        {
            if (m_Skeleton != null)
            {
                m_SkelPoints = new VertexPositionNormalTexture[m_Skeleton.Bones.Length];

                for (int i = 0; i < m_Skeleton.Bones.Length; i++)
                {
                    if (m_SkelPoints[i] == null)
                    {
                        m_SkelPoints[i] = new VertexPositionNormalTexture(m_Skeleton.Bones[i].GlobalTranslation,
                            Vector3.Forward, Vector2.One);

                        m_SkelPoints[i].Position = Vector3.Transform(m_Skeleton.Bones[i].GlobalTranslation,
                            m_Skeleton.Bones[i].AbsoluteTransform);
                    }

                    if (m_Skeleton.Bones[i].NumChildren == 1)
                    {
                        int ChildIndex = m_Skeleton.Bones[i].Children[0].ID;

                        m_SkelPoints[ChildIndex] = new VertexPositionNormalTexture(m_Skeleton.Bones[i].GlobalTranslation 
                            * m_Skeleton.Bones[ChildIndex].GlobalTranslation, Vector3.Forward, Vector2.One);

                        m_SkelPoints[ChildIndex].Position = Vector3.Transform(m_Skeleton.Bones[ChildIndex].GlobalTranslation,
                            m_Skeleton.Bones[ChildIndex].AbsoluteTransform);
                    }
                    else if (m_Skeleton.Bones[i].NumChildren > 1)
                    {
                        for (int j = 0; j < m_Skeleton.Bones[i].NumChildren; j++)
                        {
                            int ChildIndex = m_Skeleton.Bones[i].Children[j].ID;
                            m_SkelPoints[ChildIndex] = new VertexPositionNormalTexture(m_Skeleton.Bones[i].GlobalTranslation
                                * m_Skeleton.Bones[ChildIndex].GlobalTranslation, Vector3.Forward, Vector2.One);

                            m_SkelPoints[ChildIndex].Position = Vector3.Transform(m_Skeleton.Bones[ChildIndex].GlobalTranslation,
                                m_Skeleton.Bones[ChildIndex].AbsoluteTransform);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pDevice"></param>
        private void mWinForm_OnFrameRender(GraphicsDevice pDevice)
        {
            /*m_SBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.SaveState);

            m_SBatch.Draw(m_BackgroundTex, new Microsoft.Xna.Framework.Rectangle(0, 0, m_BackgroundTex.Width,
                m_BackgroundTex.Height), Microsoft.Xna.Framework.Graphics.Color.White);

            m_SBatch.End();*/

            Device.RenderState.DepthBufferEnable = true;
            Device.RenderState.DepthBufferWriteEnable = true;
            Device.RenderState.AlphaBlendEnable = false;
            Device.RenderState.PointSize = 10.0f;

            // Configure effects
            m_HeadEffect.World = this.mWorldMat;
            m_HeadEffect.View = this.mViewMat;
            m_HeadEffect.Projection = this.mProjectionMat;

            m_BodyEffect.World = this.mWorldMat;
            m_BodyEffect.View = this.mViewMat;
            m_BodyEffect.Projection = this.mProjectionMat;

            if (m_HeadTex != null)
            {
                m_HeadEffect.Texture = m_HeadTex;
                m_HeadEffect.TextureEnabled = true;

                m_HeadEffect.EnableDefaultLighting();
            }

            if (m_BodyTex != null)
            {
                m_BodyEffect.Texture = m_BodyTex;
                m_BodyEffect.TextureEnabled = true;

                m_BodyEffect.EnableDefaultLighting();
            }

            m_HeadEffect.CommitChanges();
            m_BodyEffect.CommitChanges();

            if (m_LoadBodyComplete)
            {
                /*m_CurrentMesh.TransformVertices2(m_Skeleton.Bones[0], ref mWorldMat);
                m_CurrentMesh.BlendVertices2();
                m_CurrentMesh.ProcessMesh();*/

                if (!m_RenderSkeleton)
                {
                    foreach (Face Fce in m_CurrentBodyMesh.Faces)
                    {
                        // Draw
                        m_BodyEffect.Begin();
                        m_BodyEffect.Techniques[0].Passes[0].Begin();

                        VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                        Vertex[0] = m_CurrentBodyMesh.VertexTexNormalPositions[Fce.AVertexIndex];
                        Vertex[1] = m_CurrentBodyMesh.VertexTexNormalPositions[Fce.BVertexIndex];
                        Vertex[2] = m_CurrentBodyMesh.VertexTexNormalPositions[Fce.CVertexIndex];

                        Vertex[0].TextureCoordinate = m_CurrentBodyMesh.VertexTexNormalPositions[Fce.AVertexIndex].TextureCoordinate;
                        Vertex[1].TextureCoordinate = m_CurrentBodyMesh.VertexTexNormalPositions[Fce.BVertexIndex].TextureCoordinate;
                        Vertex[2].TextureCoordinate = m_CurrentBodyMesh.VertexTexNormalPositions[Fce.CVertexIndex].TextureCoordinate;

                        pDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList,
                            Vertex, 0, 1);

                        m_BodyEffect.Techniques[0].Passes[0].End();
                        m_BodyEffect.End();
                    }
                }
                else
                {
                    if (m_SkelPoints != null)
                    {
                        m_BodyEffect.Begin();
                        m_BodyEffect.Techniques[0].Passes[0].Begin();
                        pDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.PointList,
                            m_SkelPoints, 0, m_Skeleton.Bones.Length);
                        m_BodyEffect.Techniques[0].Passes[0].End();
                        m_BodyEffect.End();
                    }
                }
            }

            if (m_LoadHeadComplete)
            {
                foreach (Face Fce in m_CurrentHeadMesh.Faces)
                {
                    // Draw
                    m_HeadEffect.Begin();
                    m_HeadEffect.Techniques[0].Passes[0].Begin();

                    VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                    Vertex[0] = m_CurrentHeadMesh.VertexTexNormalPositions[Fce.AVertexIndex];
                    Vertex[1] = m_CurrentHeadMesh.VertexTexNormalPositions[Fce.BVertexIndex];
                    Vertex[2] = m_CurrentHeadMesh.VertexTexNormalPositions[Fce.CVertexIndex];

                    Vertex[0].TextureCoordinate = m_CurrentHeadMesh.VertexTexNormalPositions[Fce.AVertexIndex].TextureCoordinate;
                    Vertex[1].TextureCoordinate = m_CurrentHeadMesh.VertexTexNormalPositions[Fce.BVertexIndex].TextureCoordinate;
                    Vertex[2].TextureCoordinate = m_CurrentHeadMesh.VertexTexNormalPositions[Fce.CVertexIndex].TextureCoordinate;

                    pDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList,
                        Vertex, 0, 1);

                    m_HeadEffect.Techniques[0].Passes[0].End();
                    m_HeadEffect.End();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pDevice"></param>
        private void mWinForm_DeviceReset(GraphicsDevice pDevice)
        {
            // Re-Create effect
            m_HeadEffect = new BasicEffect(pDevice, null);
            m_BodyEffect = new BasicEffect(pDevice, null);

            // Configure device
            pDevice.VertexDeclaration = new VertexDeclaration(pDevice, VertexPositionNormalTexture.VertexElements);
            //Should this be set to another setting?
            pDevice.RenderState.CullMode = CullMode.None;

            // Create camera and projection matrix
            mWorldMat = Matrix.Identity;
            mViewMat = Matrix.CreateLookAt(Vector3.Right * 5f, Vector3.Zero, Vector3.Forward);
            mProjectionMat = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)pDevice.PresentationParameters.BackBufferWidth / (float)pDevice.PresentationParameters.BackBufferHeight,
                    1.0f, 100.0f);

            m_SBatch = new SpriteBatch(Device);
            //m_BackgroundTex = Texture2D.FromFile(Device, File.Open("sims-online-1.jpg", FileMode.Open));
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
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutTSODressUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Code and design by Mats 'Afr0' Vederhus \r\n Thanks to Don Hopkins & Andrew D'Addesio", "About TSO DressUp");
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
    }
}