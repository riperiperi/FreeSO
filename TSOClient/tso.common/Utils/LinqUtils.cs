using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public static class LinqUtils
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int maxItems)
        {
            return items.Select((item, index) => new { item, index })
                        .GroupBy(x => x.index / maxItems)
                        .Select(g => g.Select(x => x.item));
        }
    }
}
