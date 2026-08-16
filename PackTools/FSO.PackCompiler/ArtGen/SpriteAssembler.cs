using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;

namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Assembles a set of 12 rendered (zoom, direction) frames into a real DGRP/SPR2/PALT/OBJD
    /// chunk set — the same shape PackBuilder.cs builds for compiled pack objects, using the
    /// unmodified production SPR2FrameEncoder (via SPR2Frame.SetData/Write). This is the
    /// generated-appearance counterpart to AppearanceCloner (which copies an existing
    /// appearance); this one assembles a newly rendered one.
    /// </summary>
    public static class SpriteAssembler
    {
        public static readonly (uint dir, string name)[] Directions =
        {
            (0x01, "RightBack"),
            (0x04, "RightFront"),
            (0x10, "LeftFront"),
            (0x40, "LeftBack"),
        };

        public static readonly (uint zoom, string name)[] Zooms =
        {
            (1, "Far"),
            (2, "Medium"),
            (3, "Near"),
        };

        public static readonly (double yawDeg, string dirName)[] YawByDirection =
        {
            (315, "RightBack"),
            (45, "RightFront"),
            (135, "LeftFront"),
            (225, "LeftBack"),
        };

        public static readonly (double pxPerWorldUnit, string zoomName)[] ScaleByZoom =
        {
            (7.31, "Far"),
            (14.85, "Medium"),
            (29.93, "Near"),
        };

        public static Dictionary<(uint zoom, string dirName), RenderedFrame> RenderAllFrames(Mesh mesh)
        {
            var rendered = new Dictionary<(uint, string), RenderedFrame>();
            foreach (var (pxPerWorldUnit, zoomName) in ScaleByZoom)
            {
                var zoomNum = (uint)(Array.FindIndex(Zooms, z => z.name == zoomName) + 1);
                foreach (var (yawDeg, dirName) in YawByDirection)
                {
                    var yaw = yawDeg * Math.PI / 180.0;
                    rendered[(zoomNum, dirName)] = Renderer.Render(mesh, yaw, Camera.Pitch, pxPerWorldUnit);
                }
            }
            return rendered;
        }

        /// <summary>
        /// Adds PALT/SPR2/DGRP appearance chunks for a rendered frame set into an EXISTING
        /// IffFile (e.g. one already compiled with real OBJD/BHAV/CTSS via the normal pack
        /// JSON pipeline) and sets that IffFile's given OBJD's BaseGraphicID/NumGraphics to
        /// point at them. Lets behavior come from the real compiler and appearance come from
        /// ArtGen in the same object, without a schema integration point for "appearance.generated"
        /// yet existing (ART-PIPELINE-DESIGN.md §6 step 4 — future work).
        /// </summary>
        public static void AddAppearanceChunks(IffFile iff, OBJD objd, string objectName, ushort chunkId,
            Dictionary<(uint zoom, string dirName), RenderedFrame> rendered)
        {
            // SPR2Frame.SetData quantizes through this static delegate; nothing sets it by
            // default outside FSO.IDE's own startup path, so the first SetData call here
            // would otherwise NullReferenceException.
            SimpleQuantizer.Install();
            // One PALT per object means one palette per object. Without this reset the
            // accumulator carried colours across every object in a build: past 255 entries
            // each further colour clamped to index 254, so every object after the ~sixth
            // rendered as a single flat colour — the "green toilet" that made imported
            // furniture look untextured in game. Only the calibration tests reset, which is
            // why single-object builds always looked right.
            SimpleQuantizer.Reset();

            objd.BaseGraphicID = chunkId;
            objd.NumGraphics = 1;

            var palt = NewChunk<PALT>(iff, chunkId, objectName + " palette");
            palt.Colors = new Color[256];

            var spr2 = NewChunk<SPR2>(iff, chunkId, objectName + " sprites");
            spr2.DefaultPaletteID = chunkId;
            var frames = new List<SPR2Frame>();
            var frameIndexOf = new Dictionary<(uint, string), int>();

            foreach (var kv in rendered)
            {
                var (zoom, _) = kv.Key;
                var rf = kv.Value;
                var sf = new SPR2Frame(spr2);
                // Place the tight render into the SPR2 "cage" the way base-game furniture
                // and FSO.IDE AutoOffset do (DGRPEditor.cs): half-cage width 68, baseline
                // distance 348, feet a further 24*zFactor below the baseline. zFactor is
                // 1 / 0.5 / 0.25 at Near / Medium / Far. Without this, SpriteOffset stays
                // (0,0) and DGRP3DMesh reconstructs the mesh at the wrong cage origin.
                var zFactor = ZoomFactor(zoom);
                int posX = (int)Math.Round(68 * zFactor - rf.Width / 2.0);
                int posY = (int)Math.Round(372 * zFactor - rf.Height);
                var rect = new Rectangle(posX, posY, rf.Width, rf.Height);
                var quantPalette = sf.SetData(rf.Pixels, rf.Z, rect);
                // All frames of one object must decode against the SAME palette (one PALT per
                // DGRP/SPR2 chunk set), so every frame's pixel indices mean the same colors.
                // A stateful quantizer (e.g. SimpleQuantizer) returns the full color set
                // accumulated so far on every call, growing monotonically — so keep
                // overwriting with each call; by the last frame it's the complete superset.
                // A quantizer that returns an independent per-frame-local palette would break
                // this (frame N's indices wouldn't mean what the captured palette says they
                // mean) — that contract lives with whichever QuantizeFrame implementation is
                // installed, not here.
                if (quantPalette.Length > 0)
                    Array.Copy(quantPalette, palt.Colors, Math.Min(256, quantPalette.Length));
                sf.PaletteID = chunkId;
                frameIndexOf[kv.Key] = frames.Count;
                frames.Add(sf);
            }
            spr2.Frames = frames.ToArray();

            var dgrp = NewChunk<DGRP>(iff, chunkId, objectName + " drawgroup");
            var images = new List<DGRPImage>();
            foreach (var kv in rendered)
            {
                var (zoom, dirName) = kv.Key;
                var dirBit = Directions.First(x => x.name == dirName).dir;
                var frame = frames[frameIndexOf[kv.Key]];
                var zFactor = ZoomFactor(zoom);
                // Same formula as FSO.IDE ResourceBrowser.DGRPEditor.AutoOffset.
                var spriteOffset = new Vector2(
                    (int)((-68 * zFactor) + frame.Position.X),
                    (-348 * zFactor) + frame.Height + frame.Position.Y);
                var img = new DGRPImage(dgrp) { Direction = dirBit, Zoom = zoom };
                img.Sprites = new[]
                {
                    new DGRPSprite(dgrp)
                    {
                        SpriteID = chunkId,
                        SpriteFrameIndex = (uint)frameIndexOf[kv.Key],
                        SpriteOffset = spriteOffset,
                    },
                };
                images.Add(img);
            }
            dgrp.Images = images.ToArray();
        }

        /// <summary>Near=1, Medium=1/2, Far=1/4 — matches FSO.IDE AutoOffset zFactor.</summary>
        public static float ZoomFactor(uint zoom) => zoom switch
        {
            3 => 1.0f,
            2 => 0.5f,
            _ => 0.25f,
        };

        /// <summary>Renders all 12 frames for a mesh and assembles them into a standalone .iff (own OBJD, no behavior).</summary>
        public static IffFile BuildIff(Mesh mesh, string objectName, uint guid, ushort chunkId,
            out Dictionary<(uint zoom, string dirName), RenderedFrame> rendered)
        {
            rendered = RenderAllFrames(mesh);

            var iff = new IffFile();
            var objd = NewChunk<OBJD>(iff, 1, objectName);
            objd.ObjectType = OBJDType.Normal;
            objd.GUID = guid;

            AddAppearanceChunks(iff, objd, objectName, chunkId, rendered);
            return iff;
        }

        static T NewChunk<T>(IffFile iff, ushort id, string label) where T : IffChunk, new()
        {
            var chunk = new T
            {
                ChunkID = id,
                ChunkLabel = label ?? "",
                ChunkProcessed = true,
                ChunkParent = iff,
                ChunkType = IffFile.CHUNK_TYPES.First(x => x.Value == typeof(T)).Key,
            };
            iff.AddChunk(chunk);
            return chunk;
        }
    }
}
