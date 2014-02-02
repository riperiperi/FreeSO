/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

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
    public class TextureUtils
    {
        //public static Texture2D FromSpriteFrame(GraphicsDevice gd, SpriteFrame frame)
        //{
        //    var tex = new Texture2D(gd, frame.Width, frame.Height, 1, TextureUsage.None, SurfaceFormat.Color);

        //    var colors = new Color[frame.Width * frame.Height];
        //    var rawData = frame.BitmapData.GetRawData();

        //    var i = 0;
        //    var colorIndex = 0;
        //    var isAlpha = frame.BitmapData.IsAlphaBitmap;
        //    if (isAlpha)
        //    {
        //        while (i < rawData.Length)
        //        {
        //            colors[colorIndex] = new Color(rawData[i + 2], rawData[i + 1], rawData[i], rawData[i + 3]);
        //            i += 4;
        //            colorIndex++;
        //        }
        //    }
        //    else
        //    {
        //        //var transparentPixel = new Color(frame.TransparentPixel.R, frame.TransparentPixel.G, frame.TransparentPixel.B);
        //        //transparentPixel = Color.TransparentBlack;
        //        var tp = frame.TransparentPixel;
        //        tp = System.Drawing.Color.Black;
        //        while (i < rawData.Length)
        //        {
        //            var c = new Color(rawData[i + 2], rawData[i + 1], rawData[i]);
        //            //if (c == transparentPixel) { c.A = 0; }
        //            if (c.R == tp.R && c.G == tp.G && c.B == tp.B)
        //            {
        //                c.A = 0;
        //            }

        //            colors[colorIndex] = c;
        //            i += 3;
        //            colorIndex++;
        //        }
        //    }

        //    tex.SetData<Color>(colors);

        //    return tex;
        //}

        //public static Texture2D FromFastPixels(GraphicsDevice gd, FastPixel frame)
        //{
        //    var tex = new Texture2D(gd, frame.Width, frame.Height, 1, TextureUsage.None, SurfaceFormat.Color);

        //    var colors = new Color[frame.Width * frame.Height];
        //    var rawData = frame.GetRawData();

        //    var i = 0;
        //    var colorIndex = 0;
        //    var isAlpha = frame.IsAlphaBitmap;
        //    if (isAlpha)
        //    {
        //        while (i < rawData.Length)
        //        {
        //            colors[colorIndex] = new Color(rawData[i + 2], rawData[i + 1], rawData[i], rawData[i + 3]);
        //            i += 4;
        //            colorIndex++;
        //        }
        //    }
        //    else
        //    {
        //        //var transparentPixel = new Color(frame.TransparentPixel.R, frame.TransparentPixel.G, frame.TransparentPixel.B);
        //        //transparentPixel = Color.TransparentBlack;
        //        //var tp = frame.TransparentPixel;
        //        var tp = System.Drawing.Color.Black;
        //        while (i < rawData.Length)
        //        {
        //            var c = new Color(rawData[i + 2], rawData[i + 1], rawData[i]);
        //            //if (c == transparentPixel) { c.A = 0; }
        //            if (c.R == tp.R && c.G == tp.G && c.B == tp.B)
        //            {
        //                c.A = 0;
        //            }

        //            colors[colorIndex] = c;
        //            i += 3;
        //            colorIndex++;
        //        }
        //    }

        //    tex.SetData<Color>(colors);

        //    return tex;
        //}



        public static Texture2D TextureFromColor(GraphicsDevice gd, Color color)
        {
            var tex = new Texture2D(gd, 1, 1);
            tex.SetData(new[] { color });
            return tex;
        }

        public static Texture2D TextureFromColor(GraphicsDevice gd, Color color, int width, int height)
        {
            var tex = new Texture2D(gd, width, height);
            var data = new Color[width * height];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = color;
            }
            tex.SetData(data);
            return tex;
        }



        /**
         * Because the buffers can be fairly big, its much quicker to just keep some
         * in memory and reuse them for resampleing textures
         */
        private static List<uint[]> ResampleBuffers = new List<uint[]>();
        private static ulong MaxResampleBufferSize = 1024 * 768;

        static TextureUtils()
        {
            for (var i = 0; i < 10; i++)
            {
                ResampleBuffers.Add(new uint[MaxResampleBufferSize]);
            }
        }

        private static uint[] GetBuffer()
        {
            lock (ResampleBuffers)
            {
                //if (ResampleBuffers.Count > 0)
                if (ResampleBuffers.Count > 0)
                {
                    var result = ResampleBuffers[0];
                    ResampleBuffers.RemoveAt(0);
                    return result;
                }
                else
                {
                    var newBuffer = new uint[MaxResampleBufferSize];
                    return newBuffer;
                }
            }
        }

        private static void FreeBuffer(uint[] buffer)
        {
            lock (ResampleBuffers)
            {
                ResampleBuffers.Add(buffer);
            }
        }

        public static void Trim(ref Texture2D Texture, ref Vector2 offset, bool dispose){
            var size = Texture.Width * Texture.Height;
            uint[] buffer = GetBuffer();
            Texture.GetData(buffer, 0, size);

            /**
             * Work out where the pixel edges are
             */
            var minX = Texture.Width;
            var minY = Texture.Height;
            var maxX = 0;
            var maxY = 0;

            for (var x = 0; x < Texture.Width; x++){
                for (var y = 0; y < Texture.Height; y++){
                    var color = buffer[(y * Texture.Width) + x];
                    var isEmpty = color >> 24 == 0;
                    if (!isEmpty){
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            if (minX > 0 || minY > 0 || maxX < Texture.Width || maxY < Texture.Height){
                /** We can trim it :) **/
                var width = (maxX - minX)+1;
                var height = (maxY - minY)+1;
                if(width <= 0 || height <= 0){
                    /** Empty **/
                    if (dispose){
                        Texture.Dispose();
                    }
                    Texture = null;
                    FreeBuffer(buffer);
                    return;
                }

                var newTexture = new Texture2D(Texture.GraphicsDevice, width, height, Texture.LevelCount, Texture.TextureUsage, Texture.Format);
                var newTextureData = GetBuffer();

                for (var x = minX; x <= maxX; x++){
                    for (var y = minY; y <= maxY; y++){
                        var newX = x - minX;
                        var newY = y - minY;
                        newTextureData[(newY * width) + newX] = buffer[(y * Texture.Width) + x];
                    }
                }
                newTexture.SetData(newTextureData, 0, width * height, SetDataOptions.None);

                FreeBuffer(newTextureData);

                if (dispose){
                    Texture.Dispose();
                }
                Texture = newTexture;
                offset = new Vector2(minX, minY);
            }
            FreeBuffer(buffer);
        }

        public static void MaskFromTexture(ref Texture2D Texture, Texture2D Mask, uint[] ColorsFrom)
        {
            if (Texture.Width != Mask.Width || Texture.Height != Mask.Height)
            {
                return;
            }


            var ColorTo = Color.TransparentBlack.PackedValue;

            var size = Texture.Width * Texture.Height;
            uint[] buffer = GetBuffer();
            Texture.GetData(buffer, 0, size);

            var sizeMask = Mask.Width * Mask.Height;
            var bufferMask = GetBuffer();
            Mask.GetData(bufferMask, 0, sizeMask);

            var didChange = false;
            for (int i = 0; i < size; i++)
            {
                if (ColorsFrom.Contains(bufferMask[i]))
                {
                    didChange = true;
                    buffer[i] = ColorTo;
                }
            }

            if (didChange)
            {
                Texture.SetData(buffer, 0, size, SetDataOptions.None);
            }
            FreeBuffer(buffer);
            FreeBuffer(bufferMask);
        }

        public static Texture2D Copy(GraphicsDevice device, Texture2D texture)
        {
            var newTexture = new Texture2D(device, texture.Width, texture.Height);

            var size = texture.Width * texture.Height;
            uint[] buffer = GetBuffer();
            texture.GetData(buffer, 0, size);

            newTexture.SetData(buffer, 0, size, SetDataOptions.None);
            FreeBuffer(buffer);
            return newTexture;
        }


        public static void CopyAlpha(ref Texture2D TextureTo, Texture2D TextureFrom)
        {
            if (TextureTo.Width != TextureFrom.Width || TextureTo.Height != TextureFrom.Height)
            {
                return;
            }


            var size = TextureTo.Width * TextureTo.Height;
            uint[] buffer = GetBuffer();
            TextureTo.GetData(buffer, 0, size);

            var sizeFrom = TextureFrom.Width * TextureFrom.Height;
            var bufferFrom = GetBuffer();
            TextureFrom.GetData(bufferFrom, 0, sizeFrom);

            for (int i = 0; i < size; i++)
            {
                //ARGB
                buffer[i] = (buffer[i] & 0x00FFFFFF) | (bufferFrom[i] & 0xFF000000);
            }

            TextureTo.SetData(buffer, 0, size, SetDataOptions.None);

            FreeBuffer(buffer);
            FreeBuffer(bufferFrom);
        }


        /// <summary>
        /// Manually replaces a specified color in a texture with transparent black,
        /// thereby masking it.
        /// </summary>
        /// <param name="Texture">The texture on which to apply the mask.</param>
        /// <param name="ColorFrom">The color to mask away.</param>
        public static void ManualTextureMask(ref Texture2D Texture, uint[] ColorsFrom)
        {
            var ColorTo = Color.TransparentBlack.PackedValue;

            //lock (TEXTURE_MASK_BUFFER)
            //{
                
                var size = Texture.Width * Texture.Height;
                uint[] buffer = GetBuffer();
                //uint[] buffer = new uint[size];

                //var buffer = TEXTURE_MASK_BUFFER;
                Texture.GetData(buffer, 0, size);

                var didChange = false;

                for (int i = 0; i < size; i++)
                {
                    if (ColorsFrom.Contains(buffer[i]))
                    {
                        didChange = true;
                        buffer[i] = ColorTo;
                    }
                }


                //Texture = new Texture2D(Texture.GraphicsDevice, Texture.Width, Texture.Height, Texture.LevelCount, Texture.TextureUsage, SurfaceFormat.Color);

                /*
                if (Texture.Format != SurfaceFormat.Color)
                    Texture = new Texture2D(Texture.GraphicsDevice, Texture.Width, Texture.Height, 4, TextureUsage.Linear, SurfaceFormat.Color);
                */
                if (didChange)
                {
                    Texture.SetData(buffer, 0, size, SetDataOptions.None);
                }
                FreeBuffer(buffer);
            //}
        }


        private static uint[] SINGLE_THREADED_TEXTURE_BUFFER = new uint[MaxResampleBufferSize];
        public static void ManualTextureMaskSingleThreaded(ref Texture2D Texture, uint[] ColorsFrom)
        {
            var ColorTo = Color.TransparentBlack.PackedValue;

            //lock (TEXTURE_MASK_BUFFER)
            //{

            var size = Texture.Width * Texture.Height;
            uint[] buffer = SINGLE_THREADED_TEXTURE_BUFFER;
            //uint[] buffer = new uint[size];

            //var buffer = TEXTURE_MASK_BUFFER;
            Texture.GetData(buffer, 0, size);

            var didChange = false;

            for (int i = 0; i < size; i++)
            {
                if (ColorsFrom.Contains(buffer[i]))
                {
                    didChange = true;
                    buffer[i] = ColorTo;
                }
            }


            //Texture = new Texture2D(Texture.GraphicsDevice, Texture.Width, Texture.Height, Texture.LevelCount, Texture.TextureUsage, SurfaceFormat.Color);

            /*
            if (Texture.Format != SurfaceFormat.Color)
                Texture = new Texture2D(Texture.GraphicsDevice, Texture.Width, Texture.Height, 4, TextureUsage.Linear, SurfaceFormat.Color);
            */
            if (didChange)
            {
                Texture.SetData(buffer, 0, size, SetDataOptions.None);
            }
            FreeBuffer(buffer);
            //}
        }

        /// <summary>
        /// Combines multiple textures into a single texture
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="textures"></param>
        /// <returns></returns>
        public static Texture2D MergeHorizontal(GraphicsDevice gd, params Texture2D[] textures)
        {
            return MergeHorizontal(gd, 0, textures);
        }

        /// <summary>
        /// Combines multiple textures into a single texture
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="textures"></param>
        /// <returns></returns>
        public static Texture2D MergeHorizontal(GraphicsDevice gd, int tailPx, params Texture2D[] textures)
        {
            var width = 0;
            var maxHeight = 0;
            var maxWidth = 0;

            foreach (var texture in textures)
            {
                width += texture.Width;
                maxHeight = Math.Max(maxHeight, texture.Height);
                maxWidth = Math.Max(maxWidth, texture.Width);
            }

            width += tailPx;

            Texture2D newTexture = new Texture2D(gd, width, maxHeight);
            Color[] newTextureData = new Color[width * maxHeight];
            Color[] tempTexData = new Color[maxWidth * maxHeight];

            var xo = 0;
            for (var i = 0; i < textures.Length; i++)
            {
                var tx = textures[i];
                tx.GetData<Color>(tempTexData);
                for (var y = 0; y < tx.Height; y++)
                {
                    var yOffset = y * width;

                    for (var x = 0; x < tx.Width; x++)
                    {
                        newTextureData[yOffset + xo + x] = tempTexData[tx.Width * y + x];
                    }
                }
                xo += tx.Width;
            }

            newTexture.SetData(newTextureData);
            tempTexData = null;

            return newTexture;
        }


        public static Texture2D Resize(GraphicsDevice gd, Texture2D texture, int newWidth, int newHeight)
        {
            RenderTarget2D renderTarget = new RenderTarget2D(
                gd,
                newWidth, newHeight, 1,
                SurfaceFormat.Color);


            SpriteBatch batch = new SpriteBatch(gd);

            Rectangle destinationRectangle = new Rectangle(0, 0, newWidth, newHeight);
            lock (gd)
            {
                gd.SetRenderTarget(0, renderTarget);
                batch.Begin();
                batch.Draw(texture, destinationRectangle, Color.White);
                batch.End();
                gd.SetRenderTarget(0, null);
            }
            var newTexture = renderTarget.GetTexture();
            return newTexture;
        }

        public static Texture2D Scale(GraphicsDevice gd, Texture2D texture, float scaleX, float scaleY)
        {
            var newWidth = (int)(texture.Width * scaleX);
            var newHeight = (int)(texture.Height * scaleY);

            RenderTarget2D renderTarget = new RenderTarget2D(
                gd,
                newWidth, newHeight, 1,
                SurfaceFormat.Color);

            gd.SetRenderTarget(0, renderTarget);

            SpriteBatch batch = new SpriteBatch(gd);

            Rectangle destinationRectangle = new Rectangle(0, 0, newWidth, newHeight);

            batch.Begin();
            batch.Draw(texture, destinationRectangle, Color.White);
            batch.End();

            gd.SetRenderTarget(0, null);

            var newTexture = renderTarget.GetTexture();
            return newTexture;
        }



    }
}
