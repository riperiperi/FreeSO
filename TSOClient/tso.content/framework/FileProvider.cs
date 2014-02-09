using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.common.content;
using System.Text.RegularExpressions;
using System.IO;

namespace tso.content.framework
{
    public class FileProvider<T> : IContentProvider<T>
    {
        protected Content ContentManager;
        protected Dictionary<string, string> EntriesByName;
        protected IContentCodec<T> Codec;
        protected Dictionary<string, T> Cache;
        protected List<FileContentReference<T>> Items;
        private Regex FilePattern;

        public FileProvider(Content contentManager, IContentCodec<T> codec, Regex filePattern)
        {
            this.ContentManager = contentManager;
            this.Codec = codec;
            this.FilePattern = filePattern;
        }




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
                    if (FilePattern.IsMatch(file)){
                        matchedFiles.Add(file);
                    }
                }
                foreach (var file in matchedFiles){
                    var name = Path.GetFileName(file).ToLower();
                    EntriesByName.Add(name, file);
                    Items.Add(new FileContentReference<T>(name, this));
                }
            }
        }



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
