using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Upgrades.Model
{
    public class UpgradeIff
    {
        /// <summary>
        /// The name of the iff to upgrade.
        /// </summary>
        public string Name;

        /// <summary>
        /// Default subsitutions to apply to every object in this iff. (BEFORE applying upgrades)
        /// </summary>
        public List<UpgradeSubstitution> Subs = new List<UpgradeSubstitution>();

        /// <summary>
        /// Upgrade Levels that objects in this iff can use.
        /// </summary>
        public List<UpgradeLevel> Upgrades = new List<UpgradeLevel>();

        /// <summary>
        /// Configuration determining which Upgrade Levels can be used for specific objects, which one they start on, and other modifiers.
        /// </summary>
        public List<ObjectUpgradeConfig> Config = new List<ObjectUpgradeConfig>();

        public void Load(int version, BinaryReader reader)
        {
            Name = reader.ReadString();

            Subs.Clear();
            Upgrades.Clear();
            Config.Clear();

            var subCount = reader.ReadInt32();
            for (int i=0; i<subCount; i++)
            {
                var sub = new UpgradeSubstitution();
                sub.Load(version, reader);
                Subs.Add(sub);
            }
            var upgradeCount = reader.ReadInt32();
            for (int i = 0; i < upgradeCount; i++)
            {
                var upgrade = new UpgradeLevel();
                upgrade.Load(version, reader);
                Upgrades.Add(upgrade);
            }
            var configCount = reader.ReadInt32();
            for (int i = 0; i < configCount; i++)
            {
                var config = new ObjectUpgradeConfig();
                config.Load(version, reader);
                Config.Add(config);
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Subs.Count);
            foreach (var sub in Subs) sub.Save(writer);
            writer.Write(Upgrades.Count);
            foreach (var upgrade in Upgrades) upgrade.Save(writer);
            writer.Write(Config.Count);
            foreach (var config in Config) config.Save(writer);
        }
    }
}
