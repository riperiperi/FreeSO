using FSO.Client.GameContent;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.Files;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

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

            string[] splashes = null;

            try
            {
                splashes = File.ReadAllLines("Content/SplashScreens/list.txt");

                // Validate that the listed splash screens exist.
                for (int i = 0; i < splashes.Length; i++)
                {
                    string path = Path.Combine("Content/SplashScreens/", splashes[i]);

                    if (File.Exists(path))
                    {
                        splashes[i] = path;
                    }
                    else
                    {
                        // The list is not valid.
                        splashes = null;
                        break;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // Just attempt to load the regular setup.png.
            }
            catch (DirectoryNotFoundException)
            {
                // Just attempt to load the regular setup.png.
            }

            if (splashes != null && splashes.Length > 0)
            {
                Random rng = new Random();
                using (var logostrm = File.Open(splashes[rng.Next(splashes.Length)], FileMode.Open, FileAccess.Read, FileShare.Read))
                    setupTex = ImageLoader.FromStream(GameFacade.GraphicsDevice, logostrm);
            }
            else if (File.Exists("Content/setup.png")) //Compatible with the previous version
            {
                using (var logostrm = File.Open("Content/setup.png", FileMode.Open, FileAccess.Read, FileShare.Read))
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

            BgEdge = new UIImage(splashSeg).With9Slice(64, 64, 1, 1);
            BackgroundCtnr.AddAt(0, BgEdge);
            BgEdge.Y = -1;
            BgEdge.X = Background.X - 64;
            BgEdge.SetSize(Background.Width + 64 * 2, ScreenHeight + 2);

            Add(BackgroundCtnr);
        }

        private UIImage BgEdge;

        public override void GameResized()
        {
            base.GameResized();

            var ScreenWidth = GlobalSettings.Default.GraphicsWidth;
            var ScreenHeight = GlobalSettings.Default.GraphicsHeight;

            var scale = ScreenHeight / 600.0f;
            var setupTex = Background.Texture;
            BackgroundCtnr.ScaleX = BackgroundCtnr.ScaleY = scale;
            var bgScale = 600f / setupTex.Height;
            Background.SetSize(setupTex.Width * bgScale, 600);
            Background.X = (800 - bgScale * setupTex.Width) / 2;
            BackgroundCtnr.X = (ScreenWidth - (800 * scale)) / 2;

            BgEdge.X = Background.X - 64;
            BgEdge.SetSize(Background.Width + 64 * 2, ScreenHeight + 2);
        }
        
        public override void Draw(UISpriteBatch batch)
        {
            var ScreenWidth = GlobalSettings.Default.GraphicsWidth;
            var ScreenHeight = GlobalSettings.Default.GraphicsHeight;

            DrawLocalTexture(batch, TextureGenerator.GetPxWhite(batch.GraphicsDevice), null, Vector2.Zero, new Vector2(ScreenWidth, ScreenHeight), new Color(0x09, 0x18, 0x2F), 0f);
            base.Draw(batch);
        }

    }
}
