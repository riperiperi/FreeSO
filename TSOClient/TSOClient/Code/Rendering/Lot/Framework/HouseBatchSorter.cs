using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.Rendering.Lot.Framework
{
    public class HouseBatchSorter<T> : IComparer<T> where T : HouseBatchSprite
    {
        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            if (x.DrawOrder > y.DrawOrder){
                return 1;
            }
            if (x.DrawOrder < y.DrawOrder){
                return -1;
            }
            return 0;
        }

        #endregion
    }
}
