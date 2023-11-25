using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Files.FAR3;
using System.IO;
using System.Text.RegularExpressions;
using FSO.Common.Content;
using FSO.Files.Utils;
using FSO.Common.Utils;

namespace FSO.Content.Framework
{
    /// <summary>
    /// Content provider based on the contents of
    /// FAR archives
    /// </summary>
    public class FAR3Provider <T> : IContentProvider<T>
    {
        protected Content ContentManager;
        protected Dictionary<ulong, Far3ProviderEntry<T>> EntriesById;
        public Dictionary<string, Far3ProviderEntry<T>> EntriesByName;

        protected IContentCodec<T> Codec;
        protected TimedReferenceCache<ulong, T> Cache;
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
            if (ID.FileName != null) return Get(ID.FileName);
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
            return ResolveById(ID);
        }

        public string GetNameByID(ulong ID)
        {
            Far3ProviderEntry<T> entry = null;
            if (EntriesById.TryGetValue(ID, out entry))
            {
                return entry.ToString();
            }
            return "unnamed";
        }

        public uint EstimateTypeId()
        {
            // assuming only one type is present in the far3 (avatar content), the first type id in the file should be a good estimate.
            var id = EntriesById.FirstOrDefault().Key;
            return (uint)id; // low part is ID 
        }

        protected virtual T ResolveById(ulong id)
        {
            Far3ProviderEntry<T> entry = null;
            if (EntriesById.TryGetValue(id, out entry))
            {
                return Get(entry);
            }
            return default(T);
        }

        /// <summary>
        /// Gets an archive based on its filename.
        /// </summary>
        /// <param name="Filename">The name of the archive to get.</param>
        /// <returns>A FAR3 archive.</returns>
        public T Get(string Filename)
        {
            Far3ProviderEntry<T> entry;

            if (EntriesByName.TryGetValue(Filename.ToLowerInvariant(), out entry))
            {
                return Get(entry);
            }

            return default(T);
        }

        /// <summary>
        /// Gets an archive based on a Far3ProviderEntry.
        /// </summary>
        /// <param name="Entry">The Far3ProviderEntry of the archive.</param>
        /// <returns>A FAR3 archive.</returns>
        public T Get(Far3ProviderEntry<T> Entry)
        {
            //thread safe.
            return Cache.GetOrAdd(Entry.ID, (id) =>
            {
                byte[] data = Entry.Archive.GetEntry(Entry.FarEntry);
                using (var stream = new MemoryStream(data, false))
                {
                    T result = this.Codec.Decode(stream);
                    if (result is IFileInfoUtilizer) ((IFileInfoUtilizer)result).SetFilename(Entry.FarEntry.Filename);
                    return result;
                }
            });
        }

        public bool Initialized;
        #region IContentProvider<T> Members
        
        public void Init()
        {
            if (Initialized) return;
            Initialized = true;
            Cache = new TimedReferenceCache<ulong, T>();
            lock (Cache)
            {
                EntriesById = new Dictionary<ulong, Far3ProviderEntry<T>>();
                EntriesByName = new Dictionary<string, Far3ProviderEntry<T>>();

                if (FarFilePattern != null)
                {
                    List<string> FarFiles = new List<string>();
                    foreach (var File in ContentManager.AllFiles)
                    {
                        if (FarFilePattern.IsMatch(File.Replace('\\', '/')))
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
                            EntriesByName[entry.Filename.ToLowerInvariant()] = referenceItem;
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

        public object GetGeneric()
        {
            return Get();
        }

        public object GetThrowawayGeneric()
        {
            throw new NotImplementedException();
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
