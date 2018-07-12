using FSO.Common.DatabaseService.Model;
using FSO.Common.Serialization.Primitives;
using FSO.Common.Utils;
using FSO.Server.Clients;
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

namespace FSO.Common.DatabaseService
{
    public class DatabaseService : IDatabaseService, IAriesMessageSubscriber
    {
        //private static Logger LOG = LogManager.GetCurrentClassLogger();

        public static object Sync = new object();

        private uint messageId;
        private AriesClient CityClient;
        private Dictionary<uint, PendingRequest> PendingRequests = new Dictionary<uint, PendingRequest>();

        public DatabaseService([Named("City")] AriesClient cityClient)
        {
            CityClient = cityClient;
            CityClient.AddSubscriber(this);
        }

        public Task<GetTop100Response> GetTop100(GetTop100Request request)
        {
            return Request<GetTop100Response>(DBRequestType.GetTopResultSetByID, DBResponseType.GetTopResultSetByID, null, request);
        }

        public Task<LoadAvatarByIDResponse> LoadAvatarById(LoadAvatarByIDRequest request)
        {
            return Request<LoadAvatarByIDResponse>(DBRequestType.LoadAvatarByID, DBResponseType.LoadAvatarByID, null, request);
        }

        public Task<SearchResponse> Search(SearchRequest request, bool exact)
        {
            var requestType = exact ? DBRequestType.SearchExactMatch : DBRequestType.Search;
            var responseType = exact ? DBResponseType.SearchExactMatch : DBResponseType.Search;
            return Request<SearchResponse>(requestType, responseType, null, request);
        }
        

        //[MethodImpl(MethodImplOptions.Synchronized)]
        private Task<T> Request<T>(DBRequestType type, DBResponseType responseType, uint? parameter, object complexParameter)
        {
            lock (Sync)
            {
                var id = NextMessageId();
                var taskSource = new TaskCompletionSource<T>();

                var pending = new PendingRequest();
                pending.Callback = x =>
                {
                    taskSource.SetResult((T)x);
                };
                pending.RequestType = type;
                pending.ResponseType = responseType;
                PendingRequests.Add(id, pending);

                this.CityClient.Write(new DBRequestWrapperPDU()
                {
                    Sender = new Sender
                    {
                        AriesID = "0",
                        MasterAccountID = "0",
                    },
                    SendingAvatarID = id,
                    Body = new cTSONetMessageStandard()
                    {
                        DatabaseType = type.GetRequestID(),
                        Parameter = parameter,
                        ComplexParameter = complexParameter
                    }
                });

                return (Task<T>)taskSource.Task;
            }
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (!(message is DBRequestWrapperPDU))
            {
                return;
            }

            DBRequestWrapperPDU response = (DBRequestWrapperPDU)message;
            if (response.Body is cTSONetMessageStandard)
            {
                var body = (cTSONetMessageStandard)response.Body;
                var type = DBResponseTypeUtils.FromResponseID(body.DatabaseType.Value);

                //TODO: I feel like a sequence id would be better for matching up request / responses, perhaps the old protocol has this and we've just missed it
                var callback = PendingRequests[response.SendingAvatarID];//.FirstOrDefault(x => x.ResponseType == type);
                if (callback != null)
                {
                    PendingRequests.Remove(body.SendingAvatarID);

                    GameThread.NextUpdate(x =>
                    {
                        try
                        {
                            callback.Callback(body.ComplexParameter);
                        }
                        catch (Exception ex)
                        {
                            //LOG.Error(ex);
                        }
                    });
                }
            }
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

    }

    public class PendingRequest
    {
        public DBRequestType RequestType;
        public DBResponseType ResponseType;
        public Callback<object> Callback;
    }
}
