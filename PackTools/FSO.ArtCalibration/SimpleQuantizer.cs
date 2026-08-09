using System.Collections.Generic;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// Minimal palette quantizer wired into SPR2FrameEncoder.QuantizeFrame — the production
    /// implementation (FSO.IDE's SpriteEncoderUtils.QuantizeFrame) depends on System.Drawing
    /// + SimplePaletteQuantizer, both effectively Windows-only / not portable to this macOS
    /// dev environment. Our rendered frames are flat-shaded (one solid color per face, no
    /// gradient), so an exact per-color lookup table is exact and sufficient — no real
    /// quantization loss, unlike a photographic source that would need one.
    /// </summary>
    public static class SimpleQuantizer
    {
        public static void Install()
        {
            SPR2FrameEncoder.QuantizeFrame = Quantize;
        }

        static Color[] Quantize(SPR2Frame frame, out byte[] bytes)
        {
            var px = frame.PixelData;
            bytes = new byte[px.Length];
            var palette = new List<Color>();
            var lookup = new Dictionary<uint, byte>();

            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.A == 0) { bytes[i] = 255; continue; } // reserved: matches TransparentColorIndex=255 set by SetData

                var key = ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
                if (!lookup.TryGetValue(key, out var idx))
                {
                    if (palette.Count >= 255)
                    {
                        idx = 254; // shouldn't happen for flat-shaded synthetic frames; clamp defensively
                    }
                    else
                    {
                        idx = (byte)palette.Count;
                        palette.Add(new Color(c.R, c.G, c.B, (byte)255));
                        lookup[key] = idx;
                    }
                }
                bytes[i] = idx;
            }

            while (palette.Count < 256) palette.Add(new Color((byte)0, (byte)0, (byte)0, (byte)0));
            return palette.ToArray();
        }
    }
}
