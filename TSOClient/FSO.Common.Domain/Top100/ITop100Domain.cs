using FSO.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Top100
{
    public interface ITop100Domain
    {
        Top100CategoryEntry Get(uint id);
        Top100CategoryEntry Get(Top100Category category);
        IEnumerable<Top100CategoryEntry> Categories { get; }
    }
}
