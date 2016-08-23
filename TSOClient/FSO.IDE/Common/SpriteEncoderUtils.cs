using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using SimplePaletteQuantizer.Helpers;
using SimplePaletteQuantizer.Quantizers.DistinctSelection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.Common
{
    public static class SpriteEncoderUtils
    {
        public static System.Drawing.Color[] QuantizeFrame(SPR2Frame frame, out byte[] bytes)
        {
            var bmps = frame.GetPixelAlpha(frame.Width, frame.Height, new Vector2());

            var quantpx = (Bitmap)ImageBuffer.QuantizeImage(bmps[0], new DistinctSelectionQuantizer(), null, 255, 4);
            var palt = quantpx.Palette.Entries;

            var data = quantpx.LockBits(new System.Drawing.Rectangle(0, 0, quantpx.Width, quantpx.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            bytes = new byte[data.Height * data.Width];

            // copy the bytes from bitmap to array
            for (int i = 0; i < data.Height; i++)
            {
                Marshal.Copy(data.Scan0 + i * data.Stride, bytes, i * data.Width, data.Width);
            }

            return palt;
        }
    }
}
