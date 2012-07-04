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

namespace XNAWinForms
{
    /// <summary>
    /// Windows form that inherits from XNAWinForms and adds the rendering of a simple rotating triangle
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
        private Mesh m_CurrentMesh;
        private Texture2D m_Tex;
        bool m_LoadComplete = false;

        private float mRotation = 0f;
        private Matrix mViewMat, mWorldMat, mProjectionMat;
        private BasicEffect mSimpleEffect;

        private VertexPositionNormalTexture[] m_NormVerticies;

        private SpriteBatch m_SBatch;
        //private Texture2D m_BackgroundTex;

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
                    installDir += "\\TSOClient\\";
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
                if (Pair.Value.Contains(".po") || Pair.Value.Contains(".hag"))
                    LstHeads.Items.Add(Pair.Value);
            }

            LstHeads.SelectedIndexChanged += new EventHandler(LstHeads_SelectedIndexChanged);
            LstAppearances.SelectedIndexChanged += new EventHandler(LstAppearances_SelectedIndexChanged);
        }

        /// <summary>
        /// User clicked on an item in the list containing available appearances.
        /// </summary>
        private void LstAppearances_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_Skeleton == null)
            {
                m_Skeleton = new Skeleton(this.Device, ContentManager.GetResourceFromLongID(0x100000005));
                m_Skeleton.AssignChildren(ref m_Skeleton);
            }

            m_CurrentAppearance = new Appearance(ContentManager.GetResourceFromLongID(
                (ulong)LstAppearances.SelectedItem));

            List<Binding> Bindings = new List<Binding>();

            foreach (ulong BindingID in m_CurrentAppearance.BindingIDs)
                Bindings.Add(new Binding(ContentManager.GetResourceFromLongID(BindingID)));

            m_Tex = Texture2D.FromFile(this.Device, new MemoryStream(
                ContentManager.GetResourceFromLongID(Bindings[0].TextureAssetID)));

            string SelectedStr = (string)LstHeads.SelectedItem;
            if (SelectedStr.Contains("bodies"))
            {
                m_CurrentMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), true);
                m_CurrentMesh.TransformVertices2(m_Skeleton.Bones[0], ref mSimpleEffect);
                m_CurrentMesh.BlendVertices2();
            }
            else
                m_CurrentMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), false);

            LoadMesh(m_CurrentMesh);
        }

        /// <summary>
        /// User clicked on an item in the list containing available heads and bodies.
        /// </summary>
        private void LstHeads_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_Skeleton == null)
            {
                m_Skeleton = new Skeleton(this.Device, ContentManager.GetResourceFromLongID(0x100000005));
                m_Skeleton.AssignChildren(ref m_Skeleton);
            }

            foreach(KeyValuePair<ulong, string> Pair in ContentManager.Resources)
            {
                if ((string)LstHeads.SelectedItem == Pair.Value)
                {
                    //HAndGroup files are used to group together different hand meshes and textures.
                    if (Pair.Value.Contains(".hag"))
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

                        m_Tex = Texture2D.FromFile(this.Device, new MemoryStream(
                            ContentManager.GetResourceFromLongID(Bindings[0].TextureAssetID)));

                        m_CurrentMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), false);
                        LoadMesh(m_CurrentMesh);
                    }
                    else
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

                        m_Tex = Texture2D.FromFile(this.Device, new MemoryStream(
                            ContentManager.GetResourceFromLongID(Bindings[0].TextureAssetID)));

                        //The file selected was most likely a body-mesh, so apply the adult skeleton to it.
                        if (Pair.Value.Contains("bodies"))
                        {
                            m_CurrentMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), true);
                            m_CurrentMesh.TransformVertices2(m_Skeleton.Bones[0], ref mSimpleEffect);
                            m_CurrentMesh.BlendVertices2();
                            LoadMesh(m_CurrentMesh);
                        }
                        else
                        {
                            m_CurrentMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), false);
                            LoadMesh(m_CurrentMesh);
                        }
                    }

                    m_LoadComplete = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pDevice"></param>
        private void Form1_OnFrameMove(Microsoft.Xna.Framework.Graphics.GraphicsDevice pDevice)
        {
            mRotation += 0.05f;
            this.mWorldMat = Matrix.CreateRotationY(mRotation);
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

            // Configure effect
            mSimpleEffect.World = this.mWorldMat;
            mSimpleEffect.View = this.mViewMat;
            mSimpleEffect.Projection = this.mProjectionMat;

            if (m_Tex != null)
            {
                mSimpleEffect.Texture = m_Tex;
                mSimpleEffect.TextureEnabled = true;

                mSimpleEffect.EnableDefaultLighting();
            }

            mSimpleEffect.CommitChanges();

            // Draw
            mSimpleEffect.Begin();
            mSimpleEffect.Techniques[0].Passes[0].Begin();

            if (m_NormVerticies != null)
            {
                if (m_LoadComplete)
                {
                    foreach (Face Fce in m_CurrentMesh.Faces)
                    {
                        VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                        Vertex[0] = m_NormVerticies[Fce.AVertexIndex];
                        Vertex[1] = m_NormVerticies[Fce.BVertexIndex];
                        Vertex[2] = m_NormVerticies[Fce.CVertexIndex];

                        Vertex[0].TextureCoordinate = m_NormVerticies[Fce.AVertexIndex].TextureCoordinate;
                        Vertex[1].TextureCoordinate = m_NormVerticies[Fce.BVertexIndex].TextureCoordinate;
                        Vertex[2].TextureCoordinate = m_NormVerticies[Fce.CVertexIndex].TextureCoordinate;

                        pDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList,
                            Vertex, 0, 1);
                    }
                }
            }

            mSimpleEffect.Techniques[0].Passes[0].End();
            mSimpleEffect.End();
        }

        private DepthStencilBuffer CreateDepthStencil(RenderTarget2D target)
        {
            return new DepthStencilBuffer(target.GraphicsDevice, target.Width,
                target.Height, target.GraphicsDevice.DepthStencilBuffer.Format,
                target.MultiSampleType, target.MultiSampleQuality);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pDevice"></param>
        private void mWinForm_DeviceReset(GraphicsDevice pDevice)
        {
            // Re-Create effect
            mSimpleEffect = new BasicEffect(pDevice, null);

            // Configure device
            pDevice.VertexDeclaration = new VertexDeclaration(pDevice, VertexPositionNormalTexture.VertexElements);
            //Should this be set to another setting?
            pDevice.RenderState.CullMode = CullMode.None;

            // Create camera and projection matrix
            mWorldMat = Matrix.Identity;
            mViewMat = Matrix.CreateLookAt(Vector3.Right * 5,Vector3.Zero, Vector3.Forward);
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
            if (mSimpleEffect != null)
                mSimpleEffect.Dispose();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenFDiag = new OpenFileDialog();
            OpenFDiag.Filter = "Mesh file|*.mesh";
            OpenFDiag.Title = "Select a mesh to open...";

            if (OpenFDiag.ShowDialog() == DialogResult.OK)
            {
                //WARNING: Unable to load body meshes!
                m_CurrentMesh = new Mesh(OpenFDiag.FileName, false);
                LoadMesh(m_CurrentMesh);

                string TextureName = OpenFDiag.FileName.Replace("meshes", "textures").Replace("mesh", "jpg").
                    Replace("fah", "").Replace("fa", "falgt").Replace("-head-head", "");

                m_Tex = Texture2D.FromFile(this.Device, TextureName);
                m_LoadComplete = true;
            }
        }

        private void LoadMesh(Mesh MeshToLoad)
        {
            if (!MeshToLoad.IsBodyMesh)
            {
                m_NormVerticies = new VertexPositionNormalTexture[MeshToLoad.VertexCount];

                for (int i = 0; i < MeshToLoad.VertexCount; i++)
                {
                    m_NormVerticies[i] = new VertexPositionNormalTexture();
                    m_NormVerticies[i].Position.X = MeshToLoad.VertexData[i, 0];
                    m_NormVerticies[i].Position.Y = MeshToLoad.VertexData[i, 1];
                    m_NormVerticies[i].Position.Z = MeshToLoad.VertexData[i, 2];
                    m_NormVerticies[i].Normal.X = MeshToLoad.VertexData[i, 3];
                    m_NormVerticies[i].Normal.Y = MeshToLoad.VertexData[i, 4];
                    m_NormVerticies[i].Normal.Z = MeshToLoad.VertexData[i, 5];


                    //Not really sure why this is important, but I think it has something to do
                    //with being able to see the texture.
                    //m_NormVerticies[i].Normal.Normalize();
                }

                for (int i = 0; i < MeshToLoad.TexVertexCount; i++)
                {
                    m_NormVerticies[i].TextureCoordinate.X = MeshToLoad.TextureVertData[i, 1];
                    m_NormVerticies[i].TextureCoordinate.Y = MeshToLoad.TextureVertData[i, 2];
                }
            }
            else
            {
                m_NormVerticies = new VertexPositionNormalTexture[MeshToLoad.VertexCount];

                for (int i = 0; i < MeshToLoad.VertexCount; i++)
                {
                    m_NormVerticies[i] = new VertexPositionNormalTexture();
                    m_NormVerticies[i].Position.X = MeshToLoad.TransformedVertices[i].Coord.X;
                    m_NormVerticies[i].Position.Y = MeshToLoad.TransformedVertices[i].Coord.Y;
                    m_NormVerticies[i].Position.Z = MeshToLoad.TransformedVertices[i].Coord.Z;
                    m_NormVerticies[i].Normal.X = MeshToLoad.TransformedVertices[i].Normal.X;
                    m_NormVerticies[i].Normal.Y = MeshToLoad.TransformedVertices[i].Normal.Y;
                    m_NormVerticies[i].Normal.Z = MeshToLoad.TransformedVertices[i].Normal.Z;


                    //Not really sure why this is important, but I think it has something to do
                    //with being able to see the texture.
                    //m_NormVerticies[i].Normal.Normalize();
                }

                for (int i = 0; i < MeshToLoad.TexVertexCount; i++)
                {
                    m_NormVerticies[i].TextureCoordinate.X = MeshToLoad.TextureVertData[i, 1];
                    m_NormVerticies[i].TextureCoordinate.Y = MeshToLoad.TextureVertData[i, 2];
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}