using FSO.Common.Domain;

namespace FSO.Server.DataService
{
    public class ServerNFSProvider : IServerNFSProvider
    {
        private string BasePath;
        public ServerNFSProvider(string basePath)
        {
            BasePath = basePath;
        }

        public string GetBaseDirectory()
        {
            return BasePath;
        }

        public string GetShardMapDirectory(int shardId)
        {
            return Path.Join(BasePath, $"City{shardId}");
        }
    }
}
