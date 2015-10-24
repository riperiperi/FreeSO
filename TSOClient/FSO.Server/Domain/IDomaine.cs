using FSO.Server.Domain.Shards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Domain
{
    public interface IDomain
    {
        IShards Shards { get; }
    }
}
