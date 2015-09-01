using FSO.Server.Database.DA;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
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

        public void Handle(IVoltronSession session, DBRequestWrapperPDU packet)
        {
            if(packet.Body is cTSONetMessageStandard)
            {
                HandleNetMessage(session, (cTSONetMessageStandard)packet.Body, packet);
            }
        }

        private void HandleNetMessage(IVoltronSession session, cTSONetMessageStandard msg, DBRequestWrapperPDU packet)
        {
            if (!msg.DatabaseType.HasValue) { return; }
            var requestType = DBRequestTypeUtils.FromRequestID(msg.DatabaseType.Value);

            object response = null;

            switch (requestType)
            {
                case DBRequestType.LoadAvatarByID:
                    response = HandleLoadAvatarById(session, msg);
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

        private object HandleLoadAvatarById(IVoltronSession session, cTSONetMessageStandard msg)
        {
            var request = msg.ComplexParameter as LoadAvatarByIDRequest;
            if (request == null) { return null; }

            if(request.AvatarId != session.AvatarId){
                throw new Exception("Permission denied, you cannot load an avatar you do not own");
            }

            return new cTSONetMessageStandard()
            {
                MessageID = 0x8ADF865D,
                DatabaseType = DBResponseType.LoadAvatarByID.GetResponseID(),
                Parameter = msg.Parameter,

                ComplexParameter = new LoadAvatarByIDResponse()
                {
                    AvatarId = session.AvatarId
                }
            };
        }


        private object HandleSearchExact(IVoltronSession session, cTSONetMessageStandard msg)
        {
            var request = msg.ComplexParameter as cTSOSearchRequest;
            if (request == null) { return null; }

            using (var db = DAFactory.Get())
            {
                var results = db.Avatars.SearchExact(request.Query, 100).Select(x => new cTSOSearchResponseItem
                {
                    Name = x.name,
                    EntityId = x.avatar_id
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

        private object HandleSearchWildcard(IVoltronSession session, cTSONetMessageStandard msg)
        {
            var request = msg.ComplexParameter as cTSOSearchRequest;
            if (request == null) { return null; }

            using (var db = DAFactory.Get())
            {
                var results = db.Avatars.SearchWildcard(request.Query, 100).Select(x => new cTSOSearchResponseItem
                {
                    Name = x.name,
                    EntityId = x.avatar_id
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
