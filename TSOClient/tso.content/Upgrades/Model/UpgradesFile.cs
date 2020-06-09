using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Upgrades.Model
{
    public class UpgradesFile
    {
        public int Version = 2;
        public List<UpgradeIff> Files = new List<UpgradeIff>();

        public void Load(BinaryReader reader)
        {
            Version = reader.ReadInt32();

            Files.Clear();

            var fileCount = reader.ReadInt32();
            for (int i = 0; i < fileCount; i++)
            {
                var file = new UpgradeIff();
                file.Load(Version, reader);
                Files.Add(file);
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Files.Count);
            foreach (var file in Files) file.Save(writer);
        }
    }
}
