namespace FSO.Server.Protocol.Voltron.DataService
{
    public class Bookmark
    {
        public uint Bookmark_TargetID { get; set; }
        public BookmarkType Bookmark_Type { get; set; }
    }

    public enum BookmarkType : byte
    {
        BOOKMARK = 0x01
    }
}
