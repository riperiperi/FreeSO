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
using System.Reflection;
using FSO.Common.DataService.Framework.Attributes;
using System.Collections;
using FSO.Common.DataService.Model;

namespace FSO.Common.DataService
{
    public class ClientDataService : DataService, IClientDataService, IAriesMessageSubscriber
    {
        private uint messageId;
        private AriesClient CityClient;
        private Dictionary<uint, PendingDataRequest> PendingCallbacks = new Dictionary<uint, PendingDataRequest>();
        protected TimeSpan CallbackTimeout = TimeSpan.FromSeconds(30);
        private GameThreadInterval PollInterval;

        private Dictionary<Type, MaskedStruct> DefaultDataStruct = new Dictionary<Type, MaskedStruct>();
        
        public ClientDataService(IModelSerializer serializer,
                                FSO.Content.Content content,
                                IKernel kernel) : base(serializer, content)
        {
            AddProvider(kernel.Get<ClientAvatarProvider>());
            AddProvider(kernel.Get<ClientLotProvider>());
            AddProvider(kernel.Get<ClientCityProvider>());
            AddProvider(kernel.Get<ClientNeighProvider>());
            AddProvider(kernel.Get<ClientMayorRatingProvider>());
            CityClient = kernel.Get<AriesClient>("City");
            CityClient.AddSubscriber(this);

            //When a new object is made, this data will be requested automatically
            SetDefaultDataStruct(typeof(Avatar), MaskedStruct.SimPage_Main);

            PollInterval = GameThread.SetInterval(PollTopics, 5000);
        }

        public void SetDefaultDataStruct(Type type, MaskedStruct mask)
        {
            DefaultDataStruct.Add(type, mask);
        }

        public Task<object> Request(MaskedStruct mask, uint id)
        {
            var messageId = NextMessageId();
            var request = new DataServiceWrapperPDU() {
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
            lock (PendingCallbacks) PendingCallbacks.Add(messageId, result);
            return result.Task;
        }

        private PropertyInfo GetKeyField(Type type)
        {
            var keyField = type.GetProperties().First(x => x.GetCustomAttribute<Key>() != null);
            return keyField;
        }

        private uint[] GetDotPath(object item, string fieldPath)
        {
            //the "item" is the top level. We can really serialize anything any number of fields deep.
            //dot path is: provider, id, field, field, field...
            var path = fieldPath.Split('.');
            var dotPath = new uint[path.Length + 2];

            var topField = GetFieldByName(item.GetType(), path[0]);
            var keyField = GetKeyField(item.GetType());

            var id = (uint)keyField.GetValue(item);

            dotPath[0] = topField.ParentID;
            dotPath[1] = id;
            dotPath[2] = topField.ID;
            var curObj = item.GetType().GetProperty(path[0]).GetValue(item);
            for (int i=1; i<path.Length; i++)
            {
                var curField = GetFieldByName(curObj.GetType(), path[i]);
                dotPath[2 + i] = curField.ID;
                curObj = curObj.GetType().GetProperty(path[i]).GetValue(curObj);
            }

            return dotPath;
        }

        public void SetArrayItem(object item, string fieldPath, uint index, object value)
        {
            //Set the key field to null tells the data service to remove it
            var arrayDotPath = GetDotPath(item, fieldPath);
            Array.Resize(ref arrayDotPath, arrayDotPath.Length + 1);
            arrayDotPath[arrayDotPath.Length - 1] = (uint)index;

            var update = SerializeUpdate(value, arrayDotPath);
            CityClient.Write(new DataServiceWrapperPDU()
            {
                Body = update,
                RequestTypeID = 0,
                SendingAvatarID = NextMessageId()
            });
        }

        public void RemoveFromArray(object item, string fieldPath, object value)
        {
            if (value == null) { return; }

            var array = (IList)GetFieldFromPath(item, fieldPath);
            var index = array.IndexOf(value);
            if (index != -1)
            {
                //In TSO, you set the key field to null to indicate the array item should be deleted
                //...unless we're a value type, in which case we only send the index and default(T).
                var arrayDotPath = GetDotPath(item, fieldPath);
                bool structItem = !value.GetType().IsValueType;
                var resizeCount = structItem ? 2 : 1;

                Array.Resize(ref arrayDotPath, arrayDotPath.Length + resizeCount);
                arrayDotPath[arrayDotPath.Length - resizeCount] = (uint)index;

                if (structItem)
                {
                    var keyField = GetKeyField(value.GetType());
                    var structField = GetFieldByName(value.GetType(), keyField.Name);

                    arrayDotPath[arrayDotPath.Length - 1] = structField.ID;
                }

                var update = SerializeUpdate((uint)0, arrayDotPath);
                CityClient.Write(new DataServiceWrapperPDU()
                {
                    Body = update,
                    RequestTypeID = 0,
                    SendingAvatarID = NextMessageId()
                });
            }
        }

        public void AddToArray(object item, string fieldPath, object value)
        {
            var arrayDotPath = GetDotPath(item, fieldPath);
            Array.Resize(ref arrayDotPath, arrayDotPath.Length + 1);
            //DataService appends if index is >= length
            arrayDotPath[arrayDotPath.Length - 1] = uint.MaxValue;
            var update = SerializeUpdate(value, arrayDotPath);
            CityClient.Write(new DataServiceWrapperPDU()
            {
                Body = update,
                RequestTypeID = 0,
                SendingAvatarID = NextMessageId()
            });
        }

        private object GetFieldFromPath(object item, string fieldPath)
        {
            var path = fieldPath.Split('.');
            var curObj = item.GetType().GetProperty(path[0]).GetValue(item);

            for (int i = 1; i < path.Length; i++)
            {
                var curField = GetFieldByName(curObj.GetType(), path[i]);
                curObj = curObj.GetType().GetProperty(path[i]).GetValue(curObj);
            }

            return curObj;
        }

        public void Sync(object item, string[] fieldPaths)
        {
            var updates = new List<cTSOTopicUpdateMessage>();
            foreach (var fieldPath in fieldPaths)
            {
                var path = fieldPath.Split('.');
                var dotPath = GetDotPath(item, fieldPath);
                var curObj = item.GetType().GetProperty(path[0]).GetValue(item);

                for (int i = 1; i < path.Length; i++)
                {
                    var curField = GetFieldByName(curObj.GetType(), path[i]);
                    dotPath[i + 2] = curField.ID;
                    curObj = curObj.GetType().GetProperty(path[i]).GetValue(curObj);
                }

                updates.Add(SerializeUpdate(curObj, dotPath));
            }

            if (updates.Count == 0) { return; }
            var packets = new DataServiceWrapperPDU[updates.Count];

            for (int i = 0; i < updates.Count; i++)
            {
                var messageId = NextMessageId();
                var update = updates[i];
                packets[i] = new DataServiceWrapperPDU()
                {
                    Body = update,
                    RequestTypeID = 0,
                    SendingAvatarID = messageId
                };
            }

            CityClient.Write(packets);
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

                if (PendingCallbacks.ContainsKey(dataPacket.SendingAvatarID)) {
                    pendingRequest = PendingCallbacks[dataPacket.SendingAvatarID];
                    PendingCallbacks.Remove(dataPacket.SendingAvatarID);
                }
            }

            if (pendingRequest != null) {
                pendingRequest.Resolve();
            }
        }


