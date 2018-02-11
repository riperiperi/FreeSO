using FSO.Common;
using FSO.Content.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace FSO.Content
{
    public class WorldObjectCatalog : IObjectCatalog
    {
        private static List<ObjectCatalogItem>[] ItemsByCategory;
        private static Dictionary<uint, ObjectCatalogItem> ItemsByGUID;

        public void Init(Content content)
        {
            //load and build catalog
            ItemsByGUID = new Dictionary<uint, ObjectCatalogItem>();
            ItemsByCategory = new List<ObjectCatalogItem>[30];
            for (int i = 0; i < 30; i++) ItemsByCategory[i] = new List<ObjectCatalogItem>();

            var packingslip = new XmlDocument();

            packingslip.Load(content.GetPath("packingslips/catalog.xml"));
            var objectInfos = packingslip.GetElementsByTagName("P");

            foreach (XmlNode objectInfo in objectInfos)
            {
                sbyte Category = Convert.ToSByte(objectInfo.Attributes["s"].Value);
                uint guid = Convert.ToUInt32(objectInfo.Attributes["g"].Value, 16);
                if (Category < 0) continue;
                var item = new ObjectCatalogItem()
                {
                    GUID = guid,
                    Category = Category,
                    Price = Convert.ToUInt32(objectInfo.Attributes["p"].Value),
                    Name = objectInfo.Attributes["n"].Value,
                    DisableLevel = Convert.ToByte(objectInfo.Attributes["r"]?.Value ?? "0")
                };
                ItemsByCategory[Category].Add(item);
                ItemsByGUID[guid] = item;
            }

            //load and build Content Objects into catalog
            if (File.Exists(Path.Combine(FSOEnvironment.ContentDir, "Objects/catalog_downloads.xml")))
            {
                var dpackingslip = new XmlDocument();

                dpackingslip.Load(Path.Combine(FSOEnvironment.ContentDir, "Objects/catalog_downloads.xml"));
                var downloadInfos = dpackingslip.GetElementsByTagName("P");

                foreach (XmlNode objectInfo in downloadInfos)
                {
                    sbyte dCategory = Convert.ToSByte(objectInfo.Attributes["s"].Value);
                    uint dguid = Convert.ToUInt32(objectInfo.Attributes["g"].Value, 16);
                    if (dCategory < 0) continue;
                    var ditem = new ObjectCatalogItem()
                    {
                        GUID = dguid,
                        Category = dCategory,
                        Price = Convert.ToUInt32(objectInfo.Attributes["p"].Value),
                        Name = objectInfo.Attributes["n"].Value,
                        DisableLevel = Convert.ToByte(objectInfo.Attributes["r"]?.Value ?? "0")
                    };

                    ItemsByCategory[dCategory].Add(ditem);
                    ItemsByGUID[dguid] = ditem;

                }
            }
        }

        public List<ObjectCatalogItem> All()
        {
            var result = new List<ObjectCatalogItem>();
            foreach (var cat in ItemsByCategory)
            {
                result.AddRange(cat);
            }
            return result;
        }

        public List<ObjectCatalogItem> GetItemsByCategory(sbyte category)
        {
            return ItemsByCategory[category];
        }

        public ObjectCatalogItem? GetItemByGUID(uint guid)
        {
            ObjectCatalogItem item;
            if (ItemsByGUID.TryGetValue(guid, out item))
                return item;
            else return null;
        }


    }
}
