using FSO.Common.DataService.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
