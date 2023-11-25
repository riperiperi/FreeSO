using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace FSO.Common.Utils
{
    public static class CurLoader
    {

        public static MouseCursor LoadMonoCursor(GraphicsDevice gd, Stream stream)
        {
            var cur = LoadCursor(gd, stream);
            return MouseCursor.FromTexture2D(cur.Item1, cur.Item2.X, cur.Item2.Y);
        }

        public static MouseCursor[] LoadUpgradeCursors(GraphicsDevice gd, Stream stream, int maxStars)
        {
            var cur = LoadCursor(gd, stream);
            Texture2D starTex;
            using (var str = File.Open("Content/uigraphics/upgrade_star.png", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                starTex = Texture2D.FromStream(gd, str);
            }

            var batch = new SpriteBatch(gd);
            var results = new MouseCursor[maxStars];
            for (int i = 0; i < maxStars; i++) {
                var starPos = cur.Item1.Width - 12;
                var width = Math.Max(starPos + 8 * i + 9, cur.Item1.Width);
                var tex = new RenderTarget2D(gd, width, Math.Max(width, cur.Item1.Height));
                gd.SetRenderTarget(tex);
                gd.Clear(Color.Transparent);
                batch.Begin(SpriteSortMode.Immediate);
                batch.Draw(cur.Item1, Vector2.Zero, Color.White);
                for (int j=0; j<=i; j++)
                {
                    batch.Draw(starTex, new Vector2(starPos, 5), Color.White);
                    starPos += 8;
                }
                batch.End();
                gd.SetRenderTarget(null);
                results[i] = MouseCursor.FromTexture2D(tex, cur.Item2.X, cur.Item2.Y);
            }
            starTex.Dispose();

            return results;
        }

        public static Func<GraphicsDevice, Stream, Texture2D> BmpLoaderFunc;

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
                var tex = BmpLoaderFunc(gd, tempbmp);

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
