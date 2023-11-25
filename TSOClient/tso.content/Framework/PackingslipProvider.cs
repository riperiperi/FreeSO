using System;
using System.Collections.Generic;
using System.Xml;
using FSO.Common.Content;

namespace FSO.Content.Framework
{
    /// <summary>
    /// Content provider based on a packingslip manifest file.
    /// </summary>
    /// <typeparam name="T">The type of file for which to provide access.</typeparam>
    public abstract class PackingslipProvider<T> : IContentProvider<T>
    {
        protected Content ContentManager;
        private string PackingSlipFile;
        protected Dictionary<ulong, PackingslipEntry<T>> Entries;
        protected IContentCodec<T> Codec;
        protected Dictionary<ulong, T> Cache;

        /// <summary>
        /// Creates a new instance of PackingSlipProvider.
        /// </summary>
        /// <param name="contentManager">A Content instance.</param>
        /// <param name="packingslip">The name of a packingslip (xml) file.</param>
        /// <param name="codec">The codec of the file for which to provide access.</param>
        public PackingslipProvider(Content contentManager, string packingslip, IContentCodec<T> codec)
        {
            this.ContentManager = contentManager;
            this.PackingSlipFile = packingslip;
            this.Codec = codec;
        }

        /// <summary>
        /// Gets a file from an archive.
        /// </summary>
        /// <param name="type">The TypeID of the file to get.</param>
        /// <param name="fileID">The FileID of the file to get.</param>
        /// <returns>A file of the specified type.</returns>
        public T Get(uint type, uint fileID)
        {
            var fileIDLong = ((ulong)fileID) << 32;
            return Get(fileIDLong | type);
        }

        /// <summary>
        /// Get an asset by its ID.
        /// </summary>
        /// <param name="id">The ID of the asset.</param>
        /// <returns>A file of the specified type.</returns>
        public T Get(ulong id)
        {
            lock (Cache)
            {
                if (Cache.ContainsKey(id))
                {
                    return Cache[id];
                }

                var item = (Entries.ContainsKey(id))?Entries[id]:null;
                if(item == null)
                {
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

            var packingslip = new XmlDocument();
            packingslip.Load(ContentManager.GetPath(PackingSlipFile));
            var assets = packingslip.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode asset in assets)
            {
                ulong FileID = Convert.ToUInt64(asset.Attributes["assetID"].Value, 16);
                Entries.Add(FileID, new PackingslipEntry<T>(this) {
                    ID = FileID,
                    FilePath = asset.Attributes["key"].Value.Replace('\\', '/')
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

        public T Get(string name)
        {
            throw new NotImplementedException();
        }

        public T Get(ContentID id)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    /// <summary>
    /// An entry of a file in a packingslip (*.xml).
    /// </summary>
    /// <typeparam name="T">Type of the file.</typeparam>
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

        public object GetThrowawayGeneric()
        {
            throw new NotImplementedException();
        }

        public object GetGeneric()
        {
            return Get();
        }
    }
}
