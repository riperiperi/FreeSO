using FSO.Client.Network;
using FSO.Server.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService
{
    public interface IClientDataService : IDataService
    {
        Task<object> Request(MaskedStruct mask, uint id);

        ITopicSubscription CreateTopicSubscription();
    }
}
