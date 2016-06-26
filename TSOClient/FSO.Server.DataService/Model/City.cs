using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Framework.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Model
{
    public class City : AbstractModel
    {
        [Key]
        public uint City_Id { get; set; } //unused

        private List<bool> _City_ReservedLotVector;
        public List<bool> City_ReservedLotVector {
            get { return _City_ReservedLotVector; }
            set { _City_ReservedLotVector = value; NotifyPropertyChanged("City_ReservedLotVector"); }
        }

        private List<bool> _City_OnlineLotVector;
        public List<bool> City_OnlineLotVector
        {
            get { return _City_OnlineLotVector; }
            set { _City_OnlineLotVector = value; NotifyPropertyChanged("City_OnlineLotVector"); }
        }

        private List<uint> _City_TopTenNeighborhoodsVector;
        public List<uint> City_TopTenNeighborhoodsVector
        {
            get { return _City_TopTenNeighborhoodsVector; }
            set { _City_TopTenNeighborhoodsVector = value; NotifyPropertyChanged("City_TopTenNeighborhoodsVector"); }
        }

        //City_LotDBIDByInstanceID map

        private List<uint> _City_NeighborhoodsVec;
        public List<uint> City_NeighborhoodsVec
        {
            get { return _City_NeighborhoodsVec; }
            set { _City_NeighborhoodsVec = value; NotifyPropertyChanged("City_NeighborhoodsVec"); }
        }

        private Dictionary<uint, bool> _City_ReservedLotInfo;
        public Dictionary<uint, bool> City_ReservedLotInfo
        {
            get { return _City_ReservedLotInfo; }
            set { _City_ReservedLotInfo = value; NotifyPropertyChanged("City_ReservedLotInfo"); }
        }

        private List<uint> _City_SpotlightsVector;
        public List<uint> City_SpotlightsVector
        {
            get { return _City_SpotlightsVector; }
            set { _City_SpotlightsVector = value; NotifyPropertyChanged("City_SpotlightsVector"); }
        }

        //City_LotInstanceIDByDBID map

        private List<uint> _City_Top100ListIDs;
        public List<uint> City_Top100ListIDs
        {
            get { return _City_Top100ListIDs; }
            set { _City_Top100ListIDs = value; NotifyPropertyChanged("City_Top100ListIDs"); }
        }

    }
}
