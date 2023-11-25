using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FSO.Common.Utils
{
    public class FeatureLevelTest
    {
        public static bool UpdateFeatureLevel(GraphicsDevice gd)
        {
            //if 3d is enabled, check if we support non-power-of-two mipmaps
            if (FSOEnvironment.SoftwareKeyboard && FSOEnvironment.SoftwareDepth)
            {
                FSOEnvironment.EnableNPOTMip = false;
                return true;
            }
            try
            {
                using (var mipTest = new Texture2D(gd, 11, 11, true, SurfaceFormat.Color))
                {
                    var data = new Color[11 * 11];
                    TextureUtils.UploadWithMips(mipTest, gd, data);
                }
            }
            catch (Exception e)
            {
                FSOEnvironment.EnableNPOTMip = false;
            }

            try
            {
                using (var mipTest = new Texture2D(gd, 4, 4, true, SurfaceFormat.Dxt5))
                {
                    var data = new byte[16];
                    mipTest.SetData(data);
                }
            }
            catch (Exception e)
            {
                FSOEnvironment.TexCompressSupport = false;
            }

            //msaa test
            try
            {
                var msaaTarg = new RenderTarget2D(gd, 1, 1, false, SurfaceFormat.Color, DepthFormat.None, 8, RenderTargetUsage.PreserveContents);
                gd.SetRenderTarget(msaaTarg);
                gd.Clear(Color.Red);

                var tex = TextureUtils.CopyAccelerated(gd, msaaTarg);

                var result = new Color[1];
                tex.GetData(result);
                FSOEnvironment.MSAASupport = result[0] == Color.Red;
                gd.SetRenderTarget(null);
                msaaTarg.Dispose();
                tex.Dispose();
            }
            catch
            {
                gd.SetRenderTarget(null);
                FSOEnvironment.MSAASupport = false;
            }

            return true;
        }
    }
}
