using FSO.Common.DataService.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
