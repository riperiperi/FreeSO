using FSO.Common.Content;
using FSO.Content.Codecs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Content.Framework
{
    public class TS1Provider
    {
        private FAR1Provider<object> FarProvider;
        private Content Manager;
        private Dictionary<string, string[]> BareFoldersByExtension = new Dictionary<string, string[]>()
        {
            { ".bmp", new string[] { "GameData/Skins/" } },
            { ".cmx", new string[] { "GameData/Skins/" } },
            { ".skn", new string[] { "GameData/Skins/" } },
        };

        private Dictionary<string, FileProvider<object>> FileProvidersByRegex = new Dictionary<string, FileProvider<object>>();

        public TS1Provider(Content contentManager)
        {
            FarProvider = new FAR1Provider<object>(contentManager, null, new Regex(@".*\.far"), true);
            Manager = contentManager;
            //todo: files provider?
        }
        
        public void Init()
        {
            FarProvider.Init();
        }

        public Dictionary<string, IContentReference> BuildDictionary(string ext, string exclude)
        {
            var entries = FarProvider.GetEntriesForExtension(ext);
            var result = new Dictionary<string, IContentReference>();
            /*
            string[] folders = null;
            if (BareFoldersByExtension.TryGetValue(ext, out folders))
            {
                foreach (var folder in folders)
                {
                    var test = Manager.TS1AllFiles;
                    var regexStr = folder + ".*\\" + ext;
                    FileProvider<object> provider;
                    if (!FileProvidersByRegex.TryGetValue(regexStr, out provider))
                    {
                        var regex = new Regex(regexStr);
                        provider = new FileProvider<object>(Manager, null, regex);
                        provider.UseTS1 = true;
                        provider.Init();
                        FileProvidersByRegex[regexStr] = provider;
                    }
                    var entries2 = provider.List();
                    foreach (var entry in entries2)
                    {
                        result[entry.ToString()] = entry;
                    }
                }
            }*/

            if (entries == null) return result;
            foreach (var entry in entries)
            {
                var name = Path.GetFileName(entry.FarEntry.Filename.ToLowerInvariant().Replace('\\', '/'));
                if (name.Contains(exclude)) continue;
                result[name] = entry;
            }

            return result;
        }

        public object Get(string item) {
            return FarProvider.Get(item);
        }
    }

    public class TS1SubProvider<T> : IContentProvider<T>
    {
        private TS1Provider BaseProvider;
        private Dictionary<string, IContentReference> Entries;
        private string[] Extensions;
        private Func<object, T> Converter;

        public TS1SubProvider(TS1Provider baseProvider, string extension, Func<object, T> converter) : this(baseProvider, extension)
        {
            Converter = converter;
        }

        public TS1SubProvider(TS1Provider baseProvider, string extension)
        {
            BaseProvider = baseProvider;
            Extensions = new string[] { extension };
            Converter = x => (T)x;
        }

        public TS1SubProvider(TS1Provider baseProvider, string[] extensions)
        {
            BaseProvider = baseProvider;
            Extensions = extensions;
            Converter = x => (T)x;
        }

        public void Init()
        {
            Entries = BaseProvider.BuildDictionary(Extensions[0], "globals");

            for (int i=1; i<Extensions.Length; i++)
            {
                var ents = BaseProvider.BuildDictionary(Extensions[i], "globals");
                foreach (var item in ents) Entries.Add(item.Key, item.Value);
            }
        }

        public T Get(ulong id)
        {
            throw new NotImplementedException();
        }

        public virtual T Get(string name)
        {
            IContentReference result = null;
            if (Entries.TryGetValue(name, out result))
            {
                return Converter(result.GetGeneric());
            }

            return default(T);
        }

        public T Get(uint type, uint fileID)
        {
            throw new NotImplementedException();
        }

        public List<IContentReference<T>> List()
        {
            throw new NotImplementedException();
        }

        public List<IContentReference> ListGeneric()
        {
            return new List<IContentReference>(Entries.Values);
        }

        public T Get(ContentID id)
        {
            if (id.FileName != null) return Get(id.FileName);
            throw new NotImplementedException();
        }
    }
}
