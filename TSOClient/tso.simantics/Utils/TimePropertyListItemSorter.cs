using System.Collections.Generic;
using FSO.Vitaboy;

namespace FSO.SimAntics.Utils
{
    public class TimePropertyListItemSorter : IComparer<TimePropertyListItem>
    {
        #region IComparer<TimePropertyListItem> Members

        public int Compare(TimePropertyListItem x, TimePropertyListItem y)
        {
            return x.ID.CompareTo(y.ID);
        }

        #endregion
    }
}
