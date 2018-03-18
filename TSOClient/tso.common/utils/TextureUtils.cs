/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;

namespace FSO.Common.Utils
{
    public class TextureUtils
    {
        public static Texture2D TextureFromFile(GraphicsDevice gd, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return Texture2D.FromStream(gd, stream);
            }
        }

        private static Dictionary<uint, Texture2D> _TextureColors = new Dictionary<uint, Texture2D>();

        public static Texture2D TextureFromColor(GraphicsDevice gd, Color color)
        {
            if (_TextureColors.ContainsKey(color.PackedValue))
            {
                return _TextureColors[color.PackedValue];
            }

            var tex = new Texture2D(gd, 1, 1);
            tex.SetData(new[] { color });
            _TextureColors[color.PackedValue] = tex;
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
         * in memory and reuse them for resampling textures
         * 
         * rhy: yeah, maybe, if the code actually did that. i'm also not sure about keeping ~32MB 
         * of texture buffers in memory at all times when the game is largely single threaded.
         */
        private static List<uint[]> ResampleBuffers = new List<uint[]>();
        private static ulong MaxResampleBufferSize = 1024 * 768;

        static TextureUtils()
        {
            /*for (var i = 0; i < 10; i++)
            {
                ResampleBuffers.Add(new uint[MaxResampleBufferSize]);
            }*/
        }

        private static uint[] GetBuffer(int size) //todo: maybe implement something like described, old implementation was broken
        {
            var newBuffer = new uint[size];
            return newBuffer;
        }

        private static void FreeBuffer(uint[] buffer)
        {
        }

        public static void MaskFromTexture(ref Texture2D Texture, Texture2D Mask, uint[] ColorsFrom)
        {
            if (Texture.Width != Mask.Width || Texture.Height != Mask.Height)
            {
                return;
            }

            var ColorTo = Color.Transparent.PackedValue;

            var size = Texture.Width * Texture.Height;
            uint[] buffer = GetBuffer(size);
            Texture.GetData(buffer, 0, size);

            var sizeMask = Mask.Width * Mask.Height;
            var bufferMask = GetBuffer(sizeMask);
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
                Texture.SetData(buffer, 0, size);
            }
        }

        public static Texture2D Clip(GraphicsDevice gd, Texture2D texture, Rectangle source)
        {
            var newTexture = new Texture2D(gd, source.Width, source.Height);
            var size = source.Width * source.Height;
            uint[] buffer = GetBuffer(size);
            if (FSOEnvironment.SoftwareDepth)
            {
                //opengl es does not like this
                var texBuf = GetBuffer(texture.Width * texture.Height);
                texture.GetData(texBuf);
                var destOff = 0;
                for (int y=source.Y; y<source.Bottom; y++)
                {
                    int offset = y * texture.Width + source.X;
                    for (int x = 0; x < source.Width; x++)
                    {
                        buffer[destOff++] = texBuf[offset++];
                    }
                }
            }
            else
            {
                texture.GetData(0, source, buffer, 0, size);
            }

            newTexture.SetData(buffer);
            return newTexture;
        }

        private static SpriteBatch CopyBatch;

        public static Texture2D Copy(GraphicsDevice gd, Texture2D texture)
        {
            if (texture.Format == SurfaceFormat.Dxt5)
            {
                var old = gd.GetRenderTargets();
                var rt = new RenderTarget2D(gd, texture.Width, texture.Height);
                gd.SetRenderTarget(rt);
                gd.Clear(Color.TransparentBlack);
                if (CopyBatch == null) CopyBatch = new SpriteBatch(gd);
                CopyBatch.Begin();
                CopyBatch.Draw(texture, Vector2.Zero, Color.White);
                CopyBatch.End();

                gd.SetRenderTargets(old);

                return rt;
            }
            else
            {
                var newTexture = new Texture2D(gd, texture.Width, texture.Height);

                var size = texture.Width * texture.Height;
                uint[] buffer = GetBuffer(size);
                texture.GetData(buffer, 0, size);

                newTexture.SetData(buffer, 0, size);
                return newTexture;
            }
        }

