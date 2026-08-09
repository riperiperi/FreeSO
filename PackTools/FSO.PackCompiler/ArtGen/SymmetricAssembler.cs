using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// For meshes with no directional feature (rotationally symmetric about the vertical
    /// axis — round tables on a pedestal, lamps), all 4 yaw directions render identically.
    /// ART-PIPELINE-CALIBRATION.md §6 found base-game objects like this reuse a single
    /// SpriteFrameIndex across all 4 DGRPImage direction entries rather than storing 4
    /// near-identical renders. This mirrors that: renders once per zoom (3 renders instead
    /// of 12) and points all 4 directions at the same frame.
    /// </summary>
    public static class SymmetricAssembler
    {
        public static Dictionary<(uint zoom, string dirName), RenderedFrame> RenderSymmetricFrames(Mesh mesh)
        {
            var rendered = new Dictionary<(uint, string), RenderedFrame>();
            foreach (var (pxPerWorldUnit, zoomName) in SpriteAssembler.ScaleByZoom)
            {
                var zoomNum = (uint)(Array.FindIndex(SpriteAssembler.Zooms, z => z.name == zoomName) + 1);
                // Any single yaw is representative; use the first direction's angle.
                var yaw = SpriteAssembler.YawByDirection[0].yawDeg * Math.PI / 180.0;
                var frame = Renderer.Render(mesh, yaw, Camera.Pitch, pxPerWorldUnit);
                foreach (var (_, dirName) in SpriteAssembler.YawByDirection)
                    rendered[(zoomNum, dirName)] = frame;
            }
            return rendered;
        }

        /// <summary>Renders a rotationally-symmetric mesh (3 unique renders, shared across all
        /// 4 directions) and assembles it into a standalone .iff, mirroring
        /// SpriteAssembler.BuildIff's shape for the non-symmetric case.</summary>
        public static IffFile BuildIff(Mesh mesh, string objectName, uint guid, ushort chunkId,
            out Dictionary<(uint zoom, string dirName), RenderedFrame> rendered)
        {
            rendered = RenderSymmetricFrames(mesh);

            var iff = new IffFile();
            var objd = new OBJD
            {
                ChunkID = 1,
                ChunkLabel = objectName,
                ChunkProcessed = true,
                ChunkParent = iff,
                ChunkType = IffFile.CHUNK_TYPES.First(x => x.Value == typeof(OBJD)).Key,
                ObjectType = OBJDType.Normal,
                GUID = guid,
            };
            iff.AddChunk(objd);

            SpriteAssembler.AddAppearanceChunks(iff, objd, objectName, chunkId, rendered);
            return iff;
        }
    }
}
