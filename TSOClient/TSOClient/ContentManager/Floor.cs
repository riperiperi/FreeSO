using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Microsoft.Xna.Framework;
using SimsLib.IFF;
using System.Runtime.InteropServices;
using TSOClient.LUI;

namespace TSOClient
{
    public class Floor : GameObject
    {
        string myFloorName;
        string myFloorDesc;
        Texture2D[] myFloorLods;
        string mySpriteName;
        string myFloorPrice;
        GraphicsDevice myGD;
        Texture2D myCatalog = null;

        public string FloorName { get { return myFloorName; } }
        public string FloorDesc { get { return myFloorDesc; } }
        public string SpriteName { get { return mySpriteName; } }
        public string FloorPrice { get { return myFloorPrice; } }
        public static int Width { get { switch (DrawSize) { case 0: return 16; case 1: return 32; case 2: return 64; default: return 0; } } }
        public static int Height { get { switch (DrawSize) { case 0: return 31; case 1: return 63; case 2: return 127; default: return 0; } } }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        public Texture2D CatalogImage
        {
            get
            {
                if (myCatalog == null)
                {
                    lock (LuaInterfaceManager.LuaVM)
                    {
                        RenderTarget2D preserve = (RenderTarget2D)myGD.GetRenderTarget(0);

                        SpriteBatch sb = new SpriteBatch(myGD);

                        RenderTarget2D rt = new RenderTarget2D(myGD, 180, 45, 0, SurfaceFormat.Color, RenderTargetUsage.DiscardContents);

                        myGD.Clear(new Microsoft.Xna.Framework.Graphics.Color(255, 255, 255, 0));
                        myGD.SetRenderTarget(0, rt);
                        sb.Begin(SpriteBlendMode.None);
                        sb.Draw(new Texture2D(myGD, 1, 1, 0, TextureUsage.None, SurfaceFormat.Color), new Microsoft.Xna.Framework.Rectangle(0, 0, 180, 45), Microsoft.Xna.Framework.Graphics.Color.TransparentWhite);
                        sb.End();
                        sb.Begin(SpriteBlendMode.AlphaBlend);
                        Texture2D baseTex = Texture2D.FromFile(myGD, new MemoryStream(ContentManager.GetResourceFromLongID(0x24700000001)));
                        UIScreen.ManualTextureMask(ref baseTex, new Microsoft.Xna.Framework.Graphics.Color(255, 0, 255));
                        sb.Draw(baseTex, new Vector2(0, 0), Microsoft.Xna.Framework.Graphics.Color.White);
                        sb.Draw(myFloorLods[1], new Microsoft.Xna.Framework.Rectangle(4, 13, 37, 20), Microsoft.Xna.Framework.Graphics.Color.White);
                        sb.Draw(myFloorLods[1], new Microsoft.Xna.Framework.Rectangle(49, 13, 37, 20), Microsoft.Xna.Framework.Graphics.Color.White);
                        sb.Draw(myFloorLods[1], new Microsoft.Xna.Framework.Rectangle(94, 13, 37, 20), Microsoft.Xna.Framework.Graphics.Color.White);
                        sb.Draw(myFloorLods[1], new Microsoft.Xna.Framework.Rectangle(139, 13, 37, 20), Microsoft.Xna.Framework.Graphics.Color.White);

                        sb.End();

                        myGD.SetRenderTarget(0, null);

                        myCatalog = rt.GetTexture();
                        //myCatalog.Save(@"C:\Users\Nicholas\floorCatalogImage.bmp", ImageFileFormat.Bmp);
                        myGD.SetRenderTarget(0, preserve);
                    }
                }

                return myCatalog;
            }
        }

        public override int[] ScreenPosition
        {
            get
            {
                int[] pos = base.ScreenPosition;
                switch (GlobalRotation)
                {
                    case 0:
                    pos[0] -= Width/2;
                    pos[1] += Height/2 - Height;
                break;
                    case 2:
                    pos[0] += 0;
                    pos[1] -= Height;
                break;
                    case 1:
                    pos[0] -= (Width / 2 - Width) + Width/2;
                    pos[1] -= Height/2;
                break;
                    case 3:
                    pos[0] -= Width - Width/2;
                    pos[1] -= Height;
                break;
            }
                return pos;
            }
        }

