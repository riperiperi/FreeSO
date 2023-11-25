using System.Collections.Generic;

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
