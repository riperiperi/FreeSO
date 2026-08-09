using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FSO.Files.FAR1;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Copies a base-game object's appearance (draw groups + sprites) into a pack object's
    /// .iff, so `appearance.clone_from_guid` produces something that actually renders.
    ///
    /// Why the chunks must be copied inline rather than referenced: WorldObjectProvider
    /// loads sprites from the objspf*.far archives only for Far-sourced objects; a
    /// Standalone-sourced .iff (which is what a compiled pack is, living in
    /// Content/Objects/) gets sprites=null, so GameObjectResource.Get&lt;DGRP&gt; only ever
    /// searches the object's own .iff. Without inline DGRP/SPR2, VMGameObject.RefreshGraphic
    /// finds no draw group and the object renders as nothing — silently, since
    /// DGRPRenderer's draw paths early-return on a null DrawGroup.
    ///
    /// Reads the FAR archives directly via FSO.Files rather than going through FSO.Content,
    /// so the compiler keeps its single dependency on FSO.Files and doesn't need a booted
    /// content singleton at compile time.
    /// </summary>
    public static class AppearanceCloner
    {
        public class CloneResult
        {
            public bool Ok;
            public ushort BaseGraphicID;
            public ushort NumGraphics;
            public int DrawGroupsCopied;
            public int SpritesCopied;
            public int PalettesCopied;
            public string SourceFile;
        }

        /// <summary>
        /// Copies the appearance of <paramref name="sourceGuid"/> out of the game content at
        /// <paramref name="gameDir"/> into <paramref name="target"/>, and returns the OBJD
        /// graphics fields the caller must set on the cloned object. Chunk IDs are preserved
        /// exactly as the source used them, because BaseGraphicID indexes draw groups by
        /// their chunk id — renumbering would require rewriting that reference for no gain.
        /// </summary>
        public static CloneResult Clone(uint sourceGuid, string gameDir, IffFile target, Diagnostics d, string diagPath)
        {
            var result = new CloneResult();

            var tablePath = Path.Combine(gameDir, "packingslips", "objecttable.xml");
            if (!File.Exists(tablePath))
            {
                d.Error(diagPath, $"clone_from_guid: object table not found at \"{tablePath}\" — is gameDir a TSO content directory?");
                return result;
            }

            var fileName = LookupFileName(tablePath, sourceGuid);
            if (fileName == null)
            {
                d.Error(diagPath, $"clone_from_guid: GUID 0x{sourceGuid:X8} not found in the base game object table");
                return result;
            }
            result.SourceFile = fileName;

            var objectsDir = FindObjectsDir(gameDir);
            if (objectsDir == null)
            {
                d.Error(diagPath, $"clone_from_guid: could not find objectdata/objects/ under \"{gameDir}\"");
                return result;
            }

            var iffBytes = ReadFarEntry(Path.Combine(objectsDir, "objiff.far"), fileName + ".iff");
            if (iffBytes == null)
            {
                d.Error(diagPath, $"clone_from_guid: \"{fileName}.iff\" not found in objiff.far");
                return result;
            }

            var sourceIff = ReadIff(iffBytes);
            var objd = sourceIff.List<OBJD>()?.FirstOrDefault(o => o.GUID == sourceGuid);
            if (objd == null)
            {
                d.Error(diagPath, $"clone_from_guid: \"{fileName}.iff\" has no OBJD for GUID 0x{sourceGuid:X8}");
                return result;
            }
            result.BaseGraphicID = objd.BaseGraphicID;
            result.NumGraphics = objd.NumGraphics;

            // Draw groups and sprites usually live in the paired .spf, not the .iff — but
            // check both, since that split isn't guaranteed for every object.
            var sources = new List<IffFile> { sourceIff };
            var spfBytes = FindSpfEntry(objectsDir, fileName + ".spf");
            if (spfBytes != null) sources.Add(ReadIff(spfBytes));

            foreach (var src in sources)
            {
                result.DrawGroupsCopied += CopyChunks(src.List<DGRP>(), target);
                result.SpritesCopied += CopyChunks(src.List<SPR2>(), target);
                result.SpritesCopied += CopyChunks(src.List<SPR>(), target);
                result.PalettesCopied += CopyChunks(src.List<PALT>(), target);
            }

            // SPR2Frame/SPRFrame decode lazily (Width/Height/PixelData stay 0/null until
            // DecodeIfRequired runs) — nothing else in the compile path ever calls it before
            // PackBuilder writes the target .iff, and SPR2Frame.Write serializes whatever's
            // currently in those fields with no fallback to the original raw bytes. Left
            // undecoded, every copied frame gets written out as a 0x0 sprite: chunks present,
            // DGRP resolves them, .iff loads fine — and the object is invisible in the client.
            // Force it here so the target file always holds real, already-decoded pixel data.
            ForceDecode(target);

            if (result.DrawGroupsCopied == 0)
            {
                d.Error(diagPath, $"clone_from_guid: no DGRP draw groups found for 0x{sourceGuid:X8} (\"{fileName}\") — the clone would be invisible");
                return result;
            }

            result.Ok = true;
            return result;
        }

        /// <summary>internal rather than private: PackCompilerApi's provenance stamping pass
        /// re-reads and rewrites an already-written .iff (see StampProvenance), which hits the
        /// identical lazy-decode hazard this exists for — reused rather than duplicated.</summary>
        internal static void ForceDecode(IffFile target)
        {
            var spr2Chunks = target.List<SPR2>();
            if (spr2Chunks != null)
                foreach (var spr2 in spr2Chunks)
                    foreach (var frame in spr2.Frames)
                    {
                        // Must be called with z=false first: DecodeIfRequired's z-buffer branch
                        // guards on this.Flags, which is itself only populated by the decode it
                        // guards — before the first decode, Flags is still its default 0, so an
                        // initial DecodeIfRequired(true) silently short-circuits and decodes
                        // nothing at all. z=false's guard checks PixelData==null instead, which
                        // is a real precondition, so it actually runs — and the single decode it
                        // triggers reads pixels AND the z-buffer together (whichever the frame's
                        // real flags call for), so nothing here is left half-decoded.
                        frame.DecodeIfRequired(false);
                        RestorePalData(frame, target);
                    }

            var sprChunks = target.List<SPR>();
            if (sprChunks != null)
                foreach (var spr in sprChunks)
                    foreach (var frame in spr.Frames)
                        frame.DecodeIfRequired();
        }

        /// <summary>
        /// SPR2Frame.Decode deliberately discards PalData after decoding (SPR2.cs:480, gated
        /// on the same IffFile.RETAIN_CHUNK_DATA flag) — a memory optimization that's correct
        /// for the game client's read-only rendering path, which never needs it again, but
        /// wrong here: SPR2FrameEncoder.WriteFrame indexes into PalData unconditionally, so a
        /// decoded-then-rewritten frame needs it back. Flipping the global RETAIN_CHUNK_DATA
        /// flag instead would work, but it's a process-wide static — a compile that races
        /// against another compile (xunit parallelizes test collections by default) could
        /// flip it mid-decode elsewhere. Reconstructing PalData locally from the now-decoded
        /// PixelData against the frame's own copied palette needs no shared mutable state.
        /// </summary>
        private static void RestorePalData(SPR2Frame frame, IffFile target)
        {
            if (frame.PalData != null || frame.PixelData == null) return;

            var palette = target.Get<PALT>(frame.PaletteID);
            if (palette == null) return;

            var indexByColor = new Dictionary<uint, byte>();
            for (int i = 0; i < palette.Colors.Length; i++)
            {
                var key = PackRgb(palette.Colors[i]);
                if (!indexByColor.ContainsKey(key)) indexByColor[key] = (byte)i; // first match wins; visually identical either way
            }

            var palData = new byte[frame.PixelData.Length];
            for (int i = 0; i < frame.PixelData.Length; i++)
            {
                var px = frame.PixelData[i];
                if (px.A == 0) { palData[i] = (byte)frame.TransparentColorIndex; continue; }
                palData[i] = indexByColor.TryGetValue(PackRgb(px), out var idx) ? idx : (byte)frame.TransparentColorIndex;
            }
            frame.PalData = palData;
        }

        private static uint PackRgb(Color c) => ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;

        private static int CopyChunks<T>(List<T> chunks, IffFile target) where T : IffChunk
        {
            if (chunks == null) return 0;
            var count = 0;
            foreach (var chunk in chunks)
            {
                // Skip ids the pack's own chunks already occupy rather than throwing from
                // AddChunk's dictionary — the compiler's own reserved ids are documented in
                // PackBuilder and don't overlap sprite/drawgroup ranges in practice.
                if (target.Get<T>(chunk.ChunkID) != null) continue;
                target.AddChunk(chunk);
                count++;
            }
            return count;
        }

        private static string LookupFileName(string tablePath, uint guid)
        {
            var wanted = "0x" + guid.ToString("X8");
            foreach (var line in File.ReadLines(tablePath))
            {
                var m = Regex.Match(line, "<I g=\"(?<g>0x[0-9A-Fa-f]+)\" n=\"(?<n>[^\"]+)\"");
                if (!m.Success) continue;
                if (string.Equals(m.Groups["g"].Value, wanted, StringComparison.OrdinalIgnoreCase))
                    return m.Groups["n"].Value;
            }
            return null;
        }

        // The content tree's casing differs between installs (ObjectData vs objectdata).
        private static string FindObjectsDir(string gameDir)
        {
            foreach (var dir in Directory.GetDirectories(gameDir))
            {
                if (!string.Equals(Path.GetFileName(dir), "objectdata", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var sub in Directory.GetDirectories(dir))
                    if (string.Equals(Path.GetFileName(sub), "objects", StringComparison.OrdinalIgnoreCase)) return sub;
            }
            return null;
        }

        private static byte[] FindSpfEntry(string objectsDir, string entryName)
        {
            foreach (var far in Directory.GetFiles(objectsDir, "objspf*.far"))
            {
                var bytes = ReadFarEntry(far, entryName);
                if (bytes != null) return bytes;
            }
            return null;
        }

        private static byte[] ReadFarEntry(string farPath, string entryName)
        {
            if (!File.Exists(farPath)) return null;
            // v1b: true for TSO archives — FAR1Provider passes !TS1, and these are TSO's.
            var archive = new FAR1Archive(farPath, true);
            try
            {
                var entry = archive.GetAllFarEntries()
                    .FirstOrDefault(e => string.Equals(e.Filename, entryName, StringComparison.OrdinalIgnoreCase));
                return entry == null ? null : archive.GetEntry(entry);
            }
            finally
            {
                archive.Close();
            }
        }

        private static IffFile ReadIff(byte[] bytes)
        {
            var iff = new IffFile();
            using (var stream = new MemoryStream(bytes)) iff.Read(stream);
            return iff;
        }
    }
}
