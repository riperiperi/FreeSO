using System;
using System.Threading.Tasks;

namespace FSO.Common.Utils.Cache
{
    public interface ICache : IDisposable
    {
        bool ContainsKey(CacheKey key);
        void Add(CacheKey key, byte[] bytes);
        void Remove(CacheKey key);

        Task<T> Get<T>(CacheKey key);

        //bool IsReady { get; }
        //bool Contains(string type, string key);
        //Task<byte[]> GetBytes(string type, string key);
        //Task PutBytes(string type, string key, byte[] bytes);
        //Task Init();
    }
}
