using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using tso.files.utils;
using Microsoft.Xna.Framework.Graphics;

namespace tso.files.formats.iff.chunks
{
    public class SPR : AbstractIffChunk
    {
        public List<SPRFrame> Frames { get; internal set; }
        public ushort PaletteID;

        public override void Read(Iff iff, Stream stream){
            
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN)){
                var version1 = io.ReadUInt16();
                var version2 = io.ReadUInt16();
                uint version = 0;

                if (version1 == 0){
                    io.ByteOrder = ByteOrder.BIG_ENDIAN;
                    version = version2;
                }else{
                    version = version1;
                }

                var spriteCount = io.ReadUInt32();
                PaletteID = (ushort)io.ReadUInt32();

                Frames = new List<SPRFrame>();
                if (version != 1001){
                    var offsetTable = new List<uint>();
                    for (var i = 0; i < spriteCount; i++)
                    {
                        offsetTable.Add(io.ReadUInt32());
                    }
                    for (var i = 0; i < spriteCount; i++)
                    {
                        var frame = new SPRFrame(this);
                        frame.Read(version, io);
                        Frames.Add(frame);
                    }
                }else{
                    while (io.HasMore){
                        var frame = new SPRFrame(this);
                        frame.Read(version, io);
                        Frames.Add(frame);
                    }
                }
            }
        }
    }

    public class SPRFrame : ITextureProvider
    {
        public static PALT DEFAULT_PALT = new PALT(Color.Black);

        public uint Version;
        private SPR Parent;

        public SPRFrame(SPR parent){
            this.Parent = parent;
        }

        public void Read(uint version, IoBuffer io)
        {
            if (version == 1001){
                var spriteFersion = io.ReadUInt32();
                var size = io.ReadUInt32();
                this.Version = spriteFersion;
            }else{
                this.Version = version;
            }

            var reserved = io.ReadUInt32();
            var height = io.ReadUInt16();
            var width = io.ReadUInt16();
            this.Init(width, height);
            this.Decode(io);
        }

        private void Decode(IoBuffer io){
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
                        break;
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

        public Texture2D GetTexture(GraphicsDevice device){
            var tx = new Texture2D(device, this.Width, this.Height);
            tx.SetData<Color>(this.Data);
            return tx;
        }
    }
}
