using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.vitaboy;

namespace tso.simantics.utils
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
