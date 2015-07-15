using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.FAR3;
using System.IO;
using System.Text.RegularExpressions;
using TSO.Common.content;

namespace TSO.Content.framework
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
        private string[] m_FarFiles;
        private Regex FarFilePattern;

        /// <summary>
        /// Creates a new FAR3Provider.
        /// </summary>
        /// <param name="ContentManager">A Content instance.</param>
        /// <param name="Codec">A content codec.</param>
        /// <param name="FarFiles">A list of FAR3 filenames.</param>
        public FAR3Provider(Content ContentManager, IContentCodec<T> Codec, params string[] FarFiles)
        {
            this.ContentManager = ContentManager;
            this.Codec = Codec;
            this.m_FarFiles = FarFiles;
        }

        /// <summary>
        /// Creates a new FAR3Provider.
        /// </summary>
        /// <param name="ContentManager">A Content instance.</param>
        /// <param name="Codec">A content codec.</param>
        /// <param name="FarFilePattern">Which FAR file types to use.</param>
        public FAR3Provider(Content ContentManager, IContentCodec<T> Codec, Regex FarFilePattern)
        {
            this.ContentManager = ContentManager;
            this.Codec = Codec;
            this.FarFilePattern = FarFilePattern;
        }

        /// <summary>
        /// Gets an archive based on its ContentID.
        /// </summary>
        /// <param name="id">The ContentID of the archive.</param>
        /// <returns>A FAR3 archive.</returns>
        public T Get(ContentID ID)
        {
            if (ID == null) return default(T);
            return Get(ID.TypeID, ID.FileID);
        }

        /// <summary>
        /// Gets an archive based on its TypeID and FileID.
        /// </summary>
        /// <param name="type">The TypeID of the archive.</param>
        /// <param name="fileID">The FileID of the archive.</param>
        /// <returns>A FAR3 archive.</returns>
        public T Get(uint Type, uint FileID)
        {
            var fileIDLong = ((ulong)FileID) << 32;
            return Get(fileIDLong | Type);
        }

        /// <summary>
        /// Gets a file based on its ID.
        /// </summary>
        /// <param name="id">The ID of the file.</param>
        /// <returns>A file.</returns>
        public T Get(ulong ID)
        {
            lock (Cache)
            {
                var entry = EntriesById[ID];
                if (entry != null)
                {
                    return Get(entry);
                }
                return default(T);
            }
        }

        /// <summary>
        /// Gets an archive based on its filename.
        /// </summary>
        /// <param name="Filename">The name of the archive to get.</param>
        /// <returns>A FAR3 archive.</returns>
        public T Get(string Filename)
        {
            lock (Cache)
            {

                Far3ProviderEntry<T> entry;

                if (EntriesByName.TryGetValue(Filename.ToLower(), out entry))
                {
                    return Get(entry);
                }

                return default(T);
            }
        }

        /// <summary>
        /// Gets an archive based on a Far3ProviderEntry.
        /// </summary>
        /// <param name="Entry">The Far3ProviderEntry of the archive.</param>
        /// <returns>A FAR3 archive.</returns>
        public T Get(Far3ProviderEntry<T> Entry)
        {
            lock (Cache)
            {
                if (this.Cache.ContainsKey(Entry.ID))
                {
                    return this.Cache[Entry.ID];
                }

                byte[] data = Entry.Archive.GetEntry(Entry.FarEntry);
                using (var stream = new MemoryStream(data, false))
                {
                    T result = this.Codec.Decode(stream);
                    this.Cache.Add(Entry.ID, result);
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
                    List<string> FarFiles = new List<string>();
                    foreach (var File in ContentManager.AllFiles)
                    {
                        if (FarFilePattern.IsMatch(File))
                        {
                            FarFiles.Add(File);
                        }
                    }

                    m_FarFiles = FarFiles.ToArray();
                }

                foreach (var FarPath in m_FarFiles)
                {
                    var archive = new FAR3Archive(ContentManager.GetPath(FarPath));
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
            var Result = new List<IContentReference<T>>();
            foreach (var Item in EntriesById.Values)
                Result.Add(Item);

            return Result;
        }

        #endregion
    }

    /// <summary>
    /// Entry in FAR3Provider.
    /// </summary>
    /// <typeparam name="T">The type of the FAR3Provider.</typeparam>
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

        /// <summary>
        /// The filename of this FAR3ProviderEntry.
        /// </summary>
        /// <returns>The filename as a string.</returns>
        public override string ToString()
        {
            return FarEntry.Filename;
        }
    }
    
}
