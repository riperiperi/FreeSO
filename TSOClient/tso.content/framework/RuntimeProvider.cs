using FSO.Common.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Framework
{
    public class RuntimeProvider<T> : IContentProvider<T>
    {
        public Dictionary<ulong, T> EntriesByID = new Dictionary<ulong, T>();
        public Dictionary<string, T> EntriesByName = new Dictionary<string, T>();

        public T Get(ulong id)
        {
            T entry;
            if (EntriesByID.TryGetValue(id, out entry)) return entry;
            return default(T);
        }

        public T Get(string name)
        {
            T entry;
            if (EntriesByName.TryGetValue(name, out entry)) return entry;
            return default(T);
        }

        public T Get(uint type, uint fileID)
        {
            var id = ((ulong)type << 32) | fileID;
            T entry;
            if (EntriesByID.TryGetValue(id, out entry)) return entry;
            return default(T);
        }

        public T Get(ContentID id)
        {
            T entry;
            if (EntriesByName.TryGetValue(id.FileName, out entry)) return entry;
            return default(T);
        }

        public List<IContentReference<T>> List()
        {
            return EntriesByName.Values.Select(x => new RuntimeReference<T>(x) as IContentReference<T>).ToList();
        }

        public void Add(string name, ulong id, T obj)
        {
            EntriesByName[name] = obj;
            EntriesByID[id] = obj;
        }

        public void Remove(string name)
        {
            T obj;
            if (EntriesByName.TryGetValue(name, out obj))
            {
                var id = EntriesByID.FirstOrDefault(x => x.Value.Equals(obj)).Key;
                EntriesByID.Remove(id);
            }
            EntriesByName.Remove(name);
        }
    }

    public class RuntimeReference<T> : IContentReference<T>
    {
        public T Object;
        public RuntimeReference(T obj) {
            Object = obj;
        }

        public T Get()
        {
            return Object;
        }

        public object GetGeneric()
        {
            return Object;
        }

        public object GetThrowawayGeneric()
        {
            return Object;
        }
    }
}
