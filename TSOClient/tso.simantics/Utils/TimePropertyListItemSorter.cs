/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
