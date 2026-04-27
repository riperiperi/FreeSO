namespace FSO.Common.Domain
{
    public interface IServerNFSProvider
    {
        string GetBaseDirectory();
        string GetShardMapDirectory(int shardId);
    }
}
