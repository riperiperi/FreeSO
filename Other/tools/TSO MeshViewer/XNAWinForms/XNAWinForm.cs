using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace XNAWinForms
{
    /// <summary>
    /// Windows form with a XNA renderable panel, based on this ZiggyWare (www.ziggyware.com) tutorial:
    /// http://www.ziggyware.com/readarticle.php?article_id=82
    /// 
    /// Things this implementation includes:
    /// 
    /// * A RefreshMode allows to select a "always" refresh option (see program.cs for details) or a OnPanelPaint 
    ///   option, which will refresh the render each time the panel is painted. You can always call the public Render()
    ///   method manually.
    /// * Inheritable form. Ready to use. See form1 for an example.
    /// * Cleans the viewport´s background color to the Panel Control´s back color
    /// * Implements OnFrameMove, OnFrameRender, OnDeviceResetting and OnDeviceReset events, in a DirectX framework way
    /// 
    /// Author: Iñaki Ayucar (http://graphicdna.blogspot.com)
    /// Date: 14/11/2007
    /// 
    /// This software is distributed "for free" for any non-commercial usage. The software is provided “as-is.” 
    /// You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
    /// </summary>
    public partial class XNAWinForm : Form
    {
        public enum eRefreshMode
        {
            Always,
            OnPanelPaint,        
        }

        private GraphicsDevice mDevice;
        public GraphicsDevice Device
        {
            get { return mDevice; }
        }
        private eRefreshMode mRefreshMode = eRefreshMode.Always;
        public eRefreshMode RefreshMode
        {
            get { return mRefreshMode; }
            set
            {
                mRefreshMode = value;
            }
        }
        private Microsoft.Xna.Framework.Graphics.Color mBackColor = Microsoft.Xna.Framework.Graphics.Color.AliceBlue;

        #region Events
        public delegate void GraphicsDeviceDelegate(GraphicsDevice pDevice);
        public delegate void EmptyEventHandler();
        public event GraphicsDeviceDelegate OnFrameRender = null;
        public event GraphicsDeviceDelegate OnFrameMove = null;
        public event EmptyEventHandler DeviceResetting = null;
        public event GraphicsDeviceDelegate DeviceReset = null;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public XNAWinForm()
        {
            InitializeComponent();

            // Foce color resfresh. (if panel has default backcolor, BackColorChanged won´t be called)
            this.panelViewport_BackColorChanged(null, EventArgs.Empty);
        }

        #region XNA methods
        /// <summary>
        /// Creates the graphics device when form is loaded
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            CreateGraphicsDevice();

            ResetGraphicsDevice();
        }
        /// <summary>
        /// 
        /// </summary>
        private void CreateGraphicsDevice()
        {
            // Create Presentation Parameters
            PresentationParameters pp = new PresentationParameters();
            pp.BackBufferCount = 1;
            pp.IsFullScreen = false;
            pp.SwapEffect = SwapEffect.Discard;
            pp.BackBufferWidth = panelViewport.Width;
            pp.BackBufferHeight = panelViewport.Height;
            pp.AutoDepthStencilFormat = DepthFormat.Depth24Stencil8;
            pp.EnableAutoDepthStencil = true;
            pp.PresentationInterval = PresentInterval.Default;
            pp.BackBufferFormat = SurfaceFormat.Unknown;
            pp.MultiSampleType = MultiSampleType.None;

            // Create device
            mDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter,
                DeviceType.Hardware, this.panelViewport.Handle, pp);
        }
        /// <summary>
        /// Resets the graphics device and calls the disposing and re-creating events
        /// </summary>
        private void ResetGraphicsDevice()
        {       
            // Avoid entering until panelViewport is setup and device created
            if (mDevice== null || panelViewport.Width == 0 || panelViewport.Height == 0)
                return;

            if (this.DeviceResetting != null)
                this.DeviceResetting();

            // Reset device
            mDevice.PresentationParameters.BackBufferWidth = panelViewport.Width;
            mDevice.PresentationParameters.BackBufferHeight = panelViewport.Height;
            mDevice.Reset();

            if (this.DeviceReset != null)
                this.DeviceReset(this.mDevice);
        }  
        /// <summary>
        /// 
        /// </summary>
        public void Render()
        {
            if (this.OnFrameMove != null)
                this.OnFrameMove(this.mDevice);

            mDevice.Clear(this.mBackColor);

            if (this.OnFrameRender != null)
                this.OnFrameRender(this.mDevice);
          
            mDevice.Present();

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnViewportResize(object sender, EventArgs e)
        {
            ResetGraphicsDevice();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVieweportPaint(object sender, PaintEventArgs e)
        {
            if (this.mRefreshMode != eRefreshMode.Always)
                this.Render();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelViewport_BackColorChanged(object sender, EventArgs e)
        {
            this.mBackColor = new Microsoft.Xna.Framework.Graphics.Color(panelViewport.BackColor.R, panelViewport.BackColor.G, panelViewport.BackColor.B);
        }
        #endregion
    }

}