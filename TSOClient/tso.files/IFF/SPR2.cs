using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimsLib.IFF;
using Microsoft.Xna.Framework.Graphics;
using tso.files.utils;
using System.IO;
using Microsoft.Xna.Framework;

namespace tso.files.IFF
{
    public class SPR2 : IffChunk
    {
        public SPR2Frame[] Frames;
        public uint DefaultPaletteID;

        public SPR2(IffChunk chunk) : base(chunk){
        }

        public void Read(Stream stream){
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN)){
                var version = io.ReadUInt32();
                uint spriteCount = 0;
                uint defaultPalette = 0;
                if (version == 1000){
                    spriteCount = io.ReadUInt32();
                    DefaultPaletteID = io.ReadUInt32();
                    var offsetTable = new uint[spriteCount];
                    for (var i = 0; i < spriteCount; i++){
                        offsetTable[i] = io.ReadUInt32();
                    }
                }
                else if (version == 1001)
                {
                    DefaultPaletteID = io.ReadUInt32();
                    spriteCount = io.ReadUInt32();
                }

                Frames = new SPR2Frame[spriteCount];
                for (var i = 0; i < spriteCount; i++){
                    var frame = new SPR2Frame();
                    frame.Read(this, null,version, io);
                    Frames[i] = frame;
                }
            }
        }
    }

    public class SPR2Frame
    {
        private Color[] PixelData;
        private byte[] AlphaData;
        private byte[] ZBufferData;

        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public uint Flags { get; internal set; }
        public uint PaletteID { get; internal set; }
        public Vector2 Position { get; internal set; }

        public void Read(SPR2 spr2, IList<PaletteMap> pals, uint version, IoBuffer io){
            if (version == 1001){
                var spriteVersion = io.ReadUInt32();
                var spriteSize = io.ReadUInt32();
            }

            this.Width = io.ReadUInt16();
            this.Height = io.ReadUInt16();
            this.Flags = io.ReadUInt32();
            this.PaletteID = io.ReadUInt16();

            if (this.PaletteID == 0 || this.PaletteID == 0xA3A3){
                this.PaletteID = spr2.DefaultPaletteID;
            }

            var pal = pals.FirstOrDefault(p => p.ID == this.PaletteID);
            var transparentColorIndex = io.ReadUInt16();

            var y = io.ReadInt16();
            var x = io.ReadInt16();
            this.Position = new Vector2(x, y);

            this.Decode(pal, io);
        }

        private void Decode(PaletteMap pal, IoBuffer io){
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

            while (!endmarker){
                var marker = io.ReadUInt16();
                var command = marker >> 13;
                var count = marker & 0x1FFF;

                switch (command){
                    /** Fill with pixel data **/
                    case 0x00:
                        break;
                    /**  Leave the next count rows in the color channel filled with the transparent color, in the z-buffer channel filled with 255, and in the alpha channel filled with 0. **/
                    case 0x04:
                        for (var row = 0; row < count; row++){
                            for (var col = 0; col < Width; col++){
                                var offset = (row * Width) + col;
                                if (hasPixels) { PixelData[offset] = Color.Red; }
                            }
                            y++;
                        }
                        break;
                }
            }
        }

        public Color GetPixel(int x, int y){
            return PixelData[(y * Width) + x];
        }
        public void SetPixel(int x, int y, Color color){
            PixelData[(y * Width) + x] = color;
        }
        public Texture2D GetTexture(GraphicsDevice device){
            var tx = new Texture2D(device, this.Width, this.Height);
            tx.SetData<Color>(this.PixelData);
            return tx;
        }
    }
}
