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
        private Anim m_CurrentAnim;
        private float m_AnimTime = 0.0f;
        private Texture2D m_Tex;
        bool m_LoadComplete = false;

        private float mRotation = 0f;
        private Matrix mViewMat, mWorldMat, mProjectionMat;
        private BasicEffect mSimpleEffect;

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

            m_CurrentAnim = new Anim(ContentManager.GetResourceFromLongID(0xd200000007));
        }

        /// <summary>
        /// User clicked on an item in the list containing available appearances.
        /// </summary>
        private void LstAppearances_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_Skeleton == null)
                m_Skeleton = new Skeleton(this.Device, ContentManager.GetResourceFromLongID(0x100000005));

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
                m_CurrentMesh.TransformVertices2(m_Skeleton.Bones[0], ref mWorldMat);
                m_CurrentMesh.BlendVertices2();
                m_CurrentMesh.ProcessMesh();
            }
            else
            {
                m_CurrentMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), false);
                m_CurrentMesh.ProcessMesh();
            }
        }

        /// <summary>
        /// User clicked on an item in the list containing available heads and bodies.
        /// </summary>
        private void LstHeads_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_Skeleton == null)
                m_Skeleton = new Skeleton(this.Device, ContentManager.GetResourceFromLongID(0x100000005));

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
                        m_CurrentMesh.ProcessMesh();
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
                            m_CurrentMesh.TransformVertices2(m_Skeleton.Bones[0], ref mWorldMat);
                            m_CurrentMesh.BlendVertices2();
                            m_CurrentMesh.ProcessMesh();
                        }
                        else
                        {
                            m_CurrentMesh = new Mesh(ContentManager.GetResourceFromLongID(Bindings[0].MeshAssetID), false);
                            m_CurrentMesh.ProcessMesh();
                        }
                    }
                }
            }

            m_LoadComplete = true;
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

            if (m_LoadComplete)
            {
                /*m_CurrentMesh.TransformVertices2(m_Skeleton.Bones[0], ref mWorldMat);
                m_CurrentMesh.BlendVertices2();
                m_CurrentMesh.ProcessMesh();*/

                foreach (Face Fce in m_CurrentMesh.Faces)
                {
                    // Draw
                    mSimpleEffect.Begin();
                    mSimpleEffect.Techniques[0].Passes[0].Begin();

                    VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                    Vertex[0] = m_CurrentMesh.VertexTexNormalPositions[Fce.AVertexIndex];
                    Vertex[1] = m_CurrentMesh.VertexTexNormalPositions[Fce.BVertexIndex];
                    Vertex[2] = m_CurrentMesh.VertexTexNormalPositions[Fce.CVertexIndex];

                    Vertex[0].TextureCoordinate = m_CurrentMesh.VertexTexNormalPositions[Fce.AVertexIndex].TextureCoordinate;
                    Vertex[1].TextureCoordinate = m_CurrentMesh.VertexTexNormalPositions[Fce.BVertexIndex].TextureCoordinate;
                    Vertex[2].TextureCoordinate = m_CurrentMesh.VertexTexNormalPositions[Fce.CVertexIndex].TextureCoordinate;

                    pDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList,
                        Vertex, 0, 1);

                    mSimpleEffect.Techniques[0].Passes[0].End();
                    mSimpleEffect.End();
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
            mSimpleEffect = new BasicEffect(pDevice, null);

            // Configure device
            pDevice.VertexDeclaration = new VertexDeclaration(pDevice, VertexPositionNormalTexture.VertexElements);
            //Should this be set to another setting?
            pDevice.RenderState.CullMode = CullMode.None;

            // Create camera and projection matrix
            mWorldMat = Matrix.Identity;
            mViewMat = Matrix.CreateLookAt(Vector3.Right * 20f, Vector3.Zero, Vector3.Forward);
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
                m_CurrentMesh.ProcessMesh();

                string TextureName = OpenFDiag.FileName.Replace("meshes", "textures").Replace("mesh", "jpg").
                    Replace("fah", "").Replace("fa", "falgt").Replace("-head-head", "");

                m_Tex = Texture2D.FromFile(this.Device, TextureName);
                m_LoadComplete = true;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}