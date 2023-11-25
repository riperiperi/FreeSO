using System.Collections.Generic;
using System.Linq;
using FSO.Common.Content;
using System.Text.RegularExpressions;
using System.IO;
using FSO.Content.Codecs;
using FSO.Files.Utils;

namespace FSO.Content.Framework
{
    /// <summary>
    /// Provides access to files.
    /// </summary>
    /// <typeparam name="T">The type of file to provide access to.</typeparam>
    public class FileProvider<T> : IContentProvider<T>
    {
        protected Content ContentManager;
        protected Dictionary<string, string> EntriesByName;
        protected Dictionary<ulong, string> EntryNamesById;
        protected IContentCodec<T> Codec;
        protected Dictionary<string, T> Cache;
        protected List<FileContentReference<T>> Items;
        private Regex FilePattern;
        public bool UseContent;
        public bool UseTS1;
        public bool FAR3IDs;

        /// <summary>
        /// Creates a new instance of FileProvider.
        /// </summary>
        /// <param name="contentManager">A Content instance.</param>
        /// <param name="codec">The codec of the filetype of which to provide access.</param>
        /// <param name="filePattern">Filepattern used to search for files of this type.</param>
        public FileProvider(Content contentManager, IContentCodec<T> codec, Regex filePattern)
        {
            this.ContentManager = contentManager;
            this.Codec = codec;
            this.FilePattern = filePattern;
        }

        /// <summary>
        /// Initiates loading of files of this type.
        /// </summary>
        public void Init()
        {
            this.Items = new List<FileContentReference<T>>();
            this.Cache = new Dictionary<string, T>();
            this.EntriesByName = new Dictionary<string, string>();
            this.EntryNamesById = new Dictionary<ulong, string>();

            lock (Cache)
            {
                List<string> matchedFiles = new List<string>();
                var files = UseContent ? ContentManager.ContentFiles : (UseTS1?ContentManager.TS1AllFiles:ContentManager.AllFiles);
                var basePath = UseTS1 ? ContentManager.TS1BasePath : ContentManager.BasePath;
                foreach (var file in files)
                {
                    if (FilePattern.IsMatch(file.Replace('\\', '/')))
                    {
                        matchedFiles.Add(file);
                    }
                }
                foreach (var file in matchedFiles)
                {
                    var name = Path.GetFileName(file).ToLowerInvariant();
                    if (FAR3IDs)
                    {
                        // there may be a uint64 id attached to this file
                        // an extra ., then 16 char hex, then real extension
                        // format: "test-file-name.a0b1c2d3e4f50617.anim"

                        var idsplit = name.Split('.');
                        if (idsplit.Length > 2)
                        {
                            var stringID = idsplit[idsplit.Length - 2];
                            ulong id;
                            if (ulong.TryParse(stringID, System.Globalization.NumberStyles.HexNumber, null, out id))
                            {
                                // this filename has an id
                                // remove id from name and rejoin it
                                var newSplit = idsplit.ToList();
                                newSplit.RemoveAt(idsplit.Length - 2);
                                name = string.Join(".", newSplit);

                                EntryNamesById[id] = name;
                            }
                        }
                    }
                    EntriesByName[name] = file;
                    Items.Add(new FileContentReference<T>(name, Path.Combine(basePath, file), this));
                }
            }
        }

        /// <summary>
        /// Gets a file.
        /// </summary>
        /// <param name="name">The name of the file to get.</param>
        /// <returns>The file.</returns>
        public T Get(string name)
        {
            name = name.ToLowerInvariant();
            lock (Cache)
            {
                if (Cache.ContainsKey(name))
                {
                    return Cache[name];
                }

                if (EntriesByName.ContainsKey(name))
                {
                    var fullPath = UseContent? ("Content/"+EntriesByName[name]):(UseTS1?Path.Combine(ContentManager.TS1BasePath, EntriesByName[name]):ContentManager.GetPath(EntriesByName[name]));
                    using (var reader = File.OpenRead(fullPath))
                    {
                        T item;
                        if (Codec == null) item = (T)SmartCodec.Decode(reader, Path.GetExtension(fullPath));
                        else item = Codec.Decode(reader);
                        if (item is IFileInfoUtilizer) ((IFileInfoUtilizer)item).SetFilename(Path.GetFileName(fullPath));
                        Cache.Add(name, item);
                        return item;
                    }
                }
                return default(T);
            }
        }

        public T ThrowawayGet(string name)
        {
            if (EntriesByName.ContainsKey(name))
            {
                var fullPath = UseContent ? ("Content/" + EntriesByName[name]) : (UseTS1 ? Path.Combine(ContentManager.TS1BasePath, EntriesByName[name]) : ContentManager.GetPath(EntriesByName[name]));
                using (var reader = File.OpenRead(fullPath))
                {
                    T item;
                    if (Codec == null) item = (T)SmartCodec.Decode(reader, Path.GetExtension(fullPath));
                    else item = Codec.Decode(reader);
                    return item;
                }
            }
            return default(T);
        }

        public List<FileContentReference<T>> GetEntriesForExtension(string ext)
        {
            List<FileContentReference<T>> result = Items.Where(x => Path.GetExtension(x.Name) == ext).ToList();
            return result;
        }

        protected virtual T ResolveById(ulong id)
        {
            string entry = null;
            if (EntryNamesById.TryGetValue(id, out entry))
            {
                return Get(entry);
            }
            return default(T);
        }

        #region IContentProvider<T> Members

        public T Get(ulong id)
        {
            return ResolveById(id);
        }

        public T Get(uint type, uint fileID)
        {
            var fileIDLong = ((ulong)fileID) << 32;
            return Get(fileIDLong | type);
        }

        public List<IContentReference<T>> List()
        {
            return new List<IContentReference<T>>(Items);
        }

        public T Get(ContentID id)
        {
            if (id == null) return default(T);
            if (id.FileName != null) return Get(id.FileName);
            return Get(id.TypeID, id.FileID);
        }

        #endregion
    }

    /// <summary>
    /// Reference to a file's contents.
    /// </summary>
    /// <typeparam name="T">The type of file to reference.</typeparam>
    public class FileContentReference<T> : IContentReference<T>
    {
        public string Name;
        public string Filename;
        public ulong ID;
        private FileProvider<T> Provider;

        public FileContentReference(string name, string filename, FileProvider<T> provider){
            this.Name = name;
            this.Filename = filename;
            this.Provider = provider;
        }

        #region IContentReference<T> Members

        public T Get(){
            return this.Provider.Get(Name);
        }

        public object GetThrowawayGeneric()
        {
            return this.Provider.ThrowawayGet(Name);
        }

        public object GetGeneric()
        {
            return Get();
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
