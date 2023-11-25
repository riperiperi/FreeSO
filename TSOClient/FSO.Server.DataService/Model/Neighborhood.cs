using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Framework.Attributes;
using System.Collections.Immutable;

namespace FSO.Common.DataService.Model
{
    public class Neighborhood : AbstractModel
    {
        [Key]
        public uint Id { get; set; }

        private uint _Neighborhood_CenterGridXY;
        public uint Neighborhood_CenterGridXY
        {
            get { return _Neighborhood_CenterGridXY; }
            set
            {
                _Neighborhood_CenterGridXY = value;
                NotifyPropertyChanged("Neighborhood_CenterGridXY");
            }
        }

        private uint _Neighborhood_LotCount;
        public uint Neighborhood_LotCount
        {
            get { return _Neighborhood_LotCount; }
            set
            {
                _Neighborhood_LotCount = value;
                NotifyPropertyChanged("Neighborhood_LotCount");
            }
        }
        
        private string _Neighborhood_Name;
        [Persist]
        public string Neighborhood_Name
        {
            get { return _Neighborhood_Name; }
            set
            {
                _Neighborhood_Name = value;
                NotifyPropertyChanged("Neighborhood_Name");
            }
        }
        
        private string _Neighborhood_Description;
        [Persist]
        public string Neighborhood_Description
        {
            get { return _Neighborhood_Description; }
            set
            {
                _Neighborhood_Description = value;
                NotifyPropertyChanged("Neighborhood_Description");
            }
        }

        private uint _Neighborhood_Color;
        public uint Neighborhood_Color
        {
            get { return _Neighborhood_Color; }
            set
            {
                _Neighborhood_Color = value;
                NotifyPropertyChanged("Neighborhood_Color");
            }
        }

        private uint _Neighborhood_Flag;
        public uint Neighborhood_Flag
        {
            get { return _Neighborhood_Flag; }
            set
            {
                _Neighborhood_Flag = value;
                NotifyPropertyChanged("Neighborhood_Flag");
            }
        }

        private uint _Neighborhood_TownHallXY;
        public uint Neighborhood_TownHallXY
        {
            get { return _Neighborhood_TownHallXY; }
            set
            {
                _Neighborhood_TownHallXY = value;
                NotifyPropertyChanged("Neighborhood_TownHallXY");
            }
        }

        private string _Neighborhood_IconURL;
        public string Neighborhood_IconURL
        {
            get { return _Neighborhood_IconURL; }
            set
            {
                _Neighborhood_IconURL = value;
                NotifyPropertyChanged("Neighborhood_IconURL");
            }
        }

        private ImmutableList<uint> _Neighborhood_TopLotOverall;
        public ImmutableList<uint> Neighborhood_TopLotOverall
        {
            get { return _Neighborhood_TopLotOverall; }
            set
            {
                _Neighborhood_TopLotOverall = value;
                NotifyPropertyChanged("Neighborhood_TopLotOverall");
            }
        }

        private ImmutableList<uint> _Neighborhood_TopLotCategory;
        public ImmutableList<uint> Neighborhood_TopLotCategory
        {
            get { return _Neighborhood_TopLotCategory; }
            set
            {
                _Neighborhood_TopLotCategory = value;
                NotifyPropertyChanged("Neighborhood_TopLotCategory");
            }
        }

        private uint _Neighborhood_MayorID;
        public uint Neighborhood_MayorID
        {
            get { return _Neighborhood_MayorID; }
            set
            {
                _Neighborhood_MayorID = value;
                NotifyPropertyChanged("Neighborhood_MayorID");
            }
        }

        private ElectionCycle _Neighborhood_ElectionCycle;
        public ElectionCycle Neighborhood_ElectionCycle
        {
            get { return _Neighborhood_ElectionCycle; }
            set
            {
                _Neighborhood_ElectionCycle = value;
                NotifyPropertyChanged("Neighborhood_ElectionCycle");
            }
        }

        private uint _Neighborhood_AvatarCount;
        public uint Neighborhood_AvatarCount
        {
            get { return _Neighborhood_AvatarCount; }
            set
            {
                _Neighborhood_AvatarCount = value;
                NotifyPropertyChanged("Neighborhood_AvatarCount");
            }
        }

        private uint _Neighborhood_ElectedDate;
        public uint Neighborhood_ElectedDate
        {
            get { return _Neighborhood_ElectedDate; }
            set
            {
                _Neighborhood_ElectedDate = value;
                NotifyPropertyChanged("Neighborhood_ElectedDate");
            }
        }

        private uint _Neighborhood_ActivityRating;
        public uint Neighborhood_ActivityRating
        {
            get { return _Neighborhood_ActivityRating; }
            set
            {
                _Neighborhood_ActivityRating = value;
                NotifyPropertyChanged("Neighborhood_ActivityRating");
            }
        }

        private ImmutableList<uint> _Neighborhood_TopAvatarActivity;
        public ImmutableList<uint> Neighborhood_TopAvatarActivity
        {
            get { return _Neighborhood_TopAvatarActivity; }
            set
            {
                _Neighborhood_TopAvatarActivity = value;
                NotifyPropertyChanged("Neighborhood_TopAvatarActivity");
            }
        }

        private ImmutableList<uint> _Neighborhood_TopAvatarFamous;
        public ImmutableList<uint> Neighborhood_TopAvatarFamous
        {
            get { return _Neighborhood_TopAvatarFamous; }
            set
            {
                _Neighborhood_TopAvatarFamous = value;
                NotifyPropertyChanged("Neighborhood_TopAvatarFamous");
            }
        }

        private ImmutableList<uint> _Neighborhood_TopAvatarInfamous;
        public ImmutableList<uint> Neighborhood_TopAvatarInfamous
        {
            get { return _Neighborhood_TopAvatarInfamous; }
            set
            {
                _Neighborhood_TopAvatarInfamous = value;
                NotifyPropertyChanged("Neighborhood_TopAvatarInfamous");
            }
        }
    }
}
