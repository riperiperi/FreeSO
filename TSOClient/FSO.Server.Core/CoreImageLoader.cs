using FSO.Content;
using FSO.Content.Model;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Server.Database.DA.Avatars;
using FSO.Vitaboy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;
using SixLabors.ImageSharp.Processing.Transforms.Resamplers;
using SixLabors.Primitives;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Server.Core
{
    public class CoreImageLoader
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Writes the destination file atomically: write to "<destPath>.tmp" first,
        /// then rename over the destination. Prevents readers (the API) from seeing a
        /// truncated file if the process dies mid-write.
        /// </summary>
        private static void AtomicWritePng(string destPath, Action<Stream> write)
        {
            var tempPath = destPath + ".tmp";
            using (var fs = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                write(fs);
            // .NET Core 2.2 has no File.Move(src, dest, overwrite); fall back to Replace.
            if (File.Exists(destPath))
                File.Replace(tempPath, destPath, null);
            else
                File.Move(tempPath, destPath);
        }

        public static void GenerateAvatarThumbnail(DbAvatar avatar, string nfsDir)
        {
            try
            {
                var content = FSO.Content.Content.Get();
                var outfit = content.AvatarOutfits.Get(avatar.head);
                if (outfit == null) return;
                var appId = outfit.GetAppearance((AppearanceType)avatar.skin_tone);
                var appearance = content.AvatarAppearances.Get(appId);
                if (appearance == null) return;
                var texRef = content.AvatarThumbnails.Get(appearance.ThumbnailTypeID, appearance.ThumbnailFileID);
                if (texRef == null) return;
                var bitmap = texRef.GetImage();
                if (bitmap == null || bitmap.Data == null || bitmap.Data.Length == 0) return;

                var dir = Path.Combine(nfsDir, "Avatars/" + avatar.avatar_id.ToString("x8"));
                Directory.CreateDirectory(dir);
                using (var image = Image.LoadPixelData<Bgra32>(bitmap.Data, bitmap.Width, bitmap.Height))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(512, 512),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.NearestNeighbor
                    }));
                    AtomicWritePng(Path.Combine(dir, "head.png"), fs => image.SaveAsPng(fs));
                }
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "GenerateAvatarThumbnail failed for avatar_id={0}", avatar?.avatar_id);
            }
        }

        public static void GenerateObjectThumbnail(uint guid, string nfsDir)
        {
            try
            {
                var content = FSO.Content.Content.Get();
                var obj = content.WorldObjects.Get(guid);
                if (obj == null) return;
                var objd = obj.OBJ;

                var dir = Path.Combine(nfsDir, "Objects/" + guid.ToString("x8"));
                var thumbPath = Path.Combine(dir, "thumb.png");

                // Primary path: DGRP/SPR2 sprite composite of every tile in the multi-tile
                // group, matching the in-game 2D isometric catalog renderer.
                // TSO objects store sprites in the .spf file; TS1/standalone keep them in the IFF.
                IffFile spriteIff = null;
                var worldProvider = content.WorldObjects as WorldObjectProvider;
                if (worldProvider != null)
                    spriteIff = worldProvider.GetSpritesFile(obj.Resource.Name + ".spf");
                if (spriteIff == null)
                    spriteIff = obj.Resource.Iff;

                if (spriteIff != null && TryRenderDGRPGroup(obj, objd, spriteIff, dir, thumbPath))
                    return;

                // Fallback: BMP chunk (some objects only have this; may contain 2 views side-by-side).
                var bmp = obj.Resource.Get<BMP>(objd.ThumbnailGraphic);
                if (bmp != null && bmp.data != null && bmp.data.Length > 0)
                {
                    Directory.CreateDirectory(dir);
                    using (var img = Image.Load(new MemoryStream(bmp.data)))
                    {
                        // If the BMP is wider than tall it likely contains 2 views side-by-side;
                        // crop to just the left square.
                        if (img.Width > img.Height)
                            img.Mutate(x => x.Crop(new Rectangle(0, 0, img.Height, img.Height)));
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(512, 512),
                            Mode = ResizeMode.Max,
                            Sampler = KnownResamplers.Lanczos3
                        }));
                        AtomicWritePng(thumbPath, fs => img.SaveAsPng(fs));
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "GenerateObjectThumbnail failed for guid=0x{0:X8}", guid);
            }
        }

        // Tile descriptor used by the multi-tile compositor.
        private struct TileToRender
        {
            public DGRP DGRP;
            public int TileX;
            public int TileY;
            public int Level;
        }

        // Per-zoom rendering geometry. Values mirror tso.world WorldState.WorldSpace.Invalidate
        // and the OneUnitDistance / floor-stride math; see comments below for the source.
        // Adding Medium/Far is a matter of constructing a new ZoomGeometry instance and using
        // the matching DGRP zoom value (Far=1, Medium=2, Near=3).
        private struct ZoomGeometry
        {
            public uint DgrpZoom;            // matches DGRP image Zoom field
            public int AnchorX;              // CadgeWidth / 2
            public int AnchorY;              // CadgeBaseLine
            public int TileHalfW;            // TilePxWidth / 2
            public int TileHalfH;            // TilePxHeight / 2
            public float OneUnitDistCos30;   // OneUnitDistance * cos(30°) — vertical px per Z tile
            public float FloorPxHeight;      // OneUnitDistCos30 * WorldUnitsPerTile (per-level stride)
        }

        // Near zoom — what the in-game catalog uses. Sources from WorldSpace.Invalidate:
        //   TilePxWidth=128, TilePxHeight=64, CadgeWidth=136, CadgeBaseLine=348
        //   OneUnitDistance = sqrt(128^2 / 2) ≈ 90.51,  * cos(30°) ≈ 78.4
        //   WorldUnitsPerTile = 3.0  →  per-floor stride ≈ 78.4 * 2.95 (game uses 2.95, not 3)
        private static readonly ZoomGeometry NearZoom = new ZoomGeometry
        {
            DgrpZoom = 3,
            AnchorX = 68,
            AnchorY = 348,
            TileHalfW = 64,
            TileHalfH = 32,
            OneUnitDistCos30 = 78.4f,
            FloorPxHeight = 78.4f * 2.95f,
        };

        // Direction probe order when the canonical LeftFront (0x10) view is missing.
        // In-game catalog renders NORTH-facing objects with a BottomRight camera, which
        // maps to DGRP direction 0x10. Some asymmetric objects only ship a subset of
        // directions; without a deterministic order we'd fall back to FirstOrDefault and
        // pick whichever happens to be first in the IFF — that's what produced the
        // 180°-off thumbnails. Probe LeftBack/RightFront/RightBack in that order.
        private static readonly uint[] DGRP_DIRECTION_PROBE = { 0x10, 0x40, 0x04, 0x01 };

        private static DGRPImage GetCatalogImage(DGRP dgrp, uint zoom)
        {
            foreach (var d in DGRP_DIRECTION_PROBE)
            {
                var img = dgrp.GetImage(d, zoom, 0);
                if (img != null && img.Sprites != null && img.Sprites.Length > 0) return img;
            }
            return dgrp.Images?.FirstOrDefault(i => i.Sprites != null && i.Sprites.Length > 0);
        }

        // Per-sprite resolved entry — replaces the previous per-tile resolved list so
        // depth sorting and screen placement happen at sprite granularity, matching
        // what tso.world's DGRPRenderer + Z-buffer does in-game.
        private struct ResolvedSprite
        {
            public SPR2Frame Frame;
            public int ScreenX;   // top-left of frame on the un-shifted canvas
            public int ScreenY;
            public bool Flip;
            public float Depth;   // ascending = drawn first (back to front)
        }

        private static bool TryRenderDGRPGroup(GameObject obj, OBJD masterObjd, IffFile spriteIff, string dir, string thumbPath)
        {
            // Build the list of tiles to render. Multi-tile objects: master has SubIndex == -1
            // and contributes no graphics; iterate the IFF's OBJDs to find every sub-OBJD with
            // the same MasterID, decode its tile offset from SubIndex, and use its BaseGraphicID.
            var tiles = new List<TileToRender>();
            if (masterObjd.MasterID != 0 && masterObjd.SubIndex == -1)
            {
                var allObjd = obj.Resource.Iff.List<OBJD>();
                if (allObjd != null)
                {
                    foreach (var sub in allObjd)
                    {
                        if (sub.MasterID != masterObjd.MasterID || sub.SubIndex == -1) continue;
                        if (sub.BaseGraphicID == 0) continue;
                        var subDgrp = spriteIff.Get<DGRP>(sub.BaseGraphicID);
                        if (subDgrp == null) continue;
                        // SubIndex packs the tile offset: high byte = X, low byte = Y (signed).
                        int tx = (sbyte)((ushort)sub.SubIndex >> 8);
                        int ty = (sbyte)((ushort)sub.SubIndex & 0xFF);
                        int lvl = (sbyte)sub.LevelOffset;
                        tiles.Add(new TileToRender { DGRP = subDgrp, TileX = tx, TileY = ty, Level = lvl });
                    }
                }
            }
            else
            {
                DGRP dgrp = null;
                if (masterObjd.BaseGraphicID > 0)
                    dgrp = spriteIff.Get<DGRP>(masterObjd.BaseGraphicID);
                if (dgrp == null)
                    dgrp = spriteIff.List<DGRP>()?.FirstOrDefault();
                if (dgrp != null)
                    tiles.Add(new TileToRender { DGRP = dgrp, TileX = 0, TileY = 0, Level = 0 });
            }
            if (tiles.Count == 0) return false;

            // Match the in-game 2D catalog renderer:
            //   WorldRotation.BottomRight (2) + Direction.NORTH (0x01) → DGRP direction 0x10
            //   (LeftFront). The DGRP sprites for that direction are anchored assuming the
            //   BottomRight tile→screen formula, so we use that same formula below when
            //   placing each tile.
            var z = NearZoom;
            const int pad = 16;

            // Resolve every sprite (across every tile) into a flat list with its screen
            // position and a 3D-derived depth. Sorting/compositing per-SPRITE rather than
            // per-tile matches DGRPRenderer.ValidateSprite (tso.world) — without it,
            // multi-sprite multi-tile objects layer wrong because sprites within
            // different tiles can interleave in screen space.
            var resolved = new List<ResolvedSprite>();
            foreach (var t in tiles)
            {
                var image = GetCatalogImage(t.DGRP, z.DgrpZoom);
                if (image == null) continue;

                // BottomRight isometric tile→screen + per-floor stride.
                int tileSx = (-t.TileX + t.TileY) * z.TileHalfW;
                int tileSy = (-t.TileX - t.TileY) * z.TileHalfH - (int)(t.Level * z.FloorPxHeight);
                int tileAnchorX = z.AnchorX + tileSx;
                int tileAnchorY = z.AnchorY + tileSy;

                foreach (var spr in image.Sprites)
                {
                    if (spr == null) continue;
                    var s2 = spriteIff.Get<SPR2>((ushort)spr.SpriteID);
                    if (s2 == null || spr.SpriteFrameIndex >= (uint)s2.Frames.Length) continue;
                    var frame = s2.Frames[(int)spr.SpriteFrameIndex];
                    frame.DecodeIfRequired(false);
                    if (frame.PixelData == null || frame.Width <= 0 || frame.Height <= 0) continue;

                    // Per-sprite ObjectOffset (tso.world DGRPRenderer.ValidateSprite):
                    //   centerRelative = ObjectOffset * (1/16, 1/16, 1/5)
                    //   pxOff = GetScreenFromTile(centerRelative, BottomRight)
                    //   sprite.DestRect.X/Y += pxOff
                    // Object's facing direction is NORTH for catalog thumbs, so
                    // RadianDirection = 0 and the rotation matrix is identity — we can
                    // skip Vector3.Transform and use the components directly.
                    float crX = spr.ObjectOffset.X / 16f;
                    float crY = spr.ObjectOffset.Y / 16f;
                    float crZ = spr.ObjectOffset.Z / 5f;
                    float pxOffX = (-crX + crY) * z.TileHalfW;
                    float pxOffY = (-crX - crY) * z.TileHalfH - crZ * z.OneUnitDistCos30;

                    int x = tileAnchorX + (int)spr.SpriteOffset.X + (int)pxOffX;
                    int y = tileAnchorY - frame.Height + (int)spr.SpriteOffset.Y + (int)pxOffY;

                    // Per-sprite back-to-front depth in tile-space world coords:
                    //   - Within a floor: larger (worldX + worldY) is farther from the
                    //     BottomRight camera → smaller depth = drawn first.
                    //   - Across floors: higher worldZ paints on top → larger depth.
                    //   - Z-major weighting ensures upper-floor sprites always sort
                    //     above any ground-floor sprite regardless of X+Y.
                    float worldX = t.TileX + crX;
                    float worldY = t.TileY + crY;
                    float worldZ = t.Level * 2.95f + crZ;
                    float depth = worldZ * 1000f - (worldX + worldY);

                    resolved.Add(new ResolvedSprite
                    {
                        Frame = frame,
                        ScreenX = x,
                        ScreenY = y,
                        Flip = spr.Flip,
                        Depth = depth,
                    });
                }
            }
            if (resolved.Count == 0) return false;
            resolved.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            // Pass 1 – bounding box across all sprites.
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var r in resolved)
            {
                if (r.ScreenX < minX) minX = r.ScreenX;
                if (r.ScreenY < minY) minY = r.ScreenY;
                if (r.ScreenX + r.Frame.Width > maxX) maxX = r.ScreenX + r.Frame.Width;
                if (r.ScreenY + r.Frame.Height > maxY) maxY = r.ScreenY + r.Frame.Height;
            }
            if (minX == int.MaxValue || maxX <= minX || maxY <= minY) return false;

            int canvasW = maxX - minX + pad * 2;
            int canvasH = maxY - minY + pad * 2;
            var canvasBytes = new byte[canvasW * canvasH * 4]; // RGBA, zeroed = transparent

            // Pass 2 – composite back-to-front in sorted order.
            foreach (var r in resolved)
            {
                int baseX = r.ScreenX - minX + pad;
                int baseY = r.ScreenY - minY + pad;
                int fw = r.Frame.Width, fh = r.Frame.Height;
                bool flip = r.Flip;
                var pixels = r.Frame.PixelData;

                for (int py = 0; py < fh; py++)
                {
                    for (int px = 0; px < fw; px++)
                    {
                        var c = pixels[py * fw + px];
                        if (c.A == 0) continue;
                        int cx = baseX + (flip ? fw - 1 - px : px);
                        int cy = baseY + py;
                        if ((uint)cx >= (uint)canvasW || (uint)cy >= (uint)canvasH) continue;
                        int idx = (cy * canvasW + cx) * 4;
                        canvasBytes[idx]     = c.R;
                        canvasBytes[idx + 1] = c.G;
                        canvasBytes[idx + 2] = c.B;
                        canvasBytes[idx + 3] = c.A;
                    }
                }
            }

            Directory.CreateDirectory(dir);
            using (var canvas = Image.LoadPixelData<Rgba32>(canvasBytes, canvasW, canvasH))
            {
                // Scale to 512×512 with Lanczos3 — smooth but preserves pixel-art edges better
                // than bilinear for small sprites.
                canvas.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(512, 512),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));
                AtomicWritePng(thumbPath, fs => canvas.SaveAsPng(fs));
            }
            return true;
        }

        public static TexBitmap SoftImageFetch(Stream stream, AbstractTextureRef texRef)
        {
            Image<Rgba32> result = null;
            try
            {
                result = Image.Load(stream);
            }
            catch (Exception)
            {
                return new TexBitmap() { Data = new byte[0] };
            }
            stream.Close();
            
            if (result == null) return null;
            var data = result.SavePixelData();

            for (int i = 0; i < data.Length; i += 4)
            {
                var temp = data[i];
                data[i] = data[i + 2];
                data[i + 2] = temp;
            }

            return new TexBitmap
            {
                Data = data,
                Width = result.Width,
                Height = result.Height,
                PixelSize = 4
            };
        }
    }
}
