using FSO.Common.DataService.Framework;

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
    }
}
