/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
RHY3756547. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace tso.common.utils
{
    public class TextureGenerator //a fun class for cpu generating textures
    {
        private static Texture2D PieButtonImg;

        private static Texture2D InteractionInactive;
        private static Texture2D InteractionActive;
        private static Texture2D PieBG;
        private static Texture2D[] WallZBuffer;

        public static Texture2D GetPieButtonImg(GraphicsDevice gd)
        {
            if (PieButtonImg == null) PieButtonImg = GenerateRoundedRectangle(gd, new Color(0, 40, 140), 27, 27, 6);
            return PieButtonImg;
        }

        public static Texture2D GetInteractionActive(GraphicsDevice gd)
        {
            if (InteractionActive == null) InteractionActive = GenerateObjectIconBorder(gd, new Color(255, 255, 0), new Color(56, 88, 120));
            return InteractionActive;
        }

        public static Texture2D GetInteractionInactive(GraphicsDevice gd)
        {
            if (InteractionInactive == null) InteractionInactive = GenerateObjectIconBorder(gd, new Color(128, 128, 128), new Color(56, 88, 120));
            return InteractionInactive;
        }

        public static Texture2D GetPieBG(GraphicsDevice gd)
        {
            if (PieBG == null)
            {
                PieBG = new Texture2D(gd, 200, 200);
                Color[] data = new Color[200 * 200];
                int offset = 0;
                for (int y = 0; y < 200; y++)
                {
                    for (int x = 0; x < 200; x++)
                    {
                        data[offset++] = new Color(0, 0, 0, (float)Math.Min(1, 2 - Math.Sqrt(Math.Pow(y - 100, 2) + Math.Pow(x - 100, 2)) / 50) * 0.5f);
                    }
                }
                PieBG.SetData<Color>(data);
            }

            return PieBG;
        }

        public static float[][] WallZBufferConfig = new float[][] {
            // format: width, height, startIntensity, Xdiff, Ydiff

            new float[] {64, 271, 74, 1, 0.5f}, //near top left
            new float[] {64, 271, 135, -1, 0.5f}, //near top right
            new float[] {128, 240, 89.5f, 0, 0.5f}, //near horiz diag
            new float[] {16, 232, 45, 0, 0.5f}, //near vert diag

            new float[] {32, 135, 74, 2, 1f}, //med top left
            new float[] {32, 135, 135, -2, 1f}, //med top right
            new float[] {64, 120, 89.5f, 0, 1f}, //med horiz diag
            new float[] {8, 116, 45, 0, 1f}, //med vert diag

            new float[] {16, 67, 74, 4, 2f}, //far top left
            new float[] {16, 67, 135, -4, 2f}, //far top right
            new float[] {32, 60, 89.5f, 0, 2f}, //far horiz diag
            new float[] {4, 58, 45, 0, 2f}, //far vert diag


            new float[] {128, 64, 255, 0, -1.6f}, //near junction walls up
            new float[] {64, 32, 255, 0, -3.2f}, //med junction walls up
            new float[] {32, 16, 255, 0, -6.4f}, //far junction walls up
        };

        public static Texture2D[] GetWallZBuffer(GraphicsDevice gd)
        {
            if (WallZBuffer == null)
            {
                var count = WallZBufferConfig.Length;
                WallZBuffer = new Texture2D[count];
                for (int i = 0; i < count; i++)
                {
                    var config = WallZBufferConfig[i];
                    int width = (int)config[0];
                    int height = (int)config[1];

                    WallZBuffer[i] = new Texture2D(gd, width, height);
                    Color[] data = new Color[width * height];
                    int offset = 0;

                    float yInt = config[2];
                    for (int y = 0; y < height; y++)
                    {
                        float xInt = yInt;
                        for (int x = 0; x < width; x++)
                        {
                            byte zCol = (byte)Math.Min(255, xInt);
                            data[offset++] = new Color(zCol, zCol, zCol, 255);
                            xInt += config[3];
                        }
                        yInt += config[4];
                    }
                    WallZBuffer[i].SetData<Color>(data);
                }
            }

            return WallZBuffer;
        }

        public static Texture2D GenerateObjectIconBorder(GraphicsDevice gd, Color highlight, Color bg)
        {
            var tex = new Texture2D(gd, 45, 45);
            Color[] data = new Color[45*45];
            tex.GetData<Color>(data);
            var size = new Vector2(45, 45);

            //border
            FillRect(data, size, new Rectangle(3, 0, 39, 2), highlight);
            FillRect(data, size, new Rectangle(0, 3, 2, 39), highlight);
            FillRect(data, size, new Rectangle(3, 43, 39, 2), highlight);
            FillRect(data, size, new Rectangle(43, 3, 2, 39), highlight);
            //end border

            //bg
            FillRect(data, size, new Rectangle(2, 2, 41, 41), bg);
            //end bg

            //top left rounded
            FillRect(data, size, new Rectangle(2, 1, 2, 2), highlight);
            FillRect(data, size, new Rectangle(1, 2, 2, 2), highlight);

            //top right rounded
            FillRect(data, size, new Rectangle(41, 1, 2, 2), highlight);
            FillRect(data, size, new Rectangle(42, 2, 2, 2), highlight);

            //btm left rounded
            FillRect(data, size, new Rectangle(1, 41, 2, 2), highlight);
            FillRect(data, size, new Rectangle(2, 42, 2, 2), highlight);

            //btm right rounded
            FillRect(data, size, new Rectangle(41, 42, 2, 2), highlight);
            FillRect(data, size, new Rectangle(42, 41, 2, 2), highlight);

            tex.SetData<Color>(data);
            return tex;
        }

        public static Texture2D GenerateRoundedRectangle(GraphicsDevice gd, Color color, int width, int height, int radius)
        {
            var tex = new Texture2D(gd, width, height);
            Color[] data = new Color[width * height];
            tex.GetData<Color>(data);
            var size = new Vector2(width, height);

            //rect fills
            FillRect(data, size, new Rectangle(radius, radius, width - radius * 2, height - radius * 2), color);
            FillRect(data, size, new Rectangle(radius, 0, width - radius * 2, radius), color);
            FillRect(data, size, new Rectangle(radius, height-radius, width - radius * 2, radius), color);
            FillRect(data, size, new Rectangle(0, radius, radius, height-radius*2), color);
            FillRect(data, size, new Rectangle(width - radius, radius, radius, height - radius * 2), color);

            //corners now
            for (int i = 0; i < radius; i++)
            {
                int seg = (int)Math.Round(Math.Sin(Math.Acos((radius-(i+0.5))/radius))*radius);
                FillRect(data, size, new Rectangle(radius-seg, i, seg, 1), color);
                FillRect(data, size, new Rectangle(width-radius, i, seg, 1), color);
                FillRect(data, size, new Rectangle(radius - seg, height - i - 1, seg, 1), color);
                FillRect(data, size, new Rectangle(width-radius, height - i - 1, seg, 1), color);
            }

            tex.SetData<Color>(data);
            return tex;
        }

        private static void FillRect(Color[] data, Vector2 texSize, Rectangle dest, Color fillColor)
        {
            int x;
            int y=dest.Y;
            for (int i = 0; i < dest.Height; i++)
            {
                x = dest.X;
                for (int j = 0; j < dest.Width; j++)
                {
                    data[y * (int)texSize.X + x] = fillColor;
                    x++;
                }
                y++;
            }
        }
    }
}
