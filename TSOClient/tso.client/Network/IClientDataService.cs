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
        void Sync(object item, string[] fields);
        void AddToArray(object item, string fieldPath, object value);
        void RemoveFromArray(object item, string fieldPath, object value);
        void SetArrayItem(object item, string fieldPath, uint index, object value);

        ITopicSubscription CreateTopicSubscription();

    }
}
