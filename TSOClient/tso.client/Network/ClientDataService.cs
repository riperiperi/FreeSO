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
using FSO.Client;
using FSO.Common.Utils;
using FSO.Common.DataService.Providers.Client;
using FSO.Client.Network;

namespace FSO.Common.DataService
{
    public class ClientDataService : DataService, IClientDataService, IAriesMessageSubscriber
    {
        private uint messageId;
        private AriesClient CityClient;
        private Dictionary<uint, PendingDataRequest> PendingCallbacks = new Dictionary<uint, PendingDataRequest>();
        protected TimeSpan CallbackTimeout = TimeSpan.FromSeconds(30);
        private GameThreadInterval PollInterval;

        public ClientDataService(IModelSerializer serializer,
                                FSO.Content.Content content,
                                IKernel kernel) : base(serializer, content)
        {
            AddProvider(kernel.Get<ClientAvatarProvider>());
            AddProvider(kernel.Get<ClientLotProvider>());
            AddProvider(kernel.Get<ClientCityProvider>());
            CityClient = kernel.Get<AriesClient>("City");
            CityClient.AddSubscriber(this);

            PollInterval = GameThread.SetInterval(PollTopics, 5000);
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


        private List<TopicSubscription> _Topics = new List<TopicSubscription>();
        
        public ITopicSubscription CreateTopicSubscription()
        {
            lock (_Topics){
                var sub = new TopicSubscription(this);
                _Topics.Add(sub);
                return sub;
            }
        }

        public void DiscardTopicSubscription(ITopicSubscription subscription)
        {
            lock (_Topics){
                _Topics.Remove((TopicSubscription)subscription);
            }
        }

        /// <summary>
        /// For now, data updates occor by polling the server.
        /// </summary>
        private void PollTopics()
        {
            lock (_Topics){
                //TODO: Make this more efficient
                var topics = new List<ITopic>();
                foreach(var topicSubscriptions in _Topics)
                {
                    var innerTopics = topicSubscriptions.GetTopics();
                    if(innerTopics != null)
                    {
                        foreach(var innerTopic in innerTopics){
                            var alreadyAdded = topics.FirstOrDefault(x => x.Equals(innerTopic)) != null;
                            if (!alreadyAdded)
                            {
                                topics.Add(innerTopic);
                            }
                        }
                    }
                }
                RequestTopics(topics);
            }
        }

        public void RequestTopics(List<ITopic> topics)
        {
            foreach(var topic in topics)
            {
                if(topic is EntityMaskTopic)
                {
                    var entityMaskTopic = (EntityMaskTopic)topic;
                    Request(entityMaskTopic.Mask, entityMaskTopic.EntityId);
                }
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
                //Dispatch in the update loop
                GameThread.NextUpdate(y =>
                {
                    TaskSource.SetResult(x.Result);
                });
            });
        }
    }
}
