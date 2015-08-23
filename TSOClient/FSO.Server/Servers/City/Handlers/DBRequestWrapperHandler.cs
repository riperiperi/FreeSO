using FSO.Server.Database.DA;
using FSO.Server.Framework.Aries;
using FSO.Server.Protocol.Voltron.Dataservice;
using FSO.Server.Protocol.Voltron.DataService;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Server.Protocol.Voltron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class DBRequestWrapperHandler
    {
        private IDAFactory DAFactory;

        public DBRequestWrapperHandler(IDAFactory da)
        {
            this.DAFactory = da;
        }

        public void Handle(IAriesSession session, DBRequestWrapperPDU packet)
        {
            if(packet.Body is cTSONetMessageStandard)
            {
                HandleNetMessage(session, (cTSONetMessageStandard)packet.Body, packet);
            }
        }

        private void HandleNetMessage(IAriesSession session, cTSONetMessageStandard msg, DBRequestWrapperPDU packet)
        {
            if (!msg.DatabaseType.HasValue) { return; }
            var requestType = DBRequestTypeUtils.FromRequestID(msg.DatabaseType.Value);

            object response = null;

            switch (requestType)
            {
                case DBRequestType.LoadAvatarByID:
                    HandleLoadAvatarById(session, msg);
                    break;

                case DBRequestType.SearchExactMatch:
                    response = HandleSearchExact(session, msg);
                    break;

                case DBRequestType.Search:
                    response = HandleSearchWildcard(session, msg);
                    break;
            }

            if(response != null){
                session.Write(new DBRequestWrapperPDU {
                    SendingAvatarID = packet.SendingAvatarID,
                    Badge = packet.Badge,
                    IsAlertable = packet.IsAlertable,
                    Sender = packet.Sender,
                    Body = response
                });
            }
        }

        private void HandleLoadAvatarById(IAriesSession session, cTSONetMessageStandard msg)
        {

        }


        private object HandleSearchExact(IAriesSession session, cTSONetMessageStandard msg)
        {
            var request = msg.ComplexParameter as cTSOSearchRequest;
            if (request == null) { return null; }

            using (var db = DAFactory.Get())
            {
                var results = db.Avatars.SearchExact(request.Query, 100).Select(x => new cTSOSearchResponseItem
                {
                    Name = x.name,
                    Unknown = x.avatar_id
                }).ToList();

                return new cTSONetMessageStandard()
                {
                    MessageID = 0xDBF301A9,
                    DatabaseType = DBResponseType.SearchExactMatch.GetResponseID(),
                    Parameter = msg.Parameter,

                    ComplexParameter = new cTSOSearchResponse()
                    {
                        Query = request.Query,
                        Type = request.Type,
                        Items = results
                    }
                };
            }
        }

        private object HandleSearchWildcard(IAriesSession session, cTSONetMessageStandard msg)
        {
            var request = msg.ComplexParameter as cTSOSearchRequest;
            if (request == null) { return null; }

            using (var db = DAFactory.Get())
            {
                var results = db.Avatars.SearchWildcard(request.Query, 100).Select(x => new cTSOSearchResponseItem
                {
                    Name = x.name,
                    Unknown = x.avatar_id
                }).ToList();

                return new cTSONetMessageStandard()
                {
                    MessageID = 0xDBF301A9,
                    DatabaseType = DBResponseType.Search.GetResponseID(),
                    Parameter = msg.Parameter,

                    ComplexParameter = new cTSOSearchResponse()
                    {
                        Query = request.Query,
                        Type = request.Type,
                        Items = results
                    }
                };
            }
        }
    }
}
