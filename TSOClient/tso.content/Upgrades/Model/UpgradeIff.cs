using System.Collections.Generic;
using System.IO;

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
        /// A list of tuning groups for this file. Groups allow the developer to group together tuning values with the same target effect, but
        /// used by different objects, eg. "Cheap Max Fun", "Expensive Max Fun". With these groups set up, tuning values can be easily copied
        /// between objects (as they point to the groups instead of object specific entries) for easier maintenance and rebalancing.
        /// </summary>
        public List<UpgradeGroup> Groups = new List<UpgradeGroup>();

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
            Groups.Clear();
            Upgrades.Clear();
            Config.Clear();

            var subCount = reader.ReadInt32();
            for (int i=0; i<subCount; i++)
            {
                var sub = new UpgradeSubstitution();
                sub.Load(version, reader);
                Subs.Add(sub);
            }
            if (version > 1)
            {
                var groupCount = reader.ReadInt32();
                for (int i = 0; i < groupCount; i++)
                {
                    var group = new UpgradeGroup();
                    group.Load(version, reader);
                    Groups.Add(group);
                }
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
            writer.Write(Groups.Count);
            foreach (var group in Groups) group.Save(writer);
            writer.Write(Upgrades.Count);
            foreach (var upgrade in Upgrades) upgrade.Save(writer);
            writer.Write(Config.Count);
            foreach (var config in Config) config.Save(writer);
        }
    }
}
