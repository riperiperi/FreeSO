using FSO.Client;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE
{
    public class FSOUIControl : UserControl
    {
        public UIExternalContainer FSOUI;
        private object FrameLock;
        private Bitmap Framebuffer;

        private MouseState mouse;

        public FSOUIControl()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            FrameLock = new object();
            TabStop = true;
        }


        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                RegisterCleanup();
            } 
        }

        private bool RegisteredCleanup;

        public void RegisterCleanup()
        {
            if (!RegisteredCleanup)
            {
                var parent = FindForm();
                if (parent == null) return;
                parent.FormClosing += Cleanup;

                parent.Activated += ParentGotFocus;
                parent.Deactivate += ParentLostFocus;
                RegisteredCleanup = true;
            }
        }

        private void Cleanup(object sender, FormClosingEventArgs e)
        {
            if (FSOUI != null)
            {
                //todo: probably need to be careful we don't remove things in progress
                //also cleanup the batch and stuff
                GameFacade.Screens.RemoveExternal(FSOUI);
            }
        }

        protected virtual void PaintUsingSystemDrawing(Graphics graphics, string text)
        {
            graphics.Clear(System.Drawing.Color.FromArgb(0xD1, 0xD1, 0xC3));

            using (Brush brush = new SolidBrush(System.Drawing.Color.DarkSlateGray))
            {
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;

                    graphics.DrawString(text, Font, brush, ClientRectangle, format);
                }
            }
        }

        public void SetUI(UIExternalContainer ui)
        {
            lock (ui)
            {
                FSOUI = ui;
                FSOUI.Width = Width;
                FSOUI.Height = Height;
                FSOUI.OnFrame += FSOUIFrame;
            }
            RegisterCleanup();
        }

        private void FSOUIFrame()
        {
            lock (FrameLock)
            {
                if (Framebuffer == null || Framebuffer.Width != FSOUI.Width || Framebuffer.Height != FSOUI.Height)
                {
                    Framebuffer = new Bitmap(FSOUI.Width, FSOUI.Height, PixelFormat.Format32bppArgb);
                }

                var bmpData = Framebuffer.LockBits(new Rectangle(0, 0, Framebuffer.Width, Framebuffer.Height), ImageLockMode.WriteOnly, Framebuffer.PixelFormat);
                IntPtr ptr = bmpData.Scan0;

                Marshal.Copy(FSOUI.RawImage, 0, ptr, bmpData.Stride * bmpData.Height);
                Framebuffer.UnlockBits(bmpData);

                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            SetMouseButtonState(e.Button, Microsoft.Xna.Framework.Input.ButtonState.Pressed);
            Focus();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            SetMouseButtonState(e.Button, Microsoft.Xna.Framework.Input.ButtonState.Released);
        }

        protected void ParentLostFocus(object sender, EventArgs e)
        {
            if (FSOUI != null)
            {
                lock (FSOUI)
                {
                    FSOUI.HasFocus = false;
                }
            }
        }

        protected void ParentGotFocus(object sender, EventArgs e)
        {
            if (FSOUI != null)
            {
                lock (FSOUI)
                {
                    FSOUI.HasFocus = true;
                }
            }
        }

        public void ClearMouseState()
        {
            mouse = new MouseState();
        }

        private void SetMouseButtonState(MouseButtons button, Microsoft.Xna.Framework.Input.ButtonState state)
        {
            mouse = new MouseState(mouse.X, mouse.Y, mouse.ScrollWheelValue,
                (button == MouseButtons.Left) ? state : mouse.LeftButton,
                (button == MouseButtons.Middle) ? state : mouse.MiddleButton,
                (button == MouseButtons.Right) ? state : mouse.RightButton,
                (button == MouseButtons.XButton1) ? state : mouse.XButton1,
                (button == MouseButtons.XButton2) ? state : mouse.XButton2
                );
            SubmitMouseState();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouse = new MouseState(e.X, e.Y, mouse.ScrollWheelValue, mouse.LeftButton, mouse.MiddleButton, mouse.RightButton, mouse.XButton1, mouse.XButton2);
            SubmitMouseState();  
        }

        private void SubmitMouseState()
        {
            if (FSOUI != null) {
                lock (FSOUI)
                {
                    FSOUI.mouse = mouse;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
            {
                PaintUsingSystemDrawing(e.Graphics, Text);
            }
            else
            {
                lock (FrameLock)
                {
                    if (Framebuffer != null) e.Graphics.DrawImage(Framebuffer, new Point());
                }
            }
            if (FSOUI != null) FSOUI.NeedFrames = 5;
        }

        protected override void OnResize(EventArgs e)
        {
            if (FSOUI != null)
            {
                lock (FSOUI)
                {
                    FSOUI.Width = Width;
                    FSOUI.Height = Height;
                }
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FSOUIControl
            // 
            this.Name = "FSOUIControl";
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FSOUIControl_KeyPress);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FSOUIControl_MouseDown);
            this.ResumeLayout(false);

        }

        private void FSOUIControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            FSOUI.SubmitKey(e.KeyChar);
        }

        private void FSOUIControl_MouseDown(object sender, MouseEventArgs e)
        {
            Focus();
        }
    }
}
