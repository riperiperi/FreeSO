using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using TSOClient.LUI;

namespace TSOClient
{
    public class Wall : GameObject
    {
        string myWallName;
        string myWallDesc;
        Texture2D[,] myWallRotsS1;
        Texture2D[,] myWallRotsS2;
        string mySpriteName;
        string myWallPrice;
        bool myIsDiagonal;
        GraphicsDevice myGD;

        public string WallName { get { return myWallName; } }
        public string WallDesc { get { return myWallDesc; } }
        public string SpriteName { get { return mySpriteName; } }
        public string WallPrice { get { return myWallPrice; } }
        public int Width { get
        {
            return GetCurrentTexture().Width;
        } }
        public int Height { get { return GetCurrentTexture().Height; } }

        public override int[] ScreenPosition
        {
            get
            {
                int rot = (Rotation + GlobalRotation * 2) % 8;
                int[] pos = base.ScreenPosition;
                pos[0] += 0;
                pos[1] -= Height;
                if (rot == 0)
                {
                    pos[0] -= Floor.Width / 2;
                }
                if (rot == 1)
                {
                    pos[0] += Floor.Width / 2;
                    //pos[1] += Floor.Height / 2;
                }
                if (rot == 2)
                {
                    pos[0] += Floor.Width;
                }
                if (rot == 3)
                {
                    //pos[0] += Floor.Width / 2;
                    pos[1] -= Floor.Height / 4;
                }
                if (rot == 4)
                {
                    pos[0] += Floor.Width;
                    pos[1] -= Floor.Height/4 + Floor.Height/2;
                }
                if (rot == 5)
                {
                    pos[0] += Floor.Width / 2 + Floor.Width / 4;
                    pos[1] -= Floor.Height / 2;
                }
                if (rot == 6)
                {
                    pos[0] -= Floor.Width / 2;
                    pos[1] -= Floor.Height/2 + Floor.Height/4;
                }
                if (rot == 7)
                {
                    pos[0] -= Floor.Width / 2;
                    pos[1] -= Floor.Height - Floor.Height/4;
                }
                return pos;
            }
        }

        public Texture2D GetCurrentTexture()
        {
            int rot = (Rotation + GlobalRotation * 2) % 8;

            switch (rot)
            {
                case 0:
                    return myWallRotsS1[DrawSize, 1];
                case 1:
                    return myWallRotsS1[DrawSize, 3];
                case 2:
                    return myWallRotsS1[DrawSize, 0];
                case 3:
                    return myWallRotsS1[DrawSize, 2];
                case 4:
                    return myWallRotsS2[DrawSize, 1];
                case 5:
                    return myWallRotsS2[DrawSize, 3];
                case 6:
                    return myWallRotsS2[DrawSize, 0];
                case 7:
                    return myWallRotsS2[DrawSize, 2];
                default:
                    return null;
            }
        }

        public Wall(string name, string price, string desc, Bitmap[,] rotsByLods, GraphicsDevice gd, string spriteName)
        {
            myWallName = name;
            myWallDesc = desc;
            myWallPrice = price;
            mySpriteName = spriteName;
            myWallRotsS1 = new Texture2D[3,4];
            myGD = gd;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    MemoryStream ms = new MemoryStream();
                    Microsoft.Xna.Framework.Graphics.Color[] pixels = new Microsoft.Xna.Framework.Graphics.Color[rotsByLods[i, j].Height * rotsByLods[i, j].Width];
                    for (int k = 0; k < rotsByLods[i, j].Height; k++)
                    {
                        for (int l = 0; l < rotsByLods[i, j].Width; l++)
                        {
                            System.Drawing.Color currentColor = rotsByLods[i, j].GetPixel(l, k);
                            pixels[k * rotsByLods[i, j].Width + l] = new Microsoft.Xna.Framework.Graphics.Color(currentColor.R, currentColor.G, currentColor.B, currentColor.A);
                        }
                    }

                    myWallRotsS1[i, j] = new Texture2D(gd, rotsByLods[i, j].Width, rotsByLods[i, j].Height);
                    myWallRotsS1[i, j].SetData<Microsoft.Xna.Framework.Graphics.Color>(pixels);
                }
            }
            myWallRotsS2 = myWallRotsS1;
        }

        public Wall(string name, string price, string desc, Texture2D[,] side1, Texture2D[,] side2, GraphicsDevice gd, string spriteName)
        {
            myWallName = name;
            myWallDesc = desc;
            myWallPrice = price;
            mySpriteName = spriteName;
            myWallRotsS1 = side1;
            myWallRotsS2 = side2;
            myGD = gd;
        }

        public override void Draw(SpriteBatch SBatch)
        {
            SBatch.Draw(GetCurrentTexture(), new Microsoft.Xna.Framework.Rectangle(ScreenPosition[0] + GameObject.GlobalXTranslation, ScreenPosition[1] + GameObject.GlobalYTranslation, Width, Height), Microsoft.Xna.Framework.Graphics.Color.White);
        }

        public override void DrawForPicking(IsometricView.SetPickingPixel setPixelDelegate, ushort UId)
        {
            Texture2D tex = GetCurrentTexture();
            Microsoft.Xna.Framework.Graphics.Color[] newTex = new Microsoft.Xna.Framework.Graphics.Color[tex.Width * tex.Height];
            tex.GetData<Microsoft.Xna.Framework.Graphics.Color>(newTex);

            int x_pos = ScreenPosition[0] + GameObject.GlobalXTranslation;
            int y_pos = ScreenPosition[1] + GameObject.GlobalYTranslation;

            for (int y = 0; y < tex.Height; y++)
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

        public override GameObject AddToWorld(int x, int y, int rotation)
        {
            Wall wll = new Wall(myWallName, myWallPrice, myWallDesc, myWallRotsS1, myWallRotsS2, myGD, mySpriteName);
            wll.Position = new int[] { x, y };
            wll.Rotation = rotation;
            return wll;
        }

        public override string ToString()
        {
            return myWallName;
        }
    }
}