        public static void CopyAlpha(ref Texture2D TextureTo, Texture2D TextureFrom)
        {
            CopyAlpha(ref TextureTo, TextureFrom, false);
        }

        public static void CopyAlpha(ref Texture2D TextureTo, Texture2D TextureFrom, bool scale)
        {
            if (TextureTo.Width != TextureFrom.Width || TextureTo.Height != TextureFrom.Height)
            {
                if (scale == false)
                {
                    return;
                }
                else
                {
                    TextureFrom = Scale(TextureFrom.GraphicsDevice, TextureFrom, (float)TextureTo.Width / (float)TextureFrom.Width, (float)TextureTo.Height / (float)TextureFrom.Height);
                }
            }


            var size = TextureTo.Width * TextureTo.Height;
            uint[] buffer = GetBuffer(size);
            TextureTo.GetData(buffer, 0, size);

            var sizeFrom = TextureFrom.Width * TextureFrom.Height;
            var bufferFrom = GetBuffer(sizeFrom);
            TextureFrom.GetData(bufferFrom, 0, sizeFrom);

            for (int i = 0; i < size; i++)
            {
                //ARGB
                if (bufferFrom[i] >> 24 == 0)
                {
                    //This is a hack, not sure why monogame is not multiplying alpha correctly.
                    buffer[i] = 0x00000000;
                }
                else
                {
                    buffer[i] = (buffer[i] & 0x00FFFFFF) | (bufferFrom[i] & 0xFF000000);
                }
            }

            TextureTo.SetData(buffer, 0, size);
        }

        /// <summary>
        /// Manually replaces a specified color in a texture with transparent black,
        /// thereby masking it.
        /// </summary>
        /// <param name="Texture">The texture on which to apply the mask.</param>
        /// <param name="ColorFrom">The color to mask away.</param>
        public static void ManualTextureMask(ref Texture2D Texture, uint[] ColorsFrom)
        {
            var ColorTo = Color.Transparent.PackedValue;

            //lock (TEXTURE_MASK_BUFFER)
            //{
                
                var size = Texture.Width * Texture.Height;
                uint[] buffer = GetBuffer(size);
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

                if (didChange)
                {
                    Texture.SetData(buffer, 0, size);
                }
        }

        private static uint[] SINGLE_THREADED_TEXTURE_BUFFER = new uint[MaxResampleBufferSize];
        public static void ManualTextureMaskSingleThreaded(ref Texture2D Texture, uint[] ColorsFrom)
        {
            var ColorTo = Color.Transparent.PackedValue;
            
            var size = Texture.Width * Texture.Height;
            uint[] buffer = new uint[size];

            Texture.GetData<uint>(buffer);

            var didChange = false;

            for (int i = 0; i < size; i++)
            {
                
                if (ColorsFrom.Contains(buffer[i]))
                {
                    didChange = true;
                    buffer[i] = ColorTo;
                }
            }

            if (didChange)
            {
                Texture.SetData(buffer, 0, size);
            }
            else return;
        }

