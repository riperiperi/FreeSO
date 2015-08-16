using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Utils
{
    public class PaginatedResult <T>
    {
        public List<T> Results;
        public int Start;
        public int NumResults;
        public int TotalNumResults;
    }
}
