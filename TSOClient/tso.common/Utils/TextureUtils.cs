using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;

namespace FSO.Common.Utils
{
    public class TextureUtils
    {
        public static Texture2D TextureFromFile(GraphicsDevice gd, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Texture2D.FromStream(gd, stream);
            }
        }

        public static Texture2D MipTextureFromFile(GraphicsDevice gd, string filePath)
        {
            var tex = TextureFromFile(gd, filePath);
            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);
            var newTex = new Texture2D(gd, tex.Width, tex.Height, true, SurfaceFormat.Color);
            UploadWithAvgMips(newTex, gd, data);
            tex.Dispose();
            return newTex;
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

        public static Texture2D CopyAccelerated(GraphicsDevice gd, Texture2D texture)
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

        public static Texture2D Copy(GraphicsDevice gd, Texture2D texture)
        {
            if (texture.Format == SurfaceFormat.Dxt5)
            {
                return CopyAccelerated(gd, texture);
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

        public static void UploadWithAvgMips(Texture2D Texture, GraphicsDevice gd, Color[] data)
        {
            int level = 0;
            int w = Texture.Width;
            int h = Texture.Height;
            while (data != null)
            {
                Texture.SetData(level++, null, data, 0, data.Length);
                data = AvgDecimate(data, w, h);
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

        public static Color[] DXT5Decompress(byte[] data, int width, int height)
        {
            var result = new Color[width * height];
            var blockW = width >> 2;
            var blockH = height >> 2;
            var blockI = 0;
            var targI = 0;

            for (int by = 0; by < blockH; by++)
            {
                for (int bx = 0; bx < blockW; bx++)
                {
                    //
                    var maxA = data[blockI++];
                    var minA = data[blockI++];

                    var targ2I = targI;
                    ulong alpha = data[blockI++];
                    alpha |= (ulong)data[blockI++] << 8;
                    alpha |= (ulong)data[blockI++] << 16;
                    alpha |= (ulong)data[blockI++] << 24;
                    alpha |= (ulong)data[blockI++] << 32;
                    alpha |= (ulong)data[blockI++] << 40;

                    var maxCI = (uint)data[blockI++];
                    maxCI |= (uint)data[blockI++] << 8;

                    var minCI = (uint)data[blockI++];
                    minCI |= (uint)data[blockI++] << 8;
                    
                    var maxCol = new Color((int)((maxCI >> 11) & 31), (int)((maxCI >> 6) & 31), (int)(maxCI & 31)) * (255f/31f);
                    var minCol = new Color((int)((minCI >> 11) & 31), (int)((minCI >> 6) & 31), (int)(minCI & 31)) * (255f / 31f);

                    uint col = data[blockI++];
                    col |= (uint)data[blockI++] << 8;
                    col |= (uint)data[blockI++] << 16;
                    col |= (uint)data[blockI++] << 24;

                    var i = 0;
                    for (int y=0; y<4; y++)
                    {
                        for (int x=0; x<4; x++)
                        {
                            var abit = (alpha >> (i*3)) & 0x7;
                            var cbit = (col >> (i * 2)) & 0x3;
                            i++;
                            Color col2;
                            switch (cbit)
                            {
                                case 1:
                                    col2 = minCol;break;
                                case 2:
                                    col2 = Color.Lerp(minCol, maxCol, 2/3f); break;
                                case 3:
                                    col2 = Color.Lerp(minCol, maxCol, 1 / 3f); break;
                                default:
                                    col2 = maxCol; break;
                            }
                            if (abit == 0) col2.A = maxA;
                            else if (abit == 1) col2.A = minA;
                            else
                            {
                                var a = (8 - abit) / 7f;
                                col2.A = (byte)(maxA*a + minA * (1-a));
                            }
                            
                            result[targ2I++] = col2;
                        }
                        targ2I += width - 4;
                    }
                    targI += 4;
                }
                targI += width * 3;
            }

            return result;
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

                    byte minAlpha, maxAlpha;
                    GetMinMaxAlpha(block, out minAlpha, out maxAlpha);

                    //emit alpha data

                    // Always reversed to use 8-bit alpha block
                    result[blockI++] = maxAlpha;
                    result[blockI++] = minAlpha;

                    var alpha = GetAlphaIndices(block, minAlpha, maxAlpha);

                    result[blockI++] = (byte)((alpha[0] >> 0) | (alpha[1] << 3) | (alpha[2] << 6));
                    result[blockI++] = (byte)((alpha[2] >> 2) | (alpha[3] << 1) | (alpha[4] << 4) | (alpha[5] << 7));
                    result[blockI++] = (byte)((alpha[5] >> 1) | (alpha[6] << 2) | (alpha[7] << 5));
                    result[blockI++] = (byte)((alpha[8] >> 0) | (alpha[9] << 3) | (alpha[10] << 6));
                    result[blockI++] = (byte)((alpha[10] >> 2) | (alpha[11] << 1) | (alpha[12] << 4) | (alpha[13] << 7));
                    result[blockI++] = (byte)((alpha[13] >> 1) | (alpha[14] << 2) | (alpha[15] << 5));

                    //emit color data

                    Color color0, color1;
                    ushort colorBin0, colorBin1;
                    GetExtremeColors(block, out color0, out colorBin0, out color1, out colorBin1, false);

                    result[blockI++] = (byte)(colorBin0 & 0xFF);
                    result[blockI++] = (byte)((colorBin0 >> 8) & 0xFF);

                    result[blockI++] = (byte)(colorBin1 & 0xFF);
                    result[blockI++] = (byte)((colorBin1 >> 8) & 0xFF);
                       
                    var indices = GetColorIndices(block, color0, color1);
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

                    Color color0, color1;
                    ushort colorBin0, colorBin1;
                    GetExtremeColors(block, out color0, out colorBin0, out color1, out colorBin1, true);

                    //emit color data

                    result[blockI++] = (byte)(colorBin0 & 0xFF);
                    result[blockI++] = (byte)((colorBin0 >> 8) & 0xFF);
                    result[blockI++] = (byte)(colorBin1 & 0xFF);
                    result[blockI++] = (byte)((colorBin1 >> 8) & 0xFF);

                    uint indices;
                    //if this block contains a transparent colour, it should be stored in alpha 1bit format.
                    //we invert the max and min color in GetExtremeColors to tell the gpu.
                    if (colorBin0 > colorBin1)
                    {
                        // Opaque
                        indices = GetColorIndices(block, color0, color1);
                    }
                    else
                    {
                        // Transparent
                        indices = GetA1ColorIndices(block, color0, color1);
                    }
                    
                    result[blockI++] = (byte)indices;
                    result[blockI++] = (byte)(indices >> 8);
                    result[blockI++] = (byte)(indices >> 16);
                    result[blockI++] = (byte)(indices >> 24);
                }
            }

            return new Tuple<byte[], Point>(result, new Point(blockW * 4, blockH * 4));
        }

        private static byte[] GetAlphaIndices(Color[] block, int minAlpha, int maxAlpha)
        {
            var result = new byte[16];
            int alphaRange = maxAlpha - minAlpha;
            if (alphaRange == 0) return result;
            int halfAlpha = alphaRange / 2;
            for (int ai = 0; ai < 16; ai++)
            {
                var a = block[ai].A;
                //result alpha
                //round point on line where the alpha is. 
                var aindex = Math.Min(7, Math.Max(0, ((a - minAlpha) * 7 + halfAlpha) / alphaRange));
                if (aindex == 7) aindex = 0;
                else if (aindex == 0) aindex = 1;
                else aindex = (8 - aindex);
                result[ai] = (byte)aindex;
            }
            return result;
        }

        private static uint GetColorIndices(Color[] block, Color color0, Color color1)
        {
            Color color2 = Color.Lerp(color0, color1, 1 / 3f); // Nearest to color0
            Color color3 = Color.Lerp(color0, color1, 2 / 3f); // Nearest to color1

            uint result = 0;

            for (int i = 0; i < 16; i++)
            {
                var c = block[i];

                int dist0 = ColorDistanceSq(c, color0);
                int dist1 = ColorDistanceSq(c, color1);

                // If we already know it's nearer to color0 or color1,
                // we only need to check against the second nearest
                uint besti = dist0 < dist1
                    ? ((ColorDistanceSq(c, color2) < dist0) ? 2u : 0u)
                    : ((ColorDistanceSq(c, color3) < dist1) ? 3u : 1u);

                result |= besti << (i * 2);
            }

            return result;
        }

        private static uint GetA1ColorIndices(Color[] block, Color color0, Color color1)
        {
            // transparent = 3
            Color color2 = Color.Lerp(color0, color1, .5f);

            uint result = 0;

            for (int i = 0; i < 16; i++)
            {
                var c = block[i];

                uint besti;
                if (c.A == 0) besti = 3;
                else
                {
                    int dist0 = ColorDistanceSq(c, color0);
                    int dist1 = ColorDistanceSq(c, color1);
                    int dist2 = ColorDistanceSq(c, color2);

                    besti = dist0 < dist1
                        ? ((dist2 < dist0) ? 2u : 0u)
                        : ((dist2 < dist1) ? 2u : 1u);
                }
                result |= besti << (i * 2);
            }

            return result;
        }

        private static int ColorDistanceSq(Color c0, Color c1)
        {
            // Vector distance
            int r = (c0.R - c1.R) * 5; // Weigh in BT.601 luma coefficients
            int g = (c0.G - c1.G) * 9;
            int b = (c0.B - c1.B) * 2;
            return (r * r) + (g * g) + (b * b);
        }

        private static void GetMinMaxAlpha(Color[] block, out byte minAlpha, out byte maxAlpha)
        {
            byte minA = 255;
            byte maxA = 0;
            for (int i = 0; i < 16; i++)
            {
                byte a = block[i].A;
                if (a < minA) minA = a;
                if (a > maxA) maxA = a;
            }
            minAlpha = minA;
            maxAlpha = maxA;
        }

        private static void GetExtremeColors(Color[] block, out Color color0, out ushort colorBin0, out Color color1, out ushort colorBin1, bool dxt1a)
        {
            // Calculate average of colors, skip all colors with Alpha equal to 0
            bool hasAlpha0 = false;
            int r = 0, g = 0, b = 0, t = 0;
            for (int i = 0; i < 16; i++)
            {
                if (block[i].A > 0)
                {
                    r += block[i].R;
                    g += block[i].G;
                    b += block[i].B;
                    ++t;
                }
                else
                {
                    hasAlpha0 = true;
                }
            }
            Color avg = (t == 0)
                ? new Color(0, 0, 0)
                : new Color(r / t, g / t, b / t);

            // Find color furthest from average
            int leftDist = 0;
            int leftIdx = 0;
            for (int i = 0; i < 16; i++)
            {
                if (block[i].A == 0 && t != 0)
                    continue;

                int dist = ColorDistanceSq(block[i], avg);
                if (dist > leftDist)
                {
                    leftDist = dist;
                    leftIdx = i;
                }
            }
            Color leftCol = block[leftIdx];

            // Find color furthest to furthest
            int rightDist = 0;
            int rightIdx = 0;
            for (int i = 0; i < 16; i++)
            {
                if (block[i].A == 0 && t != 0)
                    continue;

                int dist = ColorDistanceSq(block[i], leftCol);
                if (dist > rightDist)
                {
                    rightDist = dist;
                    rightIdx = i;
                }
            }
            Color rightCol = block[rightIdx];

            // RGB565 conversion
            ushort leftBin = (ushort)((leftCol.B >> 3) | ((leftCol.G >> 2) << 5) | ((leftCol.R >> 3) << 11));
            ushort rightBin = (ushort)((rightCol.B >> 3) | ((rightCol.G >> 2) << 5) | ((rightCol.R >> 3) << 11));

            // Alpha is determined in RGB565 representation
            // If alpha, Color 1 is greater or equal to color 0
            // If no alpha, Color 0 is greater than color 1
            if ((hasAlpha0 && dxt1a) != (leftBin < rightBin))
            {
                // hasAlpha0 && (leftBin >= rightbin)
                // !hasAlpha && (leftBin < rightbin)
                color0 = rightCol;
                colorBin0 = rightBin;
                color1 = leftCol;
                colorBin1 = leftBin;
            }
            else
            {
                // hasAlpha0 && (leftBin < rightbin)
                // !hasAlpha && (leftBin >= rightbin)
                color0 = leftCol;
                colorBin0 = leftBin;
                color1 = rightCol;
                colorBin1 = rightBin;
            }
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

        public static Color[] AvgDecimate(Color[] old, int w, int h)
        {
            var nw = w / 2;
            var nh = h / 2;
            bool linex = false, liney = false;
            if (nw == 0 && nh == 0) return null;
            if (nw == 0) { nw = 1; liney = true; }
            if (nh == 0) { nh = 1; linex = true; }
            var size = nw * nh;
            Color[] buffer = new Color[size];

            int tind = 0;
            int fyind = 0;
            for (int y = 0; y < nh; y++)
            {
                var yb = y * 2 == h || linex;
                int find = fyind;
                for (int x = 0; x < nw; x++)
                {
                    var xb = x * 2 == w || liney;
                    var c1 = old[find];
                    var c2 = (xb) ? Color.Transparent : old[find + 1];
                    var c3 = (yb) ? Color.Transparent : old[find + w];
                    var c4 = (xb || yb) ? Color.Transparent : old[find + 1 + w];

                    int r = 0, g = 0, b = 0, a=0, t = 0;
                    if (c1.A > 0)
                    {
                        r += c1.R; g += c1.G; b += c1.B; a += c1.A; t++;
                    }
                    if (c2.A > 0)
                    {
                        r += c2.R; g += c2.G; b += c2.B; a += c2.A; t++;
                    }
                    if (c3.A > 0)
                    {
                        r += c3.R; g += c3.G; b += c3.B; a += c3.A; t++;
                    }
                    if (c4.A > 0)
                    {
                        r += c4.R; g += c4.G; b += c4.B; a += c4.A; t++;
                    }
                    if (t == 0) t = 1;

                    buffer[tind++] = new Color(
                        (byte)(r / t),
                        (byte)(g / t),
                        (byte)(b / t),
                        (byte)(a / 4)
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
