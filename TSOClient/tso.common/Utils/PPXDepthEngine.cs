using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Common.Utils
{
    public class PPXDepthEngine
    {
        private static GraphicsDevice GD;
        private static RenderTarget2D BackbufferDepth;
        private static RenderTarget2D Backbuffer;
        private static SpriteBatch SB;
        public static int SSAA = 1;
        public static int MSAA = 0;

        public static void InitGD(GraphicsDevice gd)
        {
            GD = gd;
            SB = new SpriteBatch(gd);
        }
        public static void InitScreenTargets()
        {
            if (GD == null) return;
            if (BackbufferDepth != null) BackbufferDepth.Dispose();
            BackbufferDepth = null;
            if (Backbuffer != null) Backbuffer.Dispose();
            var scale = 1;//FSOEnvironment.DPIScaleFactor;
            if (!FSOEnvironment.Enable3D)
                BackbufferDepth = CreateRenderTarget(GD, 1, MSAA, SurfaceFormat.Color, SSAA*GD.Viewport.Width/scale, SSAA * GD.Viewport.Height / scale, DepthFormat.None);
            Backbuffer = CreateRenderTarget(GD, 1, MSAA, SurfaceFormat.Color, SSAA * GD.Viewport.Width / scale, SSAA * GD.Viewport.Height / scale, DepthFormat.Depth24Stencil8);
        }

        private static RenderTarget2D ActiveColor;
        private static RenderTarget2D ActiveDepth;
        private static int StencilValue;

        public static void SetPPXTarget(RenderTarget2D color, RenderTarget2D depth, bool clear)
        {
            SetPPXTarget(color, depth, clear, Color.TransparentBlack);
        }

        public static void SetPPXTarget(RenderTarget2D color, RenderTarget2D depth, bool clear, Color clearColor)
        {
            if (color == null && depth == null && Backbuffer != null) color = Backbuffer;
            ActiveColor = color;
            if (color == Backbuffer && depth == null && BackbufferDepth != null) depth = BackbufferDepth;
            ActiveDepth = depth;

            //if (color != null && depth != null) depth.InheritDepthStencil(color);
            var gd = GD;
            gd.SetRenderTarget(color); //can be null
            if (clear)
            {
                StencilValue = 1;

                gd.Clear(clearColor);// FSO.Common.Rendering.Framework.GameScreen.ClearColor);
                if (depth != null)
                {
                    gd.SetRenderTarget(depth);
                    gd.Clear(Color.White);
                }
            }
            if (FSOEnvironment.UseMRT)
            {
                if (depth != null) gd.SetRenderTargets(color, depth);
            }
        }

        public static RenderTarget2D GetBackbuffer()
        {
            return Backbuffer;
        }

        public delegate void RenderPPXProcedureDelegate(bool depthPass);
        public static void RenderPPXDepth(Effect effect, bool forceDepth,
            RenderPPXProcedureDelegate proc)
        {
            var color = ActiveColor;
            var depth = ActiveDepth;
            var gd = GD;
            if (FSOEnvironment.SoftwareDepth && depth != null)
            {
                var oldDS = gd.DepthStencilState;
                //completely special case.
                gd.SetRenderTarget(color);
                gd.DepthStencilState = new DepthStencilState
                {
                    StencilEnable = true,
                    StencilFunction = CompareFunction.Always,
                    StencilFail = StencilOperation.Keep,
                    StencilPass = StencilOperation.Replace,
                    CounterClockwiseStencilPass = StencilOperation.Replace,
                    StencilDepthBufferFail = StencilOperation.Keep,
                    DepthBufferEnable = forceDepth, //(ActiveColor == null),
                    DepthBufferWriteEnable = forceDepth, //(ActiveColor == null),
                    ReferenceStencil = StencilValue,
                    TwoSidedStencilMode = true
                };
                effect.Parameters["depthMap"].SetValue(depth);
                effect.Parameters["depthOutMode"].SetValue(false);
                proc(false);

                //now draw the depth using the depth test information we got previously.

                //unbind depth map since we are writing to it
                effect.Parameters["depthMap"].SetValue((Texture2D)null);
                effect.Parameters["depthOutMode"].SetValue(true);
                gd.SetRenderTarget(depth);
                gd.DepthStencilState = new DepthStencilState
                {
                    StencilEnable = true,
                    StencilFunction = CompareFunction.Equal,
                    DepthBufferEnable = forceDepth,
                    DepthBufferWriteEnable = forceDepth,
                    ReferenceStencil = StencilValue,
                };
                proc(true);

                gd.DepthStencilState = oldDS;
                StencilValue++; //can increment up to 254 times. Assume we're not going to be rendering that much between clears.
                if (StencilValue > 255) StencilValue = 1;
                gd.SetRenderTarget(color);
                effect.Parameters["depthOutMode"].SetValue(false);
            }
            else if (!FSOEnvironment.UseMRT && depth != null)
            {
                //draw color then draw depth
                gd.SetRenderTarget(color);
                proc(false);
                effect.Parameters["depthOutMode"].SetValue(true);
                gd.SetRenderTarget(depth);
                proc(true);
                effect.Parameters["depthOutMode"].SetValue(false);
            }
            else
            {
                //mrt already bound. draw in both.
                proc(false);
            }
        }

        public static Action<GraphicsDevice, RenderTarget2D> SSAAFunc;
        public static bool WithOpacity = true;

        public static void DrawBackbuffer(float opacity, float scale)
        {
            if (Backbuffer == null) return; //this gfx mode does not use a rendertarget backbuffer
            if (SSAA > 1)
            {
                SSAAFunc(GD, Backbuffer);
            }
            else
            {
                if (!WithOpacity)
                {
                    SB.Begin(blendState: BlendState.Opaque);
                    opacity = 1;
                }
                else
                    SB.Begin(blendState: BlendState.AlphaBlend);
                SB.Draw(Backbuffer, new Vector2(Backbuffer.Width * (1 - scale) / 2, Backbuffer.Height * (1 - scale) / 2), null, Color.White * opacity, 0f, new Vector2(), scale,
                    SpriteEffects.None, 0);
                SB.End();
            }
        }

        public static Point GetWidthHeight()
        {
            return new Point(Backbuffer.Width, Backbuffer.Height);
        }

        public static RenderTarget2D CreateRenderTarget(GraphicsDevice device, int numberLevels, int multisample, SurfaceFormat surface, int width, int height, DepthFormat dformat)
        {
            //apparently in xna4, there is no way to check device format... (it looks for the closest format if desired is not supported) need to look into if this affects anything.

            /*MultiSampleType type = device.PresentationParameters.MultiSampleType;

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
            }*/

            /*int width, height;

            // See if we can use our buffer size as our texture
            CheckTextureSize(device.PresentationParameters.BackBufferWidth,
                device.PresentationParameters.BackBufferHeight,
                out width, out height);*/

            // Create our render target
            return new RenderTarget2D(device,
                width, height, (numberLevels>1), surface,
                DepthFormat.Depth24Stencil8, multisample, RenderTargetUsage.PreserveContents);
        }
    }
}
