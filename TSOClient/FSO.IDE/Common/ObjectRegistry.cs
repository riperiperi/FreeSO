using FSO.Client.GameContent;
using FSO.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FSO.IDE.Common
{
    public static class ObjectRegistry
    {
        public static Dictionary<string, List<ObjectRegistryEntry>> MastersByFilename;

        public static void Init()
        {
            //if (MastersByFilename != null) return;

            MastersByFilename = new Dictionary<string, List<ObjectRegistryEntry>>();

            var objProvider = Content.Content.Get().WorldObjects;

            lock (objProvider.Entries)
            {
                lock (MastersByFilename)
                {
                    foreach (GameObjectReference objectInfo in objProvider.Entries.Values)
                    {
                        ObjectRegistryEntry entry = new ObjectRegistryEntry
                        {
                            GUID = (uint)objectInfo.ID,
                            Filename = objectInfo.FileName,
                            Name = objectInfo.Name,
                            Group = objectInfo.Group,
                            SubIndex = objectInfo.SubIndex
                        };

                        List<ObjectRegistryEntry> dest = null;
                        if (!MastersByFilename.TryGetValue(entry.Filename, out dest))
                        {
                            dest = new List<ObjectRegistryEntry>();
                            MastersByFilename.Add(entry.Filename, dest);
                        }

                        var oldMaster = dest.FirstOrDefault(x => x.Group == entry.Group);
                        if (entry.SubIndex == -1 || entry.Group == 0)
                        {
                            //master, add to main dictionary.
                            if (oldMaster != null && entry.Group != 0)
                            {
                                //master was preemptively created. copy over values to existing.
                                oldMaster.GUID = entry.GUID;
                                oldMaster.Name = entry.Name;
                            }
                            else
                            {
                                entry.Children = new List<ObjectRegistryEntry>();
                                dest.Add(entry);
                            }
                        }
                        else
                        {
                            //non master.
                            if (oldMaster == null)
                            {
                                //create a placeholder master entry.
                                oldMaster = new ObjectRegistryEntry
                                {
                                    Filename = entry.Filename,
                                    Group = entry.Group,
                                    SubIndex = -1
                                };
                                dest.Add(oldMaster);
                                oldMaster.Children = new List<ObjectRegistryEntry>();
                            }
                            oldMaster.Children.Add(entry);
                        }
                    }
                }
            }
        }
    }

    public class ObjectRegistryEntry
    {
        public uint GUID;
        public string Filename;
        public string Name;
        public short Group;
        public short SubIndex;

        public List<ObjectRegistryEntry> Children;

        public int LastSearchScore;
        private string Total;
        
        public bool SearchMatch(string[] words)
        {
            if (words == null) return true;
            if (Total == null) Total = (Filename + " " + Name).ToLowerInvariant();

            LastSearchScore = 0;
            foreach (var word in words)
            {
                if (Total.Contains(word)) LastSearchScore++;
            }
            return (LastSearchScore >= words.Length);
        }

        public override string ToString()
        {
            return ((SubIndex == -1) ? "^" : (Group != 0)?"  ":"") + Name;
        }
    }
}
