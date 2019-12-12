using FSO.Content.Upgrades.Model;
using FSO.Content.Upgrades.Model.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Upgrades
{
    public class ObjectUpgradeProvider
    {
        public UpgradesFile ActiveFile;
        public Content ContentManager;
        public bool Editable;

        public Dictionary<string, RuntimeUpgradeFile> FileUpgrades = new Dictionary<string, RuntimeUpgradeFile>();

        //== runtime structure ==
        //dictionary filename -> upgrade file
        //  dictionary guid -> upgrade level info
        //  list upgrade levels
        //    prebaked dictionary of (table -> index -> (short)value) for forwarding to objects

        public ObjectUpgradeProvider(Content contentManager)
        {
            ContentManager = contentManager;
        }

        public void LoadJSONTuning()
        {
            FileUpgrades.Clear();
            try
            {
                var json = File.ReadAllText("Content/upgrades.json");
                ActiveFile = JsonConvert.DeserializeObject<UpgradesFile>(json);
                Editable = true;
            }
            catch
            {
                ActiveFile = null;
            }
            PrepareRuntime(true);
        }

        public void SaveJSONTuning()
        {
            var result = JsonConvert.SerializeObject(ActiveFile);
            File.WriteAllText("Content/upgrades.json", result);
        }

        public void LoadNetTuning(byte[] data)
        {
            Editable = false;
            if (data.Length > 0)
            {
                using (var mem = new MemoryStream(data))
                {
                    using (var reader = new BinaryReader(mem))
                    {
                        ActiveFile = new UpgradesFile();
                        ActiveFile.Load(reader);
                    }
                }
            } else
            {
                ActiveFile = new UpgradesFile();
            }
            PrepareRuntime(false);
        }

        public UpgradeIff GetFile(string name)
        {
            return ActiveFile?.Files?.FirstOrDefault(x => x.Name == name);
        }

        public RuntimeUpgradeFile GetRuntimeFile(string name)
        {
            RuntimeUpgradeFile upgradeFile = null;
            if (FileUpgrades.TryGetValue(name, out upgradeFile))
            {
                upgradeFile.Load(ContentManager);
                return upgradeFile;
            }
            return null;
        }

        public void UpdateFile(UpgradeIff iff, bool forceLoad)
        {
            if (!ActiveFile.Files.Contains(iff)) ActiveFile.Files.Add(iff);
            var runtime = new RuntimeUpgradeFile(iff);
            if (forceLoad)
            {
                runtime.Load(ContentManager);
            }
            FileUpgrades[iff.Name] = runtime;
        }

        public void PrepareRuntime(bool forceLoad)
        {
            FileUpgrades.Clear();
            if (ActiveFile == null) return;
            foreach (var file in ActiveFile.Files)
            {
                UpdateFile(file, forceLoad);
            }
        }

        public void Init()
        {

        }

        public int? GetUpgradePrice(string file, uint guid, int level, int currentLevel = -1, int currentValue = 0)
        {
            // we need the whole upgrade list to calculate this, as it may include relative price upgrades.
            RuntimeUpgradeFile upgradeFile = null;
            if (FileUpgrades.TryGetValue(file, out upgradeFile))
            {
                upgradeFile.Load(ContentManager);
                return upgradeFile.GetPrice(guid, level, currentLevel, currentValue);
            }
            return null;
        }

        public ObjectUpgradeConfig GetUpgradeConfig(string file, uint guid, int level)
        {
            RuntimeUpgradeFile upgradeFile = null;
            if (FileUpgrades.TryGetValue(file, out upgradeFile))
            {
                upgradeFile.Load(ContentManager);
                return upgradeFile.GetConfig(guid);
            }
            return null;
        }

        public Dictionary<int, Dictionary<int, short>> GetUpgrade(string file, uint guid, int level)
        {
            // find upgrade table for this file, find entries for this object, and the specified level.
            RuntimeUpgradeFile upgradeFile = null;
            if (FileUpgrades.TryGetValue(file, out upgradeFile))
            {
                upgradeFile.Load(ContentManager);
                return upgradeFile.GetUpgradeTuning(guid, level);
            }
            return null;
        }
    }
}
