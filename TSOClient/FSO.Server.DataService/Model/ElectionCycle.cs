using FSO.Common.DataService.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Model
{
    public class ElectionCycle : AbstractModel
    {
        private uint _ElectionCycle_StartDate;
        public uint ElectionCycle_StartDate
        {
            get { return _ElectionCycle_StartDate; }
            set
            {
                _ElectionCycle_StartDate = value;
                NotifyPropertyChanged("ElectionCycle_StartDate");
            }
        }

        private uint _ElectionCycle_EndDate;
        public uint ElectionCycle_EndDate
        {
            get { return _ElectionCycle_EndDate; }
            set
            {
                _ElectionCycle_EndDate = value;
                NotifyPropertyChanged("ElectionCycle_EndDate");
            }
        }

        private byte _ElectionCycle_CurrentState;
        public byte ElectionCycle_CurrentState
        {
            get { return _ElectionCycle_CurrentState; }
            set
            {
                _ElectionCycle_CurrentState = value;
                NotifyPropertyChanged("ElectionCycle_CurrentState");
            }
        }

        private byte _ElectionCycle_ElectionType;
        public byte ElectionCycle_ElectionType
        {
            get { return _ElectionCycle_ElectionType; }
            set
            {
                _ElectionCycle_ElectionType = value;
                NotifyPropertyChanged("ElectionCycle_ElectionType");
            }
        }
    }
}
