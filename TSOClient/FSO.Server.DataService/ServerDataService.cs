using FSO.Common.DataService.Providers.Server;
using FSO.Common.Serialization;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService
{
    public class ServerDataService : DataService
    {
        public ServerDataService(IModelSerializer serializer, 
                                FSO.Content.Content content,
                                IKernel kernel) : base(serializer, content)
        {
            AddProvider(kernel.Get<ServerAvatarProvider>());
            AddProvider(kernel.Get<ServerLotProvider>());
        }
    }
}
