using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class LotServerAuthenticationHandler
    {
        private CityServerConfiguration Config;
        private ISessions Sessions;

        public LotServerAuthenticationHandler(CityServerConfiguration config, ISessions sessions){
            this.Config = config;
            this.Sessions = sessions;
        }

        public void Handle(IAriesSession session, RequestChallenge request)
        {
            var challenge = ChallengeResponse.GetChallenge();
            session.SetAttribute("challenge", challenge);
            session.SetAttribute("callSign", request.CallSign);
            session.SetAttribute("publicHost", request.PublicHost);
            session.SetAttribute("internalHost", request.InternalHost);

            session.Write(new RequestChallengeResponse {
                Challenge = challenge
            });
        }

        public void Handle(IAriesSession session, AnswerChallenge answer)
        {
            var challenge = session.GetAttribute("challenge") as string;
            if(challenge == null)
            {
                session.Close();
                return;
            }

            var myAnswer = ChallengeResponse.AnswerChallenge(challenge, Config.Secret);
            if(myAnswer != answer.Answer)
            {
                session.Close();
                return;
            }

            //Trust established, good to go
            var newSession = Sessions.UpgradeSession<GluonSession>(session, x => {
                x.IsAuthenticated = true;
                x.CallSign = (string)session.GetAttribute("callSign");
                x.PublicHost = (string)session.GetAttribute("publicHost");
                x.InternalHost = (string)session.GetAttribute("internalHost");
            });
            newSession.Write(new AnswerAccepted());
        }
    }
}
