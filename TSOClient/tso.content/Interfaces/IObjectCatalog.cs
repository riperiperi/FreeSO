using System.Collections.Generic;

namespace FSO.Content.Interfaces
{
    public interface IObjectCatalog
    {
        List<ObjectCatalogItem> All();
        List<ObjectCatalogItem> GetItemsByCategory(sbyte category);
        ObjectCatalogItem? GetItemByGUID(uint guid);
        List<uint> GetUntradableGUIDs();
    }

    public struct ObjectCatalogItem
    {
        public uint GUID;
        public sbyte Category;
        public uint Price;
        public string Name;
        public string CatalogName;
        public string Tags;
        public byte DisableLevel; //1 = only shopping, 2 = rare (unsellable?)

        public byte RoomSort;
        public byte Subsort;
        public byte DowntownSort;
        public byte VacationSort;
        public byte CommunitySort;
        public byte StudiotownSort;
        public byte MagictownSort;
    }
}
