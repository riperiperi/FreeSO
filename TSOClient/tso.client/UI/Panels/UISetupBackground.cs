using FSO.Client.GameContent;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.Files;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UISetupBackground : UIContainer
    {
        public UIContainer BackgroundCtnr;
        public UIImage Background;

        public UISetupBackground()
        {
            var ScreenWidth = GlobalSettings.Default.GraphicsWidth;
            var ScreenHeight = GlobalSettings.Default.GraphicsHeight;

            BackgroundCtnr = new UIContainer();
            var scale = ScreenHeight / 600.0f;
            BackgroundCtnr.ScaleX = BackgroundCtnr.ScaleY = scale;

            /** Background image **/
            Texture2D setupTex;
            if (File.Exists("Content/setup.png"))
            {
                using (var logostrm = File.Open("Content/setup.png", FileMode.Open))
                    setupTex = ImageLoader.FromStream(GameFacade.GraphicsDevice, logostrm);
            }
            else setupTex = GetTexture((ulong)FileIDs.UIFileIDs.setup);
            Background = new UIImage(setupTex);
            var bgScale = 600f / setupTex.Height;
            Background.SetSize(setupTex.Width * bgScale, 600);
            Background.X = (800 - bgScale * setupTex.Width) / 2;
            BackgroundCtnr.Add(Background);
            BackgroundCtnr.X = (ScreenWidth - (800 * scale)) / 2;

            Texture2D splashSeg;
            using (var logostrm = File.Open("Content/Textures/splashSeg.png", FileMode.Open, FileAccess.Read, FileShare.Read))
                splashSeg = ImageLoader.FromStream(GameFacade.GraphicsDevice, logostrm);

            var bgEdge = new UIImage(splashSeg).With9Slice(64, 64, 1, 1);
            BackgroundCtnr.AddAt(0, bgEdge);
            bgEdge.Y = -1;
            bgEdge.X = Background.X - 64;
            bgEdge.SetSize(Background.Width + 64 * 2, ScreenHeight + 2);

            Add(BackgroundCtnr);
        }
        public override void Draw(UISpriteBatch batch)
        {
            var ScreenWidth = GlobalSettings.Default.GraphicsWidth;
            var ScreenHeight = GlobalSettings.Default.GraphicsHeight;
            batch.Draw(TextureGenerator.GetPxWhite(batch.GraphicsDevice), new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0x09, 0x18, 0x2F));
            base.Draw(batch);
        }

    }
}
