using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using System.Text;

namespace FSO.Files
{
    public struct ImageData
    {
        private Color[] ColorData;
        public Color[] Data => ColorData ?? GetColorData();
        public readonly byte[] ByteData;
        public readonly int Width;
        public readonly int Height;

        public ImageData(Color[] data, int width, int height)
        {
            ColorData = data;
            ByteData = null;
            Width = width;
            Height = height;
        }

        public ImageData(byte[] data, int width, int height)
        {
            ColorData = null;
            ByteData = data;
            Width = width;
            Height = height;
        }

        public Texture2D GetTexture(GraphicsDevice gd)
        {
            if (ColorData == null && ByteData == null)
            {
                return null;
            }

            var tex = new Texture2D(gd, Width, Height);
            if (ColorData != null)
            {
                tex.SetData(ColorData);
            }
            else
            {
                tex.SetData(ByteData);
            }

            return tex;
        }

        private unsafe Color[] GetColorData()
        {
            var data = ByteData;
            Color[] colorData = new Color[data.Length / 4];

            fixed (void* ptr = colorData)
            {
                Marshal.Copy(data, 0, (IntPtr)ptr, data.Length);
            }

            ColorData = colorData;

            return colorData;
        }
    }

    public readonly struct ImageDataOrTextureProducer
    {
        public readonly ImageData? Data;
        public readonly Func<Texture2D> Producer;

        public ImageDataOrTextureProducer(ImageData data)
        {
            Data = data;
            Producer = null;
        }

        public ImageDataOrTextureProducer(Func<Texture2D> producer)
        {
            Producer = producer;
            Data = null;
        }

        public Texture2D GetTexture(GraphicsDevice gd)
        {
            if (Producer != null)
            {
                return Producer();
            }
            else if (Data != null)
            {
                return Data.Value.GetTexture(gd);
            }

            return null;
        }

        public Func<Texture2D> GetProducer(GraphicsDevice gd)
        {
            if (Producer != null)
            {
                return Producer;
            }
            else if (Data != null)
            {
                var data = Data.Value;
                return () =>
                {
                    return data.GetTexture(gd);
                };
            }
            else
            {
                return () => null;
            }
        }
    }

    public class ImageLoader
    {
        public static bool UseSoftLoad = true;
        public static int PremultiplyPNG = 0;

        public static Func<GraphicsDevice, Stream, Texture2D> BaseFunction = WinFromStream;
        public static Func<GraphicsDevice, Stream, Func<Texture2D>> BaseNonUIFunction = WinNonUIFromStream;
        public static Func<GraphicsDevice, Stream, ImageDataOrTextureProducer?> BaseDataFunction = WinDataFromStream;

        public static Texture2D FromStreamAvgMips(GraphicsDevice gd, Stream str)
        {
            var file = DataFromStream(gd, str);

            if (file == null)
            {
                return null;
            }

            if (file.Value.Producer != null)
            {
                var nonMip = file.Value.Producer();
                var data = new Color[nonMip.Width * nonMip.Height];
                nonMip.GetData(data);
                nonMip.Dispose();

                var result = new Texture2D(gd, nonMip.Width, nonMip.Height, true, SurfaceFormat.Color);
                TextureUtils.UploadWithAvgMips(result, gd, data);

                return result;
            }
            else if (file.Value.Data != null)
            {
                var data = file.Value.Data.Value;

                var result = new Texture2D(gd, data.Width, data.Height, true, SurfaceFormat.Color);
                TextureUtils.UploadWithAvgMips(result, gd, data.Data);

                return result;
            }

            return null;
        }

        public static Texture2D MipTextureFromFile(GraphicsDevice gd, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return FromStreamAvgMips(gd, stream);
            }
        }

        public static Texture2D FromStream(GraphicsDevice gd, Stream str)
        {
            return BaseFunction(gd, str);
        }

        /// <summary>
        /// Gets data or a Texture2D factory for the given stream.
        /// This runs the decoder work on the calling thread, and may return the raw image data,
        /// or returns a function that creates the texture that should be called on the main thread.
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static ImageDataOrTextureProducer? DataFromStream(GraphicsDevice gd, Stream str)
        {
            return BaseDataFunction(gd, str);
        }

        /// <summary>
        /// Get a Texture2D factory for the given stream.
        /// This runs the decoder work on the calling thread, and returns a function
        /// that creates the texture that should be called on the main thread.
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Func<Texture2D> NonUIFromStream(GraphicsDevice gd, Stream str)
        {
            return BaseNonUIFunction(gd, str);
        }

        private static Texture2D WinFromStream(GraphicsDevice gd, Stream str)
        {
            return WinFromStreamP(gd, str, 0);
        }

        private static Func<Texture2D> WinNonUIFromStream(GraphicsDevice gd, Stream str)
        {
            return WinNonUIFromStreamP(gd, str, 0);
        }

        private static ImageDataOrTextureProducer? WinDataFromStream(GraphicsDevice gd, Stream str)
        {
            return WinDataFromStreamP(gd, str, 0);
        }

        public static bool Premultiply(Color[] buffer, int premult)
        {
            if (premult == 1)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var a = buffer[i].A;
                    if (a != 255)
                    {
                        buffer[i] = new Color((byte)((buffer[i].R * a) / 255), (byte)((buffer[i].G * a) / 255), (byte)((buffer[i].B * a) / 255), a);
                    }
                }

