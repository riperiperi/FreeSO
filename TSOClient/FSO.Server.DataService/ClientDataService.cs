using FSO.Common.Serialization;
using FSO.Server.DataService.Providers.Client;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.DataService.Model;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Common.Serialization.Primitives;
using FSO.Server.Clients;
using FSO.Common.Security;
using System.Threading;
using FSO.Common.DataService.Framework;

namespace FSO.Common.DataService
{
    public class ClientDataService : DataService, IClientDataService, IAriesMessageSubscriber
    {
        private uint messageId;
        private AriesClient CityClient;
        private Dictionary<uint, PendingDataRequest> PendingCallbacks = new Dictionary<uint, PendingDataRequest>();
        protected TimeSpan CallbackTimeout = TimeSpan.FromSeconds(30);

        public ClientDataService(IModelSerializer serializer,
                                FSO.Content.Content content,
                                IKernel kernel) : base(serializer, content)
        {
            AddProvider(kernel.Get<ClientAvatarProvider>());
            CityClient = kernel.Get<AriesClient>("City");
            CityClient.AddSubscriber(this);
        }

        public Task<object> Request(MaskedStruct mask, uint id)
        {
            var messageId = NextMessageId();
            var request = new DataServiceWrapperPDU(){
                RequestTypeID = mask.GetID(),
                SendingAvatarID = messageId, //Reusing this field for easier callbacks rather than scoping them
                Body = new cTSONetMessageStandard()
                {
                    DataServiceType = mask.GetID(),
                    Parameter = id
                }
            };

            CityClient.Write(request);

            //TODO: Add timeouts
            var result = new PendingDataRequest(messageId, this, Get(mask, id));
            PendingCallbacks.Add(messageId, result);
            return result.Task;
        }

        private uint NextMessageId()
        {
            lock (this)
            {
                var val = messageId;
                messageId++;
                return val;
            }
        }

        public void MessageReceived(AriesClient client, object message)
        {
            PendingDataRequest pendingRequest = null;

            lock (PendingCallbacks)
            {
                var dataPacket = message as DataServiceWrapperPDU;
                if (dataPacket == null) { return; }

                if (dataPacket.Body is cTSOTopicUpdateMessage)
                {
                    this.ApplyUpdate((cTSOTopicUpdateMessage)dataPacket.Body, NullSecurityContext.INSTANCE);
                }

                if (PendingCallbacks.ContainsKey(dataPacket.SendingAvatarID)){
                    pendingRequest = PendingCallbacks[dataPacket.SendingAvatarID];
                    PendingCallbacks.Remove(dataPacket.SendingAvatarID);
                }
            }

            if(pendingRequest != null){
                pendingRequest.Resolve();
            }
        }
    }


    class PendingDataRequest
    {
        private uint MessageId;
        private IClientDataService DataService;
        private TaskCompletionSource<object> TaskSource = new TaskCompletionSource<object>();
        private Task<object> Resolver;

        public PendingDataRequest(uint messageId, IClientDataService ds, Task<object> resolver)
        {
            this.MessageId = messageId;
            this.DataService = ds;
            this.Resolver = resolver;
        }

        public Task<object> Task
        {
            get
            {
                return TaskSource.Task;
            }
        }

        public void Resolve()
        {
            Resolver.ContinueWith(x =>
            {
                TaskSource.SetResult(x.Result);
            });
        }
    }
}