        private List<TopicSubscription> _Topics = new List<TopicSubscription>();

        public ITopicSubscription CreateTopicSubscription()
        {
            lock (_Topics) {
                var sub = new TopicSubscription(this);
                _Topics.Add(sub);
                return sub;
            }
        }

        public void DiscardTopicSubscription(ITopicSubscription subscription)
        {
            lock (_Topics) {
                _Topics.Remove((TopicSubscription)subscription);
            }
        }

        /// <summary>
        /// For now, data updates occor by polling the server.
        /// </summary>
        private void PollTopics()
        {
            lock (_Topics) {
                //TODO: Make this more efficient
                var topics = new List<ITopic>();
                foreach (var topicSubscriptions in _Topics)
                {
                    var innerTopics = topicSubscriptions.GetTopics();
                    if (innerTopics != null)
                    {
                        foreach (var innerTopic in innerTopics) {
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
            foreach (var topic in topics)
            {
                if (topic is EntityMaskTopic)
                {
                    var entityMaskTopic = (EntityMaskTopic)topic;
                    Request(entityMaskTopic.Mask, entityMaskTopic.EntityId);
                }
            }
        }

        private uint GetId(object item)
        {
            var keyField = GetKeyField(item.GetType());
            var id = (uint)keyField.GetValue(item);

            return id;
        }

        public List<OUTPUT> EnrichList<OUTPUT, INPUT, DSENTITY>(List<INPUT> input, Func<INPUT, uint> idFunction, Func<INPUT, DSENTITY, OUTPUT> outputConverter)
        {
            var result = new List<OUTPUT>();
            var ids = input.Select(x => (object)idFunction(x)).ToArray();
            var dsEntities = GetMany(typeof(DSENTITY), ids).Result;

            var idMap = new Dictionary<uint, DSENTITY>();
            foreach(var item in dsEntities)
            {
                var id = GetId(item);
                idMap[id] = (DSENTITY)item;
            }

            foreach(var item in input)
            {
                var itemId = idFunction(item);
                result.Add(outputConverter(item, idMap[itemId]));
            }

            return result;
        }


        public List<OUTPUT> EnrichList<OUTPUT, INPUT, DSENTITY>(List<INPUT> input, Func<INPUT, uint?> idFunction, Func<INPUT, DSENTITY, OUTPUT> outputConverter)
        {
            var result = new List<OUTPUT>();
            var ids = input.Select(x => idFunction(x)).Where(x => x != null && x.HasValue).Select(x => (object)x).ToArray();
            var dsEntities = GetMany(typeof(DSENTITY), ids).Result;

            var idMap = new Dictionary<uint, DSENTITY>();
            foreach (var item in dsEntities)
            {
                var id = GetId(item);
                idMap[id] = (DSENTITY)item;
            }

            foreach (var item in input)
            {
                var itemId = idFunction(item);
                DSENTITY dsItem = default(DSENTITY);
                if(itemId != null && itemId.HasValue){
                    dsItem = idMap[itemId.Value];
                }
                result.Add(outputConverter(item, dsItem));
            }

            return result;
        }

        protected override Task<object> Get(IDataServiceProvider provider, object key)
        {
            var result = base.Get(provider, key);
            return result.ContinueWith(x =>
            {
                if(x.Result is IModel)
                {
                    var model = (IModel)x.Result;
                    if (model.RequestDefaultData)
                    {
                        model.RequestDefaultData = false;
                        if (DefaultDataStruct.ContainsKey(model.GetType()))
                        {
                            var mask = DefaultDataStruct[model.GetType()];
                            Request(mask, GetId(model));
                        }
                    }
                }
                return x.Result;
            });
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
