using FSO.Common.DatabaseService.Model;
using FSO.Common.Serialization.Primitives;
using FSO.Common.Utils;
using FSO.Server.Clients;
using FSO.Server.Protocol.Voltron.DataService;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Server.Protocol.Voltron.Packets;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Network.DB
{
    public class DBService : IAriesMessageSubscriber
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private AriesClient City;
        private cTSOSerializer Serializer;

        private List<PendingRequest> PendingRequests = new List<PendingRequest>();

        public DBService([Named("City")] AriesClient cityClient, cTSOSerializer serializer)
        {
            this.Serializer = serializer;

            this.City = cityClient;
            this.City.AddSubscriber(this);
        }

        public Task<LoadAvatarByIDResponse> LoadAvatarById(LoadAvatarByIDRequest request){
            return Request<LoadAvatarByIDResponse>(DBRequestType.LoadAvatarByID, DBResponseType.LoadAvatarByID, null, request);
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        private Task<T> Request<T>(DBRequestType type, DBResponseType responseType, uint? parameter, object complexParameter)
        {
            var taskSource = new TaskCompletionSource<T>();

            var pending = new PendingRequest();
            pending.Callback = x =>{
                taskSource.SetResult((T)x);
            };
            pending.RequestType = type;
            pending.ResponseType = responseType;
            PendingRequests.Add(pending);

            this.City.Write(new DBRequestWrapperPDU()
            {
                Sender = new Sender {
                    AriesID = "0",
                    MasterAccountID = "0",
                },
                SendingAvatarID = 0,
                Body = new cTSONetMessageStandard()
                {
                    DatabaseType = type.GetRequestID(),
                    Parameter = parameter,
                    ComplexParameter = complexParameter
                }
            });

            return (Task<T>)taskSource.Task;
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if(!(message is DBRequestWrapperPDU)){
                return;
            }

            DBRequestWrapperPDU response = (DBRequestWrapperPDU)message;
            if(response.Body is cTSONetMessageStandard){
                var body = (cTSONetMessageStandard)response.Body;
                var type = DBResponseTypeUtils.FromResponseID(body.DatabaseType.Value);

                //TODO: I feel like a sequence id would be better for matching up request / responses, perhaps the old protocol has this and we've just missed it
                var callback = PendingRequests.FirstOrDefault(x => x.ResponseType == type);
                if(callback != null){
                    try {
                        callback.Callback(body.ComplexParameter);
                    }catch(Exception ex){
                        LOG.Error(ex);
                    }
                    PendingRequests.Remove(callback);
                }
            }
        }
    }


    public class PendingRequest
    {
        public DBRequestType RequestType;
        public DBResponseType ResponseType;
        public Callback<object> Callback;
    }
}
