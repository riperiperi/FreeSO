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
        
        /// <summary>
        /// Enriches a list of data items with the associated data service object.
        /// For example, when searching avatars you have a list of avatar ids. This 
        /// utility cna be used to find all the Avatar objects that go with that result
        /// set
        /// </summary>
        List<OUTPUT> EnrichList<OUTPUT, INPUT, DSENTITY>(List<INPUT> input, Func<INPUT, uint> idFunction, Func<INPUT, DSENTITY, OUTPUT> outputConverter);


        ITopicSubscription CreateTopicSubscription();

    }
}
