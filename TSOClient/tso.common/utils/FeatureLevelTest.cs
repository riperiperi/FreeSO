using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public class FeatureLevelTest
    {
        public static bool UpdateFeatureLevel(GraphicsDevice gd)
        {
            //if 3d is enabled, check if we support non-power-of-two mipmaps
            if (FSOEnvironment.SoftwareKeyboard)
            {
                FSOEnvironment.EnableNPOTMip = false;
                return true;
            }
            if (FSOEnvironment.Enable3D)
            {
                try
                {
                    using (var mipTest = new Texture2D(gd, 11, 11, true, SurfaceFormat.Color))
                    {
                        var data = new Color[11 * 11];
                        TextureUtils.UploadWithMips(mipTest, gd, data);
                    }
                } catch (Exception e)
                {
                    FSOEnvironment.EnableNPOTMip = false;
                }
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

            return true;
        }
    }
}
