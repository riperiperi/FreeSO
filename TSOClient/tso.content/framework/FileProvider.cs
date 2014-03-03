using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.content;
using System.Text.RegularExpressions;
using System.IO;

namespace TSO.Content.framework
{
    /// <summary>
    /// Provides access to files.
    /// </summary>
    /// <typeparam name="T">The type of file to provide access to.</typeparam>
    public class FileProvider<T> : IContentProvider<T>
    {
        protected Content ContentManager;
        protected Dictionary<string, string> EntriesByName;
        protected IContentCodec<T> Codec;
        protected Dictionary<string, T> Cache;
        protected List<FileContentReference<T>> Items;
        private Regex FilePattern;

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

            lock (Cache)
            {
                List<string> matchedFiles = new List<string>();
                foreach (var file in ContentManager.AllFiles)
                {
                    if (FilePattern.IsMatch(file))
                    {
                        matchedFiles.Add(file);
                    }
                }
                foreach (var file in matchedFiles)
                {
                    var name = Path.GetFileName(file).ToLower();
                    EntriesByName.Add(name, file);
                    Items.Add(new FileContentReference<T>(name, this));
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
            name = name.ToLower();
            lock (Cache)
            {
                if (Cache.ContainsKey(name))
                {
                    return Cache[name];
                }

                if (EntriesByName.ContainsKey(name))
                {
                    var fullPath = ContentManager.GetPath(EntriesByName[name]);
                    using (var reader = File.OpenRead(fullPath))
                    {
                        var item = Codec.Decode(reader);
                        Cache.Add(name, item);
                        return item;
                    }
                }
                return default(T);
            }
        }

        #region IContentProvider<T> Members

        public T Get(ulong id)
        {
            throw new NotImplementedException();
        }

        public T Get(uint type, uint fileID)
        {
            throw new NotImplementedException();
        }

        public List<IContentReference<T>> List()
        {
            throw new NotImplementedException();
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
        private FileProvider<T> Provider;

        public FileContentReference(string name, FileProvider<T> provider){
            this.Name = name;
            this.Provider = provider;
        }

        #region IContentReference<T> Members

        public T Get(){
            return this.Provider.Get(Name);
        }

        #endregion
    }
}
