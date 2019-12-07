/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.LotView.Utils
{
    public class _2DSpriteSorter <T> : IComparer<T> where T : _2DSprite
    {
        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            if (x.DrawOrder > y.DrawOrder)
            {
                return 1;
            }else if (x.DrawOrder < y.DrawOrder)
            {
                return -1;
            }
            return 0;
        }

        #endregion
    }
}
