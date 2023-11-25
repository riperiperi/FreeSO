using FSO.Common.Domain.RealestateDomain;

namespace FSO.Common.Domain.Realestate
{
    public interface IRealestateDomain
    {
        IShardRealestateDomain GetByShard(int shardId);

        bool ValidateLotName(string name);
    }
}
