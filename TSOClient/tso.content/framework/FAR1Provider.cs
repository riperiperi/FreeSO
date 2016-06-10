/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.FAR3;
using System.IO;
using System.Text.RegularExpressions;
using FSO.Common.Content;
using FSO.Files.FAR1;
using FSO.Files.Utils;

namespace FSO.Content.Framework
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

        /// <summary>
        /// Creates a new instance of FAR1Provider.
        /// </summary>
        /// <param name="contentManager">A Content instance.</param>
        /// <param name="codec">The codec of the type of file in the FAR1 archive for which to provide access.</param>
        /// <param name="farFiles">A list of FAR1 archives with files of the specified codec.</param>
        public FAR1Provider(Content contentManager, IContentCodec<T> codec, params string[] farFiles)
        {
            this.ContentManager = contentManager;
            this.Codec = codec;
            this.FarFiles = farFiles;
        }

        /// <summary>
        /// Creates a new instance of FAR1Provider.
        /// </summary>
        /// <param name="contentManager">A Content instance.</param>
        /// <param name="codec">The codec of the type of file in the FAR1 archive for which to provide access.</param>
        /// <param name="farFilePattern">A regular expression of FAR1 archives with files of the specified codec.</param>
        public FAR1Provider(Content contentManager, IContentCodec<T> codec, Regex farFilePattern)
        {
            this.ContentManager = contentManager;
            this.Codec = codec;
            this.FarFilePattern = farFilePattern;
        }

        /// <summary>
        /// Gets an archive based on its TypeID and FileID.
        /// </summary>
        /// <param name="type">The TypeID of the archive.</param>
        /// <param name="fileID">The FileID of the archive.</param>
        /// <returns>A FAR3 archive.</returns>
        public T Get(uint type, uint fileID)
        {
            return default(T);
        }

        /// <summary>
        /// Gets a file based on its ID.
        /// </summary>
        /// <param name="id">The ID of the file.</param>
        /// <returns>A file.</returns>
        public T Get(ulong id)
        {
            return default(T);
        }

        /// <summary>
        /// Gets an archive based on its filename.
        /// </summary>
        /// <param name="Filename">The name of the archive to get.</param>
        /// <returns>A FAR3 archive.</returns>
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

        /// <summary>
        /// Gets an archive based on its filename, but avoids the cache entirely. 
        /// Used for quick accesses to data that will not be reused soon, and will be released manually. (walls, floors)
        /// </summary>
        /// <param name="Filename">The name of the archive to get.</param>
        /// <returns>A FAR3 archive.</returns>
        public T ThrowawayGet(string filename)
        {
            if (!EntriesByName.ContainsKey(filename))
            {
                return default(T);
            }

            var entry = EntriesByName[filename];
            if (entry != null)
            {
                return ThrowawayGet(entry);
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
                    if (result is IFileInfoUtilizer) ((IFileInfoUtilizer)result).SetFilename(entry.FarEntry.Filename);
                    this.Cache.Add(entry.FarEntry.Filename, result);
                    return result;
                }
            }
        }

        public T ThrowawayGet(Far1ProviderEntry<T> entry)
        {
            byte[] data = entry.Archive.GetEntry(entry.FarEntry);
            using (var stream = new MemoryStream(data, false))
            {
                T result = this.Codec.Decode(stream);
                return result;
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
                    if (FarFilePattern.IsMatch(file.Replace('\\', '/')))
                    {
                        farFiles.Add(file);
                    }
                }
                FarFiles = farFiles.ToArray();
            }

            foreach (var farPath in FarFiles){
                var archive = new FAR1Archive(ContentManager.GetPath(farPath));
                var entries = archive.GetAllFarEntries();

                foreach (var entry in entries)
                {
                    var referenceItem = new Far1ProviderEntry<T>(this)
                    {
                        Archive = archive,
                        FarEntry = entry
                    };
                    if (entry.Filename != null)
                    {
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
                result.Add(item);

            return result;
        }

        #endregion
    }

    public class Far1ProviderEntry<T> : IContentReference<T>
    {
        public FAR1Archive Archive;
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

        public object GetGeneric()
        {
            return Get();
        }

        #endregion

        /// <summary>
        /// The filename of this FAR1ProviderEntry.
        /// </summary>
        /// <returns>The filename as a string.</returns>
        public override string ToString()
        {
            return FarEntry.Filename;
        }
    }
    
}