                return true;
            }
            else if (premult == -1) //divide out a premultiply... currently needed for dx since it premultiplies pngs without reason
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var rawA = buffer[i].A;

                    if (rawA != 255)
                    {
                        var a = rawA / 255f;
                        buffer[i] = new Color((byte)(buffer[i].R / a), (byte)(buffer[i].G / a), (byte)(buffer[i].B / a), buffer[i].A);
                    }
                }

                return true;
            }

            return false;
        }

        public static bool Premultiply(byte[] buffer, int premult)
        {
            if (premult == 1)
            {
                for (int i = 0; i < buffer.Length; i += 4)
                {
                    var a = buffer[i + 3];
                    if (a != 255)
                    {
                        buffer[i] = (byte)((buffer[i] * a) / 255);
                        buffer[i + 1] = (byte)((buffer[i + 1] * a) / 255);
                        buffer[i + 2] = (byte)((buffer[i + 2] * a) / 255);
                    }
                }

                return true;
            }
            else if (premult == -1) //divide out a premultiply... currently needed for dx since it premultiplies pngs without reason
            {
                for (int i = 0; i < buffer.Length; i += 4)
                {
                    var rawA = buffer[i];

                    if (rawA != 255)
                    {
                        var a = rawA / 255f;

                        buffer[i] = (byte)(buffer[i] / a);
                        buffer[i + 1] = (byte)(buffer[i + 1] / a);
                        buffer[i + 2] = (byte)(buffer[i + 2] / a);
                    }
                }

                return true;
            }

            return false;
        }

        public static Func<Texture2D> WinNonUIFromStreamP(GraphicsDevice gd, Stream str, int premult)
        {
            return WinDataFromStreamP(gd, str, premult)?.GetProducer(gd);
        }

        public static ImageDataOrTextureProducer? WinDataFromStreamP(GraphicsDevice gd, Stream str, int premult)
        {
            //if (!UseSoftLoad)
            //{
            //attempt monogame load of image

            var magic = (str.ReadByte() | (str.ReadByte() << 8));
            str.Seek(0, SeekOrigin.Begin);
            magic += 0;
            if (magic == 0x4D42)
            {
                try
                {
                    //it's a bitmap. 
                    if (ImageLoaderHelpers.BitmapFunction != null)
                    {
                        var bmp = ImageLoaderHelpers.BitmapFunction(str);
                        if (bmp == null) return null;

                        ManualTextureMaskData(bmp.Item1);

                        return new ImageDataOrTextureProducer(new ImageData(bmp.Item1, bmp.Item2, bmp.Item3));
                    }
                    else
                    {
                        return new ImageDataOrTextureProducer(() =>
                        {
                            Texture2D tex = Texture2D.FromStream(gd, str);

                            ManualTextureMaskSingleThreaded(ref tex);
                            return tex;
                        });
                    }
                }
                catch (Exception)
                {
                    return null; //bad bitmap :(
                }
            }
            else
            {
                //test for targa
                str.Seek(-18, SeekOrigin.End);
                byte[] sig = new byte[16];
                str.Read(sig, 0, 16);
                str.Seek(0, SeekOrigin.Begin);
                if (ASCIIEncoding.Default.GetString(sig) == "TRUEVISION-XFILE")
                {
                    try
                    {
                        var tga = new TargaImagePCL.TargaImage(str);

                        return new ImageDataOrTextureProducer(new ImageData(tga.Image.ToBGRA(true), tga.Image.Width, tga.Image.Height));
                    }
                    catch (Exception)
                    {
                        return null; //bad tga
                    }
                }
                else
                {
                    //anything else
                    try
                    {
                        premult += PremultiplyPNG;

                        if (ImageLoaderHelpers.BitmapFunction != null)
                        {
                            var bmp = ImageLoaderHelpers.BitmapFunction(str);
                            if (bmp == null) return null;

                            Premultiply(bmp.Item1, premult);

                            return new ImageDataOrTextureProducer(new ImageData(bmp.Item1, bmp.Item2, bmp.Item3));

                            //buffer = bmp.Item1;
                        }
                        else
                        {
                            return new ImageDataOrTextureProducer(() =>
                            {
                                Texture2D tex = Texture2D.FromStream(gd, str);

                                if (premult != 0)
                                {
                                    var buffer = new Color[tex.Width * tex.Height];
                                    tex.GetData(buffer);
                                    Premultiply(buffer, premult);
                                    tex.SetData(buffer);
                                }

                                return tex;
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("error: " + e.ToString());
                        return new ImageDataOrTextureProducer(new ImageData(new Color[1], 1, 1));
                    }
                }
            }
        }

        public static Texture2D WinFromStreamP(GraphicsDevice gd, Stream str, int premult)
        {
            return WinNonUIFromStreamP(gd, str, premult)();
        }

        public static bool ManualTextureMaskData(byte[] buffer)
        {
            var didChange = false;

            for (int i = 0; i < buffer.Length; i += 4)
            {
                if (buffer[i] >= 248 && buffer[i + 2] >= 248 && buffer[i + 1] <= 4)
                {
                    buffer[i] = buffer[i + 1] = buffer[i + 2] = buffer[i + 3] = 0;
                    didChange = true;
                }
            }

            return didChange;
        }

        public static void ManualTextureMaskSingleThreaded(ref Texture2D Texture)
        {
            var size = Texture.Width * Texture.Height * 4;
            byte[] buffer = new byte[size];

            Texture.GetData<byte>(buffer);

            if (ManualTextureMaskData(buffer))
            {
                Texture.SetData(buffer);
            }
        }

    }
}