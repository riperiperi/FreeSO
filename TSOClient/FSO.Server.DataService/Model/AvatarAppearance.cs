using FSO.Common.DataService.Framework;

namespace FSO.Common.DataService.Model
{
    public class AvatarAppearance : AbstractModel
    {
        private ulong _AvatarAppearance_BodyOutfitID;
        public ulong AvatarAppearance_BodyOutfitID { get { return _AvatarAppearance_BodyOutfitID; } set
            {
                _AvatarAppearance_BodyOutfitID = value;
                NotifyPropertyChanged("AvatarAppearance_BodyOutfitID");
            }
        }

        private byte _AvatarAppearance_SkinTone;
        public byte AvatarAppearance_SkinTone
        {
            get { return _AvatarAppearance_SkinTone; }
            set
            {
                _AvatarAppearance_SkinTone = value;
                NotifyPropertyChanged("AvatarAppearance_SkinTone");
            }
        }

        private ulong _AvatarAppearance_HeadOutfitID;
        public ulong AvatarAppearance_HeadOutfitID
        {
            get { return _AvatarAppearance_HeadOutfitID; }
            set
            {
                _AvatarAppearance_HeadOutfitID = value;
                NotifyPropertyChanged("AvatarAppearance_HeadOutfitID");
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as AvatarAppearance;
            return (other != null
                && AvatarAppearance_BodyOutfitID == other.AvatarAppearance_BodyOutfitID
                && AvatarAppearance_SkinTone == other.AvatarAppearance_SkinTone
                && AvatarAppearance_HeadOutfitID == other.AvatarAppearance_HeadOutfitID);
        }
    }
}
