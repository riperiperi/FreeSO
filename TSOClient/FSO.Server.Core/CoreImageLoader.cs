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
using System;
using System.IO;
using System.Linq;

namespace FSO.Server.Core
{
    public class CoreImageLoader
    {
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
                    using (var fs = File.Open(Path.Combine(dir, "head.png"), FileMode.Create))
                        image.SaveAsPng(fs);
                }
            }
            catch { }
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

                // Primary path: DGRP/SPR2 sprite composite.
                // TSO objects store sprites in the .spf file; TS1/standalone keep them in the IFF.
                IffFile spriteIff = null;
                var worldProvider = content.WorldObjects as WorldObjectProvider;
                if (worldProvider != null)
                    spriteIff = worldProvider.GetSpritesFile(obj.Resource.Name + ".spf");
                if (spriteIff == null)
                    spriteIff = obj.Resource.Iff;

                if (spriteIff != null && TryRenderDGRP(objd, spriteIff, dir, thumbPath))
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
                        using (var fs = File.Open(thumbPath, FileMode.Create))
                            img.SaveAsPng(fs);
                    }
                }
            }
            catch { }
        }

        private static bool TryRenderDGRP(OBJD objd, IffFile spriteIff, string dir, string thumbPath)
        {
            // Select the DGRP for this object.  BaseGraphicID is the canonical reference;
            // fall back to the first DGRP in the file if it is unset.
            DGRP dgrp = null;
            if (objd.BaseGraphicID > 0)
                dgrp = spriteIff.Get<DGRP>(objd.BaseGraphicID);
            if (dgrp == null)
            {
                var all = spriteIff.List<DGRP>();
                dgrp = all?.FirstOrDefault();
            }
            if (dgrp == null) return false;

            // Near zoom (WorldZoom.Near = 3) gives the largest native sprites.
            // Direction 4 = RightFront (south-east) with worldRotation 0 → stored direction 4.
            // Near-zoom rendering constants from WorldSpace.Invalidate:
            //   CadgeWidth = 136  → anchorX = 68
            //   CadgeBaseLine = 348
            DGRPImage image = dgrp.GetImage(4, 3, 0);
            if (image == null)
                image = dgrp.Images?.FirstOrDefault(i => i.Sprites?.Length > 0);
            if (image == null || image.Sprites == null || image.Sprites.Length == 0) return false;

            const int anchorX = 68;
            const int anchorY = 348;
            const int pad = 16;

            // Pass 1 – bounding box.
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var spr in image.Sprites)
            {
                var s2 = spriteIff.Get<SPR2>((ushort)spr.SpriteID);
                if (s2 == null || spr.SpriteFrameIndex >= (uint)s2.Frames.Length) continue;
                var frame = s2.Frames[(int)spr.SpriteFrameIndex];
                frame.DecodeIfRequired(false);
                if (frame.PixelData == null || frame.Width <= 0 || frame.Height <= 0) continue;
                int x = anchorX + (int)spr.SpriteOffset.X;
                int y = anchorY - frame.Height + (int)spr.SpriteOffset.Y;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x + frame.Width > maxX) maxX = x + frame.Width;
                if (y + frame.Height > maxY) maxY = y + frame.Height;
            }
            if (minX == int.MaxValue || maxX <= minX || maxY <= minY) return false;

            int canvasW = maxX - minX + pad * 2;
            int canvasH = maxY - minY + pad * 2;
            var canvasBytes = new byte[canvasW * canvasH * 4]; // RGBA, zeroed = transparent

            // Pass 2 – composite back-to-front.
            foreach (var spr in image.Sprites)
            {
                var s2 = spriteIff.Get<SPR2>((ushort)spr.SpriteID);
                if (s2 == null || spr.SpriteFrameIndex >= (uint)s2.Frames.Length) continue;
                var frame = s2.Frames[(int)spr.SpriteFrameIndex];
                frame.DecodeIfRequired(false);
                if (frame.PixelData == null) continue;

                int baseX = anchorX + (int)spr.SpriteOffset.X - minX + pad;
                int baseY = anchorY - frame.Height + (int)spr.SpriteOffset.Y - minY + pad;
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
                using (var fs = File.Open(thumbPath, FileMode.Create))
                    canvas.SaveAsPng(fs);
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
