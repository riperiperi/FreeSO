using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler;
using FSO.PackCompiler.ArtGen;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace FSO.ContactSheet
{
    /// <summary>
    /// Compiles every pack in a directory through the real compiler, decodes the emitted
    /// .iff fresh (new IffFile, not the in-memory chunks the compiler just built — this is
    /// the step that catches the palette/DGRP-locality class of bug that's bitten this
    /// project twice, per AppearanceCloneTests/GeneratedAppearanceTests' own reasoning), and
    /// composites one row per object x one column per zoom level into a single labeled PNG.
    /// One canonical direction (RightFront) per object — the point is judging a whole
    /// collection's look and palette coherence at a glance, not re-deriving all 12 frames.
    /// </summary>
    public static class ContactSheetBuilder
    {
        const uint RightFrontDirection = 0x04;
        static readonly (uint zoom, string name)[] Zooms = { (1, "FAR"), (2, "MEDIUM"), (3, "NEAR") };

        public class Cell
        {
            public string Label;      // object id + source, e.g. "chair_a (gen:chair)"
            public string PackFile;
            public List<string> Errors = new List<string>();
            public Dictionary<string, RenderedFrame> FramesByZoom = new Dictionary<string, RenderedFrame>(); // zoom name -> frame, missing = not rendered
        }

        public class RenderedFrame
        {
            public int Width, Height;
            public Color[] Pixels;
        }

        public static List<Cell> BuildCells(string packDir, string tsoDir)
        {
            var cells = new List<Cell>();
            var jsonFiles = Directory.GetFiles(packDir, "*.json").OrderBy(f => f, StringComparer.Ordinal).ToArray();

            foreach (var packPath in jsonFiles)
            {
                var packJson = JObject.Parse(File.ReadAllText(packPath));
                var sources = SourceTagsByObjectId(packJson);

                var outDir = Path.Combine(Path.GetTempPath(), "fso-contactsheet", Guid.NewGuid().ToString("N"));
                var result = PackCompilerApi.Build(packPath, outDir, tsoDir);

                if (!result.Success)
                {
                    cells.Add(new Cell
                    {
                        Label = Path.GetFileNameWithoutExtension(packPath) + " (COMPILE ERROR)",
                        PackFile = Path.GetFileName(packPath),
                        Errors = result.Diagnostics.Errors.ToList(),
                    });
                    continue;
                }

                foreach (var objReport in result.Report.Objects)
                {
                    var cell = new Cell
                    {
                        Label = objReport.Id + " (" + (sources.TryGetValue(objReport.Id, out var s) ? s : "?") + ")",
                        PackFile = Path.GetFileName(packPath),
                    };

                    var iffPath = Path.Combine(outDir, objReport.Iff);
                    if (!File.Exists(iffPath))
                    {
                        cell.Errors.Add("no .iff written for object \"" + objReport.Id + "\"");
                        cells.Add(cell);
                        continue;
                    }

                    // Fresh decode: a brand-new IffFile instance reading the file back off disk,
                    // never touching the IffFile the compiler built in-process.
                    var freshIff = new IffFile();
                    using (var stream = new FileStream(iffPath, FileMode.Open))
                        freshIff.Read(stream);

                    var objd = freshIff.List<OBJD>()?.FirstOrDefault();
                    if (objd == null || objd.BaseGraphicID == 0)
                    {
                        cell.Errors.Add("no graphics (BaseGraphicID=0) — invisible in the client");
                        cells.Add(cell);
                        continue;
                    }

                    var dgrp = freshIff.Get<DGRP>(objd.BaseGraphicID);
                    if (dgrp == null)
                    {
                        cell.Errors.Add("BaseGraphicID=" + objd.BaseGraphicID + " but no DGRP with that id — would render nothing");
                        cells.Add(cell);
                        continue;
                    }

                    foreach (var (zoom, zoomName) in Zooms)
                    {
                        // RightFront exists on every direction-varying object; for rotationally
                        // symmetric generators (lamp, round pedestal table) all four direction
                        // entries point at the same sprite anyway, so this is still correct.
                        var image = dgrp.Images.FirstOrDefault(i => i.Zoom == zoom && i.Direction == RightFrontDirection)
                                    ?? dgrp.Images.FirstOrDefault(i => i.Zoom == zoom);
                        if (image == null || image.Sprites.Length == 0)
                        {
                            cell.Errors.Add(zoomName + ": no image in DGRP");
                            continue;
                        }

                        var sprite = image.Sprites[0];
                        var spr2 = freshIff.Get<SPR2>((ushort)sprite.SpriteID);
                        if (spr2 == null)
                        {
                            cell.Errors.Add(zoomName + ": SPR2 " + sprite.SpriteID + " referenced by DGRP not found in this file");
                            continue;
                        }

                        var frame = spr2.Frames[sprite.SpriteFrameIndex];
                        frame.DecodeIfRequired(false);
                        if (frame.Width == 0 || frame.Height == 0 || (frame.PixelData?.All(p => p.A == 0) ?? true))
                        {
                            // Structural presence (chunk exists, DGRP resolves it) is not the
                            // same claim as "it renders" — this is exactly the gap that's bitten
                            // this project before. A frame that decodes to nothing is a real
                            // rendering failure, not a harness quirk, so it's reported like one.
                            cell.Errors.Add(zoomName + ": SPR2 frame decodes to " + frame.Width + "x" + frame.Height + " with no visible pixels");
                            continue;
                        }
                        cell.FramesByZoom[zoomName] = new RenderedFrame
                        {
                            Width = frame.Width,
                            Height = frame.Height,
                            Pixels = frame.PixelData,
                        };
                    }

                    cells.Add(cell);
                }
            }

            return cells;
        }

        /// <summary>id -> a short human-readable tag for how its appearance was produced, read straight
        /// from the pack JSON (not the build report) since that's where clone_from_guid/generated live.</summary>
        static Dictionary<string, string> SourceTagsByObjectId(JObject packJson)
        {
            var result = new Dictionary<string, string>();
            var objects = packJson["objects"] as JArray;
            if (objects == null) return result;

            foreach (var obj in objects)
            {
                var id = (string)obj["id"];
                if (id == null) continue;
                var appearance = obj["appearance"] as JObject;
                if (appearance == null) { result[id] = "no appearance"; continue; }

                var clone = (string)appearance["clone_from_guid"];
                if (clone != null) { result[id] = "clone:" + clone; continue; }

                var generated = appearance["generated"] as JObject;
                if (generated != null) { result[id] = "gen:" + (string)generated["generator"]; continue; }

                result[id] = "no appearance";
            }
            return result;
        }
    }
}
