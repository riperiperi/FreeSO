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
    public class LotAdmitInfo : AbstractModel
    {
        private ImmutableList<uint> _LotAdmitInfo_AdmitList;
        [Persist]
        public ImmutableList<uint> LotAdmitInfo_AdmitList
        {
            get { return _LotAdmitInfo_AdmitList; }
            set
            {
                _LotAdmitInfo_AdmitList = value;
                NotifyPropertyChanged("LotAdmitInfo_AdmitList");
            }
        }

        private byte _LotAdmitInfo_AdmitMode;
        [Persist]
        public byte LotAdmitInfo_AdmitMode
        {
            get { return _LotAdmitInfo_AdmitMode; }
            set
            {
                _LotAdmitInfo_AdmitMode = value;
                NotifyPropertyChanged("LotAdmitInfo_AdmitMode");
            }
        }
        
        private ImmutableList<uint> _LotAdmitInfo_BanList;
        [Persist]
        public ImmutableList<uint> LotAdmitInfo_BanList
        {
            get { return _LotAdmitInfo_BanList; }
            set
            {
                _LotAdmitInfo_BanList = value;
                NotifyPropertyChanged("LotAdmitInfo_BanList");
            }
        }

    }
}