        public Floor(string name, string price, string desc, Bitmap[] lods, GraphicsDevice gd, string spriteName)
        {
            myFloorName = name;
            myFloorDesc = desc;
            myFloorPrice = price;
            mySpriteName = spriteName;
            myFloorLods = new Texture2D[3];
            myGD = gd;

            for (int i = 0; i < 3; i++)
            {
                MemoryStream ms = new MemoryStream();
                Microsoft.Xna.Framework.Graphics.Color[] pixels = new Microsoft.Xna.Framework.Graphics.Color[lods[i].Height * lods[i].Width];
                for (int j = 0; j < lods[i].Height; j++)
                {
                    for (int k = 0; k < lods[i].Width; k++)
                    {
                        System.Drawing.Color currentColor = lods[i].GetPixel(k, j);
                        pixels[j * lods[i].Width + k] = new Microsoft.Xna.Framework.Graphics.Color(currentColor.R, currentColor.G, currentColor.B, currentColor.A);
                    }
                }

                myFloorLods[i] = new Texture2D(gd, lods[i].Width, lods[i].Height);
                myFloorLods[i].SetData<Microsoft.Xna.Framework.Graphics.Color>(pixels);
            }
            Texture2D dummy = CatalogImage;
        }

        public Floor(string name, string price, string desc, Texture2D[] lods, GraphicsDevice gd, string spriteName)
        {
            myFloorName = name;
            myFloorDesc = desc;
            myFloorPrice = price;
            mySpriteName = spriteName;
            myFloorLods = new Texture2D[3];
            myGD = gd;
            myFloorLods = lods;
        }

        public override void Draw(SpriteBatch SBatch)
        {
            SBatch.Draw(myFloorLods[DrawSize], new Microsoft.Xna.Framework.Rectangle(ScreenPosition[0] + GameObject.GlobalXTranslation, ScreenPosition[1] + GameObject.GlobalYTranslation, myFloorLods[DrawSize].Width, myFloorLods[DrawSize].Height)
                , new Microsoft.Xna.Framework.Rectangle(0, 0, myFloorLods[DrawSize].Width, myFloorLods[DrawSize].Height), Microsoft.Xna.Framework.Graphics.Color.White, 0.0f, new Vector2(myFloorLods[DrawSize].Width/2, myFloorLods[DrawSize].Height/2), (GameObject.GlobalRotation == 0) ? SpriteEffects.None : (GameObject.GlobalRotation == 1) ? SpriteEffects.FlipHorizontally : (GameObject.GlobalRotation == 2) ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0.0f);
        }

        public static Texture2D ManualTextureMask(Texture2D Texture, Microsoft.Xna.Framework.Graphics.Color AlphaColor, Microsoft.Xna.Framework.Graphics.Color NonAlphaColor)
        {
            Microsoft.Xna.Framework.Graphics.Color[] data = new Microsoft.Xna.Framework.Graphics.Color[Texture.Width * Texture.Height];
            Texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                data[i].A = 255;
                if (data[i].A != 0)
                    data[i] = NonAlphaColor;
                else
                    data[i] = AlphaColor;
            }

            Texture2D ret = new Texture2D(Texture.GraphicsDevice, Texture.Width, Texture.Height);

            ret.SetData(data);

            return ret;
        }

        public override void DrawForPicking(IsometricView.SetPickingPixel setPixelDelegate, ushort UId)
        {
            int x_pos = ScreenPosition[0] + GameObject.GlobalXTranslation;
            int y_pos = ScreenPosition[1] + GameObject.GlobalYTranslation;

            x_pos -= Width;
            y_pos -= Height / 4;

            if (x_pos + Width < 0 || x_pos > 800 || y_pos + Height > 600 || y_pos < 0)
                return;

            Texture2D tex = myFloorLods[DrawSize];
            Microsoft.Xna.Framework.Graphics.Color[] newTex = new Microsoft.Xna.Framework.Graphics.Color[tex.Width * tex.Height];
            tex.GetData<Microsoft.Xna.Framework.Graphics.Color>(newTex);

            

            for (int y = tex.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < tex.Width; x++)
                {
                    int i = tex.Width * y + x;

                    if (x_pos + x < 800 && y_pos + y < 600 && x_pos + x >= 0 && y_pos + y >= 0)
                    {
                        if (newTex[i].A != 0)
                        {
                            Microsoft.Xna.Framework.Graphics.Color c = new Microsoft.Xna.Framework.Graphics.Color((byte)myPosition[0], (byte)myPosition[1], (byte)(UId & 0xFF), (byte)(UId >> 8));
                            setPixelDelegate(c, x_pos + x, y_pos + y);
                        }
                        else
                        {
                            Microsoft.Xna.Framework.Graphics.Color c = new Microsoft.Xna.Framework.Graphics.Color(255, 255, 0, 0);
                            setPixelDelegate(c, x_pos + x, y_pos + y);
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return myFloorName;
        }

        public override GameObject AddToWorld(int x, int y, int rotation)
        {
            Floor f = new Floor(myFloorName, myFloorPrice, myFloorDesc, myFloorLods, myGD, mySpriteName);
            f.Position[0] = x;
            f.Position[1] = y;
            f.Rotation = rotation;
            return f;
        }
    }
}
