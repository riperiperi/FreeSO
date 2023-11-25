using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Framework.Attributes;

namespace FSO.Common.DataService.Model
{
    public class Bookmark : AbstractModel
    {
        private uint _Bookmark_TargetID;

        [Key]
        public uint Bookmark_TargetID
        {
            get { return _Bookmark_TargetID; }
            set
            {
                _Bookmark_TargetID = value;
                NotifyPropertyChanged("Bookmark_TargetID");
            }
        }

        private byte _Bookmark_Type;
        public byte Bookmark_Type
        {
            get { return _Bookmark_Type; }
            set
            {
                _Bookmark_Type = value;
                NotifyPropertyChanged("Bookmark_Type");
            }
        }
    }

    public enum BookmarkType : byte
    {
        AVATAR = 0x01,
        IGNORE_AVATAR = 0x05
    }
}
