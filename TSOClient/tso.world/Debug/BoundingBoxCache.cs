using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FSO.LotView.Debug
{
    internal static class BoundingBoxCache
    {
        private static Dictionary<uint, BoundingBox> GuidToBox = new Dictionary<uint, BoundingBox>();

        static BoundingBoxCache()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                using (var file = new FileStream("Content/bboxCache.txt", FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new StreamReader(file))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            var entry = line.Split(' ');

                            if (entry.Length != 7) continue; // should be a guid and bbox...

                            uint guid = uint.Parse(entry[0], NumberStyles.HexNumber);

                            float minX = float.Parse(entry[1], CultureInfo.InvariantCulture);
                            float minY = float.Parse(entry[2], CultureInfo.InvariantCulture);
                            float minZ = float.Parse(entry[3], CultureInfo.InvariantCulture);

                            float maxX = float.Parse(entry[4], CultureInfo.InvariantCulture);
                            float maxY = float.Parse(entry[5], CultureInfo.InvariantCulture);
                            float maxZ = float.Parse(entry[6], CultureInfo.InvariantCulture);

                            GuidToBox[guid] = new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
                        }
                    }
                }
            }
            catch
            {

            }
        }

        public static void AddToCacheIfMissing(uint guid, BoundingBox box)
        {
            if (!GuidToBox.ContainsKey(guid))
            {
                GuidToBox.Add(guid, box);

                Save();
            }
        }

        public static bool TryGetValue(uint guid, out BoundingBox box)
        {
            return GuidToBox.TryGetValue(guid, out box);
        }

        public static void Save()
        {
            using (var file = new FileStream("Content/bboxCache.txt", FileMode.Create))
            {
                using (var writer = new StreamWriter(file))
                {
                    foreach (var entry in GuidToBox)
                    {
                        writer.WriteLine($"{entry.Key:X8} {entry.Value.Min.X} {entry.Value.Min.Y} {entry.Value.Min.Z} {entry.Value.Max.X} {entry.Value.Max.Y} {entry.Value.Max.Z}");
                    }
                }
            }
        }
    }
}
