/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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

namespace TSOClient.Code.Utils
{
    public class TextureGenerator //a fun class for cpu generating textures
    {
        private static Texture2D PieButtonImg;

        private static Texture2D InteractionInactive;
        private static Texture2D InteractionActive;
        private static Texture2D PieBG;

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