        public static Texture2D Decimate(Texture2D Texture, GraphicsDevice gd, int factor, bool disposeOld)
        {
            if (Texture.Width < factor || Texture.Height < factor) return Texture;
            var size = Texture.Width * Texture.Height*4;
            byte[] buffer = new byte[size];

            Texture.GetData(buffer);

            var newWidth = Texture.Width / factor;
            var newHeight = Texture.Height / factor;
            var target = new byte[newWidth * newHeight * 4];

            for (int y=0; y<Texture.Height; y += factor)
            {
                for (int x = 0; x < Texture.Width; x += factor)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        var targy = (y / factor);
                        var targx = (x / factor);
                        if (targy >= newHeight || targx >= newWidth) continue;
                        int avg = 0;
                        int total = 0;
                        for (int yo = y; yo < y+factor && yo < Texture.Height; yo++)
                        {
                            for (int xo = x; xo < x+factor && xo < Texture.Width; xo++)
                            {
                                avg += (int)buffer[(yo * Texture.Width + xo)*4 + c];
                                total++;
                            }
                        }

                        avg /= total;
                        target[(targy * newWidth + targx)*4 + c] = (byte)avg;
                    }
                }
            }
            if (disposeOld) Texture.Dispose();

            var outTex = new Texture2D(gd, newWidth, newHeight);
            outTex.SetData(target);
            return outTex;
        }

        public static void UploadWithMips(Texture2D Texture, GraphicsDevice gd, Color[] data)
        {
            int level = 0;
            int w = Texture.Width;
            int h = Texture.Height;
            while (data != null)
            {
                Texture.SetData(level++, null, data, 0, data.Length);
                data = Decimate(data, w, h);
                w /= 2;
                h /= 2;
            }
        }

        private static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        public static bool OverrideCompression(int w, int h)
        {
            return (!FSOEnvironment.DirectX && !(IsPowerOfTwo(w) && IsPowerOfTwo(h)));
        }

        public static void UploadDXT5WithMips(Texture2D Texture, int w, int h, GraphicsDevice gd, Color[] data)
        {
            int level = 0;
            int dw = ((w + 3) / 4) * 4;
            int dh = ((h + 3) / 4) * 4;
            Tuple<byte[], Point> dxt = null;
            while (data != null)
            {
                dxt = DXT5Compress(data, Math.Max(1,w), Math.Max(1,h), Math.Max(1, (dw+3)/4), Math.Max(1, (dh+3)/4));
                Texture.SetData(level++, null, dxt.Item1, 0, dxt.Item1.Length);
                data = Decimate(data, w, h);
                w /= 2;
                h /= 2;
                dw /= 2;
                dh /= 2;
            }

            while (dw > 0 || dh > 0)
            {
                Texture.SetData(level++, null, dxt.Item1, 0, dxt.Item1.Length);
                dw /= 2;
                dh /= 2;
            }
        }

        public static void UploadDXT1WithMips(Texture2D Texture, int w, int h, GraphicsDevice gd, Color[] data)
        {
            int level = 0;
            int dw = ((w + 3) / 4) * 4;
            int dh = ((h + 3) / 4) * 4;
            Tuple<byte[], Point> dxt = null;
            while (data != null)
            {
                dxt = DXT1Compress(data, Math.Max(1, w), Math.Max(1, h), Math.Max(1, (dw + 3) / 4), Math.Max(1, (dh + 3) / 4));
                Texture.SetData<byte>(level++, null, dxt.Item1, 0, dxt.Item1.Length*2);
                data = Decimate(data, w, h);
                w /= 2;
                h /= 2;
                dw /= 2;
                dh /= 2;
            }

            while (dw > 0 || dh > 0)
            {
                Texture.SetData<byte>(level++, null, dxt.Item1, 0, dxt.Item1.Length*2);
                dw /= 2;
                dh /= 2;
            }
        }


        public static Tuple<byte[], Point> DXT5Compress(Color[] data, int width, int height)
        {
            return DXT5Compress(data, width, height, (width + 3) / 4, (height + 3) / 4);
        }

        public static Tuple<byte[], Point> DXT5Compress(Color[] data, int width, int height, int blockW, int blockH)
        {
            var result = new byte[blockW * blockH * 16];
            var blockI = 0;
            for (int by = 0; by < blockH; by++)
            {
                for (int bx = 0; bx < blockW; bx++) {
                    var block = new Color[16];

                    var ti = 0;
                    for (int y = 0; y < 4; y++)
                    {
                        var realy = ((by << 2) + y);
                        if (realy >= height) break;
                        var i = realy * width + (bx<<2);
                        
                        for (int x = 0; x < 4; x++)
                        {
                            if ((x + (bx << 2)) >= width)
                                ti++;
                            else
                                block[ti++] = data[i++];
                        }
                    }

                    Color minCol, maxCol;
                    GetMinMaxColor(block, out minCol, out maxCol);

                    //emit alpha data

                    result[blockI++] = maxCol.A;
                    result[blockI++] = minCol.A;

                    var alpha = GetAlphaIndices(block, minCol, maxCol);

                    result[blockI++] = (byte)((alpha[0] >> 0) | (alpha[1] << 3) | (alpha[2] << 6));
                    result[blockI++] = (byte)((alpha[2] >> 2) | (alpha[3] << 1) | (alpha[4] << 4) | (alpha[5] << 7));
                    result[blockI++] = (byte)((alpha[5] >> 1) | (alpha[6] << 2) | (alpha[7] << 5));
                    result[blockI++] = (byte)((alpha[8] >> 0) | (alpha[9] << 3) | (alpha[10] << 6));
                    result[blockI++] = (byte)((alpha[10] >> 2) | (alpha[11] << 1) | (alpha[12] << 4) | (alpha[13] << 7));
                    result[blockI++] = (byte)((alpha[13] >> 1) | (alpha[14] << 2) | (alpha[15] << 5));

                    //emit color data

                    result[blockI++] = (byte)((maxCol.B >> 3) | (((maxCol.G >> 2) << 5) & 0xFF));
                    result[blockI++] = (byte)(((maxCol.R >> 3) << 3) | (maxCol.G >> 2) >> 3);

                    result[blockI++] = (byte)((minCol.B >> 3) | (((minCol.G >> 2) << 5) & 0xFF));
                    result[blockI++] = (byte)(((minCol.R >> 3) << 3) | (minCol.G >> 2) >> 3);

                    var indices = GetColorIndices(block, minCol, maxCol);
                    result[blockI++] = (byte)indices;
                    result[blockI++] = (byte)(indices >> 8);
                    result[blockI++] = (byte)(indices >> 16);
                    result[blockI++] = (byte)(indices >> 24);
                }
            }

            return new Tuple<byte[], Point>(result, new Point(blockW * 4, blockH * 4));
        }

        public static Tuple<byte[], Point> DXT1Compress(Color[] data, int width, int height)
        {
            return DXT1Compress(data, width, height, (width + 3) / 4, (height + 3) / 4);
        }

        public static Tuple<byte[], Point> DXT1Compress(Color[] data, int width, int height, int blockW, int blockH)
        {
            var result = new byte[blockW * blockH * 8];
            var blockI = 0;
            for (int by = 0; by < blockH; by++)
            {
                for (int bx = 0; bx < blockW; bx++)
                {
                    var block = new Color[16];

                    var ti = 0;
                    for (int y = 0; y < 4; y++)
                    {
                        var realy = ((by << 2) + y);
                        if (realy >= height) break;
                        var i = realy * width + (bx << 2);

                        for (int x = 0; x < 4; x++)
                        {
                            if ((x + (bx << 2)) >= width)
                                ti++;
                            else
                                block[ti++] = data[i++];
                        }
                    }

                    Color minCol, maxCol;
                    GetMinMaxColor(block, out minCol, out maxCol);

                    //emit color data

                    uint indices;
                    //if this block contains a transparent colour, it should be stored in alpha 1bit format.
                    //we invert the max and min color to tell the gpu.
                    if (minCol.A == 0)
                    {
                        result[blockI++] = (byte)((minCol.B >> 3) | (((minCol.G >> 2) << 5) & 0xFF));
                        result[blockI++] = (byte)(((minCol.R >> 3) << 3) | (minCol.G >> 2) >> 3);

                        result[blockI++] = (byte)((maxCol.B >> 3) | (((maxCol.G >> 2) << 5) & 0xFF));
                        result[blockI++] = (byte)(((maxCol.R >> 3) << 3) | (maxCol.G >> 2) >> 3);

                        indices = GetA1ColorIndices(block, minCol, maxCol);
                    } else {
                        result[blockI++] = (byte)((maxCol.B >> 3) | (((maxCol.G >> 2) << 5) & 0xFF));
                        result[blockI++] = (byte)(((maxCol.R >> 3) << 3) | (maxCol.G >> 2) >> 3);

                        result[blockI++] = (byte)((minCol.B >> 3) | (((minCol.G >> 2) << 5) & 0xFF));
                        result[blockI++] = (byte)(((minCol.R >> 3) << 3) | (minCol.G >> 2) >> 3);

                        indices = GetColorIndices(block, minCol, maxCol);
                    }
                    
                    result[blockI++] = (byte)indices;
                    result[blockI++] = (byte)(indices >> 8);
                    result[blockI++] = (byte)(indices >> 16);
                    result[blockI++] = (byte)(indices >> 24);
                }
            }

            return new Tuple<byte[], Point>(result, new Point(blockW * 4, blockH * 4));
        }

        private static byte[] GetAlphaIndices(Color[] block, Color minCol, Color maxCol)
        {
            var result = new byte[16];
            int alphaRange = maxCol.A - minCol.A;
            if (alphaRange == 0) return result;
            int halfAlpha = alphaRange / 2;
            for (int ai = 0; ai < 16; ai++)
            {
                var a = block[ai].A;
                //result alpha
                //round point on line where the alpha is. 
                var aindex = Math.Min(7, Math.Max(0, ((a - minCol.A) * 7 + halfAlpha) / alphaRange));
                if (aindex == 7) aindex = 0;
                else if (aindex == 0) aindex = 1;
                else aindex = (8 - aindex);
                result[ai] = (byte)aindex;
            }
            return result;
        }

        private static uint GetColorIndices(Color[] block, Color minCol, Color maxCol)
        {
            
            var pal = new Color[]
            {
                maxCol,
                minCol,
                Color.Lerp(minCol, maxCol, 2/3f),
                Color.Lerp(minCol, maxCol, 1/3f),
            };

            uint result = 0;

            for (int i = 0; i < 16; i++)
            {
                var c = block[i];
                int best = 10000;
                uint besti = 0;
                for (uint j = 0; j < 4; j++)
                {
                    int d = Math.Abs(pal[j].R - c.R) + Math.Abs(pal[j].G - c.G) + Math.Abs(pal[j].B - c.B);
                    if (d < best)
                    {
                        best = d;
                        besti = j;
                    }
                }
                result |= besti << (i * 2);
            }

            return result;
        }

        private static uint GetA1ColorIndices(Color[] block, Color minCol, Color maxCol)
        {

            var pal = new Color[]
            {
                maxCol,
                minCol,
                Color.Lerp(minCol, maxCol, 1/2f),
            };

            uint result = 0;

            for (int i = 0; i < 16; i++)
            {
                var c = block[i];

                int best = 10000;
                uint besti = 0;
                if (c.A == 0) besti = 3;
                else
                {
                    for (uint j = 0; j < 3; j++)
                    {
                        int d = Math.Abs(pal[j].R - c.R) + Math.Abs(pal[j].G - c.G) + Math.Abs(pal[j].B - c.B);
                        if (d < best)
                        {
                            best = d;
                            besti = j;
                        }
                    }
                }
                result |= besti << (i * 2);
            }

            return result;
        }

        private static void GetMinMaxColor(Color[] block, out Color minCol, out Color maxCol)
        {
            const int INSET_SHIFT = 4;
            maxCol = Color.TransparentBlack;
            minCol = Color.White;

            for (int i = 0; i < 16; i++)
            {
                var col = block[i];

                if (col.A < minCol.A) minCol.A = col.A;
                if (col.A > maxCol.A) maxCol.A = col.A;
                if (col.A == 0) continue;

                if (col.R < minCol.R) minCol.R = col.R;
                if (col.G < minCol.G) minCol.G = col.G;
                if (col.B < minCol.B) minCol.B = col.B;

                if (col.R > maxCol.R) maxCol.R = col.R;
                if (col.G > maxCol.G) maxCol.G = col.G;
                if (col.B > maxCol.B) maxCol.B = col.B;

            }

            //important to note that these packed value calculations can never overflow from
            //one byte into the next.

            //var inset = new Color(maxCol.PackedValue - minCol.PackedValue);
            //inset.R >>= INSET_SHIFT;
            //inset.G >>= INSET_SHIFT;
            //inset.B >>= INSET_SHIFT;
            //inset.A >>= INSET_SHIFT;

            //minCol = new Color(minCol.PackedValue + inset.PackedValue);
            //maxCol = new Color(maxCol.PackedValue - inset.PackedValue);
        }

        public static Color[] Decimate(Color[] old, int w, int h)
        {
            var nw = w / 2;
            var nh = h / 2;
            bool linex = false, liney = false;
            if (nw == 0 && nh == 0) return null;
            if (nw == 0) { nw = 1; liney = true; }
            if (nh == 0) { nh = 1; linex = true; }
            var size = nw*nh;
            Color[] buffer = new Color[size];

            int tind = 0;
            int fyind = 0;
            for (int y = 0; y < nh; y ++)
            {
                var yb = y * 2 == h || linex;
                int find = fyind;
                for (int x = 0; x < nw; x ++)
                {
                    var xb = x * 2 == h || liney;
                    var c1 = old[find];
                    var c2 = (xb)?Color.Transparent:old[find + 1];
                    var c3 = (yb)?Color.Transparent:old[find + w];
                    var c4 = (xb || yb)?Color.Transparent:old[find + 1 + w];

                    int r=0, g=0, b=0, t=0;
                    if (c1.A > 0)
                    {
                        r += c1.R; g += c1.G; b += c1.B; t++;
                    }
                    if (c2.A > 0)
                    {
                        r += c2.R; g += c2.G; b += c2.B; t++;
                    }
                    if (c3.A > 0)
                    {
                        r += c3.R; g += c3.G; b += c3.B; t++;
                    }
                    if (c4.A > 0)
                    {
                        r += c4.R; g += c4.G; b += c4.B; t++;
                    }
                    if (t == 0) t = 1;

                    buffer[tind++] = new Color(
                        (byte)(r / t),
                        (byte)(g / t),
                        (byte)(b / t),
                        Math.Max(Math.Max(Math.Max(c1.A, c2.A), c3.A), c4.A)
                        );
                    find += 2;
                }
                fyind += w * 2;
            }
            return buffer;
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
                newWidth, newHeight, false,
                SurfaceFormat.Color, DepthFormat.None);
           
            Rectangle destinationRectangle = new Rectangle(0, 0, newWidth, newHeight);
            lock (gd)
            {
                gd.SetRenderTarget(renderTarget);
                gd.Clear(Color.TransparentBlack);
                SpriteBatch batch = new SpriteBatch(gd);
                batch.Begin();
                batch.Draw(texture, destinationRectangle, Color.White);
                batch.End();
                gd.SetRenderTarget(null);
            }
            var newTexture = renderTarget;
            return newTexture;
        }

        public static Texture2D Scale(GraphicsDevice gd, Texture2D texture, float scaleX, float scaleY)
        {
            var newWidth = (int)(Math.Round(texture.Width * scaleX));
            var newHeight = (int)(Math.Round(texture.Height * scaleY));

            RenderTarget2D renderTarget = new RenderTarget2D(
                gd,
                newWidth, newHeight, false,
                SurfaceFormat.Color, DepthFormat.None);

            gd.SetRenderTarget(renderTarget);

            SpriteBatch batch = new SpriteBatch(gd);

            Rectangle destinationRectangle = new Rectangle(0, 0, newWidth, newHeight);

            batch.Begin();
            batch.Draw(texture, destinationRectangle, Color.White);
            batch.End();

            gd.SetRenderTarget(null);

            var newTexture = renderTarget;
            return newTexture;
        }
    }
}
