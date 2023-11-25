using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Common.Utils.Cache
{
    public class FileSystemCache : ICache
    {
        private const int DIGEST_DELAY = 10000;

        private string _Directory { get; set; }
        private Queue<FileSystemCacheMutation> _Mutations;
        private Thread _DigestThread;
        private bool _Active;
        private long _CacheSize;
        private long _MaxCacheSize;
        private Thread _MainThread;

        private LinkedList<FileSystemCacheEntry> _Cache;
        private Dictionary<CacheKey, FileSystemCacheEntry> _Index;
        

        public FileSystemCache(string directory, long maxSize)
        {
            _MainThread = Thread.CurrentThread;
            _Directory = directory;
            _Mutations = new Queue<FileSystemCacheMutation>();
            _CacheSize = 0;
            _MaxCacheSize = maxSize;
            _Cache = new LinkedList<FileSystemCacheEntry>();
            _Index = new Dictionary<CacheKey, FileSystemCacheEntry>();
        }

        public void Init()
        {
            var tempList = new List<FileSystemCacheEntry>();
            _ScanDirectory(CacheKey.Root, tempList);

            foreach(var item in tempList.OrderByDescending(x => x.LastRead)){
                _Cache.AddLast(item);
                _Index.Add(item.Key, item);
                _CacheSize += item.Length;
            }

            _Active = true;
            _DigestThread = new Thread(DigestLoop);
            _DigestThread.Priority = ThreadPriority.BelowNormal;
            _DigestThread.Start();
        }


        private void DigestLoop()
        {
            while (_Active && _MainThread?.IsAlive != false)
            {
                Digest();
                Thread.Sleep(DIGEST_DELAY);
            }
        }

        private void Digest()
        {
            lock (_Mutations)
            {
                /**
                 * We can avoid some work & disk hits by removing tasks for keys modified in later tasks
                 */
                List<FileSystemCacheMutation> tasks = new List<FileSystemCacheMutation>();
                Dictionary<CacheKey, FileSystemCacheMutation> taskIndex = new Dictionary<CacheKey, FileSystemCacheMutation>();
                FileSystemCacheMutation mutation;
                while (_Mutations.Count > 0 && (mutation = _Mutations.Dequeue()) != null)
                {
                    tasks.Add(mutation);

                    FileSystemCacheMutation previousTask = null;
                    taskIndex.TryGetValue(mutation.Key, out previousTask);
                    if (previousTask == null) {
                        taskIndex[mutation.Key] = mutation;
                        continue;
                    }
                    
                    tasks.Remove(previousTask);
                    taskIndex[mutation.Key] = mutation;
                }


                int bytesRequired = 0;
                foreach(var task in tasks){
                    bytesRequired += task.GetBytesRequired(this);
                }

                if(bytesRequired > 0 && (_CacheSize + bytesRequired) >= _MaxCacheSize)
                {
                    //We need to evict some entries to make room
                    while((_CacheSize + bytesRequired) >= _MaxCacheSize && _Cache.Count > 0)
                    {
                        var last = _Cache.Last;
                        try {
                            new FileSystemCacheRemoveMutation() {
                                Key = last.Value.Key
                            }.Execute(this);
                        }catch(Exception ex){
                        }
                        CalculateCacheSize();
                    }
                }

                foreach (var task in tasks)
                {
                    try
                    {
                        task.Execute(this);
                    }
                    catch (Exception ex)
                    {
                    }
                }

                //Recalculate cache size if we made changes
                if(tasks.Count > 0){
                    CalculateCacheSize();
                }
            }
        }

        private void CalculateCacheSize()
        {
            long size = 0;
            foreach(var item in _Cache){
                size += item.Length;
            }
            _CacheSize = size;
        }

        private void _ScanDirectory(CacheKey parent, List<FileSystemCacheEntry> tempList)
        {
            var dir = GetFilePath(parent);
            if (!Directory.Exists(dir)) { return; }

            var info = new DirectoryInfo(dir);

            foreach(FileInfo file in info.GetFiles())
            {
                var key = CacheKey.Combine(parent, file.Name);
                tempList.Add(new FileSystemCacheEntry {
                    Key = key,
                    LastRead = file.LastAccessTime,
                    LastWrite = file.LastWriteTime,
                    Length = file.Length
                });
            }

            foreach(var subDir in info.GetDirectories())
            {
                var key = CacheKey.Combine(parent, subDir.Name);
                _ScanDirectory(key, tempList);
            }
        }

        public bool ContainsKey(CacheKey key)
        {
            return _Index.ContainsKey(key);
        }

        public void Add(CacheKey key, byte[] bytes)
        {
            var clone = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, clone, 0, bytes.Length);

            lock (_Mutations)
            {
                _Mutations.Enqueue(new FileSystemCacheAddMutation
                {
                    Key = key,
                    Data = clone
                });
            }
        }

        public void Remove(CacheKey key)
        {
            lock (_Mutations)
            {
                _Mutations.Enqueue(new FileSystemCacheRemoveMutation
                {
                    Key = key
                });
            }
        }

        public Task<T> Get<T>(CacheKey key)
        {
            if(typeof(T) == typeof(byte[]))
            {
                return Task.Factory.StartNew(() =>
                {
                    var file = GetFilePath(key);
                    byte[] result = null;
                    if (File.Exists(file)){
                        result = File.ReadAllBytes(file);
                    }else{
                        throw new Exception("File not found");
                    }
                    TouchEntry(key);
                    return (T)(object)result;
                });
            }

            throw new Exception("Not implemented yet");
        }

        public void Dispose()
        {
            _Active = false;
        }

        internal string GetFilePath(CacheKey key)
        {
            return Path.Combine(_Directory, Path.Combine(key.Components));
        }

        internal FileSystemCacheEntry GetEntry(CacheKey key)
        {
            FileSystemCacheEntry entry = null;
            _Index.TryGetValue(key, out entry);
            return entry;
        }

        internal void AddEntry(FileSystemCacheEntry entry)
        {
            var existing = GetEntry(entry.Key);
            if(existing != null){
                _Cache.Remove(existing);
            }

            _Cache.AddLast(entry);
            _Index[entry.Key] = entry;
        }

        internal void RemoveEntry(CacheKey key)
        {
            var existing = GetEntry(key);
            if (existing != null)
            {
                _Cache.Remove(existing);
                _Index.Remove(key);
            }
        }

        internal void TouchEntry(CacheKey key)
        {
            var existing = GetEntry(key);
            if (existing != null)
            {
                _Cache.AddFirst(existing);
            }
        }
    }

    public interface FileSystemCacheMutation
    {
        CacheKey Key { get; }

        int GetBytesRequired(FileSystemCache cache);
        void Execute(FileSystemCache cache);
    }

    public class FileSystemCacheAddMutation : FileSystemCacheMutation
    {
        public CacheKey Key { get; set; }
        public byte[] Data { get; set; }

        public void Execute(FileSystemCache cache)
        {
            var path = cache.GetFilePath(Key);
            var finalPart = path.LastIndexOf('/');
            Directory.CreateDirectory((finalPart == -1)?path:path.Substring(0, finalPart));
            File.WriteAllBytes(path, Data);

            var entry = cache.GetEntry(Key);
            if (entry == null)
            {
                entry = new FileSystemCacheEntry();
                entry.Key = Key;
                entry.LastRead = DateTime.MinValue;
                entry.LastWrite = DateTime.Now;
                entry.Length = Data.Length;

                cache.AddEntry(entry);
            }
            else
            {
                entry.Length = Data.Length;
            }
        }

        public int GetBytesRequired(FileSystemCache cache)
        {
            var existingFile = cache.GetEntry(Key);
            if(existingFile != null)
            {
                return Data.Length - (int)existingFile.Length;
            }

            return Data.Length;
        }
    }

    public class FileSystemCacheRemoveMutation : FileSystemCacheMutation
    {
        public CacheKey Key { get; set; }

        public void Execute(FileSystemCache cache)
        {
            File.Delete(cache.GetFilePath(Key));
            cache.RemoveEntry(Key);
        }

        public int GetBytesRequired(FileSystemCache cache)
        {
            var existingFile = cache.GetEntry(Key);
            if (existingFile != null)
            {
                return -((int)existingFile.Length);
            }
            return 0;
        }
    }

    public class FileSystemCacheEntry
    {
        public CacheKey Key;
        public DateTime LastWrite;
        public DateTime LastRead;
        public long Length;
    }
}
