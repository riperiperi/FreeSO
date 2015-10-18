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
using System.Drawing;

namespace FSO.Content.Model
{
    public interface ITextureRef
    {
        Texture2D Get(GraphicsDevice device);
        System.Drawing.Image GetImage();
    }

    public class InMemoryTextureRef : ITextureRef
    {
        private Texture2D _Instance;
        private byte[] _Data;

        public InMemoryTextureRef(byte[] data)
        {
            _Data = data;
        }

        public Texture2D Get(GraphicsDevice device)
        {
            lock (this)
            {
                if (_Instance != null && !_Instance.IsDisposed)
                {
                    return _Instance;
                }

                using (var stream = new MemoryStream(_Data, false))
                {
                    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
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
                    return _Instance;
                }
            }
        }

        protected virtual Texture2D Process(GraphicsDevice device, Stream stream)
        {
            return ImageLoader.FromStream(device, stream);
        }

        public Image GetImage()
        {
            using (var stream = new MemoryStream(_Data, false))
            {
                try {
                    return Image.FromStream(stream);
                }catch(Exception ex)
                {
                    Bitmap bmp = null;
                    try
                    {
                        bmp = (Bitmap)Image.FromStream(stream); //try as bmp
                        return bmp;
                    }
                    catch (Exception)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        var tga = new Paloma.TargaImage(stream);
                        return tga.Image;
                    }
                }
            }
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
            TextureUtils.ManualTextureMaskSingleThreaded(ref texture, _MaskColors);
            return texture;
        }
    }
}
