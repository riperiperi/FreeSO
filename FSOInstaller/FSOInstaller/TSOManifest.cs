using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSOInstaller
{
    public class TSOManifest
    {
        public string Name;
        public int Version;
        public string SystemMessage;
        public int NumFiles;
        public List<TSOManifestEntry> Entries;

        public TSOManifest(string file)
        {
            Entries = new List<TSOManifestEntry>();
            var lines = file.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var keyValueStrings = line.Split(new string[] { "\" " }, StringSplitOptions.RemoveEmptyEntries);
                if (keyValueStrings.Length == 0) continue;

                var read = new List<KeyValuePair>();
                var byKey = new Dictionary<string, string>();
                foreach (var kvs in keyValueStrings)
                {
                    var kv = kvs.Split('=');
                    if (kv.Length < 2) break; //comment or invalid
                    var obj = new KeyValuePair(kv[0], kv[1].Trim('"'));
                    read.Add(obj);
                    byKey.Add(obj.Key, obj.Value);
                }

                if (read.Count == 0) continue;

                switch (read[0].Key)
                {
                    case "Name":
                        Name = byKey["Name"];
                        break;
                    case "Version":
                        Version = Convert.ToInt32(byKey["Version"]);
                        break;
                    case "System Message":
                        SystemMessage = byKey["System Message"];
                        break;
                    case "Num Files":
                        NumFiles = Convert.ToInt32(byKey["Num Files"]);
                        break;
                    case "File":
                        Entries.Add(new TSOManifestEntry()
                        {
                            Filename = byKey["File"],
                            Size = Convert.ToInt32(byKey["Size"]),
                            Hash = byKey["Hash"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToByte(x)).ToArray()
                        });
                        break;
                }
            }
        }
    }

    public class KeyValuePair
    {
        public string Key;
        public string Value;
        public KeyValuePair (string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    public class TSOManifestEntry
    {
        public string Filename;
        public int Size;
        public byte[] Hash;
    }
}
