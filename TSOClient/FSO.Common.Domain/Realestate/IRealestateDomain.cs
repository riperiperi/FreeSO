using FSO.Common.Domain.RealestateDomain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Realestate
{
    public interface IRealestateDomain
    {
        IShardRealestateDomain GetByShard(int shardId);

        bool ValidateLotName(string name);
    }
}
