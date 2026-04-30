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

            // Match the in-game 2D catalog renderer exactly:
            //   WorldRotation.BottomRight (2) + Direction.NORTH (0x01) → DGRP direction 0x10
            //   (LeftFront). The DGRP sprites for that direction are anchored assuming the
            //   BottomRight tile→screen formula, so we MUST use that same formula when
            //   placing each tile or multi-tile objects (beds, sofas) end up scrambled.
            const uint dgrpDirection = 0x10;
            const uint dgrpZoom = 3;          // Near
            const uint dgrpRotation = 0;      // already-rotated direction supplied directly

            // Near-zoom rendering constants from WorldSpace.Invalidate:
            //   TilePxWidth = 128, TilePxHeight = 64 → halves 64, 32
            //   CadgeWidth = 136 → anchorX = 68
            //   CadgeBaseLine = 348 → anchorY
            //   OneUnitDistance = sqrt(128^2 / 2) ≈ 90.51, * cos(30°) ≈ 78.4 px per Z unit
            const int anchorX = 68;
            const int anchorY = 348;
            const int tileHalfW = 64;
            const int tileHalfH = 32;
            const float floorPxHeight = 78.4f * 2.95f; // pixels per LevelOffset step
            const int pad = 16;

            // Resolve every tile's DGRPImage and per-tile screen offset, then sort
            // back-to-front so closer tiles overpaint farther ones (no Z-buffer here).
            var resolved = new List<(DGRPImage Image, int OffsetX, int OffsetY, int Depth)>();
            foreach (var t in tiles)
            {
                var image = t.DGRP.GetImage(dgrpDirection, dgrpZoom, dgrpRotation);
                if (image == null)
                    image = t.DGRP.Images?.FirstOrDefault(i => i.Sprites?.Length > 0);
                if (image == null || image.Sprites == null || image.Sprites.Length == 0) continue;

                // BottomRight isometric tile→screen + level offset.
                int sx = (-t.TileX + t.TileY) * tileHalfW;
                int sy = (-t.TileX - t.TileY) * tileHalfH - (int)(t.Level * floorPxHeight);
                // Depth: lower-right (front) tiles have larger depth and must draw last.
                // Higher levels also draw later (in front of lower floors).
                int depth = (-t.TileX - t.TileY) * 1000 + t.Level * 10000;
                // Negate so smaller "back" depth sorts first.
                resolved.Add((image, sx, sy, -depth));
            }
            if (resolved.Count == 0) return false;
            resolved.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            // Pass 1 – bounding box across all tiles.
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var r in resolved)
            {
                int tileAnchorX = anchorX + r.OffsetX;
                int tileAnchorY = anchorY + r.OffsetY;
                foreach (var spr in r.Image.Sprites)
                {
                    var s2 = spriteIff.Get<SPR2>((ushort)spr.SpriteID);
                    if (s2 == null || spr.SpriteFrameIndex >= (uint)s2.Frames.Length) continue;
                    var frame = s2.Frames[(int)spr.SpriteFrameIndex];
                    frame.DecodeIfRequired(false);
                    if (frame.PixelData == null || frame.Width <= 0 || frame.Height <= 0) continue;
                    int x = tileAnchorX + (int)spr.SpriteOffset.X;
                    int y = tileAnchorY - frame.Height + (int)spr.SpriteOffset.Y;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x + frame.Width > maxX) maxX = x + frame.Width;
                    if (y + frame.Height > maxY) maxY = y + frame.Height;
                }
            }
            if (minX == int.MaxValue || maxX <= minX || maxY <= minY) return false;

            int canvasW = maxX - minX + pad * 2;
            int canvasH = maxY - minY + pad * 2;
            var canvasBytes = new byte[canvasW * canvasH * 4]; // RGBA, zeroed = transparent

            // Pass 2 – composite back-to-front using the depth-sorted order from above so
            // closer tiles overpaint farther ones (no Z-buffer in the CPU compositor).
            foreach (var r in resolved)
            {
                int tileAnchorX = anchorX + r.OffsetX;
                int tileAnchorY = anchorY + r.OffsetY;
                foreach (var spr in r.Image.Sprites)
                {
                    var s2 = spriteIff.Get<SPR2>((ushort)spr.SpriteID);
                    if (s2 == null || spr.SpriteFrameIndex >= (uint)s2.Frames.Length) continue;
                    var frame = s2.Frames[(int)spr.SpriteFrameIndex];
                    frame.DecodeIfRequired(false);
                    if (frame.PixelData == null) continue;

                    int baseX = tileAnchorX + (int)spr.SpriteOffset.X - minX + pad;
                    int baseY = tileAnchorY - frame.Height + (int)spr.SpriteOffset.Y - minY + pad;
                    int fw = frame.Width, fh = frame.Height;
                    bool flip = spr.Flip;

                    for (int py = 0; py < fh; py++)
                    {
                        for (int px = 0; px < fw; px++)
                        {
                            var c = frame.PixelData[py * fw + px];
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
