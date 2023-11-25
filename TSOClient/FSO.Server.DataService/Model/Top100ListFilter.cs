using FSO.Common.DataService.Framework;
using System.Collections.Immutable;

namespace FSO.Common.DataService.Model
{
    public class Top100ListFilter : AbstractModel
    {
        private uint _Top100ListFilter_Top100ListID;
        public uint Top100ListFilter_Top100ListID
        {
            get { return _Top100ListFilter_Top100ListID; }
            set
            {
                _Top100ListFilter_Top100ListID = value;
                NotifyPropertyChanged("Top100ListFilter_Top100ListID");
            }
        }

        public ImmutableList<uint> _Top100ListFilter_ResultsVec;
        public ImmutableList<uint> Top100ListFilter_ResultsVec
        {
            get { return _Top100ListFilter_ResultsVec; }
            set
            {
                _Top100ListFilter_ResultsVec = value;
                NotifyPropertyChanged("Top100ListFilter_ResultsVec");
            }
        }
    }
}
