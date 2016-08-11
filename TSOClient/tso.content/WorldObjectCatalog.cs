using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace FSO.Content
{
    public class WorldObjectCatalog
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
                    Name = objectInfo.Attributes["n"].Value
                };
                ItemsByCategory[Category].Add(item);
                ItemsByGUID.Add(guid, item);
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

        public struct ObjectCatalogItem
        {
            public uint GUID;
            public sbyte Category;
            public uint Price;
            public string Name;
            public byte DisableLevel; //1 = only shopping, 2 = rare (unsellable?)
        }
    }
}
