using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using FSO.Files.FAR1;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler.ArtGen;
using Microsoft.Xna.Framework;

namespace FSO.EaBillboards
{
    /// <summary>
    /// Exports real TSO object art as browser billboards.
    ///
    /// The browser draws furniture as one PNG per object GUID (VmLotClient's
    /// texByGuid), because the DGRP sprite path through KNI's 2D batch is still
    /// blocked. Every EA object's *behaviour* already ships in the content bundle
    /// via objiff.far, so the only thing standing between the demo and real Sims
    /// furniture is a picture. The sprites live in objspf1-9.far — 425 MB, far too
    /// much to ship — but a lot only places a couple of dozen objects, so we render
    /// those few to PNG here, offline, against a full TSO install.
    ///
    /// usage: FSO.EaBillboards --tso-dir DIR --spec spec.json --out-dir DIR
    ///                         [--zoom near|medium|far] [--dir N]
    /// spec.json: [{ "id": "ea_bed_single", "guid": "0x0E82C943" }, ...]
    /// </summary>
    public static class Program
    {
        // DGRP zoom levels, and the direction every direction-varying object has.
        static readonly Dictionary<string, uint> Zooms =
            new Dictionary<string, uint> { { "far", 1 }, { "medium", 2 }, { "near", 3 } };
        const uint RightFrontDirection = 0x04;

        public static int Main(string[] args)
        {
            string tsoDir = null, spec = null, outDir = null, zoomName = "near", inspect = null;
            uint direction = RightFrontDirection;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--tso-dir": tsoDir = args[++i]; break;
                    case "--spec": spec = args[++i]; break;
                    case "--out-dir": outDir = args[++i]; break;
                    case "--zoom": zoomName = args[++i].ToLowerInvariant(); break;
                    case "--dir": direction = uint.Parse(args[++i]); break;
                    case "--inspect": inspect = args[++i]; break;
                    default:
                        Console.Error.WriteLine("unknown argument: " + args[i]);
                        return 2;
                }
            }
            if (tsoDir != null && inspect != null) return Inspect(tsoDir, inspect);
            if (tsoDir == null || spec == null || outDir == null || !Zooms.ContainsKey(zoomName))
            {
                Console.Error.WriteLine(
                    "usage: FSO.EaBillboards --tso-dir DIR --spec spec.json --out-dir DIR [--zoom near|medium|far] [--dir N]");
                return 2;
            }

            var wanted = ReadSpec(spec);
            if (wanted.Count == 0) { Console.Error.WriteLine("spec lists no objects"); return 2; }

            var names = ReadObjectTable(Path.Combine(tsoDir, "packingslips", "objecttable.xml"));
            Console.WriteLine($"objecttable: {names.Count} GUIDs");

