using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Framework.Attributes;

namespace FSO.Common.DataService.Model
{
    public class MayorRating : AbstractModel
    {
        [Key]
        public uint Id { get; set; }

        private uint _MayorRating_FromAvatar;
        public uint MayorRating_FromAvatar
        {
            get { return _MayorRating_FromAvatar; }
            set
            {
                _MayorRating_FromAvatar = value;
                NotifyPropertyChanged("MayorRating_FromAvatar");
            }
        }

        private uint _MayorRating_ToAvatar;
        public uint MayorRating_ToAvatar
        {
            get { return _MayorRating_ToAvatar; }
            set
            {
                _MayorRating_ToAvatar = value;
                NotifyPropertyChanged("MayorRating_ToAvatar");
            }
        }

        private uint _MayorRating_HalfStars;
        public uint MayorRating_HalfStars
        {
            get { return _MayorRating_HalfStars; }
            set
            {
                _MayorRating_HalfStars = value;
                NotifyPropertyChanged("MayorRating_HalfStars");
            }
        }

        private string _MayorRating_Comment = "Retrieving...";
        public string MayorRating_Comment
        {
            get { return _MayorRating_Comment; }
            set
            {
                _MayorRating_Comment = value;
                NotifyPropertyChanged("MayorRating_Comment");
            }
        }

        private uint _MayorRating_Date;
        public uint MayorRating_Date
        {
            get { return _MayorRating_Date; }
            set
            {
                _MayorRating_Date = value;
                NotifyPropertyChanged("MayorRating_Date");
            }
        }

        private uint _MayorRating_Neighborhood;
        public uint MayorRating_Neighborhood
        {
            get { return _MayorRating_Neighborhood; }
            set
            {
                _MayorRating_Neighborhood = value;
                NotifyPropertyChanged("MayorRating_Neighborhood");
            }
        }
    }
}
