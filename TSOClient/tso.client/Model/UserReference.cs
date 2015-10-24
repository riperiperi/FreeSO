using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.Content.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Model
{
    public abstract class UserReference : AbstractModel
    {
        public abstract UserReferenceType Type { get; }

        private uint _Id;
        public uint Id
        {
            get { return _Id; }
            protected set
            {
                _Id = value;
                NotifyPropertyChanged("Id");
            }
        }

        private string _Name;
        public string Name
        {
            get { return _Name; }
            protected set
            {
                _Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private ITextureRef _Icon;
        public ITextureRef Icon
        {
            get { return _Icon; }
            protected set
            {
                _Icon = value;
                NotifyPropertyChanged("Icon");
            }
        }

        public static UserReference Wrap(Avatar avatar)
        {
            return new AvatarUserReference(avatar);
        }

        public static UserReference Of(UserReferenceType type)
        {
            return new BuiltInUserReference(type);
        }
    }

    public class BuiltInUserReference : UserReference
    {
        private UserReferenceType _Type;

        public BuiltInUserReference(UserReferenceType type)
        {
            _Type = type;

            var content = FSO.Content.Content.Get();
            switch (type)
            {
                case UserReferenceType.EA:
                    Icon = content.UIGraphics.Get(0x00000B0000000001);
                    Name = GameFacade.Strings.GetString("195", "33");
                    break;
                case UserReferenceType.MAXIS:
                    Icon = content.UIGraphics.Get(0x00000B0100000001);
                    Name = GameFacade.Strings.GetString("195", "34");
                    break;
                case UserReferenceType.MOMI:
                    Icon = content.UIGraphics.Get(0x00000B0200000001);
                    Name = "M.O.M.I";
                    break;
                case UserReferenceType.TSO:
                    Icon = content.UIGraphics.Get(0x00000B0300000001);
                    Name = GameFacade.Strings.GetString("195", "35");
                    break;
            }
        }

        public override UserReferenceType Type
        {
            get
            {
                return _Type;
            }
        }
    }

    public class AvatarUserReference : UserReference
    {
        private Binding<Avatar> CurrentAvatar;

        public AvatarUserReference(Avatar avatar)
        {
            CurrentAvatar = new Binding<Avatar>()
                .WithBinding(this, "Name", "Avatar_Name")
                .WithBinding(this, "HeadOutfitId", "Avatar_Appearance.AvatarAppearance_HeadOutfitID")
                .WithBinding(this, "Id", "Avatar_Id");

            CurrentAvatar.Value = avatar;
        }

        private ulong _HeadOutfitId;
        public ulong HeadOutfitId
        {
            get { return _HeadOutfitId; }
            set
            {
                _HeadOutfitId = value;
                RefreshHead();
            }
        }

        private void RefreshHead()
        {
            var avatar = CurrentAvatar.Value;
            if (avatar != null)
            {
                var content = FSO.Content.Content.Get();
                var outfit = content.AvatarOutfits.Get(_HeadOutfitId);
                var appearanceId = outfit.GetAppearance((Vitaboy.AppearanceType)avatar.Avatar_Appearance.AvatarAppearance_SkinTone);
                var appearance = content.AvatarAppearances.Get(appearanceId);
                var thumbnail = content.AvatarThumbnails.Get(appearance.ThumbnailID);
                Icon = thumbnail;
            }
            else
            {
                Icon = null;
            }
        }

        public override UserReferenceType Type
        {
            get
            {
                return UserReferenceType.AVATAR;
            }
        }
    }

    public enum UserReferenceType
    {
        EA,
        MAXIS,
        MOMI,
        TSO,
        AVATAR
    }
}
