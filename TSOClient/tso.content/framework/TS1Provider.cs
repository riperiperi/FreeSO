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

        public TS1Provider(Content contentManager)
        {
            FarProvider = new FAR1Provider<object>(contentManager, null, new Regex(@".*\.far"), true);
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
            if (entries == null) return result;
            foreach (var entry in entries)
            {
                var name = Path.GetFileName(entry.FarEntry.Filename.ToLowerInvariant());
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
        private string Extension;
        private Func<object, T> Converter;

        public TS1SubProvider(TS1Provider baseProvider, string extension, Func<object, T> converter) : this(baseProvider, extension)
        {
            Converter = converter;
        }

        public TS1SubProvider(TS1Provider baseProvider, string extension)
        {
            BaseProvider = baseProvider;
            Extension = extension;
            Converter = x => (T)x;
        }

        public void Init()
        {
            Entries = BaseProvider.BuildDictionary(Extension, "globals");
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
