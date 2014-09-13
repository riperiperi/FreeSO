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
using TSO.Files.utils;
using System.IO;
using Microsoft.Xna.Framework;

namespace TSO.Files.formats.iff.chunks
{
    /// <summary>
    /// This chunk type holds a number of paletted sprites that may have z-buffer and/or alpha channels.
    /// </summary>
    public class SPR2 : IffChunk
    {
        public SPR2Frame[] Frames;
        public uint DefaultPaletteID;

        public override void Read(Iff iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var version = io.ReadUInt32();
                uint spriteCount = 0;

                if (version == 1000)
                {
                    spriteCount = io.ReadUInt32();
                    DefaultPaletteID = io.ReadUInt32();
                    var offsetTable = new uint[spriteCount];
                    for (var i = 0; i < spriteCount; i++){
                        offsetTable[i] = io.ReadUInt32();
                    }

                    Frames = new SPR2Frame[spriteCount];
                    for (var i = 0; i < spriteCount; i++)
                    {
                        var frame = new SPR2Frame(this);
                        io.Seek(SeekOrigin.Begin, offsetTable[i]);

                        frame.Read(version, io);
                        Frames[i] = frame;
                    }
                }
                else if (version == 1001)
                {
                    DefaultPaletteID = io.ReadUInt32();
                    spriteCount = io.ReadUInt32();

                    Frames = new SPR2Frame[spriteCount];
                    for (var i = 0; i < spriteCount; i++)
                    {
                        var frame = new SPR2Frame(this);
                        frame.Read(version, io);
                        Frames[i] = frame;
                    }
                }
            }
        }
    }

    public class SPR2Frame : ITextureProvider, IWorldTextureProvider
    {
        private Color[] PixelData;
        private byte[] AlphaData;
        private byte[] ZBufferData;

        private Texture2D ZCache;
        private Texture2D PixelCache;

        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public uint Flags { get; internal set; }
        public ushort PaletteID { get; internal set; }
        public ushort TransparentColorIndex { get; internal set; }
        public Vector2 Position { get; internal set; }
        
        private SPR2 Parent;

        public SPR2Frame(SPR2 parent)
        {
            this.Parent = parent;
        }

        public void Read(uint version, IoBuffer io)
        {
            if (version == 1001){
                var spriteVersion = io.ReadUInt32();
                var spriteSize = io.ReadUInt32();
            }

            this.Width = io.ReadUInt16();
            this.Height = io.ReadUInt16();
            this.Flags = io.ReadUInt32();
            io.ReadUInt16();

            if (this.PaletteID == 0 || this.PaletteID == 0xA3A3)
            {
                this.PaletteID = (ushort)Parent.DefaultPaletteID;
            }

            TransparentColorIndex = io.ReadUInt16();

            var y = io.ReadInt16();
            var x = io.ReadInt16();
            this.Position = new Vector2(x, y);

            this.Decode(io);
        }

        private void Decode(IoBuffer io)
        {
            var y = 0;
            var endmarker = false;

            var hasPixels = (this.Flags & 0x01) == 0x01;
            var hasZBuffer = (this.Flags & 0x02) == 0x02;
            var hasAlpha = (this.Flags & 0x04) == 0x04;

            var numPixels = this.Width * this.Height;
            if (hasPixels){
                this.PixelData = new Color[numPixels];
            }
            if (hasZBuffer){
                this.ZBufferData = new byte[numPixels];
            }
            if (hasAlpha){
                this.AlphaData = new byte[numPixels];
            }

            var palette = Parent.ChunkParent.Get<PALT>(this.PaletteID);
            var transparentPixel = palette.Colors[TransparentColorIndex];

            while (!endmarker){
                var marker = io.ReadUInt16();
                var command = marker >> 13;
                var count = marker & 0x1FFF;

                switch (command){
                    /** Fill with pixel data **/
                    case 0x00:
                        var bytes = count;
                        bytes -= 2;

                        var x = 0;

                        while (bytes > 0){
                            var pxMarker = io.ReadUInt16();
                            var pxCommand = pxMarker >> 13;
                            var pxCount = pxMarker & 0x1FFF;
                            bytes -= 2;

                            switch (pxCommand){
                                case 0x01:
                                case 0x02:
                                    var pxWithAlpha = pxCommand == 0x02;
                                    for (var col = 0; col < pxCount; col++){
                                        var zValue = io.ReadByte();
                                        var pxValue = io.ReadByte();
                                        bytes -= 2;

                                        var pxColor = palette.Colors[pxValue];
                                        if (pxWithAlpha)
                                        {
                                            pxColor.A = (byte)(io.ReadByte() * 8.2258064516129032258064516129032);
                                            bytes--;
                                        }else{
                                            if (pxColor.PackedValue == transparentPixel.PackedValue)
                                            {
                                                pxColor.A = 0;
                                            }
                                        }
                                        var offset = (y * Width) + x;
                                        this.PixelData[offset] = pxColor;
                                        this.ZBufferData[offset] = zValue;
                                        x++;
                                    }
                                    if (pxWithAlpha){
                                        /** Padding? **/
                                        if ((pxCount * 3) % 2 != 0){
                                            bytes--;
                                            io.ReadByte();
                                        }
                                    }
                                    break;
                                case 0x03:
                                    for (var col = 0; col < pxCount; col++){
                                        var offset = (y * Width) + x;
                                        this.PixelData[offset] = transparentPixel;
                                        this.PixelData[offset].A = 0;
                                        if (hasZBuffer){
                                            this.ZBufferData[offset] = 255;
                                        }
                                        x++;
                                    }
                                    break;
                                case 0x06:
                                    for (var col = 0; col < pxCount; col++)
                                    {
                                        var pxIndex = io.ReadByte();
                                        bytes--;
                                        var offset = (y * Width) + x;
                                        var pxColor = palette.Colors[pxIndex];
                                        byte z = 0;
                                        if (pxColor.PackedValue == transparentPixel.PackedValue){
                                            pxColor.A = 0;
                                            z = 255;
                                        }
                                        this.PixelData[offset] = pxColor;
                                        if (hasZBuffer){
                                            this.ZBufferData[offset] = z;
                                        }
                                        x++;
                                    }
                                    if (pxCount % 2 != 0){
                                        bytes--;
                                        io.ReadByte();
                                    }
                                    break;
                            }
                        }

                        /** If row isnt filled in, the rest is transparent **/
                        while (x < Width){
                            var offset = (y * Width) + x;
                            if (hasZBuffer){
                                this.ZBufferData[offset] = 255;
                            }
                            x++;
                        }
                        break;
                    /**  Leave the next count rows in the color channel filled with the transparent color, 
                     * in the z-buffer channel filled with 255, and in the alpha channel filled with 0. **/
                    case 0x04:
                        for (var row = 0; row < count; row++){
                            for (var col = 0; col < Width; col++){
                                var offset = ((y+row) * Width) + col;
                                if (hasPixels) {
                                    this.PixelData[offset] = transparentPixel;
                                }
                                if (hasAlpha){
                                    this.PixelData[offset].A = 0;
                                }
                                if (hasZBuffer){
                                    ZBufferData[offset] = 255;
                                }
                            }
                        }
                        y += count - 1;
                        break;
                    case 0x05:
                        endmarker = true;
                        break;
                }
                y++;
            }
        }

        public Color GetPixel(int x, int y){
            return PixelData[(y * Width) + x];
        }
        public void SetPixel(int x, int y, Color color)
        {
            PixelData[(y * Width) + x] = color;
        }
        public Texture2D GetTexture(GraphicsDevice device)
        {
            if (PixelCache == null)
            {
                if (this.Width == 0 || this.Height == 0)
                {
                    return null;
                }
                PixelCache = new Texture2D(device, this.Width, this.Height);
                PixelCache.SetData<Color>(this.PixelData);
            }
            return PixelCache;
        }

        public Texture2D GetZTexture(GraphicsDevice device)
        {
            if (ZCache == null)
            {
                if (this.Width == 0 || this.Height == 0)
                {
                    return null;
                }
                ZCache = new Texture2D(device, this.Width, this.Height, 0, TextureUsage.None, SurfaceFormat.Luminance8);
                ZCache.SetData<byte>(this.ZBufferData);
            }
            return ZCache;
        }

        #region IWorldTextureProvider Members

        public WorldTexture GetWorldTexture(GraphicsDevice device)
        {
            var result = new WorldTexture
            {
                Pixel = this.GetTexture(device)
            };
            if (this.ZBufferData != null){
                result.ZBuffer = this.GetZTexture(device);
            }
            return result;
        }

        #endregion
    }
}
