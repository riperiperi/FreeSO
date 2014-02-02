using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimsLib.FAR3;
using System.IO;
using System.Text.RegularExpressions;
using tso.common.content;
using SimsLib.FAR1;

namespace tso.content.framework
{
    /// <summary>
    /// Content provider based on the contents of
    /// FAR archives
    /// </summary>
    public class FAR1Provider <T> : IContentProvider<T>
    {
        protected Content ContentManager;
        protected Dictionary<string, Far1ProviderEntry<T>> EntriesByName;

        protected IContentCodec<T> Codec;
        protected Dictionary<string, T> Cache;
        private string[] FarFiles;
        private Regex FarFilePattern;

        public FAR1Provider(Content contentManager, IContentCodec<T> codec, params string[] farFiles)
        {
            this.ContentManager = contentManager;
            this.Codec = codec;
            this.FarFiles = farFiles;
        }

        public FAR1Provider(Content contentManager, IContentCodec<T> codec, Regex farFilePattern)
        {
            this.ContentManager = contentManager;
            this.Codec = codec;
            this.FarFilePattern = farFilePattern;
        }

        public T Get(uint type, uint fileID){
            return default(T);
        }

        public T Get(ulong id)
        {
            return default(T);
        }

        public T Get(string filename)
        {
            if (!EntriesByName.ContainsKey(filename))
            {
                return default(T);
            }

            var entry = EntriesByName[filename];
            if (entry != null)
            {
                return Get(entry);
            }
            return default(T);
        }

        public T Get(Far1ProviderEntry<T> entry)
        {
            lock (Cache)
            {
                if (this.Cache.ContainsKey(entry.FarEntry.Filename))
                {
                    return this.Cache[entry.FarEntry.Filename];
                }

                byte[] data = entry.Archive.GetEntry(entry.FarEntry);
                using (var stream = new MemoryStream(data, false))
                {
                    T result = this.Codec.Decode(stream);
                    this.Cache.Add(entry.FarEntry.Filename, result);
                    return result;
                }
            }
        }


        #region IContentProvider<T> Members

        public void Init()
        {
            Cache = new Dictionary<string, T>();
            EntriesByName = new Dictionary<string, Far1ProviderEntry<T>>();

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


            foreach (var farPath in FarFiles){
                var archive = new FARArchive(ContentManager.GetPath(farPath));
                var entries = archive.GetAllFarEntries();

                foreach (var entry in entries){
                    var referenceItem = new Far1ProviderEntry<T>(this)
                    {
                        Archive = archive,
                        FarEntry = entry
                    };
                    if (entry.Filename != null){
                        if (EntriesByName.ContainsKey(entry.Filename))
                        {
                            System.Diagnostics.Debug.WriteLine("Duplicate! " + entry.Filename);
                        }
                        EntriesByName[entry.Filename] = referenceItem;
                    }
                }
            }
        }

        public List<IContentReference<T>> List()
        {
            var result = new List<IContentReference<T>>();
            foreach (var item in EntriesByName.Values)
            {
                //System.Diagnostics.Debug.WriteLine(item.FarEntry.Filename);
                result.Add(item);
            }
            return result;
        }

        #endregion
    }


    public class Far1ProviderEntry<T> : IContentReference<T>
    {
        public FARArchive Archive;
        public FarEntry FarEntry;

        private FAR1Provider<T> Provider;
        public Far1ProviderEntry(FAR1Provider<T> provider)
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
