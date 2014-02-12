using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using tso.common.content;

namespace tso.content.framework
{
    /// <summary>
    /// Content provider based on a packingslip manifest file
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PackingslipProvider<T> : IContentProvider<T>
    {
        protected Content ContentManager;
        private string PackingSlipFile;
        protected Dictionary<ulong, PackingslipEntry<T>> Entries;
        protected IContentCodec<T> Codec;
        protected Dictionary<ulong, T> Cache;

        public PackingslipProvider(Content contentManager, string packingslip, IContentCodec<T> codec)
        {
            this.ContentManager = contentManager;
            this.PackingSlipFile = packingslip;
            this.Codec = codec;
        }

        public T Get(uint type, uint fileID)
        {
            var fileIDLong = ((ulong)fileID) << 32;
            return Get(fileIDLong | type);
        }

        /// <summary>
        /// Get an asset by its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get(ulong id)
        {
            lock (Cache)
            {
                if (Cache.ContainsKey(id))
                {
                    return Cache[id];
                }

                var item = Entries[id];
                if(item == null){
                    return default(T);
                }
                using (var dataStream = ContentManager.GetResource(item.FilePath, id))
                {
                    if (dataStream == null){
                        return default(T);
                    }

                    T value = this.Codec.Decode(dataStream);
                    Cache.Add(id, value);
                    return value;
                }
            }
        }

        #region IContentProvider Members
        public void Init()
        {
            Entries = new Dictionary<ulong, PackingslipEntry<T>>();
            Cache = new Dictionary<ulong, T>();

            var packingslip = new XmlDataDocument();
            packingslip.Load(ContentManager.GetPath(PackingSlipFile));
            var assets = packingslip.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode asset in assets)
            {
                ulong FileID = Convert.ToUInt64(asset.Attributes["assetID"].Value, 16);
                Entries.Add(FileID, new PackingslipEntry<T>(this) {
                    ID = FileID,
                    FilePath = asset.Attributes["key"].Value
                });
            }
        }

        public List<IContentReference<T>> List()
        {
            var result = new List<IContentReference<T>>();
            foreach(var item in Entries.Values){
                result.Add(item);
            }
            return result;
        }
        #endregion
    }

    public class PackingslipEntry <T> : IContentReference <T>
    {
        public ulong ID;
        public string FilePath;
        private PackingslipProvider<T> Provider;

        public PackingslipEntry(PackingslipProvider<T> provider){
            this.Provider = provider;
        }

        public T Get()
        {
            return Provider.Get(ID);
        }
    }
}
