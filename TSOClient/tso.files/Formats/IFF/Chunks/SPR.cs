using System;
using System.Collections.Generic;
using System.IO;
using FSO.Files.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;
using FSO.Common;
using FSO.Common.Rendering;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type holds a number of paletted sprites that share a common color palette and lack z-buffers and 
    /// alpha buffers. SPR# chunks can be either big-endian or little-endian, which must be determined by comparing 
    /// the first two bytes to zero (since no version number uses more than two bytes).
    /// </summary>
    public class SPR : IffChunk
    {
        public List<SPRFrame> Frames { get; internal set; }
        public ushort PaletteID;
        private List<uint> Offsets;
        public ByteOrder ByteOrd;
        public bool WallStyle;

        /// <summary>
        /// Reads a SPR chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a SPR chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var version1 = io.ReadUInt16();
                var version2 = io.ReadUInt16();
                uint version = 0;

                if (version1 == 0)
                {
                    io.ByteOrder = ByteOrder.BIG_ENDIAN;
                    version = (uint)(((version2|0xFF00)>>8) | ((version2&0xFF)<<8));
                }
                else
                {
                    version = version1;
                }
                ByteOrd = io.ByteOrder;

                var spriteCount = io.ReadUInt32();
                PaletteID = (ushort)io.ReadUInt32();

                Frames = new List<SPRFrame>();
                if (version != 1001)
                {
                    var offsetTable = new List<uint>();
                    for (var i = 0; i < spriteCount; i++)
                    {
                        offsetTable.Add(io.ReadUInt32());
                    }
                    Offsets = offsetTable;
                    for (var i = 0; i < spriteCount; i++)
                    {
                        var frame = new SPRFrame(this);
                        io.Seek(SeekOrigin.Begin, offsetTable[i]);
                        var guessedSize = ((i + 1 < offsetTable.Count) ? offsetTable[i + 1] : (uint)stream.Length) - offsetTable[i];
                        frame.Read(version, io, guessedSize);
                        Frames.Add(frame);
                    }
                }
                else
                {
                    while (io.HasMore)
                    {
                        var frame = new SPRFrame(this);
                        frame.Read(version, io, 0);
                        Frames.Add(frame);
                    }
                }
            }
        }
    }

    /// <summary>
    /// The frame (I.E sprite) of a SPR chunk.
    /// </summary>
    public class SPRFrame : ITextureProvider
    {
        public static PALT DEFAULT_PALT = new PALT(Color.Black);

        public uint Version;
        private SPR Parent;
        private Texture2D PixelCache;
        private byte[] ToDecode;

        /// <summary>
        /// Constructs a new SPRFrame instance.
        /// </summary>
        /// <param name="parent">A SPR parent.</param>
        public SPRFrame(SPR parent)
        {
            this.Parent = parent;
        }

        /// <summary>
        /// Reads a SPRFrame from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a SPRFrame.</param>
        public void Read(uint version, IoBuffer io, uint guessedSize)
        {
            if (version == 1001)
            {
                var spriteFersion = io.ReadUInt32();

                var size = io.ReadUInt32();
                this.Version = spriteFersion;

                if (IffFile.RETAIN_CHUNK_DATA) ReadDeferred(1001, io);
                else ToDecode = io.ReadBytes(size);
            }
            else
            {
                this.Version = version;
                if (IffFile.RETAIN_CHUNK_DATA) ReadDeferred(1000, io);
                else ToDecode = io.ReadBytes(guessedSize);
            }
        }

        public void ReadDeferred(uint version, IoBuffer io)
        {
            var reserved = io.ReadUInt32();
            var height = io.ReadUInt16();
            var width = io.ReadUInt16();
            this.Init(width, height);
            this.Decode(io);
        }

        public void DecodeIfRequired()
        {
            if (ToDecode != null)
            {
                using (IoBuffer buf = IoBuffer.FromStream(new MemoryStream(ToDecode), Parent.ByteOrd))
                {
                    ReadDeferred(Version, buf);
                }

                ToDecode = null;
            }
        }

        /// <summary>
        /// Decodes this SPRFrame.
        /// </summary>
        /// <param name="io">IOBuffer used to read a SPRFrame.</param>
        private void Decode(IoBuffer io)
        {
            var palette = Parent.ChunkParent.Get<PALT>(Parent.PaletteID);
            if (palette == null)
            {
                palette = DEFAULT_PALT;
            }

            var y = 0;
            var endmarker = false;

            while (!endmarker){
                var command = io.ReadByte();
                var count = io.ReadByte();

                switch (command){
                    /** Start marker **/
                    case 0x00:
                    case 0x10:
                        break;
                    /** Fill row with pixel data **/
                    case 0x04:
                        var bytes = count - 2;
                        var x = 0;

                        while (bytes > 0){
                            var pxCommand = io.ReadByte();
                            var pxCount = io.ReadByte();
                            bytes -= 2;

                            switch (pxCommand){
                                /** Next {n} pixels are transparent **/
                                case 0x01:
                                    x += pxCount;
                                    break;
                                /** Next {n} pixels are the same palette color **/
                                case 0x02:
                                    var index = io.ReadByte();
                                    var padding = io.ReadByte();
                                    bytes -= 2;

                                    var color = palette.Colors[index];
                                    for (var j=0; j < pxCount; j++){
                                        this.SetPixel(x, y, color);
                                        x++;
                                    }
                                    break;
                                /** Next {n} pixels are specific palette colours **/
                                case 0x03:
                                    for (var j=0; j < pxCount; j++){
                                        var index2 = io.ReadByte();
                                        var color2 = palette.Colors[index2];
                                        this.SetPixel(x, y, color2);
                                        x++;
                                    }
                                    bytes -= pxCount;
                                    if (pxCount % 2 != 0){
                                        //Padding
                                        io.ReadByte();
                                        bytes--;
                                    }
                                    break;
                            }
                        }

                        y++;
                        break;
                    /** End marker **/
                    case 0x05:
                        endmarker = true;
                        break;
                    /** Leave next rows transparent **/
                    case 0x09:
                        y += count;
                        continue;
                }

            }
        }

        private Color[] Data;
        public int Width { get; internal set; }
        public int Height { get; internal set; }

        protected void Init(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            Data = new Color[Width * Height];
        }

        public Color GetPixel(int x, int y)
        {
            return Data[(y * Width) + x];
        }

        public void SetPixel(int x, int y, Color color)
        {
            Data[(y * Width) + x] = color;
        }

        public Texture2D GetTexture(GraphicsDevice device)
        {
            DecodeIfRequired();
            if (PixelCache == null)
            {
                var mip = !Parent.WallStyle && FSOEnvironment.Enable3D && FSOEnvironment.EnableNPOTMip;
                var tc = FSOEnvironment.TexCompress;

                if (Width * Height > 0)
                {
                    var w = Math.Max(1, Width);
                    var h = Math.Max(1, Height);
                    if (mip && TextureUtils.OverrideCompression(w, h)) tc = false;
                    if (tc)
                    {
                        PixelCache = new Texture2D(device, ((w+3)/4)*4, ((h+3)/4)*4, mip, SurfaceFormat.Dxt5);
                        if (mip)
                            TextureUtils.UploadDXT5WithMips(PixelCache, w, h, device, Data);
                        else
                            PixelCache.SetData<byte>(TextureUtils.DXT5Compress(Data, w, h).Item1);
                    }
                    else
                    {
                        PixelCache = new Texture2D(device, w, h, mip, SurfaceFormat.Color);
                        if (mip)
                            TextureUtils.UploadWithMips(PixelCache, device, Data);
                        else
                            PixelCache.SetData<Color>(this.Data);
                    }
                }
                else
                {
                    PixelCache = new Texture2D(device, Math.Max(1, Width), Math.Max(1, Height), mip, SurfaceFormat.Color);
                    PixelCache.SetData<Color>(new Color[] { Color.Transparent });
                }

                PixelCache.Tag = new TextureInfo(PixelCache, Width, Height);
                if (!IffFile.RETAIN_CHUNK_DATA) Data = null;
            }
            return PixelCache;
        }
    }
}
