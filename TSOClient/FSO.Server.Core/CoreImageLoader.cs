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

                // Try BMP chunk first (a small subset of objects have this)
                var bmp = obj.Resource.Get<BMP>(objd.ThumbnailGraphic);
                if (bmp != null && bmp.data != null && bmp.data.Length > 0)
                {
                    Directory.CreateDirectory(dir);
                    using (var img = Image.Load(new MemoryStream(bmp.data)))
                    {
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(1024, 1024),
                            Mode = ResizeMode.Max,
                            Sampler = KnownResamplers.NearestNeighbor
                        }));
                        using (var fs = File.Open(thumbPath, FileMode.Create))
                            img.SaveAsPng(fs);
                    }
                    return;
                }

                // Fallback: composite sprite thumbnail from DGRP + SPR2 chunks.
                // TSO objects keep these in the .spf file; TS1 / standalone objects keep
                // them in the main .iff.  Try SPF first, then fall back to the main IFF.
                IffFile spriteIff = null;
                var worldProvider = content.WorldObjects as WorldObjectProvider;
                if (worldProvider != null)
                    spriteIff = worldProvider.GetSpritesFile(obj.Resource.Name + ".spf");
                if (spriteIff == null)
                    spriteIff = obj.Resource.Iff;
                if (spriteIff == null) return;

                var dgrps = spriteIff.List<DGRP>();
                if (dgrps == null || dgrps.Count == 0) return;

                // Medium zoom (WorldZoom.Medium = 2), south-east view (direction 4 = RightFront).
                // GetImage(direction, zoom, worldRotation=0) with direction=4, zoom=2, rot=0
                // maps directly to stored direction=4, zoom=2.
                var dgrp = dgrps[0];
                DGRPImage image = dgrp.GetImage(4, 2, 0);
                if (image == null)
                    image = dgrp.Images?.FirstOrDefault(i => i.Sprites?.Length > 0);
                if (image == null || image.Sprites == null || image.Sprites.Length == 0) return;

                // Medium-zoom rendering constants (from WorldSpace.Invalidate, WorldZoom.Medium):
                //   CadgeWidth = 68  → anchorX = CadgeWidth / 2 = 34
                //   CadgeBaseLine = 174  → anchorY
                const int anchorX = 34;
                const int anchorY = 174;
                const int pad = 10;

                // Pass 1 – compute the bounding box of all sprite placements.
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
                if (minX == int.MaxValue || maxX <= minX || maxY <= minY) return;

                int canvasW = maxX - minX + pad * 2;
                int canvasH = maxY - minY + pad * 2;
                var canvasBytes = new byte[canvasW * canvasH * 4]; // RGBA

                // Pass 2 – composite sprites back-to-front onto the canvas.
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
                    canvas.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(512, 512),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.NearestNeighbor
                    }));
                    using (var fs = File.Open(thumbPath, FileMode.Create))
                        canvas.SaveAsPng(fs);
                }
            }
            catch { }
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
