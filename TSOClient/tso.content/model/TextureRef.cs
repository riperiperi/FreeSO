using FSO.Common.Utils;
using FSO.Files;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FSO.Common;
using FSO.Common.Rendering;

namespace FSO.Content.Model
{
    public interface ITextureRef
    {
        Texture2D Get(GraphicsDevice device);
        TexBitmap GetImage();
    }

    public class FileTextureRef : AbstractTextureRef
    {
        private string _FilePath;

        public FileTextureRef(string filepath)
        {
            _FilePath = filepath;
        }

        protected override Stream GetStream()
        {
            return new FileStream(_FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }

    public class InMemoryTextureRef : AbstractTextureRef
    {
        private byte[] _Data;

        public InMemoryTextureRef(byte[] data)
        {
            _Data = data;
        }

        protected override Stream GetStream()
        {
            return new MemoryStream(_Data, false);
        }
    }

    public class InMemoryTextureRefWithMask : InMemoryTextureRef
    {
        private byte[] _Data;
        private uint[] _MaskColors;

        public InMemoryTextureRefWithMask(byte[] data, uint[] maskColors) : base(data)
        {
            _Data = data;
            _MaskColors = maskColors;
        }

        protected override Texture2D Process(GraphicsDevice device, Stream stream)
        {
            var texture = base.Process(device, stream);
            //TextureUtils.ManualTextureMaskSingleThreaded(ref texture, _MaskColors);
            return texture;
        }
    }

    public abstract class AbstractTextureRef : ITextureRef
    {
        public delegate TexBitmap SimpleBitmapProvider(Stream stream, AbstractTextureRef texRef);
        public static SimpleBitmapProvider ImageFetchFunction;
        private Texture2D _Instance;

        public AbstractTextureRef()
        {
        }

        protected abstract Stream GetStream();


        private bool WasReferenced = true;
        ~AbstractTextureRef()
        {
            if (WasReferenced)
            {
                TimedReferenceController.KeepAlive(this, KeepAliveType.DEREFERENCED);
                WasReferenced = false;
                GC.ReRegisterForFinalize(this);
            }
        }
        public void Rereferenced()
        {
            WasReferenced = true;
        }

        public Texture2D Get(GraphicsDevice device)
        {
            lock (this)
            {
                if (_Instance == null || _Instance.IsDisposed) //|| ((CachableTexture2D)result).BeingDisposed)
                {
                    using (var stream = GetStream())
                    {
                        if (Thread.CurrentThread == FSOEnvironment.GameThread)
                        {
                            _Instance = Process(device, stream);
                        }
                        else
                        {
                            //We need to get into the game thread to do this work
                            _Instance = GameThread.NextUpdate<Texture2D>(x =>
                            {
                                return Process(device, stream);
                            }).Result;
                        }
                    }
                    GC.SuppressFinalize(_Instance); //do not run the default finalizer on the texture.
                    _Instance.Tag = this; //form a destiny bond with the texture
                }
                return _Instance;
            }
        }

        public static GraphicsDevice FetchDevice;
        public static TexBitmap ImageFetchWithDevice(Stream stream, AbstractTextureRef texRef)
        {
            var tex = ImageLoader.FromStream(FetchDevice, stream);
            var data = new byte[tex.Width * tex.Height * 4];
            tex.GetData(data);
            for (int i = 0; i < data.Length; i += 4)
            {
                //output expects bgra.
                var temp = data[i + 2];
                data[i + 2] = data[i];
                data[i] = temp;
            }
            tex.Dispose();

            return new TexBitmap
            {
                Data = data,
                Width = tex.Width,
                Height = tex.Height,
                PixelSize = 4
            };
        }

        public TexBitmap GetImage()
        {
            if (ImageFetchFunction == null) return null;
            return ImageFetchFunction(GetStream(), this);
        }

        protected virtual Texture2D Process(GraphicsDevice device, Stream stream)
        {
            return ImageLoader.FromStream(device, stream);
        }
    }

    public class TexBitmap
    {
        public int Width;
        public int Height;
        public byte[] Data;
        public int PixelSize;
    }
}