            var iffs = new FAR1Archive(Path.Combine(tsoDir, "objectdata", "objects", "objiff.far"), true);
            var iffByName = Index(iffs);
            var spfArchives = Directory
                .GetFiles(Path.Combine(tsoDir, "objectdata", "objects"), "objspf*.far")
                .OrderBy(f => f, StringComparer.Ordinal)
                .Select(f => new FAR1Archive(f, true))
                .ToList();
            var spfByName = new Dictionary<string, (FAR1Archive far, FarEntry entry)>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var far in spfArchives)
                foreach (var e in far.GetAllFarEntries())
                    spfByName[e.Filename] = (far, e);
            Console.WriteLine($"archives: {iffByName.Count} iffs, {spfByName.Count} sprite files");

            Directory.CreateDirectory(outDir);
            var zoom = Zooms[zoomName];
            var rows = new List<(string id, string guid, string png)>();
            int failed = 0;
            foreach (var w in wanted)
            {
                try
                {
                    foreach (var r in Export(w, names, iffs, iffByName, spfByName, zoom, direction, outDir))
                    {
                        Console.WriteLine($"  {r.id,-24} {r.guid} -> {r.png ?? "(master, behaviour only)"}");
                        rows.Add(r);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"  {w.Id,-24} {w.GuidText} FAILED: {e.Message}");
                    failed++;
                }
            }

            // Manifest fragment: guid -> png is what the browser's billboard loader
            // keys on, and make_pack_manifest.py merges this alongside the compiled
            // packs so a regeneration does not drop these rows.
            var frag = Path.Combine(outDir, "ea-manifest.json");
            File.WriteAllText(frag, "[\n" + string.Join(",\n", rows.Select(r =>
                $" {{ \"id\": \"{r.id}\", \"iff\": null, \"guid\": \"{r.guid}\", " +
                $"\"png\": {(r.png == null ? "null" : "\"" + r.png + "\"")} }}")) + "\n]\n");
            Console.WriteLine($"{rows.Count} billboards, {failed} objects failed -> {outDir}");
            Console.WriteLine($"manifest fragment: {frag}");
            return failed == 0 ? 0 : 1;
        }

        /// <summary>
        /// Dump every OBJD in one object file: GUID, name, master/sub index and
        /// BaseGraphicID. Multi-tile masters carry BaseGraphicID=0 — their art is on
        /// their parts — so authoring a billboard spec means reading this first.
        /// </summary>
        static int Inspect(string tsoDir, string fileName)
        {
            var iffs = new FAR1Archive(Path.Combine(tsoDir, "objectdata", "objects", "objiff.far"), true);
            var byName = Index(iffs);
            if (!byName.TryGetValue(fileName + ".iff", out var entry))
            {
                Console.Error.WriteLine($"{fileName}.iff not in objiff.far");
                return 1;
            }
            var iff = new IffFile();
            using (var ms = new MemoryStream(iffs.GetEntry(entry))) iff.Read(ms);
            Console.WriteLine($"{fileName}.iff");
            foreach (var o in iff.List<OBJD>().OrderBy(o => o.MasterID).ThenBy(o => o.SubIndex))
                Console.WriteLine($"  0x{o.GUID:X8}  master=0x{o.MasterID:X4} sub={o.SubIndex,-6} " +
                    $"graphic={o.BaseGraphicID,-6} ttab={o.TreeTableID,-6} slot={o.SlotID,-4} {o.ChunkLabel}");
            return 0;
        }

        static List<(string id, string guid, string png)> Export(SpecEntry w,
            Dictionary<uint, string> names, FAR1Archive iffs,
            Dictionary<string, FarEntry> iffByName,
            Dictionary<string, (FAR1Archive far, FarEntry entry)> spfByName,
            uint zoom, uint direction, string outDir)
        {
            if (!names.TryGetValue(w.Guid, out var fileName))
                throw new Exception("GUID not in objecttable.xml — not a base-game object?");

            if (!iffByName.TryGetValue(fileName + ".iff", out var iffEntry))
                throw new Exception($"{fileName}.iff not in objiff.far");
            var iff = new IffFile();
            using (var ms = new MemoryStream(iffs.GetEntry(iffEntry))) iff.Read(ms);

            var objd = iff.List<OBJD>()?.FirstOrDefault(o => o.GUID == w.Guid);
            if (objd == null) throw new Exception($"no OBJD with GUID in {fileName}.iff");

            // A multi-tile master has no art of its own — every EA bed and sofa is
            // drawn by its parts. The VM instantiates those parts at their own tiles
            // anyway, so exporting one billboard per part lets the lot reassemble the
            // object correctly with no compositing here and no tile maths.
            var targets = new List<(string id, OBJD objd)>();
            var rowsOnly = new List<(string id, string guid, string png)>();
            if (objd.BaseGraphicID == 0 && objd.SubIndex == -1)
            {
                var parts = iff.List<OBJD>()
                    .Where(o => o.MasterID == objd.MasterID && o.SubIndex >= 0)
                    .OrderBy(o => o.SubIndex)
                    .ToList();
                if (parts.Count == 0)
                    throw new Exception("multi-tile master with no parts and no art");
                // The master itself gets a manifest row with no png: the host places
                // furniture by looking up id -> guid, and CreateObjectInstance on the
                // master is what spawns the whole group.
                rowsOnly.Add((w.Id, $"0x{objd.GUID:X8}", null));
                foreach (var part in parts)
                    targets.Add(($"{w.Id}_p{part.SubIndex}", part));
            }
            else if (objd.BaseGraphicID == 0)
            {
                throw new Exception("BaseGraphicID=0 and not a multi-tile master — no art");
            }
            else
            {
                targets.Add((w.Id, objd));
            }

            // Art lives in a separate archive keyed <name>.spf, the same pairing
            // WorldObjectProvider makes — DGRP, SPR2 and PALT all sit there for base
            // game objects, so look the draw group up in the sprite file first and
            // decode frames where their palette lives.
            if (!spfByName.TryGetValue(fileName + ".spf", out var spf))
                throw new Exception($"{fileName}.spf not in any objspf*.far");
            var sprIff = new IffFile();
            using (var ms = new MemoryStream(spf.far.GetEntry(spf.entry))) sprIff.Read(ms);

            var rows = new List<(string id, string guid, string png)>(rowsOnly);
            foreach (var (id, target) in targets)
            {
                var dgrp = sprIff.Get<DGRP>((ushort)target.BaseGraphicID)
                           ?? iff.Get<DGRP>((ushort)target.BaseGraphicID);
                if (dgrp == null)
                {
                    Console.Error.WriteLine($"  {id,-24} no DGRP {target.BaseGraphicID} in {fileName}.spf or .iff");
                    continue;
                }

                var image = dgrp.Images.FirstOrDefault(i => i.Zoom == zoom && i.Direction == direction)
                            ?? dgrp.Images.FirstOrDefault(i => i.Zoom == zoom);
                if (image == null || image.Sprites.Length == 0)
                {
                    Console.Error.WriteLine($"  {id,-24} DGRP has no sprites at zoom {zoom}");
                    continue;
                }

                var composed = Compose(image, sprIff, iff);
                if (composed.pixels == null)
                {
                    Console.Error.WriteLine($"  {id,-24} all sprites decoded empty");
                    continue;
                }

                var path = Path.Combine(outDir, id + ".png");
                PngWriter.Write(path, composed.pixels, composed.width, composed.height);
                rows.Add((id, $"0x{target.GUID:X8}", Path.GetFileName(path)));
            }
            if (rows.Count == 0) throw new Exception("nothing exported");
            return rows;
        }

        /// <summary>
        /// Composite every sprite of one DGRP image at its offset. Taking only
        /// Sprites[0] (as the contact sheet does, where one-sprite pack objects are
        /// the rule) loses whole halves of EA furniture — beds and sofas are drawn
        /// from several sprites.
        /// </summary>
        static (Color[] pixels, int width, int height) Compose(DGRPImage image, IffFile sprIff, IffFile objIff)
        {
            var parts = new List<(SPR2Frame frame, int x, int y, bool flip)>();
            foreach (var sprite in image.Sprites)
            {
                var spr2 = sprIff.Get<SPR2>((ushort)sprite.SpriteID) ?? objIff.Get<SPR2>((ushort)sprite.SpriteID);
                if (spr2 == null || sprite.SpriteFrameIndex >= spr2.Frames.Length) continue;
                var frame = spr2.Frames[sprite.SpriteFrameIndex];
                frame.DecodeIfRequired(false);
                if (frame.Width == 0 || frame.Height == 0 || frame.PixelData == null) continue;
                if (frame.PixelData.All(p => p.A == 0)) continue;
                parts.Add((frame, (int)sprite.SpriteOffset.X, (int)sprite.SpriteOffset.Y, sprite.Flip));
            }
            if (parts.Count == 0) return (null, 0, 0);

            var minX = parts.Min(p => p.x);
            var minY = parts.Min(p => p.y);
            var maxX = parts.Max(p => p.x + p.frame.Width);
            var maxY = parts.Max(p => p.y + p.frame.Height);
            var w = maxX - minX;
            var h = maxY - minY;
            var canvas = new Color[w * h];

            // Back to front: DGRP sprite order is already draw order.
            foreach (var (frame, ox, oy, flip) in parts)
            {
                for (int y = 0; y < frame.Height; y++)
                {
                    for (int x = 0; x < frame.Width; x++)
                    {
                        var src = frame.PixelData[y * frame.Width + (flip ? frame.Width - 1 - x : x)];
                        if (src.A == 0) continue;
                        canvas[(y + oy - minY) * w + (x + ox - minX)] = src;
                    }
                }
            }
            return (canvas, w, h);
        }

        static Dictionary<string, FarEntry> Index(FAR1Archive far)
        {
            var map = new Dictionary<string, FarEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in far.GetAllFarEntries()) map[e.Filename] = e;
            return map;
        }

        /// <summary>GUID → base file name, from the packing slip the engine uses.</summary>
        static Dictionary<uint, string> ReadObjectTable(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            var map = new Dictionary<uint, string>();
            foreach (XmlNode node in doc.GetElementsByTagName("I"))
            {
                var guidText = node.Attributes["g"]?.Value;
                var fileName = node.Attributes["n"]?.Value;
                if (guidText == null || fileName == null) continue;
                var guid = Convert.ToUInt32(guidText, 16);
                // "objectdata\objects\beds" → "beds"
                map[guid] = Path.GetFileName(fileName.Replace('\\', '/'));
            }
            return map;
        }

        static List<SpecEntry> ReadSpec(string path)
        {
            var list = new List<SpecEntry>();
            var json = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(path));
            foreach (var o in json)
            {
                var guidText = (string)o["guid"];
                if (guidText == null) continue;
                list.Add(new SpecEntry
                {
                    Id = (string)o["id"],
                    GuidText = guidText,
                    Guid = Convert.ToUInt32(guidText.Replace("0x", ""), 16),
                });
            }
            return list;
        }

        class SpecEntry
        {
            public string Id;
            public string GuidText;
            public uint Guid;
        }
    }
}
