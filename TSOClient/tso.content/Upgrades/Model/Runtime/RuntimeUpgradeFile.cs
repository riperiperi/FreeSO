using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Upgrades.Model.Runtime
{
    public class RuntimeUpgradeFile
    {
        public UpgradeIff File;
        public bool Loaded;

        public Dictionary<uint, ObjectUpgradeConfig> Configs = new Dictionary<uint, ObjectUpgradeConfig>();
        public Dictionary<int, Dictionary<int, short>> DefaultReplacement = new Dictionary<int, Dictionary<int, short>>();

        public List<RuntimeUpgradeLevel> Levels = new List<RuntimeUpgradeLevel>();

        public RuntimeUpgradeFile(UpgradeIff file)
        {
            File = file;

            //generate configs.
            foreach (var config in File.Config)
            {
                uint guid;
                if (!uint.TryParse(config.GUID, System.Globalization.NumberStyles.HexNumber, null, out guid))
                    throw new Exception("Invalid GUID for Upgrade! " + guid);
                Configs[guid] = config;
            }
        }

        public void Load(Content content)
        {
            if (Loaded) return;
            //we need to load an object from this file to get access to the tuning
            //first, try to find the object from .
            GameObject obj = null;
            foreach (var config in Configs.Keys)
            {
                obj = content.WorldObjects.Get(config);
                if (obj != null) break;
            }
            if (obj == null) {
                //last resort, attempt to find an entry that uses this file.
                obj = content.WorldObjects.Get(File.Name);
                if (obj == null) throw new Exception($"Could not load upgrades for file {File.Name}. If this object does not exist, remove it from the upgrades file.");
            }

            var res = obj.Resource;

            DefaultReplacement = LoadSubs(File.Subs, res);

            foreach (var level in File.Upgrades)
            {
                var runtime = new RuntimeUpgradeLevel(level, res, content);
                Levels.Add(runtime);
            }
            Loaded = true;
        }

        public static Dictionary<int, Dictionary<int, short>> LoadSubs(List<UpgradeSubstitution> subs, GameObjectResource res)
        {
            var result = new Dictionary<int, Dictionary<int, short>>();
            foreach (var sub in subs)
            {
                //parse old
                var oldSplit = sub.Old.Split(':');
                if (oldSplit.Length != 2) throw new Exception("Tuning to substitute invalid: " + sub.Old);
                int table;
                int index;
                if (!int.TryParse(oldSplit[0], out table)) throw new Exception("Tuning table for sub target invalid: " + sub.Old);
                if (!int.TryParse(oldSplit[1], out index)) throw new Exception("Tuning index for sub target invalid: " + sub.Old);

                //parse new
                if (sub.New.Length == 0) throw new Exception("Substitution value cannot be empty.");
                short value;
                switch (sub.New[0])
                {
                    case 'C':
                        //lookup constant in the file.
                        var newSplit = sub.New.Substring(1).Split(':');
                        if (newSplit.Length != 2) throw new Exception("Substitution value tuning ref invalid: " + sub.New);
                        int ntable;
                        int nindex;
                        if (!int.TryParse(newSplit[0], out ntable)) throw new Exception("Tuning table for sub value invalid: " + sub.New);
                        if (!int.TryParse(newSplit[1], out nindex)) throw new Exception("Tuning index for sub value invalid: " + sub.New);

                        uint id = (uint)((ntable << 16) | nindex);
                        if (ntable >= 8192 && res.SemiGlobal != null)
                            res.SemiGlobal.TuningCache.TryGetValue(id, out value); //ignore missing
                        else if (ntable >= 4096)
                            res.TuningCache.TryGetValue(id, out value); //ignore missing
                        else
                            Content.Get().WorldObjectGlobals.Get("global").Resource.TuningCache.TryGetValue(id, out value); //ignore missing
                        break;
                    case 'V':
                        //it's just a value.
                        if (!short.TryParse(sub.New.Substring(1), out value)) throw new Exception("Invalid substitution value (not a short): " + sub.New);
                        break;
                    default:
                        throw new Exception("Invalid substitution value: " + sub.New);
                }
                Dictionary<int, short> tableDict;
                if (!result.TryGetValue(table, out tableDict))
                {
                    tableDict = new Dictionary<int, short>();
                    result[table] = tableDict;
                }
                tableDict[index] = value;
            }

            return result;
        }

        public Dictionary<int, Dictionary<int, short>> GetUpgradeTuning(uint guid, int level)
        {
            //find the config for this guid
            ObjectUpgradeConfig config;
            if (!Configs.TryGetValue(guid, out config) || (level == 0 && (config.Special ?? false)))
            {
                return DefaultReplacement;
            }

            level += config.Level;
            if (level < 0 || level >= Levels.Count) return DefaultReplacement;
            return Levels[level].Subs;
        }

        public int? GetPrice(uint guid, int offset, int currentLevel = -1, int currentValue = 0)
        {
            var config = GetConfig(guid);
            if (config == null) return null;
            if (config != null) offset += config.Level;
            
            if (offset < 0 || offset >= Levels.Count) return null;
            var newLevel = Levels[offset];
            return newLevel.Price;
        }

        public ObjectUpgradeConfig GetConfig(uint guid)
        {
            ObjectUpgradeConfig result;
            if (Configs.TryGetValue(guid, out result)) return result;
            return null;
        }

        public RuntimeUpgradeLevel GetLevel(uint guid, int offset)
        {
            var config = GetConfig(guid);
            if (config != null) offset += config.Level;

            if (offset < 0 || offset >= Levels.Count) return null;
            return Levels[offset];
        }
    }

    public class RuntimeUpgradeLevel
    {
        public UpgradeLevel Level;
        public bool Relative;
        public int Price; //total price, resolved with catalog
        public string[] Ads;

        public Dictionary<int, Dictionary<int, short>> Subs;

        public RuntimeUpgradeLevel(UpgradeLevel level, GameObjectResource res, Content content)
        {
            Level = level;
            if (Level.Price.Length == 0) throw new Exception("Missing price.");
            if (Level.Price[0] == 'R' || Level.Price[0] == '$')
            {
                if (Level.Price[0] == 'R') Relative = true;
                if (!int.TryParse(Level.Price.Substring(1), out Price))
                    throw new Exception("Could parse literal upgrade level price! " + Level.Price);
            } else
            {
                uint guid;
                if (!uint.TryParse(Level.Price, System.Globalization.NumberStyles.HexNumber, null, out guid))
                    throw new Exception("Could parse guid for upgrade level price! " + Level.Price);
                var item = content.WorldCatalog.GetItemByGUID(guid);
                if (item == null)
                    throw new Exception("Could not find catalog entry for price reference! This error is fatal because a 0 price upgrade would be really bad." + Level.Price);
                Price = (int)item.Value.Price;
            }

            Ads = Level.Ad.Split(';');

            Subs = RuntimeUpgradeFile.LoadSubs(Level.Subs, res);
        }
    }
}
