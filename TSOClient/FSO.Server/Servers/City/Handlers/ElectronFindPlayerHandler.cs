using FSO.Server.Database.DA;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class ElectronFindAvatarHandler
    {
        private IDAFactory DAFactory;
        private CityServerContext Context;
        public ElectronFindAvatarHandler(IDAFactory da, CityServerContext context)
        {
            this.DAFactory = da;
            this.Context = context;
        }

        public void Handle(IVoltronSession session, FindAvatarRequest packet)
        {
            if (session.IsAnonymous) return;
            using (var da = DAFactory.Get()) {
                var privacy = da.Avatars.GetPrivacyMode(packet.AvatarId);
                if (privacy > 0)
                {
                    session.Write(new FindAvatarResponse
                    {
                        AvatarId = packet.AvatarId,
                        LotId = 0,
                        Status = FindAvatarResponseStatus.PRIVACY_ENABLED
                    });
                    return;
                }
                //TODO: get ignore status

                var claim = da.AvatarClaims.GetByAvatarID(packet.AvatarId);
                //maybe check shard id against avatar in future. The client should do this anyways, and the server providing this functionality to everyone isnt a disaster.
                var location = claim?.location ?? 0;
                session.Write(new FindAvatarResponse
                {
                    AvatarId = packet.AvatarId,
                    LotId = location,
                    Status = (location == 0)?FindAvatarResponseStatus.NOT_ON_LOT:FindAvatarResponseStatus.FOUND
                });
            }
        }
    }
}
