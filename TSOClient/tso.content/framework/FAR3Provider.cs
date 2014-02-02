using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimsLib.FAR3;
using System.IO;
using System.Text.RegularExpressions;
using tso.common.content;

namespace tso.content.framework
{
    /// <summary>
    /// Content provider based on the contents of
    /// FAR archives
    /// </summary>
    public class FAR3Provider <T> : IContentProvider<T>
    {
        protected Content ContentManager;
        protected Dictionary<ulong, Far3ProviderEntry<T>> EntriesById;
        protected Dictionary<string, Far3ProviderEntry<T>> EntriesByName;

        protected IContentCodec<T> Codec;
        protected Dictionary<ulong, T> Cache;
        private string[] FarFiles;
        private Regex FarFilePattern;

        public FAR3Provider(Content contentManager, IContentCodec<T> codec, params string[] farFiles)
        {
            this.ContentManager = contentManager;
            this.Codec = codec;
            this.FarFiles = farFiles;
        }

        public FAR3Provider(Content contentManager, IContentCodec<T> codec, Regex farFilePattern)
        {
            this.ContentManager = contentManager;
            this.Codec = codec;
            this.FarFilePattern = farFilePattern;
        }

        public T Get(ContentID id)
        {
            return Get(id.TypeID, id.FileID);
        }

        public T Get(uint type, uint fileID){
            var fileIDLong = ((ulong)fileID) << 32;
            return Get(fileIDLong | type);
        }

        public T Get(ulong id)
        {
            lock (Cache)
            {
                var entry = EntriesById[id];
                if (entry != null)
                {
                    return Get(entry);
                }
                return default(T);
            }
        }

        public T Get(string filename)
        {
            lock (Cache)
            {
                var entry = EntriesByName[filename.ToLower()];
                if (entry != null)
                {
                    return Get(entry);
                }
                return default(T);
            }
        }

        public T Get(Far3ProviderEntry<T> entry)
        {
            lock (Cache)
            {
                if (this.Cache.ContainsKey(entry.ID))
                {
                    return this.Cache[entry.ID];
                }

                byte[] data = entry.Archive.GetEntry(entry.FarEntry);
                using (var stream = new MemoryStream(data, false))
                {
                    T result = this.Codec.Decode(stream);
                    this.Cache.Add(entry.ID, result);
                    return result;
                }
            }
        }


        #region IContentProvider<T> Members

        public void Init()
        {

            Cache = new Dictionary<ulong, T>();
            lock (Cache)
            {
                EntriesById = new Dictionary<ulong, Far3ProviderEntry<T>>();
                EntriesByName = new Dictionary<string, Far3ProviderEntry<T>>();

                if (FarFilePattern != null)
                {
                    List<string> farFiles = new List<string>();
                    foreach (var file in ContentManager.AllFiles)
                    {
                        if (FarFilePattern.IsMatch(file))
                        {
                            farFiles.Add(file);
                        }
                    }
                    FarFiles = farFiles.ToArray();
                }


                foreach (var farPath in FarFiles)
                {
                    var archive = new FAR3Archive(ContentManager.GetPath(farPath));
                    var entries = archive.GetAllFAR3Entries();

                    foreach (var entry in entries)
                    {
                        var fileID = ((ulong)entry.FileID) << 32;

                        var referenceItem = new Far3ProviderEntry<T>(this)
                        {
                            ID = fileID | entry.TypeID,
                            Archive = archive,
                            FarEntry = entry
                        };

                        EntriesById.Add(referenceItem.ID, referenceItem);
                        if (entry.Filename != null)
                        {
                            EntriesByName.Add(entry.Filename.ToLower(), referenceItem);
                        }
                    }
                }
            }
        }

        public List<IContentReference<T>> List()
        {
            var result = new List<IContentReference<T>>();
            foreach (var item in EntriesById.Values)
            {
                //System.Diagnostics.Debug.WriteLine(item.FarEntry.Filename);
                result.Add(item);
            }
            return result;
        }

        #endregion
    }


    public class Far3ProviderEntry<T> : IContentReference<T>
    {
        public ulong ID;
        public FAR3Archive Archive;
        public Far3Entry FarEntry;
        
        private FAR3Provider<T> Provider;
        public Far3ProviderEntry(FAR3Provider<T> provider)
        {
            this.Provider = provider;
        }

        #region IContentReference<T> Members

        public T Get()
        {
            return this.Provider.Get(this);
        }

        #endregion

        public override string ToString()
        {
            return FarEntry.Filename;
        }
    }
    
}
