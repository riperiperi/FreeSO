using FSO.Content.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Server.Database.DA.Avatars;
using FSO.Vitaboy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;

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
                var bmp = obj.Resource.Get<BMP>(objd.ThumbnailGraphic);
                if (bmp == null || bmp.data == null || bmp.data.Length == 0) return;

                var dir = Path.Combine(nfsDir, "Objects/" + guid.ToString("x8"));
                Directory.CreateDirectory(dir);
                using (var img = Image.Load(new MemoryStream(bmp.data)))
                {
                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(1024, 1024),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.NearestNeighbor
                    }));
                    using (var fs = File.Open(Path.Combine(dir, "thumb.png"), FileMode.Create))
                        img.SaveAsPng(fs);
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
