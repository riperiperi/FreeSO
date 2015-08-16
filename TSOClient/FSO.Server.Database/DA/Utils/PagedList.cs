using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Utils
{
    public class PagedList<T> : List<T>
    {
        public int Offset;
        public int Total;

        public PagedList(IEnumerable<T> items, int offset, int total) : base(items)
        {
            this.Offset = offset;
            this.Total = total;
        }

    }
}
