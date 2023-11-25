using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace FSO.Files.Utils
{
    public static class CurLoader
    {
        public static MouseCursor LoadMonoCursor(GraphicsDevice gd, Stream stream)
        {
            var cur = LoadCursor(gd, stream);
            return MouseCursor.FromTexture2D(cur.Item1, cur.Item2.X, cur.Item2.Y);
        }

        public static Tuple<Texture2D, Point> LoadCursor(GraphicsDevice gd, Stream stream)
        {
            using (var io = new BinaryReader(stream))
            {
                //little endian
                var tempbmp = new MemoryStream();
                var outIO = new BinaryWriter(tempbmp);

                var reserved = io.ReadInt16();
                var type = io.ReadInt16();
                if (type != 2) throw new Exception("Not a cursor!");
                var images = io.ReadInt16(); //currently discard extra images...

                //read first image
                var width = io.ReadByte();
                var height = io.ReadByte();
                var colors = io.ReadByte();
                var reserved2 = io.ReadByte();
                var xOffset = io.ReadInt16();
                var yOffset = io.ReadInt16();
                var size = io.ReadInt32();
                var offset = io.ReadInt32();
                stream.Seek(offset - 22, SeekOrigin.Current);

                //ok... now write the bitmap data to a fake bitmap
                outIO.Write(new char[] { 'B', 'M' });
                outIO.Write(size + 14); //size, plus header
                outIO.Write(0);
                outIO.Write(14);
                var data = new byte[size];
                stream.Read(data, 0, size);
                outIO.Write(data);

                tempbmp.Seek(0, SeekOrigin.Begin);
                var tex = ImageLoader.FromStream(gd, tempbmp);

                //our mask is on top. the image is on bottom.
                var odata = new byte[tex.Width * tex.Height * 4];
                tex.GetData(odata);
                var ndata = new byte[tex.Width * tex.Height * 2];
                var limit = ndata.Length;
                for (int i=0; i< limit; i+=4)
                {
                    var j = i + limit;
                    ndata[i] = (byte)((~odata[i]) & odata[j]);
                    ndata[i+1] = ndata[i];
                    ndata[i+2] = ndata[i];
                    ndata[i+3] = (byte)(~odata[i]);
                }
                var oTex = new Texture2D(gd, width, height);
                oTex.SetData(ndata);
                tex.Dispose();

                return new Tuple<Texture2D, Point>(oTex, new Point(xOffset, yOffset));
            }
        }
    }
}
