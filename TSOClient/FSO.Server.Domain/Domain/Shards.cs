using FSO.Common.Domain;
using FSO.Common.Domain.Shards;
using FSO.Content.Model;
using FSO.Server.Database.DA;
using FSO.Server.Protocol.CitySelector;
using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace FSO.Server.Domain
{
    public class Shards : IShardsDomain
    {
        private IServerNFSProvider NFS;
        private List<ShardStatusItem> _Shards = new List<ShardStatusItem>();
        private IDAFactory _DbFactory;
        private DateTime _LastPoll;

        public Shards(IDAFactory factory, IServerNFSProvider nfs)
        {
            _DbFactory = factory;
            Poll();
            NFS = nfs;
        }

        public List<ShardStatusItem> All
        {
            get {
                return _Shards;
            }
        }

        public int? CurrentShard
        {
            get
            {
                throw new Exception("CurrentShard not avaliable in server domain");
            }
        }

        public void AutoUpdate()
        {
            Task.Delay(60000).ContinueWith(x =>
            {
                try {
                    Poll();
                } catch (Exception ex) {
                }
                AutoUpdate();
            });
        }

        public void Update()
        {

        }

        private void Poll()
        {
            _LastPoll = DateTime.UtcNow;

            using (var db = _DbFactory.Get())
            {
                _Shards = db.Shards.All().Select(x => new ShardStatusItem()
                {
                    Id = x.shard_id,
                    Name = x.name,
                    Map = x.map,
                    Rank = x.rank,
                    Status = (Server.Protocol.CitySelector.ShardStatus)(byte)x.status,
                    PublicHost = x.public_host,
                    InternalHost = x.internal_host,
                    VersionName = x.version_name,
                    VersionNumber = x.version_number,
                    UpdateID = x.update_id
                }).ToList();
            }
        }

        public ShardStatusItem GetById(int id)
        {
            return _Shards.FirstOrDefault(x => x.Id == id);
        }

        public ShardStatusItem GetByName(string name)
        {
            return _Shards.FirstOrDefault(x => x.Name == name);
        }

        public CityMap GetMapForId(int id)
        {
            var shard = GetById(id);

            if (shard.Map.StartsWith("dynamic"))
            {
                var path = NFS.GetShardMapDirectory(id);

                return new CityMap(path);
            }

            return FSO.Content.Content.Get().CityMaps.Get(shard.Map);
        }

        public void MakeDynamic(int id, Action<Color[], int, int, Stream> savePNG)
        {
            var shard = GetById(id);

            // Try and copy the current map data into the dynamic map folder.

            var baseMap = FSO.Content.Content.Get().CityMaps.Get(shard.Map);

            var target = NFS.GetShardMapDirectory(id);

            Directory.CreateDirectory(target);

            // Save all aspects

            SaveTex(target, "terraintype", baseMap.TerrainTypeColorData, savePNG);
            SaveTex(target, "elevation", baseMap.ElevationColorData, savePNG);
            SaveTex(target, "roadmap", baseMap.RoadColorData, savePNG);
            SaveTex(target, "forestdensity", baseMap.ForestDensityColorData, savePNG);
            SaveTex(target, "foresttype", baseMap.ForestTypeColorData, savePNG);

            var thumbImage = baseMap.Thumbnail.GetImage();
            SaveTex(target, "thumbnail", GetImageColor(thumbImage), savePNG, thumbImage.Width, thumbImage.Height);

            using (var db = _DbFactory.Get())
            {
                db.Shards.UpdateInfo(id, shard.Name, "dynamic");
            }

            Poll();
        }

        private static Color[] GetImageColor(TexBitmap bmp)
        {
            var width = bmp.Width;
            var height = bmp.Height;
            var pixelSize = bmp.PixelSize;
            var length = width * height;
            var result = new Color[length];
            var bytes = bmp.Data;

            var index = 0;

            int i = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var a = pixelSize == 3 ? 255 : bytes[index + 3];
                    var r = bytes[index + 2];
                    var g = bytes[index + 1];
                    var b = bytes[index];

                    index += pixelSize;

                    result[i++] = new Color(r, g, b, a);
                }
            }

            return result;
        }

        private static void SaveTex(string baseDir, string filename, Color[] data, Action<Color[], int, int, Stream> savePNG, int width = 512, int height = 512)
        {
            string filePath = Path.Combine(baseDir, $"{filename}.png");

            Directory.CreateDirectory(baseDir);

            using (FileStream fs = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                savePNG(data, width, height, fs);
            }
        }
    }
}
