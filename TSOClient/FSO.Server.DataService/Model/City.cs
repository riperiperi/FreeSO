using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Framework.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Model
{
    public class City : AbstractModel
    {
        [Key]
        public uint City_Id { get; set; } //unused

        private ImmutableList<bool> _City_ReservedLotVector;
        public ImmutableList<bool> City_ReservedLotVector {
            get { return _City_ReservedLotVector; }
            set { _City_ReservedLotVector = value; NotifyPropertyChanged("City_ReservedLotVector"); }
        }

        private ImmutableList<bool> _City_OnlineLotVector;
        public ImmutableList<bool> City_OnlineLotVector
        {
            get { return _City_OnlineLotVector; }
            set { _City_OnlineLotVector = value; NotifyPropertyChanged("City_OnlineLotVector"); }
        }

        private ImmutableList<uint> _City_TopTenNeighborhoodsVector;
        public ImmutableList<uint> City_TopTenNeighborhoodsVector
        {
            get { return _City_TopTenNeighborhoodsVector; }
            set { _City_TopTenNeighborhoodsVector = value; NotifyPropertyChanged("City_TopTenNeighborhoodsVector"); }
        }

        //City_LotDBIDByInstanceID map

        private ImmutableList<uint> _City_NeighborhoodsVec;
        public ImmutableList<uint> City_NeighborhoodsVec
        {
            get { return _City_NeighborhoodsVec; }
            set { _City_NeighborhoodsVec = value; NotifyPropertyChanged("City_NeighborhoodsVec"); }
        }

        private ImmutableDictionary<uint, bool> _City_ReservedLotInfo;
        public ImmutableDictionary<uint, bool> City_ReservedLotInfo
        {
            get { return _City_ReservedLotInfo; }
            set { _City_ReservedLotInfo = value; NotifyPropertyChanged("City_ReservedLotInfo"); }
        }

        private ImmutableList<uint> _City_SpotlightsVector;
        public ImmutableList<uint> City_SpotlightsVector
        {
            get { return _City_SpotlightsVector; }
            set { _City_SpotlightsVector = value; NotifyPropertyChanged("City_SpotlightsVector"); }
        }

        //City_LotInstanceIDByDBID map

        private ImmutableList<uint> _City_Top100ListIDs;
        public ImmutableList<uint> City_Top100ListIDs
        {
            get { return _City_Top100ListIDs; }
            set { _City_Top100ListIDs = value; NotifyPropertyChanged("City_Top100ListIDs"); }
        }

    }
}
