using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Content;

namespace FSO.Content.Framework
{
    public class CompositeProvider<T> : IContentProvider<T>
    {
        private IEnumerable<IContentProvider<T>> Providers;

        public CompositeProvider(IEnumerable<IContentProvider<T>> providers)
        {
            Providers = providers;
        }

        public T Get(ContentID id)
        {
            foreach (var provider in Providers)
            {
                var result = provider.Get(id);
                if (!object.Equals(result, default(T))) return result;
            }
            return default(T);
        }

        public T Get(string name)
        {
            foreach (var provider in Providers)
            {
                var result = provider.Get(name);
                if (!object.Equals(result, default(T))) return result;
            }
            return default(T);
        }

        public T Get(ulong id)
        {
            foreach (var provider in Providers)
            {
                var result = provider.Get(id);
                if (!object.Equals(result, default(T))) return result;
            }
            return default(T);
        }

        public T Get(uint type, uint fileID)
        {
            foreach (var provider in Providers)
            {
                var result = provider.Get(type, fileID);
                if (!object.Equals(result, default(T))) return result;
            }
            return default(T);
        }

        public List<IContentReference<T>> List()
        {
            var total = new List<IContentReference<T>>();
            foreach (var provider in Providers)
            {
                total.AddRange(provider.List());
            }
            return total;
        }

        public List<IContentReference> ListGeneric()
        {
            var total = new List<IContentReference>();
            foreach (var provider in Providers)
            {
                if (provider is TS1SubProvider<T>)
                    total.AddRange(((TS1SubProvider<T>)provider).ListGeneric());
                else
                    total.AddRange(provider.List());
            }
            return total;
        }
    }
}
