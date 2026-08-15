using System.Collections.Generic;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;

namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Minimal palette quantizer wired into SPR2FrameEncoder.QuantizeFrame — the production
    /// implementation (FSO.IDE's SpriteEncoderUtils.QuantizeFrame) depends on System.Drawing
    /// + SimplePaletteQuantizer, not portable to this macOS dev environment without extra
    /// native deps. Rendered frames here are flat-shaded per face (no gradient beyond the
    /// small number of distinct lit-face colors), so an exact per-color lookup table has no
    /// real quantization loss for this generator's output.
    /// </summary>
    public static class SimpleQuantizer
    {
        // Shared across all SetData calls for one object build: SPR2FrameEncoder.QuantizeFrame
        // is called once per frame (12 times per object), but SpriteAssembler.AddAppearanceChunks
        // assembles ONE PALT shared by every frame in the DGRP. A per-frame-local palette (the
        // previous version of this class) assigns index 0,1,2... independently per frame, so two
        // frames with different color sets disagree on what index N means — whichever single
        // frame's palette SpriteAssembler captures, every OTHER frame's pixels decode against
        // the wrong colors, including landing on unused padding slots (0,0,0,0), which is
        // "reasonably lit" geometry decoding as flat black. Fixed by accumulating one global
        // color->index table across the whole object instead of resetting it per frame.
        //
        // [ThreadStatic], not a plain static: xUnit runs test classes/methods concurrently on
        // different threads by default, and SPR2FrameEncoder.QuantizeFrame is one shared static
        // delegate — a plain static Dictionary here meant two objects being quantized on two
        // threads at once corrupted each other's palette (one test's Reset() clearing state a
        // concurrently-running test's SetData call was mid-accumulating into), which surfaced
        // as PixelData decoding to null on an object that never had anything wrong with its own
        // generator. Each thread gets its own accumulator; a single object build still happens
        // on one thread (SpriteAssembler.AddAppearanceChunks's loop is sequential), so nothing
        // about the accumulation logic itself changes — only which threads see which state.
        [System.ThreadStatic] static Dictionary<uint, byte> _lookup;
        [System.ThreadStatic] static List<Color> _palette;
        [System.ThreadStatic] static bool _warnedOverflow;

        static void EnsureInitialized()
        {
            _lookup ??= new Dictionary<uint, byte>();
            _palette ??= new List<Color>();
        }

        public static void Install()
        {
            SPR2FrameEncoder.QuantizeFrame = Quantize;
        }

        /// <summary>Call once before rendering each new object's frames — the palette
        /// accumulates across SetData calls within one object build and must not leak
        /// into the next object's (unrelated) color set. Thread-local, so this only resets
        /// the calling thread's accumulator.</summary>
        public static void Reset()
        {
            EnsureInitialized();
            _lookup.Clear();
            _palette.Clear();
            _warnedOverflow = false;
        }

        static Color[] Quantize(SPR2Frame frame, out byte[] bytes)
        {
            EnsureInitialized();
            var px = frame.PixelData;
            bytes = new byte[px.Length];

            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.A == 0) { bytes[i] = 255; continue; } // reserved: matches TransparentColorIndex=255 set by SetData

                var key = ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
                if (!_lookup.TryGetValue(key, out var idx))
                {
                    if (_palette.Count >= 255)
                    {
                        // Silent clamping is how a leaked palette turned whole objects into
                        // one flat colour without anyone noticing; say so once per build.
                        if (!_warnedOverflow)
                        {
                            _warnedOverflow = true;
                            System.Console.Error.WriteLine(
                                "WARNING: sprite palette overflowed 255 colours — further pixels " +
                                "collapse to one index (objects will render flat). Is Reset() " +
                                "being called per object?");
                        }
                        idx = 254; // defensive clamp; shouldn't trigger for flat-shaded output
                    }
                    else
                    {
                        idx = (byte)_palette.Count;
                        _palette.Add(new Color(c.R, c.G, c.B, (byte)255));
                        _lookup[key] = idx;
                    }
                }
                bytes[i] = idx;
            }

            // Always return the full accumulated-so-far palette, padded to 256 — this is what
            // makes it safe for SpriteAssembler to take whichever call's return value it wants
            // (in practice, the last one processed is the most complete superset).
            var result = new Color[256];
            for (int i = 0; i < 256; i++)
                result[i] = i < _palette.Count ? _palette[i] : new Color((byte)0, (byte)0, (byte)0, (byte)0);
            return result;
        }
    }
}
